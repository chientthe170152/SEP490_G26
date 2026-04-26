namespace MTCA.Application.Common.Constants;

public static class ErrorCodes
{
    public const string EmailRequired = "EMAIL_REQUIRED";
    public const string EmailInvalid = "EMAIL_INVALID";
    public const string PasswordRequired = "PASSWORD_REQUIRED";
    public const string PasswordTooShort = "PASSWORD_TOO_SHORT";

    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string ProfileInactive = "PROFILE_INACTIVE";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";
    public const string MustChangePasswordNotRequired = "MUST_CHANGE_PASSWORD_NOT_REQUIRED";
    public const string RefreshTokenInvalid = "REFRESH_TOKEN_INVALID";
    public const string UserNotFound = "USER_NOT_FOUND";

    public const string Unexpected = "UNEXPECTED";
    public const string Validation = "VALIDATION";
    public const string ValidationError = "VALIDATION_ERROR";
}
