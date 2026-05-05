using Backend.Constants;
using Backend.DTOs.Promotion;
using FluentValidation;

namespace Backend.Validators.Promotion;

public class CreatePromotionRequestValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionRequestValidator()
    {
        RuleFor(x => x.SourcePersonalBankId)
            .GreaterThan(0).WithMessage("SourcePersonalBankId must be > 0");

        RuleFor(x => x.TargetSharedBankId)
            .GreaterThan(0).WithMessage("TargetSharedBankId must be > 0");

        RuleFor(x => x.QuestionIds)
            .NotEmpty().WithErrorCode(ErrorCodes.PromotionEmptyQuestions)
            .Must(x => x.Count <= 50).WithErrorCode(ErrorCodes.PromotionTooManyQuestions);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note max length is 500");
    }
}
