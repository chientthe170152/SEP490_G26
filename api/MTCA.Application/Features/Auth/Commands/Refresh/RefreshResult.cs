namespace MTCA.Application.Features.Auth.Commands.Refresh;

public record RefreshResult(
    bool MustChangePassword,
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt);
