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
    private const int RawTokenByteLength = 32;

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

        var rawBytes = RandomNumberGenerator.GetBytes(RawTokenByteLength);
        var rawToken = Base64UrlEncode(rawBytes);
        var hash = Sha256Hex(rawToken);

        var db = redis.GetDatabase();
        var tokenKey = Keys.Token(userId, jti);
        var hashKey = Keys.Hash(hash);
        var userSetKey = Keys.UserSet(userId);

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

    public async Task<RefreshTokenValidation?> ValidateAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = Sha256Hex(rawToken);
        var hashKey = Keys.Hash(hash);
        var db = redis.GetDatabase();

        var value = await db.StringGetAsync(hashKey);
        if (!value.HasValue)
        {
            return null;
        }

        var parts = value.ToString().Split('|');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
        {
            return null;
        }

        var jti = parts[1];
        var tokenKey = Keys.Token(userId, jti);
        var storedHash = await db.HashGetAsync(tokenKey, "hash");

        if (!storedHash.HasValue || storedHash.ToString() != hash)
        {
            // Token Reuse Detection
            await RevokeAllAsync(userId, cancellationToken);
            return null;
        }

        return new RefreshTokenValidation(userId, jti);
    }

    public Task RevokeAsync(Guid userId, string jti, CancellationToken cancellationToken)
    {
        // Keep rt-hash:{hash} as tombstone so a replayed token resolves to userId|jti
        // and ValidateAsync detects it as reuse (rt:userId:jti missing). Tombstone expires at TTL.
        var db = redis.GetDatabase();
        var tokenKey = Keys.Token(userId, jti);

        var batch = db.CreateBatch();
        _ = batch.KeyDeleteAsync(tokenKey);
        _ = batch.SetRemoveAsync(Keys.UserSet(userId), jti);
        batch.Execute();

        return Task.CompletedTask;
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var userSetKey = Keys.UserSet(userId);

        var jtis = await db.SetMembersAsync(userSetKey);
        if (jtis.Length == 0)
        {
            return;
        }

        // Gather all hashes first to delete their reverse indexes
        var hashTasks = jtis.Select(jti => db.HashGetAsync(Keys.Token(userId, jti.ToString()), "hash")).ToList();
        await Task.WhenAll(hashTasks);

        var batch = db.CreateBatch();
        for (int i = 0; i < jtis.Length; i++)
        {
            var jti = jtis[i].ToString();
            var hashTask = hashTasks[i];

            if (hashTask.Result.HasValue)
            {
                _ = batch.KeyDeleteAsync(Keys.Hash(hashTask.Result.ToString()));
            }
            _ = batch.KeyDeleteAsync(Keys.Token(userId, jti));
        }

        _ = batch.KeyDeleteAsync(userSetKey);
        batch.Execute();
    }

    private static string Sha256Hex(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static class Keys
    {
        public static string Token(Guid userId, string jti) => $"rt:{userId}:{jti}";
        public static string Hash(string hash) => $"rt-hash:{hash}";
        public static string UserSet(Guid userId) => $"rt-user:{userId}";
    }
}
