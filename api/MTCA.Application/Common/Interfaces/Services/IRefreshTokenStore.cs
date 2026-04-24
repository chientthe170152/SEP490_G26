namespace MTCA.Application.Common.Interfaces.Services;

public interface IRefreshTokenStore
{
    Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        Guid userId,
        string jti,
        string? ip,
        string? userAgent,
        CancellationToken cancellationToken);
}
