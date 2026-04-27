using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.AuthUnitTest;

// F46 - ResetPasswordAsync
// Source: AuthService.cs:418-445
// Branches:
//   1. cache miss / cachedOtp empty   -> throw UnauthorizedAccessException("Mã OTP đã hết hạn hoặc không tồn tại.")
//   2. cachedOtp != request.OtpCode   -> throw UnauthorizedAccessException("Mã OTP không chính xác.")
//   3. user == null                   -> throw InvalidOperationException(ErrorMessages.UserNotFound)
//   4. Success                        -> hash new password, update SecurityStamp, save (cache already removed)
public class F46_ResetPasswordAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F46_ResetPasswordAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    [Fact(DisplayName = "ResetPasswordAsync - UTCID01 - OTP đúng + user tồn tại -> đổi password + SecurityStamp")]
    [TestType("N")]
    public async Task ResetPasswordAsync_UTCID01_ValidOtpAndUser_ShouldUpdatePassword()
    {
        const string email = "user@test.com";
        const string otp = "123456";
        const string newPwd = "NewPa$$1";
        _cache.Set($"RESET_OTP_{email}", otp, TimeSpan.FromMinutes(10));
        var oldStamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = UserBuilder.New().WithEmail(email).WithSecurityStamp(oldStamp).Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);

        await _service.ResetPasswordAsync(new ResetPasswordRequest { Email = email, OtpCode = otp, NewPassword = newPwd });

        Assert.True(BCrypt.Net.BCrypt.Verify(newPwd, user.PasswordHash));
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        Assert.False(_cache.TryGetValue($"RESET_OTP_{email}", out _));
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ResetPasswordAsync - UTCID02 - Cache miss -> Unauthorized OTP expired")]
    [TestType("A")]
    public async Task ResetPasswordAsync_UTCID02_CacheMiss_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ResetPasswordAsync(new ResetPasswordRequest { Email = "x@y.com", OtpCode = "111111", NewPassword = "X" }));

        // NOTE: source string ("Mã OTP đã hết hạn hoặc không tồn tại.") differs from
        // ErrorMessages.OtpExpiredOrNotExists ("OTP đã hết hạn hoặc không tồn tại.") — pre-existing
        // inconsistency in AuthService.cs:424 vs Messages.cs. Test mirrors actual thrown text.
        Assert.Equal("Mã OTP đã hết hạn hoặc không tồn tại.", ex.Message);
    }

    [Fact(DisplayName = "ResetPasswordAsync - UTCID03 - Cached OTP empty string -> Unauthorized expired (boundary)")]
    [TestType("B")]
    public async Task ResetPasswordAsync_UTCID03_CachedOtpEmpty_ShouldThrow()
    {
        const string email = "x@y.com";
        _cache.Set($"RESET_OTP_{email}", string.Empty, TimeSpan.FromMinutes(10));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ResetPasswordAsync(new ResetPasswordRequest { Email = email, OtpCode = "111111", NewPassword = "X" }));

        // NOTE: source string ("Mã OTP đã hết hạn hoặc không tồn tại.") differs from
        // ErrorMessages.OtpExpiredOrNotExists ("OTP đã hết hạn hoặc không tồn tại.") — pre-existing
        // inconsistency in AuthService.cs:424 vs Messages.cs. Test mirrors actual thrown text.
        Assert.Equal("Mã OTP đã hết hạn hoặc không tồn tại.", ex.Message);
    }

    [Fact(DisplayName = "ResetPasswordAsync - UTCID04 - OTP sai -> Unauthorized OTP không chính xác")]
    [TestType("A")]
    public async Task ResetPasswordAsync_UTCID04_WrongOtp_ShouldThrow()
    {
        const string email = "x@y.com";
        _cache.Set($"RESET_OTP_{email}", "111111", TimeSpan.FromMinutes(10));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ResetPasswordAsync(new ResetPasswordRequest { Email = email, OtpCode = "000000", NewPassword = "X" }));

        Assert.Equal(ErrorMessages.InvalidOtp, ex.Message);
        // Cache should NOT be removed (per source order: remove only after both checks pass)
        Assert.True(_cache.TryGetValue($"RESET_OTP_{email}", out _));
    }

    [Fact(DisplayName = "ResetPasswordAsync - UTCID05 - OTP đúng nhưng user không tồn tại -> InvalidOperationException UserNotFound")]
    [TestType("A")]
    public async Task ResetPasswordAsync_UTCID05_UserNotFound_ShouldThrow()
    {
        const string email = "ghost@test.com";
        const string otp = "654321";
        _cache.Set($"RESET_OTP_{email}", otp, TimeSpan.FromMinutes(10));
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResetPasswordAsync(new ResetPasswordRequest { Email = email, OtpCode = otp, NewPassword = "X" }));

        Assert.Equal(ErrorMessages.UserNotFound, ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }
}
