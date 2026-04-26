namespace MTCA.Application.Common.Interfaces.Services;

public record RefreshTokenValidation(Guid UserId, string Jti);

public interface IRefreshTokenStore
{
    Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        Guid userId,
        string jti,
        string? ip,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<RefreshTokenValidation?> ConsumeAsync(string rawToken, CancellationToken cancellationToken);

    Task RevokeAsync(Guid userId, string jti, CancellationToken cancellationToken);
    
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}
