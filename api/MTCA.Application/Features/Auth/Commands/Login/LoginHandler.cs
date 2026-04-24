using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MTCA.Application.Common.Errors;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Application.Common.Models;
using MTCA.Domain.Identity;
using MTCA.Domain.Identity.Enums;

namespace MTCA.Application.Features.Auth.Commands.Login;

public sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore,
    IAppDbContext dbContext)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            return AuthErrors.AccountLocked;
        }
        if (!signInResult.Succeeded)
        {
            return AuthErrors.InvalidCredentials;
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        if (profile is null || profile.Status != UserProfileStatus.ACTIVE)
        {
            return AuthErrors.ProfileInactive;
        }

        var roles = await userManager.GetRolesAsync(user);

        var (accessToken, jti, accessExpiresAt) = jwtTokenService.Issue(user, roles, user.MustChangePassword);
        var (refreshToken, refreshExpiresAt) = await refreshTokenStore.IssueAsync(
            user.Id, jti, request.Ip, request.UserAgent, cancellationToken);

        return new LoginResult(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: profile.FullName,
            Roles: roles.ToArray(),
            MustChangePassword: user.MustChangePassword,
            AccessToken: accessToken,
            AccessExpiresAt: accessExpiresAt,
            RefreshToken: refreshToken,
            RefreshExpiresAt: refreshExpiresAt);
    }
}
