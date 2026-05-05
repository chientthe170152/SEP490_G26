using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class PromotionErrors
{
    public static readonly Error EmptyQuestions          = new(ErrorCodes.PromotionEmptyQuestions, ErrorType.Validation);
    public static readonly Error TooManyQuestions        = new(ErrorCodes.PromotionTooManyQuestions, ErrorType.Validation);
    public static readonly Error MultipleSubjects        = new(ErrorCodes.PromotionMultipleSubjects, ErrorType.Validation);
    public static readonly Error SubjectMismatch         = new(ErrorCodes.PromotionSubjectMismatch, ErrorType.Validation);
    public static readonly Error SourceNotOwned          = new(ErrorCodes.PromotionSourceNotOwned, ErrorType.Forbidden);
    public static readonly Error TargetNotShared         = new(ErrorCodes.PromotionTargetNotShared, ErrorType.Validation);
    public static readonly Error QuestionNotInSource     = new(ErrorCodes.PromotionQuestionNotInSource, ErrorType.Validation);
    public static readonly Error RequestNotFound         = new(ErrorCodes.PromotionRequestNotFound, ErrorType.NotFound);
    public static readonly Error RejectionReasonRequired = new(ErrorCodes.PromotionRejectionReasonRequired, ErrorType.Validation);
    public static readonly Error ConcurrentUpdate        = new(ErrorCodes.PromotionConcurrentUpdate, ErrorType.Conflict);
    public static readonly Error AlreadyWithdrawn        = new(ErrorCodes.PromotionAlreadyWithdrawn, ErrorType.Conflict);
    public static readonly Error AlreadyResolved         = new(ErrorCodes.PromotionAlreadyResolved, ErrorType.Conflict);
    public static readonly Error QuestionAlreadyPending  = new(ErrorCodes.PromotionQuestionAlreadyPending, ErrorType.Conflict);
    public static readonly Error FinalizeIncomplete      = new(ErrorCodes.PromotionFinalizeIncomplete, ErrorType.Validation);
    public static readonly Error FinalizeUnknownItem     = new(ErrorCodes.PromotionFinalizeUnknownItem, ErrorType.Validation);
    public static readonly Error FinalizeDuplicateItem   = new(ErrorCodes.PromotionFinalizeDuplicateItem, ErrorType.Validation);
    public static readonly Error FinalizeInvalidDecision = new(ErrorCodes.PromotionFinalizeInvalidDecision, ErrorType.Validation);
}
