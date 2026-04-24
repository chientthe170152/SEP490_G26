namespace MTCA.Api.Controllers.Auth.Dtos;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword);
