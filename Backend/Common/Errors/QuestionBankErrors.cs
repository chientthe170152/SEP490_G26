using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class QuestionBankErrors
{
    public static readonly Error NotFound         = new(ErrorCodes.BankNotFound,         ErrorType.NotFound);
    public static readonly Error SubjectClosed    = new(ErrorCodes.BankSubjectClosed,    ErrorType.Conflict);
    public static readonly Error SubjectNotFound  = new(ErrorCodes.BankSubjectNotFound,  ErrorType.NotFound);
    public static readonly Error InvalidPurpose   = new(ErrorCodes.BankInvalidPurpose,   ErrorType.Validation);
    public static readonly Error NotOwned         = new(ErrorCodes.BankNotOwned,         ErrorType.Forbidden);
    public static readonly Error CannotEditShared = new(ErrorCodes.BankCannotEditShared, ErrorType.Forbidden);
    public static readonly Error AlreadyArchived  = new(ErrorCodes.BankAlreadyArchived,  ErrorType.Conflict);
    public static readonly Error ConcurrentUpdate = new(ErrorCodes.BankConcurrentUpdate, ErrorType.Conflict);
}
