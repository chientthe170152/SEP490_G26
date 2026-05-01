using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Services.Implements;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Backend_UnitTest.OtpStoreUnitTest
{
    public class RedisOtpStoreTests
    {
        private readonly Mock<IConnectionMultiplexer> _mux;
        private readonly Mock<IDatabase> _db;
        private readonly RedisOtpStore _store;

        public RedisOtpStoreTests()
        {
            _mux = new Mock<IConnectionMultiplexer>();
            _db = new Mock<IDatabase>();
            _mux.Setup(m => m.GetDatabase(RedisKeys.AuthDb, It.IsAny<object>())).Returns(_db.Object);
            _store = new RedisOtpStore(_mux.Object);
        }

        private static string ExpectedKey(string email) =>
            $"{RedisKeys.OtpPrefix}reset:{email.ToLowerInvariant()}";

        [Fact(DisplayName = "SetResetOtpAsync - UTCID01 - Lưu OTP vào Redis với TTL 10p")]
        public async Task SetResetOtpAsync_UTCID01_ShouldStoreWithTtl()
        {
            var email = "User@Example.com";
            _db.Setup(d => d.StringSetAsync(
                ExpectedKey(email), "123456", TimeSpan.FromMinutes(10),
                false, When.Always, CommandFlags.None)).ReturnsAsync(true);

            await _store.SetResetOtpAsync(email, "123456");

            _db.Verify(d => d.StringSetAsync(
                ExpectedKey(email), "123456", TimeSpan.FromMinutes(10),
                false, When.Always, CommandFlags.None), Times.Once);
        }

        [Fact(DisplayName = "GetResetOtpAsync - UTCID01 - Có giá trị -> trả về OTP")]
        public async Task GetResetOtpAsync_UTCID01_ValuePresent_ShouldReturnOtp()
        {
            var email = "abc@example.com";
            _db.Setup(d => d.StringGetAsync(ExpectedKey(email), CommandFlags.None))
                .ReturnsAsync(new RedisValue("999999"));

            var otp = await _store.GetResetOtpAsync(email);

            Assert.Equal("999999", otp);
        }

        [Fact(DisplayName = "GetResetOtpAsync - UTCID02 - Không có giá trị -> trả về null")]
        public async Task GetResetOtpAsync_UTCID02_ValueAbsent_ShouldReturnNull()
        {
            var email = "abc@example.com";
            _db.Setup(d => d.StringGetAsync(ExpectedKey(email), CommandFlags.None))
                .ReturnsAsync(RedisValue.Null);

            var otp = await _store.GetResetOtpAsync(email);

            Assert.Null(otp);
        }

        [Fact(DisplayName = "RemoveResetOtpAsync - UTCID01 - Gọi KeyDelete với key chuẩn hoá lowercase")]
        public async Task RemoveResetOtpAsync_UTCID01_ShouldDeleteKey()
        {
            var email = "MIXED@Test.COM";
            _db.Setup(d => d.KeyDeleteAsync(ExpectedKey(email), CommandFlags.None))
                .ReturnsAsync(true);

            await _store.RemoveResetOtpAsync(email);

            _db.Verify(d => d.KeyDeleteAsync(ExpectedKey(email), CommandFlags.None), Times.Once);
        }

        [Fact(DisplayName = "Key building - UTCID01 - Email khác hoa thường tạo cùng key")]
        public async Task Keys_UTCID01_ShouldBeCaseInsensitive()
        {
            var lower = "user@example.com";
            var upper = "USER@EXAMPLE.COM";
            _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ReturnsAsync(RedisValue.Null);

            await _store.GetResetOtpAsync(lower);
            await _store.GetResetOtpAsync(upper);

            _db.Verify(d => d.StringGetAsync(ExpectedKey(lower), CommandFlags.None), Times.Exactly(2));
        }
    }
}
