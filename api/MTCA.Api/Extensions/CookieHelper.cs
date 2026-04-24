using MTCA.Api.Options;

namespace MTCA.Api.Extensions;

public static class CookieHelper
{
    private const string RefreshCookiePath = "/api/auth";

    public static void SetAccessCookie(HttpResponse response, string token, DateTimeOffset expiresAt, AuthCookieOptions opts) =>
        response.Cookies.Append(opts.Name, token, BuildOptions(opts, path: "/", expiresAt));

    public static void SetRefreshCookie(HttpResponse response, string token, DateTimeOffset expiresAt, AuthCookieOptions opts) =>
        response.Cookies.Append(opts.RefreshName, token, BuildOptions(opts, path: RefreshCookiePath, expiresAt));

    public static void ClearAccessCookie(HttpResponse response, AuthCookieOptions opts) =>
        response.Cookies.Delete(opts.Name, BuildDeleteOptions(opts, path: "/"));

    public static void ClearRefreshCookie(HttpResponse response, AuthCookieOptions opts) =>
        response.Cookies.Delete(opts.RefreshName, BuildDeleteOptions(opts, path: RefreshCookiePath));

    private static CookieOptions BuildOptions(AuthCookieOptions opts, string path, DateTimeOffset expiresAt) => new()
    {
        HttpOnly = opts.HttpOnly,
        Secure = opts.Secure,
        SameSite = ParseSameSite(opts.SameSite),
        Domain = string.IsNullOrWhiteSpace(opts.Domain) ? null : opts.Domain,
        Path = path,
        Expires = expiresAt,
        IsEssential = true
    };

    private static CookieOptions BuildDeleteOptions(AuthCookieOptions opts, string path) => new()
    {
        HttpOnly = opts.HttpOnly,
        Secure = opts.Secure,
        SameSite = ParseSameSite(opts.SameSite),
        Domain = string.IsNullOrWhiteSpace(opts.Domain) ? null : opts.Domain,
        Path = path
    };

    private static SameSiteMode ParseSameSite(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "strict" => SameSiteMode.Strict,
        "none" => SameSiteMode.None,
        _ => SameSiteMode.Lax
    };
}
