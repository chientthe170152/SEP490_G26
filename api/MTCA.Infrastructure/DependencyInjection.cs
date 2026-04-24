using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Infrastructure.Identity;
using MTCA.Infrastructure.Identity.Services;
using MTCA.Infrastructure.Persistence;
using MTCA.Infrastructure.Persistence.Interceptors;
using MTCA.Infrastructure.Persistence.Seeders;
using MTCA.Infrastructure.Services.Tokens;
using StackExchange.Redis;

namespace MTCA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlConn = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(sqlConn);
            opts.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddMtcaIdentity();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();

        services.AddScoped<RoleSeeder>();
        services.AddScoped<AdminUserSeeder>();
        services.AddScoped<DbInitializer>();

        var redisConn = configuration.GetSection("Redis")["Connection"]
            ?? throw new InvalidOperationException("Redis:Connection is missing.");
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("sqlserver")
            .AddRedis(redisConn, name: "redis");

        return services;
    }
}
