using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace BackEnd_UnitTest._Shared;

public static class TestConfigBuilder
{
    public const string JwtKey = "test-jwt-key-must-be-at-least-32-bytes-long-for-hmac-sha256";
    public const string JwtIssuer = "test-issuer";
    public const string JwtAudience = "test-audience";
    public const string GoogleClientId = "test-google-client-id";
    public const string FrontendBaseUrl = "https://frontend.test";

    public static IConfiguration Default()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Google:ClientId"] = GoogleClientId,
                ["FrontendSettings:BaseUrl"] = FrontendBaseUrl
            })
            .Build();

    public static IConfiguration Override(params (string Key, string? Value)[] overrides)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = JwtKey,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            ["Google:ClientId"] = GoogleClientId,
            ["FrontendSettings:BaseUrl"] = FrontendBaseUrl
        };
        foreach (var (k, v) in overrides) dict[k] = v;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }
}
