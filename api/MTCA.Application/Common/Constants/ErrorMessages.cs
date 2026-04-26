namespace MTCA.Application.Common.Constants;

public static class ErrorMessages
{
    public const string InvalidCredentials = "Email hoặc mật khẩu không đúng.";
    public const string AccountLocked = "Tài khoản đã bị khoá tạm thời. Vui lòng thử lại sau.";
    public const string ProfileInactive = "Tài khoản chưa được kích hoạt hoặc đã bị vô hiệu hoá.";
    public const string Unauthenticated = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";
    public const string PasswordChangeRequired = "Bạn cần đổi mật khẩu trước khi tiếp tục.";
    public const string MustChangePasswordNotRequired = "Tài khoản không ở trạng thái bắt buộc đổi mật khẩu.";
    public const string RefreshTokenInvalid = "Refresh token không hợp lệ hoặc đã hết hạn.";
    public const string UserNotFound = "Không tìm thấy người dùng.";

    public const string Unexpected = "Unexpected error.";
    public const string ValidationOccurred = "One or more validation errors occurred.";
}
