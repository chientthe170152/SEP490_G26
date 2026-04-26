using MediatR;
using Microsoft.EntityFrameworkCore;
using MTCA.Application.Common.Errors;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Application.Common.Models;
using MTCA.Domain.Identity.Enums;

namespace MTCA.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshHandler(
    IRefreshTokenStore refreshTokenStore,
    IJwtTokenService jwtTokenService,
    IAppDbContext dbContext)
    : IRequestHandler<RefreshCommand, Result<RefreshResult>>
{
    public async Task<Result<RefreshResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        // Atomic validate + consume; defensible against concurrent rotation.
        var validation = await refreshTokenStore.ConsumeAsync(request.RefreshToken, cancellationToken);
        if (validation == null)
        {
            return AuthErrors.RefreshTokenInvalid;
        }

        // Single round-trip: AspNetUsers + UserProfiles + AspNetUserRoles join.
        // Trade-off: Consume already burned the rt; transient DB errors here
        // force the user to log in again. Acceptable — the alternative is a race.
        var snapshot = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == validation.UserId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.UserName,
                u.MustChangePassword,
                ProfileStatus = dbContext.UserProfiles
                    .Where(p => p.UserId == u.Id)
                    .Select(p => (UserProfileStatus?)p.Status)
                    .FirstOrDefault(),
                Roles = dbContext.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToArray()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return AuthErrors.RefreshTokenInvalid;
        }

        if (snapshot.ProfileStatus != UserProfileStatus.ACTIVE)
        {
            return AuthErrors.ProfileInactive;
        }

        var (accessToken, newJti, accessExpiresAt) = jwtTokenService.Issue(
            snapshot.Id,
            snapshot.Email,
            snapshot.UserName,
            snapshot.Roles,
            snapshot.MustChangePassword);

        var (refreshToken, refreshExpiresAt) = await refreshTokenStore.IssueAsync(
            snapshot.Id, newJti, request.Ip, request.UserAgent, cancellationToken);

        return new RefreshResult(
            MustChangePassword: snapshot.MustChangePassword,
            AccessToken: accessToken,
            AccessExpiresAt: accessExpiresAt,
            RefreshToken: refreshToken,
            RefreshExpiresAt: refreshExpiresAt);
    }
}
