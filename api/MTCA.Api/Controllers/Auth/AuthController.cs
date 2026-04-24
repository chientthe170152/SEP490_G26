using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MTCA.Api.Controllers.Auth.Dtos;
using MTCA.Api.Extensions;
using MTCA.Api.Options;
using MTCA.Application.Features.Auth.Commands.Login;
using MTCA.Application.Features.Auth.Queries.GetCurrentUser;

namespace MTCA.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender mediator, IOptions<AuthCookieOptions> cookieOptions) : ControllerBase
{
    private readonly AuthCookieOptions _cookieOpts = cookieOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(HttpContext);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToActionResult(this);
        }

        var login = result.Value;
        CookieHelper.SetAccessCookie(Response, login.AccessToken, login.AccessExpiresAt, _cookieOpts);
        CookieHelper.SetRefreshCookie(Response, login.RefreshToken, login.RefreshExpiresAt, _cookieOpts);

        return Ok(new LoginResponse(
            login.UserId,
            login.Email,
            login.FullName,
            login.Roles,
            login.MustChangePassword));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult(this);
        }

        var me = result.Value;
        var response = new MeResponse(
            me.UserId,
            me.Email,
            me.FullName,
            me.Roles,
            me.MustChangePassword,
            new ProfileSnapshotDto(me.Profile.Status, me.Profile.StudentCode, me.Profile.Nickname));
        return Ok(response);
    }
}
