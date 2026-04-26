namespace MTCA.Application.Common.Interfaces.Services;

public interface IJwtTokenService
{
    (string Token, string Jti, DateTimeOffset ExpiresAt) Issue(
        Guid userId,
        string? email,
        string? fullName,
        IEnumerable<string> roles,
        bool mustChangePassword);
}
