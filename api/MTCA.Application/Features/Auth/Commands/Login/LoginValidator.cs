using FluentValidation;

namespace MTCA.Application.Features.Auth.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMAIL_REQUIRED")
            .EmailAddress().WithErrorCode("EMAIL_INVALID");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("PASSWORD_REQUIRED")
            .MinimumLength(8).WithErrorCode("PASSWORD_TOO_SHORT");
    }
}
