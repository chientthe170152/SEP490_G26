using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MTCA.Infrastructure.Persistence.Seeders;

public class DbInitializer(
    AppDbContext db,
    RoleSeeder roleSeeder,
    AdminUserSeeder adminSeeder,
    ILogger<DbInitializer> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        logger.LogInformation("DbInitializer: applying pending migrations");
        await db.Database.MigrateAsync(ct);

        await roleSeeder.SeedAsync(ct);
        await adminSeeder.SeedAsync(ct);

        logger.LogInformation("DbInitializer: seed completed");
    }
}
