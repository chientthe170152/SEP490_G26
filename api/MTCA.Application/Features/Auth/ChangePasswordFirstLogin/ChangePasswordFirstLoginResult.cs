namespace MTCA.Application.Features.Auth.ChangePasswordFirstLogin;

public sealed record ChangePasswordFirstLoginResult(
    bool MustChangePassword,
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt);
