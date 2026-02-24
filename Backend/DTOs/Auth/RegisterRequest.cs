using System.ComponentModel.DataAnnotations;
using Backend.Constants;

namespace Backend.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = ValidationMessages.EmailRequired)]
        [RegularExpression(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$", ErrorMessage = ValidationMessages.EmailFormatInvalid)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
        [StringLength(72, MinimumLength = 8, ErrorMessage = ValidationMessages.PasswordLengthInvalid)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$", ErrorMessage = ValidationMessages.PasswordComplexityInvalid)]
        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; }
    }
}
