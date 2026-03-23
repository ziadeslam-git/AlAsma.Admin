using Microsoft.EntityFrameworkCore;
using AlAsma.Admin.Data;
using AlAsma.Admin.Data.Seeder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register AppDbContext
builder.Services.AddDbContext<AlAsma.Admin.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register Services and Repositories
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IUnitOfWork, AlAsma.Admin.Repositories.UnitOfWork>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IAuthorService, AlAsma.Admin.Services.AuthorService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.ISaleService, AlAsma.Admin.Services.SaleService>();
builder.Services.AddScoped<AlAsma.Admin.Interfaces.IDashboardService, AlAsma.Admin.Services.DashboardService>();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

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
