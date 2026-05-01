using System;
using System.Threading.Tasks;
using Backend.Common.Options;
using Backend.Constants;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Backend_UnitTest.RefreshTokenStoreUnitTest
{
    public class RefreshTokenStoreTests
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IConnectionMultiplexer> _mux = new();
        private readonly Mock<IDatabase> _db = new();
        private readonly Mock<ITransaction> _tran = new();
        private readonly Mock<IBatch> _batch = new();
        private readonly Mock<ILogger<RefreshTokenStore>> _logger = new();
        private readonly JwtOptions _opts = new()
        {
            Issuer = "i",
            Audience = "a",
            Key = "k-very-long-test-secret-key-256bits-okok",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        public RefreshTokenStoreTests()
        {
            _mux.Setup(m => m.GetDatabase(RedisKeys.AuthDb, It.IsAny<object>())).Returns(_db.Object);
            _db.Setup(d => d.CreateTransaction(It.IsAny<object>())).Returns(_tran.Object);
            _db.Setup(d => d.CreateBatch(It.IsAny<object>())).Returns(_batch.Object);
        }

        private RefreshTokenStore BuildStore() =>
            new RefreshTokenStore(_mux.Object, Options.Create(_opts), new FakeTimeProvider(FixedNow), _logger.Object);

        [Fact(DisplayName = "IssueAsync - UTCID01 - Commit thành công -> trả raw token + expiresAt")]
        public async Task IssueAsync_UTCID01_CommitSuccess_ShouldReturnTokenAndExpiry()
        {
            _tran.Setup(t => t.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
                .Returns(Task.CompletedTask);
            _tran.Setup(t => t.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();

            var (token, exp) = await store.IssueAsync(7, "jti-123", "1.2.3.4", "ua-string");

            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.Equal(FixedNow.AddDays(7), exp);
            _tran.Verify(t => t.ExecuteAsync(It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact(DisplayName = "IssueAsync - UTCID02 - Commit fail -> throw InvalidOperationException")]
        public async Task IssueAsync_UTCID02_CommitFails_ShouldThrow()
        {
            _tran.Setup(t => t.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
                .Returns(Task.CompletedTask);
            _tran.Setup(t => t.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(false);

            var store = BuildStore();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.IssueAsync(7, "jti-123", null, null));
        }

        [Fact(DisplayName = "IssueAsync - UTCID03 - userAgent dài quá 512 ký tự -> truncate")]
        public async Task IssueAsync_UTCID03_LongUa_ShouldTruncate()
        {
            HashEntry[]? captured = null;
            _tran.Setup(t => t.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, HashEntry[], CommandFlags>((_, e, _) => captured = e)
                .Returns(Task.CompletedTask);
            _tran.Setup(t => t.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _tran.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();
            var longUa = new string('x', 1000);

            await store.IssueAsync(1, "jti", "ip", longUa);

            Assert.NotNull(captured);
            var uaEntry = Array.Find(captured!, e => e.Name == "ua");
            Assert.Equal(512, ((string)uaEntry.Value!).Length);
        }

        [Fact(DisplayName = "ConsumeAsync - UTCID01 - Hash key không tồn tại -> trả null")]
        public async Task ConsumeAsync_UTCID01_HashMissing_ShouldReturnNull()
        {
            _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            var store = BuildStore();

            var result = await store.ConsumeAsync("any-token");

            Assert.Null(result);
        }

        [Fact(DisplayName = "ConsumeAsync - UTCID02 - Owner string sai format (không có |) -> null")]
        public async Task ConsumeAsync_UTCID02_BadOwnerFormat_ShouldReturnNull()
        {
            _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync("badowner");

            var store = BuildStore();

            var result = await store.ConsumeAsync("any-token");

            Assert.Null(result);
        }

        [Fact(DisplayName = "ConsumeAsync - UTCID03 - userId không phải số -> null")]
        public async Task ConsumeAsync_UTCID03_BadUserId_ShouldReturnNull()
        {
            _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync("abc|jti");

            var store = BuildStore();

            var result = await store.ConsumeAsync("any-token");

            Assert.Null(result);
        }

        [Fact(DisplayName = "ConsumeAsync - UTCID04 - Hash khớp + commit OK -> trả về RefreshTokenValidation")]
        public async Task ConsumeAsync_UTCID04_HashMatch_CommitOk_ShouldReturnValidation()
        {
            _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync("42|some-jti");

            _tran.Setup(t => t.AddCondition(It.IsAny<Condition>())).Returns((ConditionResult)null!);
            _tran.Setup(t => t.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _tran.Setup(t => t.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _tran.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();

            var result = await store.ConsumeAsync("any-token");

            Assert.NotNull(result);
            Assert.Equal(42, result!.UserId);
            Assert.Equal("some-jti", result.Jti);
        }

        [Fact(DisplayName = "ConsumeAsync - UTCID05 - Hash sai (replay) -> revoke all + null")]
        public async Task ConsumeAsync_UTCID05_HashMismatch_ShouldRevokeAllAndReturnNull()
        {
            _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync("42|some-jti");

            _tran.Setup(t => t.AddCondition(It.IsAny<Condition>())).Returns((ConditionResult)null!);
            _tran.Setup(t => t.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _tran.Setup(t => t.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _tran.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(false);

            // RevokeAllAsync path
            _db.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(Array.Empty<RedisValue>());

            var store = BuildStore();

            var result = await store.ConsumeAsync("any-token");

            Assert.Null(result);
            _db.Verify(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact(DisplayName = "RevokeAsync - UTCID01 - Có hash -> xoá hashKey, tokenKey, set member")]
        public async Task RevokeAsync_UTCID01_WithHash_ShouldDeleteAll()
        {
            _db.Setup(d => d.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("existing-hash"));
            _batch.Setup(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _batch.Setup(b => b.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();

            await store.RevokeAsync(1, "jti");

            _batch.Verify(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(2));
            _batch.Verify(b => b.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact(DisplayName = "RevokeAsync - UTCID02 - Không có hash -> bỏ qua xoá hashKey")]
        public async Task RevokeAsync_UTCID02_NoHash_ShouldSkipHashDelete()
        {
            _db.Setup(d => d.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);
            _batch.Setup(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _batch.Setup(b => b.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();

            await store.RevokeAsync(1, "jti");

            _batch.Verify(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
            _batch.Verify(b => b.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact(DisplayName = "RevokeAllAsync - UTCID01 - Empty set -> không làm gì")]
        public async Task RevokeAllAsync_UTCID01_EmptySet_ShouldDoNothing()
        {
            _db.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(Array.Empty<RedisValue>());

            var store = BuildStore();

            await store.RevokeAllAsync(1);

            _db.Verify(d => d.CreateBatch(It.IsAny<object>()), Times.Never);
        }

        [Fact(DisplayName = "RevokeAllAsync - UTCID02 - Có jti với hash -> xoá tất cả")]
        public async Task RevokeAllAsync_UTCID02_WithJti_ShouldDeleteAll()
        {
            _db.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new[] { new RedisValue("jti1"), new RedisValue("jti2") });
            _db.Setup(d => d.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue("hash-x"));
            _batch.Setup(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();

            await store.RevokeAllAsync(1);

            // 2 jti × (delete hash + delete token) + 1 delete user set = 5
            _batch.Verify(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(5));
        }

        [Fact(DisplayName = "RevokeAllAsync - UTCID03 - Có jti nhưng không có hash -> chỉ xoá token + set")]
        public async Task RevokeAllAsync_UTCID03_NoHash_ShouldSkipHashDelete()
        {
            _db.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new[] { new RedisValue("jti1") });
            _db.Setup(d => d.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);
            _batch.Setup(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var store = BuildStore();

            await store.RevokeAllAsync(1);

            // 1 jti without hash × delete token + 1 delete user set = 2
            _batch.Verify(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(2));
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;
            public FakeTimeProvider(DateTimeOffset now) => _now = now;
            public override DateTimeOffset GetUtcNow() => _now;
        }
    }
}
