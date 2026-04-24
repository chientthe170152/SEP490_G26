using MTCA.Application.Common.Models;

namespace MTCA.Application.Common.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.");

    public static readonly Error AccountLocked = Error.Locked(
        "ACCOUNT_LOCKED", "Tài khoản đã bị khoá tạm thời. Vui lòng thử lại sau.");

    public static readonly Error ProfileInactive = Error.Forbidden(
        "PROFILE_INACTIVE", "Tài khoản chưa được kích hoạt hoặc đã bị vô hiệu hoá.");

    public static readonly Error Unauthenticated = Error.Unauthorized(
        "UNAUTHENTICATED", "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");

    public static readonly Error PasswordChangeRequired = Error.Forbidden(
        "PASSWORD_CHANGE_REQUIRED", "Bạn cần đổi mật khẩu trước khi tiếp tục.");

    public static readonly Error MustChangePasswordNotRequired = Error.Conflict(
        "MUST_CHANGE_PASSWORD_NOT_REQUIRED", "Tài khoản không ở trạng thái bắt buộc đổi mật khẩu.");

    public static readonly Error RefreshTokenInvalid = Error.Unauthorized(
        "REFRESH_TOKEN_INVALID", "Refresh token không hợp lệ hoặc đã hết hạn.");

    public static readonly Error UserNotFound = Error.NotFound(
        "USER_NOT_FOUND", "Không tìm thấy người dùng.");
}
