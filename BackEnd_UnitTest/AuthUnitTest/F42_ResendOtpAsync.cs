using System;
using System.Dynamic;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.AuthUnitTest;

// F42 - ResendOtpAsync
// Source: AuthService.cs:278-301
// Branches:
//   1. cache miss / cacheData == null  -> throw UnauthorizedAccessException(OtpExpiredOrNotExists)
//   2. cache hit                       -> generate new OTP, overwrite cache, send email
public class F42_ResendOtpAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F42_ResendOtpAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    [Fact(DisplayName = "ResendOtpAsync - UTCID01 - Cache hit -> sinh OTP mới, overwrite cache, gửi email")]
    [TestType("N")]
    public async Task ResendOtpAsync_UTCID01_CacheHit_ShouldRegenerateAndResend()
    {
        const string email = "user@test.com";
        var oldOtp = "111111";
        var regRequest = new RegisterRequest { Email = email, FullName = "Nguyen Van A", Password = "Pa$$1234", RoleId = 1 };
        // Use ExpandoObject so dynamic access from another assembly (the service) works.
        // Anonymous types are internal-scoped per assembly and aren't reachable via dynamic across assemblies.
        dynamic seed = new ExpandoObject();
        seed.Request = regRequest;
        seed.Otp = oldOtp;
        _cache.Set($"OTP_{email}", (object)seed, TimeSpan.FromMinutes(10));
        _emailMock.Setup(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.ResendOtpAsync(email);

        Assert.True(_cache.TryGetValue($"OTP_{email}", out object? cached));
        Assert.NotNull(cached);
        // Service writes back its own anonymous type — read via reflection
        var newOtpProp = cached!.GetType().GetProperty("Otp");
        Assert.NotNull(newOtpProp);
        var newOtp = (string)newOtpProp!.GetValue(cached)!;
        Assert.NotEqual(oldOtp, newOtp);
        Assert.Equal(6, newOtp.Length);
        _emailMock.Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact(DisplayName = "ResendOtpAsync - UTCID02 - Cache miss -> Unauthorized OtpExpiredOrNotExists")]
    [TestType("A")]
    public async Task ResendOtpAsync_UTCID02_CacheMiss_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ResendOtpAsync("ghost@test.com"));

        Assert.Equal(ErrorMessages.OtpExpiredOrNotExists, ex.Message);
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
