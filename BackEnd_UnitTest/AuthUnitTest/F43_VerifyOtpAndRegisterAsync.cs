using System;
using System.Dynamic;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs;
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

// F43 - VerifyOtpAndRegisterAsync
// Source: AuthService.cs:303-353
// Branches:
//   1. cache miss / cacheData null            -> throw UnauthorizedAccessException(OtpExpiredOrNotExists)
//   2. cacheData.Otp != request.OtpCode       -> throw UnauthorizedAccessException(InvalidOtp)
//   3. existingUser != null (race)            -> throw InvalidOperationException(UserAlreadyExists) + cache cleanup
//   4. Success                                -> create user, save, return token
public class F43_VerifyOtpAndRegisterAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F43_VerifyOtpAndRegisterAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    private RegisterRequest BuildRegRequest(string email = "new@test.com", int roleId = 1) => new()
    {
        Email = email,
        FullName = "Nguyen Van A",
        Password = "Pa$$word1",
        PhoneNumber = "0123456789",
        StudentId = roleId == 2 ? "HE172047" : null,
        RoleId = roleId
    };

    private static object MakeCacheData(RegisterRequest req, string otp)
    {
        // Service reads via `dynamic` — anonymous types are internal per-assembly so they fail
        // cross-assembly dynamic dispatch. ExpandoObject implements IDynamicMetaObjectProvider
        // and is dynamically reachable from any caller.
        dynamic e = new ExpandoObject();
        e.Request = req;
        e.Otp = otp;
        return (object)e;
    }

    [Fact(DisplayName = "VerifyOtpAndRegisterAsync - UTCID01 - OTP đúng, user chưa tồn tại -> tạo user và trả token")]
    [TestType("N")]
    public async Task VerifyOtpAndRegisterAsync_UTCID01_ValidOtp_ShouldCreateUserAndReturnToken()
    {
        const string email = "new@test.com";
        const string otp = "123456";
        var reg = BuildRegRequest(email);
        _cache.Set($"OTP_{email}", MakeCacheData(reg, otp), TimeSpan.FromMinutes(10));
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);
        User? captured = null;
        _repoMock.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) =>
                 {
                     captured = u;
                     u.UserId = 42;
                     // Simulate EF loading the Role navigation property after AddUser — covers `user.Role?.Name ?? "User"` non-null branch.
                     u.Role = new Role { RoleId = u.RoleId, Name = UserRoles.Teacher };
                     return u;
                 });

        var result = await _service.VerifyOtpAndRegisterAsync(new VerifyOtpRequest { Email = email, OtpCode = otp });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal(UserRoles.Teacher, result.RoleName);
        Assert.NotNull(captured);
        Assert.Equal(email, captured!.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify(reg.Password, captured.PasswordHash));
        Assert.False(_cache.TryGetValue($"OTP_{email}", out _));
    }

    [Fact(DisplayName = "VerifyOtpAndRegisterAsync - UTCID02 - Cache miss -> Unauthorized OtpExpiredOrNotExists")]
    [TestType("A")]
    public async Task VerifyOtpAndRegisterAsync_UTCID02_CacheMiss_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.VerifyOtpAndRegisterAsync(new VerifyOtpRequest { Email = "x@y.com", OtpCode = "111111" }));

        Assert.Equal(ErrorMessages.OtpExpiredOrNotExists, ex.Message);
    }

    [Fact(DisplayName = "VerifyOtpAndRegisterAsync - UTCID03 - OTP sai -> Unauthorized InvalidOtp")]
    [TestType("A")]
    public async Task VerifyOtpAndRegisterAsync_UTCID03_WrongOtp_ShouldThrow()
    {
        const string email = "x@y.com";
        var reg = BuildRegRequest(email);
        _cache.Set($"OTP_{email}", MakeCacheData(reg, "111111"), TimeSpan.FromMinutes(10));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.VerifyOtpAndRegisterAsync(new VerifyOtpRequest { Email = email, OtpCode = "000000" }));

        Assert.Equal(ErrorMessages.InvalidOtp, ex.Message);
        Assert.True(_cache.TryGetValue($"OTP_{email}", out _));
    }

    [Fact(DisplayName = "VerifyOtpAndRegisterAsync - UTCID05 - FullName/Phone/StudentId null -> trim coalesces về empty/null")]
    [TestType("B")]
    public async Task VerifyOtpAndRegisterAsync_UTCID05_NullOptionalFields_ShouldHandleNulls()
    {
        const string email = "blank@test.com";
        const string otp = "987654";
        var reg = new RegisterRequest
        {
            Email = email,
            FullName = null!,
            Password = "Pa$$word1",
            PhoneNumber = null,
            StudentId = null,
            RoleId = 1
        };
        _cache.Set($"OTP_{email}", MakeCacheData(reg, otp), TimeSpan.FromMinutes(10));
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);
        User? captured = null;
        _repoMock.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { captured = u; u.UserId = 99; return u; });

        await _service.VerifyOtpAndRegisterAsync(new VerifyOtpRequest { Email = email, OtpCode = otp });

        Assert.NotNull(captured);
        Assert.Equal(string.Empty, captured!.FullName);
        Assert.Null(captured.PhoneNumber);
        Assert.Null(captured.StudentId);
    }

    [Fact(DisplayName = "VerifyOtpAndRegisterAsync - UTCID06 - PhoneNumber/StudentId có khoảng trắng -> trim")]
    [TestType("B")]
    public async Task VerifyOtpAndRegisterAsync_UTCID06_PaddedFields_ShouldTrim()
    {
        const string email = "trim@test.com";
        const string otp = "246810";
        var reg = new RegisterRequest
        {
            Email = email,
            FullName = "  Nguyen Van B  ",
            Password = "Pa$$word2",
            PhoneNumber = "  0987654321  ",
            StudentId = "  HE123456  ",
            RoleId = 2
        };
        _cache.Set($"OTP_{email}", MakeCacheData(reg, otp), TimeSpan.FromMinutes(10));
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);
        User? captured = null;
        _repoMock.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { captured = u; u.UserId = 100; return u; });

        await _service.VerifyOtpAndRegisterAsync(new VerifyOtpRequest { Email = email, OtpCode = otp });

        Assert.Equal("Nguyen Van B", captured!.FullName);
        Assert.Equal("0987654321", captured.PhoneNumber);
        Assert.Equal("HE123456", captured.StudentId);
    }

    [Fact(DisplayName = "VerifyOtpAndRegisterAsync - UTCID04 - Race: user đã tồn tại sau verify OTP -> InvalidOperationException UserAlreadyExists")]
    [TestType("A")]
    public async Task VerifyOtpAndRegisterAsync_UTCID04_UserAlreadyExists_ShouldThrow()
    {
        const string email = "race@test.com";
        const string otp = "654321";
        var reg = BuildRegRequest(email);
        _cache.Set($"OTP_{email}", MakeCacheData(reg, otp), TimeSpan.FromMinutes(10));
        var existing = UserBuilder.New().WithEmail(email).Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.VerifyOtpAndRegisterAsync(new VerifyOtpRequest { Email = email, OtpCode = otp }));

        Assert.Equal(ErrorMessages.UserAlreadyExists, ex.Message);
        _repoMock.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        // Cache đã bị remove ở đầu (one-time use) + một lần cleanup thừa khi exception
        Assert.False(_cache.TryGetValue($"OTP_{email}", out _));
    }
}
