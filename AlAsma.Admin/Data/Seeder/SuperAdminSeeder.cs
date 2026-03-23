using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace AlAsma.Admin.Data.Seeder
{
    public static class SuperAdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var config = services.GetRequiredService<IConfiguration>();
            var db = services.GetRequiredService<AppDbContext>();

            // Check if a SuperAdmin already exists
            var exists = await db.Authors.AnyAsync(a => a.Role == "SuperAdmin");
            if (exists) return;

            var section = config.GetSection("SuperAdmin");
            var name = section.GetValue<string>("Name") ?? "Super Admin";
            var code = section.GetValue<string>("Code") ?? "SUPER001";
            var password = section.GetValue<string>("Password") ?? "SuperAdmin@123";

            var hashed = BCrypt.Net.BCrypt.HashPassword(password);

            var author = new Models.Author
            {
                Name = name,
                Code = code,
                Password = hashed,
                Role = "SuperAdmin",
            };

            db.Authors.Add(author);
            await db.SaveChangesAsync();
        }
    }
}
