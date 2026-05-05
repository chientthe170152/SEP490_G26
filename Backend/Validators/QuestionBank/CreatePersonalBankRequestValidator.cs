using Backend.Constants;
using Backend.DTOs.QuestionBank;
using FluentValidation;

namespace Backend.Validators.QuestionBank;

public class CreatePersonalBankRequestValidator : AbstractValidator<CreatePersonalBankRequest>
{
    public CreatePersonalBankRequestValidator()
    {
        RuleFor(x => x.SubjectId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Purpose).Must(p => BankPurpose.IsValid(p))
            .WithMessage("Purpose must be 1 (Exam) or 2 (Practice).");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
