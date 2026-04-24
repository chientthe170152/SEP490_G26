namespace MTCA.Application.Features.Auth.Commands.Login;

public sealed record LoginResult(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt);
