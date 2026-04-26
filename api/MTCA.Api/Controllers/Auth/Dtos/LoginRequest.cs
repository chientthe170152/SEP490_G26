using Microsoft.AspNetCore.Http;
using MTCA.Api.Extensions;
using MTCA.Application.Features.Auth.Commands.Login;

namespace MTCA.Api.Controllers.Auth.Dtos;

public sealed record LoginRequest(string Email, string Password)
{
    public LoginCommand ToCommand(HttpContext httpContext)
    {
        var (ip, userAgent) = HttpClientContext.Resolve(httpContext);
        return new LoginCommand(Email, Password, ip, userAgent);
    }
}
