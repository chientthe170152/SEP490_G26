namespace Backend.DTOs
{
    public class GoogleRegisterRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}
