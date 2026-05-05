using Backend.Constants;
using Backend.DTOs.Promotion;
using FluentValidation;

namespace Backend.Validators.Promotion;

public class FinalizePromotionRequestValidator : AbstractValidator<FinalizePromotionRequest>
{
    public FinalizePromotionRequestValidator()
    {
        RuleFor(x => x.ConcurrencyStamp)
            .NotEmpty().WithMessage("ConcurrencyStamp is required");

        RuleFor(x => x.Decisions)
            .NotEmpty().WithErrorCode(ErrorCodes.PromotionFinalizeIncomplete);

        RuleForEach(x => x.Decisions).SetValidator(new FinalizeItemDecisionValidator());
    }
}

public class FinalizeItemDecisionValidator : AbstractValidator<FinalizeItemDecision>
{
    public FinalizeItemDecisionValidator()
    {
        RuleFor(x => x.QuestionId).GreaterThan(0);
        
        RuleFor(x => x.Decision)
            .Must(d => d == "approve" || d == "reject")
            .WithErrorCode(ErrorCodes.PromotionFinalizeInvalidDecision);

        RuleFor(x => x.RejectionReason)
            .NotEmpty().When(x => x.Decision == "reject").WithErrorCode(ErrorCodes.PromotionRejectionReasonRequired)
            .MaximumLength(500).WithErrorCode(ErrorCodes.PromotionRejectionReasonRequired);
    }
}
