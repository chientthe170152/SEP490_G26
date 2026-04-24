using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Infrastructure.Options;
using StackExchange.Redis;

namespace MTCA.Infrastructure.Services.Tokens;

public sealed class RefreshTokenStore(
    IConnectionMultiplexer redis,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IRefreshTokenStore
{
    private readonly JwtOptions _options = options.Value;

    public async Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        Guid userId,
        string jti,
        string? ip,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddDays(_options.RefreshTokenDays);
        var ttl = expiresAt - now;

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncode(rawBytes);
        var hash = Sha256Hex(rawToken);

        var db = redis.GetDatabase();
        var tokenKey = $"rt:{userId}:{jti}";
        var hashKey = $"rt-hash:{hash}";
        var userSetKey = $"rt-user:{userId}";

        var batch = db.CreateBatch();
        var hashSetTask = batch.HashSetAsync(tokenKey, new HashEntry[]
        {
            new("hash", hash),
            new("issuedAt", now.ToUnixTimeSeconds()),
            new("ua", userAgent ?? string.Empty),
            new("ip", ip ?? string.Empty),
            new("expiresAt", expiresAt.ToUnixTimeSeconds())
        });
        var tokenExpireTask = batch.KeyExpireAsync(tokenKey, ttl);
        var hashIndexTask = batch.StringSetAsync(hashKey, $"{userId}|{jti}", ttl);
        var userSetTask = batch.SetAddAsync(userSetKey, jti);
        var userSetExpireTask = batch.KeyExpireAsync(userSetKey, ttl);

        batch.Execute();

        await Task.WhenAll(hashSetTask, tokenExpireTask, hashIndexTask, userSetTask, userSetExpireTask);

        return (rawToken, expiresAt);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string Sha256Hex(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
