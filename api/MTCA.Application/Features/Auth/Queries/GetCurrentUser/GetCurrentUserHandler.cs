using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MTCA.Application.Common.Errors;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Application.Common.Models;
using MTCA.Domain.Identity;

namespace MTCA.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserHandler(
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IAppDbContext dbContext)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResult>>
{
    public async Task<Result<CurrentUserResult>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            return AuthErrors.Unauthenticated;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return AuthErrors.UserNotFound;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new CurrentUserResult(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            FullName: profile.FullName,
            Roles: roles.ToArray(),
            MustChangePassword: user.MustChangePassword,
            Profile: new ProfileSnapshot(
                Status: profile.Status.ToString(),
                StudentCode: profile.StudentCode,
                Nickname: profile.Nickname));
    }
}
