using MTCA.Application.Common.Constants;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Common.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(ErrorCodes.InvalidCredentials);
    public static readonly Error AccountLocked = Error.Locked(ErrorCodes.AccountLocked);
    public static readonly Error ProfileInactive = Error.Forbidden(ErrorCodes.ProfileInactive);
    public static readonly Error Unauthenticated = Error.Unauthorized(ErrorCodes.Unauthenticated);
    public static readonly Error PasswordChangeRequired = Error.Forbidden(ErrorCodes.PasswordChangeRequired);
    public static readonly Error RefreshTokenInvalid = Error.Unauthorized(ErrorCodes.RefreshTokenInvalid);
    public static readonly Error UserNotFound = Error.NotFound(ErrorCodes.UserNotFound);
    public static readonly Error InvalidCurrentPassword = Error.Unauthorized(ErrorCodes.InvalidCurrentPassword);
    public static readonly Error MustChangePasswordNotRequired = Error.Conflict(ErrorCodes.MustChangePasswordNotRequired);
}
