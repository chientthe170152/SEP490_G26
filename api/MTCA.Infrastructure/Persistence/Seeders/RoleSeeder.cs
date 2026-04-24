using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MTCA.Domain.Common;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Seeders;

public class RoleSeeder(RoleManager<ApplicationRole> roleManager, ILogger<RoleSeeder> logger)
{
    private static readonly string[] Roles =
    [
        Domain.Common.Roles.Admin,
        Domain.Common.Roles.HoD,
        Domain.Common.Roles.Teacher,
        Domain.Common.Roles.Student
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var created = 0;
        foreach (var name in Roles)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole(name));
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed role '{name}': {errors}");
                }
                created++;
            }
        }
        logger.LogInformation("RoleSeeder: ensured {TotalRoles} roles ({CreatedCount} newly created)", Roles.Length, created);
    }
}
