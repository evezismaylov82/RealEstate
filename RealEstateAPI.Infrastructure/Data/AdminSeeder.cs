using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Infrastructure.Helpers;

namespace RealEstateAPI.Infrastructure.Data
{

    public static class AdminSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, IConfiguration configuration, ILogger logger)
        {
            var adminSection = configuration.GetSection("AdminSeedSettings");

            var email = adminSection["Email"];
            var password = adminSection["Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation("AdminSeedSettings not configured — skipping admin seed.");
                return;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var existing = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existing != null)
            {
                var changed = false;

                if (existing.Role != UserRole.Admin)
                {
                    existing.Role = UserRole.Admin;
                    changed = true;
                }

                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    changed = true;
                }

                if (!existing.IsEmailVerified)
                {
                    existing.IsEmailVerified = true;
                    existing.EmailVerificationToken = null;
                    changed = true;
                }

                if (changed)
                {
                    await context.SaveChangesAsync(CancellationToken.None);
                    logger.LogInformation("Existing user {Email} promoted/repaired to Admin.", normalizedEmail);
                }
                else
                {
                    logger.LogInformation("Admin user {Email} already present — no changes needed.", normalizedEmail);
                }

                return;
            }

            var admin = new User
            {
                FirstName = adminSection["FirstName"] ?? "System",
                LastName = adminSection["LastName"] ?? "Administrator",
                Email = email.Trim(),
                PasswordHash = PasswordHelper.HashPassword(password),
                Role = UserRole.Admin,
                IsEmailVerified = true
            };

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync(CancellationToken.None);

            logger.LogInformation("Admin user {Email} created.", normalizedEmail);
        }
    }
}
