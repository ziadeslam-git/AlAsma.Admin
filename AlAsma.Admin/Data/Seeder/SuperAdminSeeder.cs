using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace AlAsma.Admin.Data.Seeder
{
    public static class SuperAdminSeeder
    {
        // Values that indicate placeholder / unconfigured state
        private static readonly string[] PlaceholderPatterns = new[]
        {
            "CHANGE_ME", "YOUR_PASSWORD", "YOUR_CODE", "YOUR_SERVER",
            "SuperAdmin@123", "<", ">"
        };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var config = services.GetRequiredService<IConfiguration>();
            var db = services.GetRequiredService<AppDbContext>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SuperAdminSeeder");

            // Check if a SuperAdmin already exists
            var exists = await db.Authors.AnyAsync(a => a.Role == "SuperAdmin");
            if (exists) return;

            var section = config.GetSection("SuperAdmin");
            var name = section.GetValue<string>("Name");
            var code = section.GetValue<string>("Code");
            var password = section.GetValue<string>("Password");

            // Validate all required values are present and not placeholder-like
            if (IsPlaceholderOrEmpty(name) || IsPlaceholderOrEmpty(code) || IsPlaceholderOrEmpty(password))
            {
                logger.LogWarning(
                    "SuperAdmin seeding skipped: one or more SuperAdmin config values are missing, " +
                    "empty, or still set to placeholder values. " +
                    "Configure SuperAdmin:Name, SuperAdmin:Code, and SuperAdmin:Password " +
                    "via user-secrets or environment variables. See appsettings.Example.json.");
                return;
            }

            var hashed = BCrypt.Net.BCrypt.HashPassword(password);

            var author = new Models.Author
            {
                Name = name!,
                Code = code!,
                Password = hashed,
                Role = "SuperAdmin",
            };

            db.Authors.Add(author);
            await db.SaveChangesAsync();

            logger.LogInformation("SuperAdmin account seeded successfully.");
        }

        private static bool IsPlaceholderOrEmpty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            foreach (var pattern in PlaceholderPatterns)
            {
                if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
