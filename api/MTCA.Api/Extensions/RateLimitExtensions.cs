using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MTCA.Api.Authorization;

namespace MTCA.Api.Extensions;

public static class RateLimitExtensions
{
    private const int LoginPermitLimit = 10;
    private const int LoginWindowMinutes = 1;

    // Legit users hit /refresh only when access token nears expiry
    // (every AccessTokenMinutes ≈ 2h per active tab). 30/min/IP is generous
    // for shared-NAT offices yet caps replay floods well below log-DoS threshold.
    private const int RefreshPermitLimit = 30;
    private const int RefreshWindowMinutes = 1;

    public static IServiceCollection AddMtcaRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(RateLimitPolicies.Login, context =>
            {
                var partitionKey = HttpClientContext.ResolveIp(context) ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LoginPermitLimit,
                    Window = TimeSpan.FromMinutes(LoginWindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.AddPolicy(RateLimitPolicies.Refresh, context =>
            {
                var partitionKey = HttpClientContext.ResolveIp(context) ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = RefreshPermitLimit,
                    Window = TimeSpan.FromMinutes(RefreshWindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}