using Microsoft.AspNetCore.Authorization;
using MTCA.Api.Authorization;

namespace MTCA.Api.Middleware;

public sealed class FirstLoginPasswordMiddleware(RequestDelegate next)
{
    private const string ProblemBody = """{"type":"https://mtca.local/errors/PASSWORD_CHANGE_REQUIRED","title":"PASSWORD_CHANGE_REQUIRED","status":403,"detail":"Bạn cần đổi mật khẩu trước khi tiếp tục."}""";

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

        var mcp = context.User.FindFirst("mcp")?.Value;
        if (!string.Equals(mcp, "true", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(ProblemBody);
    }
}
