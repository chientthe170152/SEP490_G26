namespace Backend.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool NeedsRegistration { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
