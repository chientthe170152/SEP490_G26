using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MTCA.Application.Common.Errors;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Application.Common.Models;
using MTCA.Domain.Identity;
using MTCA.Domain.Identity.Enums;

namespace MTCA.Application.Features.Auth.ChangePasswordFirstLogin;

public sealed class ChangePasswordFirstLoginHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore,
    IAppDbContext dbContext)
    : IRequestHandler<ChangePasswordFirstLoginCommand, Result<ChangePasswordFirstLoginResult>>
{
    private const string IdentityPasswordMismatchCode = "PasswordMismatch";

    public async Task<Result<ChangePasswordFirstLoginResult>> Handle(
        ChangePasswordFirstLoginCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            return AuthErrors.Unauthenticated;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.Unauthenticated;
        }

        if (!user.MustChangePassword)
        {
            return AuthErrors.MustChangePasswordNotRequired;
        }

        var changeResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
        {
            if (changeResult.Errors.Any(e => e.Code == IdentityPasswordMismatchCode))
            {
                return AuthErrors.InvalidCurrentPassword;
            }

            return Result<ChangePasswordFirstLoginResult>.Failure(
                changeResult.Errors.Select(e => Error.Validation(e.Code)));
        }

        user.MustChangePassword = false;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<ChangePasswordFirstLoginResult>.Failure(
                updateResult.Errors.Select(e => Error.Validation(e.Code)));
        }

        var snapshot = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.Status,
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

        await refreshTokenStore.RevokeAllAsync(user.Id, cancellationToken);

        var (accessToken, jti, accessExpiresAt) = jwtTokenService.Issue(
            user.Id, user.Email, user.UserName, snapshot.Roles, user.MustChangePassword);
        var (refreshToken, refreshExpiresAt) = await refreshTokenStore.IssueAsync(
            user.Id, jti, request.Ip, request.UserAgent, cancellationToken);

        return new ChangePasswordFirstLoginResult(
            MustChangePassword: user.MustChangePassword,
            AccessToken: accessToken,
            AccessExpiresAt: accessExpiresAt,
            RefreshToken: refreshToken,
            RefreshExpiresAt: refreshExpiresAt);
    }
}
