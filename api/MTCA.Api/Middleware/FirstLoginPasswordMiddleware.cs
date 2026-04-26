using Microsoft.AspNetCore.Authorization;
using MTCA.Api.Authorization;
using MTCA.Api.Common;
using MTCA.Application.Common.Constants;

namespace MTCA.Api.Middleware;

public sealed class FirstLoginPasswordMiddleware(RequestDelegate next)
{
    private static readonly string ProblemBody =
        $$"""{"type":"{{ProblemTypes.Prefix}}{{ErrorCodes.PasswordChangeRequired}}","title":"{{ErrorCodes.PasswordChangeRequired}}","status":403}""";

    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            await next(context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        var optedOut = endpoint.Metadata
            .GetOrderedMetadata<AuthorizeAttribute>()
            .Any(a => string.Equals(a.Policy, AuthPolicies.AllowPasswordChange, StringComparison.Ordinal));
        if (optedOut)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var mcp = context.User.FindFirst(AuthClaims.MustChangePassword)?.Value;
        if (!string.Equals(mcp, AuthClaims.True, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(ProblemBody);
    }
}
