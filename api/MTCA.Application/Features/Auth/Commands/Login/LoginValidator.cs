using FluentValidation;
using MTCA.Application.Common.Constants;

namespace MTCA.Application.Features.Auth.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().Matches(PasswordRules.Pattern);
    }
}
