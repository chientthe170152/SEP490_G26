using Microsoft.AspNetCore.Http;
using MTCA.Api.Extensions;
using MTCA.Application.Features.Auth.ChangePasswordFirstLogin;

namespace MTCA.Api.Controllers.Auth.Dtos;

public sealed record ChangePasswordFirstLoginRequest(string CurrentPassword, string NewPassword)
{
    public ChangePasswordFirstLoginCommand ToCommand(HttpContext httpContext)
    {
        var (ip, userAgent) = HttpClientContext.Resolve(httpContext);
        return new ChangePasswordFirstLoginCommand(CurrentPassword, NewPassword, ip, userAgent);
    }
}
