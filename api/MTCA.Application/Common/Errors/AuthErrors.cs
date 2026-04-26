using MTCA.Application.Common.Constants;
using MTCA.Application.Common.Models;

namespace MTCA.Application.Common.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        ErrorCodes.InvalidCredentials, ErrorMessages.InvalidCredentials);

    public static readonly Error AccountLocked = Error.Locked(
        ErrorCodes.AccountLocked, ErrorMessages.AccountLocked);

    public static readonly Error ProfileInactive = Error.Forbidden(
        ErrorCodes.ProfileInactive, ErrorMessages.ProfileInactive);

    public static readonly Error Unauthenticated = Error.Unauthorized(
        ErrorCodes.Unauthenticated, ErrorMessages.Unauthenticated);

    public static readonly Error PasswordChangeRequired = Error.Forbidden(
        ErrorCodes.PasswordChangeRequired, ErrorMessages.PasswordChangeRequired);

    public static readonly Error MustChangePasswordNotRequired = Error.Conflict(
        ErrorCodes.MustChangePasswordNotRequired, ErrorMessages.MustChangePasswordNotRequired);

    public static readonly Error RefreshTokenInvalid = Error.Unauthorized(
        ErrorCodes.RefreshTokenInvalid, ErrorMessages.RefreshTokenInvalid);

    public static readonly Error UserNotFound = Error.NotFound(
        ErrorCodes.UserNotFound, ErrorMessages.UserNotFound);
}
