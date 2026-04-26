using FluentValidation;
using MTCA.Application.Common.Constants;

namespace MTCA.Application.Features.Auth.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress().WithErrorCode(ErrorCodes.EmailInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(ErrorCodes.PasswordRequired)
            .MinimumLength(PasswordPolicy.MinimumLength).WithErrorCode(ErrorCodes.PasswordTooShort);
    }
}
