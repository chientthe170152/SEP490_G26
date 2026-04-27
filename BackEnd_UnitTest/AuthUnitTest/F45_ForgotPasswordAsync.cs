using System.Threading.Tasks;
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

// F45 - ForgotPasswordAsync
// Source: AuthService.cs:393-416
// Branches:
//   1. user == null      -> silent return (anti email-enumeration)
//   2. user found        -> generate OTP, cache(RESET_OTP_{email}, otp, 10min), send email
public class F45_ForgotPasswordAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F45_ForgotPasswordAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    [Fact(DisplayName = "ForgotPasswordAsync - UTCID01 - Email tồn tại -> cache OTP và gửi email")]
    [TestType("N")]
    public async Task ForgotPasswordAsync_UTCID01_EmailExists_ShouldCacheOtpAndSend()
    {
        const string email = "user@test.com";
        var user = UserBuilder.New().WithEmail(email).Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(user);
        _emailMock
            .Setup(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

        Assert.True(_cache.TryGetValue($"RESET_OTP_{email}", out string? otp));
        Assert.False(string.IsNullOrEmpty(otp));
        Assert.Equal(6, otp!.Length);
        _emailMock.Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ForgotPasswordAsync - UTCID02 - Email không tồn tại -> silent return (anti enumeration)")]
    [TestType("B")]
    public async Task ForgotPasswordAsync_UTCID02_EmailNotFound_ShouldSilentReturn()
    {
        const string email = "ghost@test.com";
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);

        await _service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

        Assert.False(_cache.TryGetValue($"RESET_OTP_{email}", out _));
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repoMock.VerifyAll();
    }
}
