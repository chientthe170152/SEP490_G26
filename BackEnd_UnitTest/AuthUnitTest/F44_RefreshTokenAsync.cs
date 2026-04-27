using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.AuthUnitTest;

// F44 - RefreshTokenAsync
// Source: AuthService.cs:355-391
// Branches:
//   1. request == null OR string.IsNullOrEmpty(RefreshToken)  -> throw ArgumentNullException
//   2. CreatePrincipalFromExpiredToken returns null           -> throw UnauthorizedAccessException("Invalid refresh token.")
//   3. user == null OR SecurityStamp mismatch                 -> throw UnauthorizedAccessException("Refresh token is invalid or has been revoked.")
//   4. Success                                                -> generate new access + refresh tokens
public class F44_RefreshTokenAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F44_RefreshTokenAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    /// <summary>
    /// Builds a JWT signed with the same key/issuer/audience the service uses for refresh tokens
    /// (audience = Jwt:Audience + "_Refresh").
    /// </summary>
    private static string BuildRefreshToken(string email, DateTime securityStamp, DateTime? expires = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestConfigBuilder.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Email, email),
            new("AspNet.Identity.SecurityStamp", securityStamp.ToString("o"))
        };
        var token = new JwtSecurityToken(
            issuer: TestConfigBuilder.JwtIssuer,
            audience: TestConfigBuilder.JwtAudience + "_Refresh",
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID01 - Refresh token hợp lệ + SecurityStamp khớp -> trả token mới")]
    [TestType("N")]
    public async Task RefreshTokenAsync_UTCID01_ValidToken_ShouldReturnNewTokens()
    {
        const string email = "user@test.com";
        var stamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var refreshToken = BuildRefreshToken(email, stamp);
        var user = UserBuilder.New().WithEmail(email).WithSecurityStamp(stamp).WithRole(UserRoles.Student, 2).Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync(new TokenModel { RefreshToken = refreshToken });

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        // We don't assert the new refresh token differs in bytes from the input, because if the
        // service runs within the same JWT-second-resolution as our test setup, the iat/exp claims
        // would be identical and the resulting tokens would collide. What matters is that the
        // service issued a valid token (signed with the same key, contains the user's claims).
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == email);
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID02 - request = null -> ArgumentNullException")]
    [TestType("A")]
    public async Task RefreshTokenAsync_UTCID02_NullRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RefreshTokenAsync(null!));
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID03 - RefreshToken empty -> ArgumentNullException (boundary)")]
    [TestType("B")]
    public async Task RefreshTokenAsync_UTCID03_EmptyToken_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.RefreshTokenAsync(new TokenModel { RefreshToken = string.Empty }));
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID04 - Token không decode được -> Unauthorized 'Invalid refresh token.'")]
    [TestType("A")]
    public async Task RefreshTokenAsync_UTCID04_MalformedToken_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.RefreshTokenAsync(new TokenModel { RefreshToken = "not.a.jwt" }));

        Assert.Equal("Invalid refresh token.", ex.Message);
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID05 - Token hết hạn -> Unauthorized 'Invalid refresh token.'")]
    [TestType("A")]
    public async Task RefreshTokenAsync_UTCID05_ExpiredToken_ShouldThrow()
    {
        const string email = "user@test.com";
        var stamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expired = BuildRefreshToken(email, stamp, expires: DateTime.UtcNow.AddDays(-1));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.RefreshTokenAsync(new TokenModel { RefreshToken = expired }));

        Assert.Equal("Invalid refresh token.", ex.Message);
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID06 - User không tồn tại -> Unauthorized 'invalid or revoked'")]
    [TestType("A")]
    public async Task RefreshTokenAsync_UTCID06_UserNotFound_ShouldThrow()
    {
        const string email = "ghost@test.com";
        var stamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = BuildRefreshToken(email, stamp);
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.RefreshTokenAsync(new TokenModel { RefreshToken = token }));

        Assert.Equal("Refresh token is invalid or has been revoked.", ex.Message);
    }

    /// <summary>
    /// Builds a refresh token without Email/SecurityStamp claims (only NameIdentifier) so that
    /// `principal.Claims.FirstOrDefault(...)?.Value ?? ""` falls into the null-side branch.
    /// </summary>
    private static string BuildTokenWithoutClaims()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestConfigBuilder.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestConfigBuilder.JwtIssuer,
            audience: TestConfigBuilder.JwtAudience + "_Refresh",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID08 - Token không có Email/SecurityStamp claim -> ?? '' fallback + user not found")]
    [TestType("B")]
    public async Task RefreshTokenAsync_UTCID08_TokenMissingClaims_ShouldFallbackEmptyAndThrow()
    {
        // Khi token thiếu Email/SecurityStamp claim, `?.Value ?? ""` cho ra "".
        // Sau đó GetUserByEmailAsync("") -> null -> throw "invalid or revoked".
        var token = BuildTokenWithoutClaims();
        _repoMock.Setup(r => r.GetUserByEmailAsync(string.Empty)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.RefreshTokenAsync(new TokenModel { RefreshToken = token }));

        Assert.Equal("Refresh token is invalid or has been revoked.", ex.Message);
    }

    [Fact(DisplayName = "RefreshTokenAsync - UTCID07 - SecurityStamp lệch -> Unauthorized 'invalid or revoked'")]
    [TestType("A")]
    public async Task RefreshTokenAsync_UTCID07_StampMismatch_ShouldThrow()
    {
        const string email = "user@test.com";
        var stampInToken = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var stampInDb = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = BuildRefreshToken(email, stampInToken);
        var user = UserBuilder.New().WithEmail(email).WithSecurityStamp(stampInDb).Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.RefreshTokenAsync(new TokenModel { RefreshToken = token }));

        Assert.Equal("Refresh token is invalid or has been revoked.", ex.Message);
    }
}
