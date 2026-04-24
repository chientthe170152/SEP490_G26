namespace MTCA.Api.Options;

public sealed class AuthCookieOptions
{
    public const string SectionName = "Auth:Cookie";

    public string Name { get; set; } = "mtca.auth";
    public string RefreshName { get; set; } = "mtca.rt";
    public string CsrfName { get; set; } = "mtca.csrf";
    public bool Secure { get; set; } = true;
    public string SameSite { get; set; } = "Lax";
    public bool HttpOnly { get; set; } = true;
    public string? Domain { get; set; }
}
