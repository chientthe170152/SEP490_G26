using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MTCA.Application.Common.Interfaces.Services;
using MTCA.Infrastructure.Options;
using StackExchange.Redis;

namespace MTCA.Infrastructure.Services.Tokens;

public sealed class RefreshTokenStore(
    IConnectionMultiplexer redis,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<RefreshTokenStore> logger) : IRefreshTokenStore
{
    private const int RawTokenByteLength = 32;
    private const int UserAgentMaxLength = 512;

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

        var ua = Truncate(userAgent ?? string.Empty, UserAgentMaxLength);

        // MULTI/EXEC: all-or-nothing so Validate never observes partial state.
        var tran = db.CreateTransaction();
        _ = tran.HashSetAsync(tokenKey, new HashEntry[]
        {
            new("hash", hash),
            new("issuedAt", now.ToUnixTimeSeconds()),
            new("ua", ua),
            new("ip", ip ?? string.Empty),
            new("expiresAt", expiresAt.ToUnixTimeSeconds())
        });
        _ = tran.KeyExpireAsync(tokenKey, ttl);
        _ = tran.StringSetAsync(hashKey, $"{userId}|{jti}", ttl);
        _ = tran.SetAddAsync(userSetKey, jti);
        _ = tran.KeyExpireAsync(userSetKey, ttl);

        var committed = await tran.ExecuteAsync();
        if (!committed)
        {
            throw new InvalidOperationException("Failed to persist refresh token to Redis.");
        }

        return (rawToken, expiresAt);
    }

    public async Task<RefreshTokenValidation?> ConsumeAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = Sha256Hex(rawToken);
        var hashKey = Keys.Hash(hash);
        var db = redis.GetDatabase();

        var owner = await db.StringGetAsync(hashKey);
        if (owner.IsNull)
        {
            return null;
        }

        var ownerStr = owner.ToString();
        var sep = ownerStr.IndexOf('|');
        if (sep <= 0 || !Guid.TryParse(ownerStr[..sep], out var userId))
        {
            return null;
        }
        var jti = ownerStr[(sep + 1)..];

        var tokenKey = Keys.Token(userId, jti);
        var userSetKey = Keys.UserSet(userId);

        var tran = db.CreateTransaction();
        tran.AddCondition(Condition.HashEqual(tokenKey, "hash", hash));
        var delTask = tran.KeyDeleteAsync(tokenKey);
        var sremTask = tran.SetRemoveAsync(userSetKey, jti);

        var committed = await tran.ExecuteAsync();
        if (committed)
        {
            await Task.WhenAll(delTask, sremTask);
            return new RefreshTokenValidation(userId, jti);
        }

        await RevokeAllAsync(userId, cancellationToken);
        return null;
    }

    public async Task RevokeAsync(Guid userId, string jti, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var tokenKey = Keys.Token(userId, jti);

        var batch = db.CreateBatch();
        var del = batch.KeyDeleteAsync(tokenKey);
        var srem = batch.SetRemoveAsync(Keys.UserSet(userId), jti);
        batch.Execute();

        await Task.WhenAll(del, srem);
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

        var hashTasks = jtis
            .Select(jti => db.HashGetAsync(Keys.Token(userId, jti.ToString()), "hash"))
            .ToList();
        await Task.WhenAll(hashTasks);

        var batch = db.CreateBatch();
        var pending = new List<Task>(jtis.Length * 2 + 1);
        for (int i = 0; i < jtis.Length; i++)
        {
            var jti = jtis[i].ToString();
            var storedHash = hashTasks[i].Result;

            if (storedHash.HasValue)
            {
                pending.Add(batch.KeyDeleteAsync(Keys.Hash(storedHash.ToString())));
            }
            pending.Add(batch.KeyDeleteAsync(Keys.Token(userId, jti)));
        }
        pending.Add(batch.KeyDeleteAsync(userSetKey));
        batch.Execute();

        await Task.WhenAll(pending);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string Sha256Hex(string value)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static class Keys
    {
        public static string Token(Guid userId, string jti) => $"rt:{userId}:{jti}";
        public static string Hash(string hash) => $"rt-hash:{hash}";
        public static string UserSet(Guid userId) => $"rt-user:{userId}";
    }
}
