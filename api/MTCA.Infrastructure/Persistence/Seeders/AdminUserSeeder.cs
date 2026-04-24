using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.Identity.Enums;

namespace MTCA.Infrastructure.Persistence.Seeders;

public class AdminUserSeeder(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IConfiguration configuration,
    ILogger<AdminUserSeeder> logger)
{
    private const string AdminStudentCode = "ADMIN-0001";
    private const string AdminFullName = "System Administrator";

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
            await EnsureProfileAsync(existing, ct);
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

        await EnsureProfileAsync(admin, ct);

        logger.LogInformation("AdminUserSeeder: created admin {Email} with MustChangePassword=true", email);
    }

    private async Task EnsureProfileAsync(ApplicationUser user, CancellationToken ct)
    {
        var existing = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (existing is not null)
        {
            return;
        }

        db.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            StudentCode = AdminStudentCode,
            FullName = AdminFullName,
            Status = UserProfileStatus.ACTIVE
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("AdminUserSeeder: ensured UserProfile for admin {UserId}", user.Id);
    }
}
