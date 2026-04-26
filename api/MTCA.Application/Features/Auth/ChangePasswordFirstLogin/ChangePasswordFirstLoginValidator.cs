using FluentValidation;
using MTCA.Application.Common.Constants;

namespace MTCA.Application.Features.Auth.ChangePasswordFirstLogin;

public sealed class ChangePasswordFirstLoginValidator : AbstractValidator<ChangePasswordFirstLoginCommand>
{
    public ChangePasswordFirstLoginValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Matches(PasswordRules.Pattern)
            .NotEqual(x => x.CurrentPassword);
    }
}
