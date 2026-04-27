using System;
using System.Threading.Tasks;
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

// F48 - LogoutAsync
// Source: AuthService.cs:471-480
// Branches:
//   1. user == null              -> no-op (silent)
//   2. user found                -> SecurityStamp = utcNow + UpdateUserAsync (invalidates all refresh tokens)
public class F48_LogoutAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F48_LogoutAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    [Fact(DisplayName = "LogoutAsync - UTCID01 - User tồn tại -> SecurityStamp được cập nhật + save")]
    [TestType("N")]
    public async Task LogoutAsync_UTCID01_UserExists_ShouldUpdateSecurityStamp()
    {
        var oldStamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = UserBuilder.New().WithId(1).WithSecurityStamp(oldStamp).Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);

        await _service.LogoutAsync(1);

        Assert.NotEqual(oldStamp, user.SecurityStamp);
        Assert.True(user.SecurityStamp > oldStamp);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LogoutAsync - UTCID02 - User không tồn tại -> no-op (boundary)")]
    [TestType("B")]
    public async Task LogoutAsync_UTCID02_UserNotFound_ShouldBeNoOp()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

        await _service.LogoutAsync(999);

        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _repoMock.VerifyAll();
    }
}
