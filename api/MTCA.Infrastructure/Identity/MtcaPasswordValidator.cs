using Microsoft.AspNetCore.Identity;
using MTCA.Application.Common.Constants;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Identity;

public sealed class MtcaPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (string.IsNullOrEmpty(password) || !PasswordRules.Pattern.IsMatch(password))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = ErrorCodes.Validation,
                Description = string.Empty
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
