using Backend.DTOs.QuestionBank;
using FluentValidation;

namespace Backend.Validators.QuestionBank;

public class UpdatePersonalBankRequestValidator : AbstractValidator<UpdatePersonalBankRequest>
{
    public UpdatePersonalBankRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
