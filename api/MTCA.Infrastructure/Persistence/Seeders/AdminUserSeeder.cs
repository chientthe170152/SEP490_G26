using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MTCA.Domain.Common;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Seeders;

public class AdminUserSeeder(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AdminUserSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var email = Environment.GetEnvironmentVariable("MTCA_ADMIN_EMAIL")
            ?? configuration["Seed:Admin:Email"];
        var password = Environment.GetEnvironmentVariable("MTCA_ADMIN_PASSWORD")
            ?? configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminUserSeeder: MTCA_ADMIN_EMAIL / MTCA_ADMIN_PASSWORD not configured — skipping admin seed");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            logger.LogInformation("AdminUserSeeder: admin {Email} already exists", email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            MustChangePassword = true
        };

        var create = await userManager.CreateAsync(admin, password);
        if (!create.Succeeded)
        {
            var errors = string.Join("; ", create.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }

        var assign = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!assign.Succeeded)
        {
            var errors = string.Join("; ", assign.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign admin role: {errors}");
        }

        logger.LogInformation("AdminUserSeeder: created admin {Email} with MustChangePassword=true", email);
    }
}
