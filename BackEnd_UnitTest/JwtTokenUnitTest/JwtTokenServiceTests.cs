using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Backend.Common;
using Backend.Common.Options;
using Backend.Services.Implements;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backend_UnitTest.JwtTokenUnitTest
{
    public class JwtTokenServiceTests
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private static JwtOptions BuildOptions() => new()
        {
            Issuer = "MTCA-Issuer",
            Audience = "MTCA-Audience",
            Key = "this-is-a-very-long-test-secret-key-for-jwt-256-bit",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        };

        private static JwtTokenService BuildService(JwtOptions? opts = null, DateTimeOffset? now = null)
        {
            var options = Options.Create(opts ?? BuildOptions());
            var fakeTime = new FakeTimeProvider(now ?? FixedNow);
            return new JwtTokenService(options, fakeTime);
        }

        [Fact(DisplayName = "Issue - UTCID01 - Tham số hợp lệ -> trả về token, jti, expiresAt đúng")]
        public void Issue_UTCID01_ValidArgs_ShouldReturnToken()
        {
            var service = BuildService();

            var (token, jti, expiresAt) = service.Issue(
                userId: 1,
                email: "u@test.com",
                role: "Student",
                authProvider: "local",
                mustChangePassword: false);

            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.False(string.IsNullOrWhiteSpace(jti));
            Assert.Equal(FixedNow.AddMinutes(30), expiresAt);
        }

        [Fact(DisplayName = "Issue - UTCID02 - Token chứa các claim chuẩn (sub, email, role, jti, iat)")]
        public void Issue_UTCID02_ShouldContainAllExpectedClaims()
        {
            var service = BuildService();

            var (token, jti, _) = service.Issue(7, "abc@test.com", "Teacher", "google", true);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.Equal("7", jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("abc@test.com", jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
            Assert.Equal("Teacher", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
            Assert.Equal(jti, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
            Assert.Equal("google", jwt.Claims.Single(c => c.Type == "auth_provider").Value);
            Assert.Equal(AuthClaims.True, jwt.Claims.Single(c => c.Type == AuthClaims.MustChangePassword).Value);
            Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
        }

        [Fact(DisplayName = "Issue - UTCID03 - mustChangePassword=false -> claim mcp=false")]
        public void Issue_UTCID03_MustChangePasswordFalse_ShouldSetFalseClaim()
        {
            var service = BuildService();

            var (token, _, _) = service.Issue(1, "x@test.com", "Student", "local", false);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal(AuthClaims.False, jwt.Claims.Single(c => c.Type == AuthClaims.MustChangePassword).Value);
        }

        [Fact(DisplayName = "Issue - UTCID04 - Token issuer/audience đúng theo options")]
        public void Issue_UTCID04_ShouldUseConfiguredIssuerAudience()
        {
            var opts = BuildOptions();
            var service = BuildService(opts);

            var (token, _, _) = service.Issue(1, "u@test.com", "Student", "local", false);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal(opts.Issuer, jwt.Issuer);
            Assert.Equal(opts.Audience, jwt.Audiences.Single());
        }

        [Fact(DisplayName = "Issue - UTCID05 - Token có thể validate bằng cùng key")]
        public void Issue_UTCID05_TokenShouldValidate()
        {
            var opts = BuildOptions();
            var service = BuildService(opts);

            var (token, _, _) = service.Issue(1, "u@test.com", "Student", "local", false);

            var validator = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = opts.Issuer,
                ValidateAudience = true,
                ValidAudience = opts.Audience,
                ValidateLifetime = true,
                LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
                    expires > FixedNow.UtcDateTime,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Key)),
                ValidateIssuerSigningKey = true
            };
            var principal = validator.ValidateToken(token, parameters, out _);
            Assert.NotNull(principal.Identity);
            Assert.True(principal.Identity!.IsAuthenticated);
        }

        [Fact(DisplayName = "Issue - UTCID06 - Hai lần issue -> jti khác nhau")]
        public void Issue_UTCID06_DistinctJti()
        {
            var service = BuildService();

            var (_, jti1, _) = service.Issue(1, "u@test.com", "Student", "local", false);
            var (_, jti2, _) = service.Issue(1, "u@test.com", "Student", "local", false);

            Assert.NotEqual(jti1, jti2);
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;
            public FakeTimeProvider(DateTimeOffset now) => _now = now;
            public override DateTimeOffset GetUtcNow() => _now;
        }
    }
}
