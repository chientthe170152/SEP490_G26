using MTCA.Domain.Identity;

namespace MTCA.Application.Common.Interfaces.Services;

public interface IJwtTokenService
{
    (string Token, string Jti, DateTimeOffset ExpiresAt) Issue(
        ApplicationUser user,
        IEnumerable<string> roles,
        bool mustChangePassword);
}
