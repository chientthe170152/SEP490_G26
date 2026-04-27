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

// F47 - ChangePasswordAsync
// Source: AuthService.cs:447-469
// Branches:
//   1. user == null                                                  -> throw InvalidOperationException(ErrorMessages.UserNotFound)
//   2. user.PasswordHash null/empty (Google account)                 -> throw InvalidOperationException("Tài khoản của bạn được liên kết với Google...")
//   3. !BCrypt.Verify(request.OldPassword, user.PasswordHash)        -> throw UnauthorizedAccessException("Mật khẩu cũ không chính xác.")
//   4. Success                                                       -> update PasswordHash + SecurityStamp + save
public class F47_ChangePasswordAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    private const string OldPassword = "Old$Pass1";
    private const string NewPassword = "New$Pass2";

    public F47_ChangePasswordAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    [Fact(DisplayName = "ChangePasswordAsync - UTCID01 - Old password đúng -> cập nhật hash + SecurityStamp")]
    [TestType("N")]
    public async Task ChangePasswordAsync_UTCID01_ValidOldPassword_ShouldUpdate()
    {
        var oldHash = BCrypt.Net.BCrypt.HashPassword(OldPassword);
        var oldStamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = UserBuilder.New().WithId(1).WithPasswordHash(oldHash).WithSecurityStamp(oldStamp).Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);

        await _service.ChangePasswordAsync(1, new ChangePasswordRequest { OldPassword = OldPassword, NewPassword = NewPassword });

        Assert.NotEqual(oldHash, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, user.PasswordHash));
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ChangePasswordAsync - UTCID02 - User không tồn tại -> InvalidOperationException UserNotFound")]
    [TestType("A")]
    public async Task ChangePasswordAsync_UTCID02_UserNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangePasswordAsync(999, new ChangePasswordRequest { OldPassword = OldPassword, NewPassword = NewPassword }));

        Assert.Equal(ErrorMessages.UserNotFound, ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact(DisplayName = "ChangePasswordAsync - UTCID03 - PasswordHash null (Google account) -> InvalidOperationException Google hint")]
    [TestType("A")]
    public async Task ChangePasswordAsync_UTCID03_GoogleAccountNullHash_ShouldThrow()
    {
        var user = UserBuilder.New().WithId(2).WithPasswordHash(null).Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangePasswordAsync(2, new ChangePasswordRequest { OldPassword = OldPassword, NewPassword = NewPassword }));

        Assert.Equal("Tài khoản của bạn được liên kết với Google. Vui lòng thiết lập mật khẩu bằng tính năng Quên mật khẩu hoặc Đăng nhập qua Google.", ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact(DisplayName = "ChangePasswordAsync - UTCID04 - PasswordHash empty -> InvalidOperationException Google hint (boundary)")]
    [TestType("B")]
    public async Task ChangePasswordAsync_UTCID04_GoogleAccountEmptyHash_ShouldThrow()
    {
        var user = UserBuilder.New().WithId(3).WithPasswordHash(string.Empty).Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(3)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangePasswordAsync(3, new ChangePasswordRequest { OldPassword = OldPassword, NewPassword = NewPassword }));

        Assert.Equal("Tài khoản của bạn được liên kết với Google. Vui lòng thiết lập mật khẩu bằng tính năng Quên mật khẩu hoặc Đăng nhập qua Google.", ex.Message);
    }

    [Fact(DisplayName = "ChangePasswordAsync - UTCID05 - Old password sai -> UnauthorizedAccessException")]
    [TestType("A")]
    public async Task ChangePasswordAsync_UTCID05_WrongOldPassword_ShouldThrow()
    {
        var user = UserBuilder.New().WithId(4).WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(OldPassword)).Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(4)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ChangePasswordAsync(4, new ChangePasswordRequest { OldPassword = "wrong-old-pass", NewPassword = NewPassword }));

        Assert.Equal("Mật khẩu cũ không chính xác.", ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }
}
