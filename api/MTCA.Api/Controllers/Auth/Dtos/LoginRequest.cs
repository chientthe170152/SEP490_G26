using Microsoft.AspNetCore.Http;
using MTCA.Application.Features.Auth.Commands.Login;

namespace MTCA.Api.Controllers.Auth.Dtos;

public sealed record LoginRequest(string Email, string Password)
{
    public LoginCommand ToCommand(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        return new LoginCommand(Email, Password, ip, string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
    }
}
