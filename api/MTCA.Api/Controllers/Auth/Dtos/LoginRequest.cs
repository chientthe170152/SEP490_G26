using Microsoft.AspNetCore.Http;
using MTCA.Application.Features.Auth.Commands.Login;

namespace MTCA.Api.Controllers.Auth.Dtos;

public sealed record LoginRequest(string Email, string Password)
{
    public LoginCommand ToCommand(HttpContext httpContext)
    {
        var ip = ResolveClientIp(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        return new LoginCommand(Email, Password, ip, string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
    }

    private static string? ResolveClientIp(HttpContext httpContext)
    {
        var cfIp = httpContext.Request.Headers["CF-Connecting-IP"].ToString();
        if (!string.IsNullOrWhiteSpace(cfIp))
        {
            return cfIp;
        }
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
