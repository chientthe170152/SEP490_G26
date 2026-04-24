namespace MTCA.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record CurrentUserResult(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    ProfileSnapshot Profile);

public sealed record ProfileSnapshot(
    string Status,
    string StudentCode,
    string? Nickname);
