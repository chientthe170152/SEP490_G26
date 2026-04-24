namespace MTCA.Api.Controllers.Auth.Dtos;

public sealed record MeResponse(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    ProfileSnapshotDto Profile);

public sealed record ProfileSnapshotDto(
    string Status,
    string StudentCode,
    string? Nickname);
