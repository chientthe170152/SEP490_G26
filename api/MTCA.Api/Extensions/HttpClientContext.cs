namespace MTCA.Api.Extensions;

internal static class HttpClientContext
{
    private const string CfConnectingIp = "CF-Connecting-IP";

    public static (string? Ip, string? UserAgent) Resolve(HttpContext context)
    {
        var ip = ResolveIp(context);
        var ua = context.Request.Headers.UserAgent.ToString();
        return (ip, string.IsNullOrWhiteSpace(ua) ? null : ua);
    }

    public static string? ResolveIp(HttpContext context)
    {
        var cfIp = context.Request.Headers[CfConnectingIp].ToString();
        return !string.IsNullOrWhiteSpace(cfIp)
            ? cfIp
            : context.Connection.RemoteIpAddress?.ToString();
    }
}
