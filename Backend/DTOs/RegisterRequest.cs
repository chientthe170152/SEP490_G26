using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [RegularExpression(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$", ErrorMessage = "Email không đúng định dạng chuẩn RFC 5322.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(72, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 72 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$", ErrorMessage = "Mật khẩu bắt buộc bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; }
    }
}
