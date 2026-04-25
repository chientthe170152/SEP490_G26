using MediatR;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Features.Auth.Commands.Logout;

public sealed class LogoutHandler(IRefreshTokenStore refreshTokenStore) 
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Success();
        }

        var validation = await refreshTokenStore.ValidateAsync(request.RefreshToken, cancellationToken);
        if (validation != null)
        {
            await refreshTokenStore.RevokeAsync(validation.UserId, validation.Jti, cancellationToken);
        }

        return Result.Success();
    }
}
