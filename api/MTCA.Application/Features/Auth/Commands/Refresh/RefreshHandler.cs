using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MTCA.Application.Common.Errors;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Application.Common.Models;
using MTCA.Domain.Identity;
using MTCA.Domain.Identity.Enums;

namespace MTCA.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshHandler(
    IRefreshTokenStore refreshTokenStore,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    IAppDbContext dbContext)
    : IRequestHandler<RefreshCommand, Result<RefreshResult>>
{
    public async Task<Result<RefreshResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var validation = await refreshTokenStore.ValidateAsync(request.RefreshToken, cancellationToken);
        if (validation == null)
        {
            return AuthErrors.RefreshTokenInvalid;
        }

        var user = await userManager.FindByIdAsync(validation.UserId.ToString());
        if (user == null)
        {
            await refreshTokenStore.RevokeAsync(validation.UserId, validation.Jti, cancellationToken);
            return AuthErrors.RefreshTokenInvalid;
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        if (profile is null || profile.Status != UserProfileStatus.ACTIVE)
        {
            await refreshTokenStore.RevokeAsync(validation.UserId, validation.Jti, cancellationToken);
            return AuthErrors.ProfileInactive;
        }

        var roles = await userManager.GetRolesAsync(user);

        // Revoke the old token (rotation)
        await refreshTokenStore.RevokeAsync(validation.UserId, validation.Jti, cancellationToken);

        // Issue new tokens
        var (accessToken, newJti, accessExpiresAt) = jwtTokenService.Issue(
            user, roles, user.MustChangePassword);

        var (refreshToken, refreshExpiresAt) = await refreshTokenStore.IssueAsync(
            user.Id, newJti, request.Ip, request.UserAgent, cancellationToken);

        return new RefreshResult(
            MustChangePassword: user.MustChangePassword,
            AccessToken: accessToken,
            AccessExpiresAt: accessExpiresAt,
            RefreshToken: refreshToken,
            RefreshExpiresAt: refreshExpiresAt);
    }
}
