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

        // Consume already deletes the tokenKey atomically; no separate Revoke needed.
        await refreshTokenStore.ConsumeAsync(request.RefreshToken, cancellationToken);
        return Result.Success();
    }
}
