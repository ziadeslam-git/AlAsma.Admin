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

// 1. Cookie Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// 2. Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Seed SuperAdmin
await SuperAdminSeeder.SeedAsync(app.Services);

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
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();
