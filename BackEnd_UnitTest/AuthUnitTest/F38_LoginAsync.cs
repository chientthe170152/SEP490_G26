using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs;
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

// F38 - LoginAsync
// Branches in source (AuthService.cs:31-58):
//   1. user == null                                  -> UnauthorizedAccessException(ErrorMessages.InvalidEmailOrPassword)
//   2. string.IsNullOrEmpty(user.PasswordHash)       -> UnauthorizedAccessException("Tài khoản này đăng nhập bằng Google...")
//   3. !BCrypt.Verify(request.Password, hash)        -> UnauthorizedAccessException(ErrorMessages.InvalidEmailOrPassword)
//   4. Success path with user.Role != null           -> RoleName = user.Role.Name
//   5. Success path with user.Role == null           -> RoleName = "User" (?? fallback)
public class F38_LoginAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    private const string ValidPassword = "Pa$$word123";

    public F38_LoginAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    [Fact(DisplayName = "LoginAsync - UTCID01 - Email + password đúng + Role gán -> trả token")]
    [TestType("N")]
    public async Task LoginAsync_UTCID01_ValidCredentialsWithRole_ShouldReturnToken()
    {
        var user = UserBuilder.New()
            .WithId(1)
            .WithEmail("teacher@test.com")
            .WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(ValidPassword))
            .WithRole(UserRoles.Teacher, roleId: 1)
            .Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync("teacher@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "teacher@test.com",
            Password = ValidPassword
        });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal(UserRoles.Teacher, result.RoleName);
        Assert.Equal("teacher@test.com", result.Email);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(TestConfigBuilder.JwtIssuer, jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == "teacher@test.com");
        Assert.Contains(jwt.Claims, c => c.Type == "auth_provider" && c.Value == "password");

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LoginAsync - UTCID02 - Email + password đúng nhưng Role = null -> RoleName = 'User'")]
    [TestType("N")]
    public async Task LoginAsync_UTCID02_ValidCredentialsWithoutRole_ShouldFallbackUser()
    {
        var user = UserBuilder.New()
            .WithEmail("noRole@test.com")
            .WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(ValidPassword))
            .Build();
        // Role explicitly null (UserBuilder.WithRole not called)
        user.Role = null!;
        _repoMock.Setup(r => r.GetUserByEmailAsync("noRole@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "noRole@test.com",
            Password = ValidPassword
        });

        Assert.Equal("User", result.RoleName);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LoginAsync - UTCID03 - Email không tồn tại -> Unauthorized InvalidEmailOrPassword")]
    [TestType("A")]
    public async Task LoginAsync_UTCID03_EmailNotFound_ShouldThrowUnauthorized()
    {
        _repoMock.Setup(r => r.GetUserByEmailAsync("ghost@test.com")).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.LoginAsync(new LoginRequest { Email = "ghost@test.com", Password = ValidPassword }));

        Assert.Equal(ErrorMessages.InvalidEmailOrPassword, ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LoginAsync - UTCID04 - PasswordHash null (Google account) -> Unauthorized với hint Google")]
    [TestType("A")]
    public async Task LoginAsync_UTCID04_GoogleAccountNullHash_ShouldThrowGoogleHint()
    {
        var user = UserBuilder.New()
            .WithEmail("google@test.com")
            .WithPasswordHash(null)
            .WithRole(UserRoles.Student, roleId: 2)
            .Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync("google@test.com")).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.LoginAsync(new LoginRequest { Email = "google@test.com", Password = ValidPassword }));

        Assert.Equal("Tài khoản này đăng nhập bằng Google. Vui lòng sử dụng Đăng nhập bằng Google.", ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LoginAsync - UTCID05 - PasswordHash empty -> Unauthorized với hint Google (boundary)")]
    [TestType("B")]
    public async Task LoginAsync_UTCID05_GoogleAccountEmptyHash_ShouldThrowGoogleHint()
    {
        var user = UserBuilder.New()
            .WithEmail("google2@test.com")
            .WithPasswordHash(string.Empty)
            .Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync("google2@test.com")).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.LoginAsync(new LoginRequest { Email = "google2@test.com", Password = ValidPassword }));

        Assert.Equal("Tài khoản này đăng nhập bằng Google. Vui lòng sử dụng Đăng nhập bằng Google.", ex.Message);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LoginAsync - UTCID06 - Password sai -> Unauthorized InvalidEmailOrPassword")]
    [TestType("A")]
    public async Task LoginAsync_UTCID06_WrongPassword_ShouldThrowUnauthorized()
    {
        var user = UserBuilder.New()
            .WithEmail("teacher@test.com")
            .WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(ValidPassword))
            .WithRole(UserRoles.Teacher, roleId: 1)
            .Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync("teacher@test.com")).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.LoginAsync(new LoginRequest { Email = "teacher@test.com", Password = "wrong-password" }));

        Assert.Equal(ErrorMessages.InvalidEmailOrPassword, ex.Message);
        _repoMock.VerifyAll();
    }
}
