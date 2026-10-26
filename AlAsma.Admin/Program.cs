using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AlAsma.Admin.Data;
using AlAsma.Admin.Data.Seeder;

var builder = WebApplication.CreateBuilder(args);

// Fail only when the connection string is actually missing.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is missing. Configure a real connection string via environment variables, user-secrets, or appsettings.");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register AppDbContext
builder.Services.AddDbContext<AlAsma.Admin.Data.AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Register Services and Repositories
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IUnitOfWork, AlAsma.Admin.Repositories.UnitOfWork>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IAuthorService, AlAsma.Admin.Services.AuthorService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.ISaleService, AlAsma.Admin.Services.SaleService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IOperationService, AlAsma.Admin.Services.OperationService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IDashboardService, AlAsma.Admin.Services.DashboardService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IExportService, AlAsma.Admin.Services.ExportService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IPdfExportService, AlAsma.Admin.Services.PdfExportService>();

// 1. Cookie Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "AlAsma.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Re-validate the signed-in user on every request: a deleted account or a changed role
        // must lose access immediately instead of keeping a valid cookie for up to 8 hours.
        // (The Author global query filter already excludes soft-deleted users.)
        options.Events.OnValidatePrincipal = async context =>
        {
            var idValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;

            var stillValid = false;
            if (int.TryParse(idValue, out var userId) && !string.IsNullOrEmpty(role))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AlAsma.Admin.Data.AppDbContext>();
                stillValid = await db.Authors.AsNoTracking().AnyAsync(a => a.Id == userId && a.Role == role);
            }

            if (!stillValid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });

// 2. Authorization — every endpoint requires a signed-in user unless it is explicitly [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 3. Rate limiting — slows down password guessing on the login endpoint (per client IP).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Apply pending migrations automatically at startup
// This ensures new tables (like Operations) exist before any request
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AlAsma.Admin.Data.AppDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Auto-migration failed at startup. Some pages (e.g. Operations) may not work until migrations are applied manually.");
}

// Seed SuperAdmin AFTER migrations so the Authors table exists on a fresh database
try
{
    await SuperAdminSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "SuperAdmin seeding failed at startup.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Status code handling for 404, 403, etc.
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Basic security headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();
