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

        // Single round-trip: UserProfile + roles join. Identity already loaded `user`,
        // so we project only what's still missing.
        var snapshot = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.Status,
                p.FullName,
                Roles = dbContext.UserRoles
                    .Where(ur => ur.UserId == p.UserId)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToArray()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null || snapshot.Status != UserProfileStatus.ACTIVE)
        {
            return AuthErrors.ProfileInactive;
        }

        var (accessToken, jti, accessExpiresAt) = jwtTokenService.Issue(
            user.Id, user.Email, snapshot.FullName, snapshot.Roles, user.MustChangePassword);
        var (refreshToken, refreshExpiresAt) = await refreshTokenStore.IssueAsync(
            user.Id, jti, request.Ip, request.UserAgent, cancellationToken);

        return new LoginResult(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: snapshot.FullName,
            Roles: snapshot.Roles,
            MustChangePassword: user.MustChangePassword,
            AccessToken: accessToken,
            AccessExpiresAt: accessExpiresAt,
            RefreshToken: refreshToken,
            RefreshExpiresAt: refreshExpiresAt);
    }
}
