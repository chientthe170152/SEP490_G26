using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MTCA.Api.Authorization;

namespace MTCA.Api.Extensions;

public static class RateLimitExtensions
{
    private const int LoginPermitLimit = 10;
    private const int LoginWindowMinutes = 1;

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
        });

        return services;
    }
}