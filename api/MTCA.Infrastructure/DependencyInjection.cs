using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTCA.Infrastructure.Persistence;
using StackExchange.Redis;

namespace MTCA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlConn = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(sqlConn));

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
