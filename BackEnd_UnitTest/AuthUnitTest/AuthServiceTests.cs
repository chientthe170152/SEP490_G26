using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Common.Options;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Backend_UnitTest.AuthUnitTest
{
    public class AuthServiceTests
    {
        private readonly Mock<IAuthRepository> _authRepo = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly Mock<IOtpStore> _otp = new();
        private readonly Mock<IJwtTokenService> _jwt = new();
        private readonly Mock<IRefreshTokenStore> _refresh = new();
        private readonly IConfiguration _config;
        private readonly DefaultHttpContext _httpContext = new();
        private readonly Mock<IHttpContextAccessor> _accessor = new();
        private readonly IOptions<AuthCookieOptions> _cookieOpts =
            Options.Create(new AuthCookieOptions { Secure = false, SameSite = "Lax" });

        public AuthServiceTests()
        {
            _accessor.Setup(a => a.HttpContext).Returns(_httpContext);
            _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "YOUR_GOOGLE_CLIENT_ID_HERE"
            }).Build();
            _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        }

        private AuthService BuildService() =>
            new AuthService(_authRepo.Object, _config, _email.Object, _otp.Object,
                _jwt.Object, _refresh.Object, _accessor.Object, _cookieOpts);

        private void SetupJwtAndRefreshOk(int userId = 1, string jti = "jti-1")
        {
            _jwt.Setup(j => j.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(("access-token", jti, DateTimeOffset.UtcNow.AddMinutes(15)));
            _refresh.Setup(r => r.IssueAsync(userId, jti, It.IsAny<string?>(), It.IsAny<string?>(), default))
                .ReturnsAsync(("rt-raw", DateTimeOffset.UtcNow.AddDays(7)));
        }

        // ---------- LoginAsync ----------

        [Fact(DisplayName = "LoginAsync - UTCID01 - User không tồn tại -> InvalidCredentials")]
        public async Task Login_UTCID01_UserNotFound()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal(AuthErrors.InvalidCredentials.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LoginAsync - UTCID02 - PasswordHash rỗng -> InvalidCredentials")]
        public async Task Login_UTCID02_GoogleAccountNoPassword()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = "", RoleId = 2 });
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal(AuthErrors.InvalidCredentials.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LoginAsync - UTCID03 - Mật khẩu sai -> InvalidCredentials")]
        public async Task Login_UTCID03_WrongPassword()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("real"), RoleId = 2 });
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "wrong" });

            Assert.Equal(AuthErrors.InvalidCredentials.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LoginAsync - UTCID04 - User Locked -> AccountLocked")]
        public async Task Login_UTCID04_AccountLocked()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Locked });
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal(AuthErrors.AccountLocked.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LoginAsync - UTCID05 - Hợp lệ -> issue token + cookie")]
        public async Task Login_UTCID05_Success()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Active, MustChangePassword = false });
            SetupJwtAndRefreshOk();
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.True(result.IsSuccess);
            Assert.Equal("Student", result.Value.RoleName);
            Assert.Equal("x@x", result.Value.Email);
        }

        [Fact(DisplayName = "LoginAsync - UTCID06 - Role không hợp lệ -> UnknownRole")]
        public async Task Login_UTCID06_UnknownRole()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 99, Status = UserStatus.Active });
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal(AuthErrors.UnknownRole.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LoginAsync - UTCID07 - Có CF-Connecting-IP -> dùng IP đó")]
        public async Task Login_UTCID07_UsesCfConnectingIp()
        {
            _httpContext.Request.Headers["CF-Connecting-IP"] = "1.2.3.4";
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Active });
            string? capturedIp = null;
            _jwt.Setup(j => j.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(("at", "jti", DateTimeOffset.UtcNow.AddMinutes(15)));
            _refresh.Setup(r => r.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), default))
                .Callback<int, string, string?, string?, System.Threading.CancellationToken>((_, _, ip, _, _) => capturedIp = ip)
                .ReturnsAsync(("rt", DateTimeOffset.UtcNow.AddDays(7)));
            var svc = BuildService();

            await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal("1.2.3.4", capturedIp);
        }

        [Fact(DisplayName = "LoginAsync - UTCID08 - Có X-Forwarded-For (multi) -> dùng IP đầu")]
        public async Task Login_UTCID08_UsesFirstForwardedFor()
        {
            _httpContext.Request.Headers["X-Forwarded-For"] = "5.6.7.8, 9.9.9.9";
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Active });
            string? capturedIp = null;
            _jwt.Setup(j => j.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(("at", "jti", DateTimeOffset.UtcNow.AddMinutes(15)));
            _refresh.Setup(r => r.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), default))
                .Callback<int, string, string?, string?, System.Threading.CancellationToken>((_, _, ip, _, _) => capturedIp = ip)
                .ReturnsAsync(("rt", DateTimeOffset.UtcNow.AddDays(7)));
            var svc = BuildService();

            await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal("5.6.7.8", capturedIp);
        }

        [Fact(DisplayName = "LoginAsync - UTCID09 - User có Role nav -> RoleName từ entity")]
        public async Task Login_UTCID09_RoleNavName()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User
                {
                    UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"),
                    RoleId = 2, Role = new Role { Name = "Học sinh", RoleId = 2 }, Status = UserStatus.Active
                });
            SetupJwtAndRefreshOk();
            var svc = BuildService();

            var result = await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal("Học sinh", result.Value.RoleName);
        }

        [Fact(DisplayName = "LoginAsync - UTCID10 - Không có HttpContext -> throw InvalidOperationException")]
        public async Task Login_UTCID10_NoHttpContext_Throws()
        {
            _accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Active });
            var svc = BuildService();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" }));
        }

        [Fact(DisplayName = "LoginAsync - UTCID11 - X-Forwarded-For chỉ chứa khoảng trắng -> fallback RemoteIpAddress")]
        public async Task Login_UTCID11_BlankForwardedFor_FallsBackToRemote()
        {
            _httpContext.Request.Headers["X-Forwarded-For"] = "  ,  ";
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Active });
            string? capturedIp = null;
            _jwt.Setup(j => j.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(("at", "jti", DateTimeOffset.UtcNow.AddMinutes(15)));
            _refresh.Setup(r => r.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), default))
                .Callback<int, string, string?, string?, System.Threading.CancellationToken>((_, _, ip, _, _) => capturedIp = ip)
                .ReturnsAsync(("rt", DateTimeOffset.UtcNow.AddDays(7)));
            var svc = BuildService();

            await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Equal("10.0.0.1", capturedIp);
        }

        [Fact(DisplayName = "LoginAsync - UTCID12 - Không có IP nào -> capturedIp null")]
        public async Task Login_UTCID12_NoIp_NullIp()
        {
            _httpContext.Connection.RemoteIpAddress = null;
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 2, Status = UserStatus.Active });
            string? capturedIp = "init";
            _jwt.Setup(j => j.Issue(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(("at", "jti", DateTimeOffset.UtcNow.AddMinutes(15)));
            _refresh.Setup(r => r.IssueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), default))
                .Callback<int, string, string?, string?, System.Threading.CancellationToken>((_, _, ip, _, _) => capturedIp = ip)
                .ReturnsAsync(("rt", DateTimeOffset.UtcNow.AddDays(7)));
            var svc = BuildService();

            await svc.LoginAsync(new LoginRequest { Email = "x@x", Password = "p" });

            Assert.Null(capturedIp);
        }

        // ---------- GoogleLoginAsync ----------

        [Fact(DisplayName = "GoogleLoginAsync - UTCID01 - Token không hợp lệ -> InvalidGoogleToken")]
        public async Task GoogleLogin_UTCID01_InvalidToken()
        {
            var svc = BuildService();

            var result = await svc.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "not-a-real-id-token" });

            Assert.True(result.IsFailure);
            Assert.Equal(AuthErrors.InvalidGoogleToken.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GoogleLoginAsync - UTCID02 - Token không phải JWT -> InvalidGoogleToken")]
        public async Task GoogleLogin_UTCID02_GarbageToken()
        {
            var svc = BuildService();

            var result = await svc.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "garbage" });

            Assert.True(result.IsFailure);
            Assert.Equal(AuthErrors.InvalidGoogleToken.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GoogleLoginAsync - UTCID03 - ClientId thật trong config -> token vẫn invalid (path khác)")]
        public async Task GoogleLogin_UTCID03_RealClientId_InvalidToken()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "1234.apps.googleusercontent.com"
            }).Build();
            var svc = new AuthService(_authRepo.Object, config, _email.Object, _otp.Object,
                _jwt.Object, _refresh.Object, _accessor.Object, _cookieOpts);

            var result = await svc.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "anything" });

            Assert.Equal(AuthErrors.InvalidGoogleToken.Code, result.Error.Code);
        }

        // ---------- RefreshTokenAsync ----------

        [Fact(DisplayName = "RefreshTokenAsync - UTCID01 - Không có HttpContext -> throw")]
        public async Task Refresh_UTCID01_NoHttpContext_ShouldThrow()
        {
            _accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            var svc = BuildService();

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RefreshTokenAsync());
        }

        [Fact(DisplayName = "RefreshTokenAsync - UTCID02 - Cookie không có -> RefreshTokenNotFound")]
        public async Task Refresh_UTCID02_NoCookie()
        {
            var svc = BuildService();

            var result = await svc.RefreshTokenAsync();

            Assert.Equal(AuthErrors.RefreshTokenNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "RefreshTokenAsync - UTCID03 - Validation null -> InvalidRefreshToken")]
        public async Task Refresh_UTCID03_InvalidValidation()
        {
            _httpContext.Request.Headers["Cookie"] = $"{AuthCookieDefaults.RefreshName}=raw-token";
            _refresh.Setup(r => r.ConsumeAsync("raw-token", default)).ReturnsAsync((RefreshTokenValidation?)null);
            var svc = BuildService();

            var result = await svc.RefreshTokenAsync();

            Assert.Equal(AuthErrors.InvalidRefreshToken.Code, result.Error.Code);
        }

        [Fact(DisplayName = "RefreshTokenAsync - UTCID04 - User không tồn tại -> UserNotFound")]
        public async Task Refresh_UTCID04_UserMissing()
        {
            _httpContext.Request.Headers["Cookie"] = $"{AuthCookieDefaults.RefreshName}=raw-token";
            _refresh.Setup(r => r.ConsumeAsync("raw-token", default))
                .ReturnsAsync(new RefreshTokenValidation(7, "jti"));
            _authRepo.Setup(r => r.GetUserByIdAsync(7)).ReturnsAsync((User?)null);
            var svc = BuildService();

            var result = await svc.RefreshTokenAsync();

            Assert.Equal(AuthErrors.UserNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "RefreshTokenAsync - UTCID05 - Hợp lệ -> Success")]
        public async Task Refresh_UTCID05_Success()
        {
            _httpContext.Request.Headers["Cookie"] = $"{AuthCookieDefaults.RefreshName}=raw-token";
            _refresh.Setup(r => r.ConsumeAsync("raw-token", default))
                .ReturnsAsync(new RefreshTokenValidation(7, "jti-old"));
            _authRepo.Setup(r => r.GetUserByIdAsync(7))
                .ReturnsAsync(new User { UserId = 7, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 1, Status = UserStatus.Active });
            SetupJwtAndRefreshOk(7, "jti-new");
            var svc = BuildService();

            var result = await svc.RefreshTokenAsync();

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "RefreshTokenAsync - UTCID06 - Role không hợp lệ -> UnknownRole")]
        public async Task Refresh_UTCID06_UnknownRole()
        {
            _httpContext.Request.Headers["Cookie"] = $"{AuthCookieDefaults.RefreshName}=raw-token";
            _refresh.Setup(r => r.ConsumeAsync("raw-token", default))
                .ReturnsAsync(new RefreshTokenValidation(7, "jti-old"));
            _authRepo.Setup(r => r.GetUserByIdAsync(7))
                .ReturnsAsync(new User { UserId = 7, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("p"), RoleId = 99, Status = UserStatus.Active });
            var svc = BuildService();

            var result = await svc.RefreshTokenAsync();

            Assert.Equal(AuthErrors.UnknownRole.Code, result.Error.Code);
        }

        // ---------- ForgotPasswordAsync ----------

        [Fact(DisplayName = "ForgotPasswordAsync - UTCID01 - User không tồn tại -> Success silent (không gửi email)")]
        public async Task Forgot_UTCID01_UserNotFound_SilentSuccess()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("none@x")).ReturnsAsync((User?)null);
            var svc = BuildService();

            var result = await svc.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "none@x" });

            Assert.True(result.IsSuccess);
            _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _otp.Verify(o => o.SetResetOtpAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Fact(DisplayName = "ForgotPasswordAsync - UTCID02 - User tồn tại -> set OTP + send email")]
        public async Task Forgot_UTCID02_UserExists_SendEmail()
        {
            _authRepo.Setup(r => r.GetUserByEmailAsync("u@x"))
                .ReturnsAsync(new User { UserId = 1, Email = "u@x", PasswordHash = "h", RoleId = 2 });
            _otp.Setup(o => o.SetResetOtpAsync("u@x", It.IsAny<string>(), default)).Returns(Task.CompletedTask);
            _email.Setup(e => e.SendEmailAsync("u@x", It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var svc = BuildService();

            var result = await svc.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "u@x" });

            Assert.True(result.IsSuccess);
            _otp.Verify(o => o.SetResetOtpAsync("u@x", It.IsAny<string>(), default), Times.Once);
            _email.Verify(e => e.SendEmailAsync("u@x", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ---------- ResetPasswordAsync ----------

        [Fact(DisplayName = "ResetPasswordAsync - UTCID01 - Không có OTP cache -> OtpExpired")]
        public async Task Reset_UTCID01_OtpExpired()
        {
            _otp.Setup(o => o.GetResetOtpAsync("x@x", default)).ReturnsAsync((string?)null);
            var svc = BuildService();

            var result = await svc.ResetPasswordAsync(new ResetPasswordRequest { Email = "x@x", OtpCode = "111111", NewPassword = "n" });

            Assert.Equal(AuthErrors.OtpExpired.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResetPasswordAsync - UTCID02 - OTP sai -> OtpInvalid")]
        public async Task Reset_UTCID02_OtpInvalid()
        {
            _otp.Setup(o => o.GetResetOtpAsync("x@x", default)).ReturnsAsync("123456");
            var svc = BuildService();

            var result = await svc.ResetPasswordAsync(new ResetPasswordRequest { Email = "x@x", OtpCode = "000000", NewPassword = "n" });

            Assert.Equal(AuthErrors.OtpInvalid.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResetPasswordAsync - UTCID03 - User không tồn tại -> UserNotFound")]
        public async Task Reset_UTCID03_UserNotFound()
        {
            _otp.Setup(o => o.GetResetOtpAsync("x@x", default)).ReturnsAsync("123456");
            _otp.Setup(o => o.RemoveResetOtpAsync("x@x", default)).Returns(Task.CompletedTask);
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x")).ReturnsAsync((User?)null);
            var svc = BuildService();

            var result = await svc.ResetPasswordAsync(new ResetPasswordRequest { Email = "x@x", OtpCode = "123456", NewPassword = "n" });

            Assert.Equal(AuthErrors.UserNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResetPasswordAsync - UTCID04 - Hợp lệ -> update + revoke + clear cookie")]
        public async Task Reset_UTCID04_Success()
        {
            var user = new User { UserId = 1, Email = "x@x", PasswordHash = "old", RoleId = 2 };
            _otp.Setup(o => o.GetResetOtpAsync("x@x", default)).ReturnsAsync("123456");
            _otp.Setup(o => o.RemoveResetOtpAsync("x@x", default)).Returns(Task.CompletedTask);
            _authRepo.Setup(r => r.GetUserByEmailAsync("x@x")).ReturnsAsync(user);
            _authRepo.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);
            var svc = BuildService();

            var result = await svc.ResetPasswordAsync(new ResetPasswordRequest { Email = "x@x", OtpCode = "123456", NewPassword = "newpass" });

            Assert.True(result.IsSuccess);
            Assert.NotEqual("old", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", user.PasswordHash));
            _refresh.Verify(r => r.RevokeAllAsync(1, default), Times.Once);
        }

        // ---------- LogoutAsync ----------

        [Fact(DisplayName = "LogoutAsync - UTCID01 - Revoke + clear cookies")]
        public async Task Logout_UTCID01_Success()
        {
            var svc = BuildService();

            var result = await svc.LogoutAsync(7, "jti-1");

            Assert.True(result.IsSuccess);
            _refresh.Verify(r => r.RevokeAsync(7, "jti-1", default), Times.Once);
        }

        [Fact(DisplayName = "LogoutAsync - UTCID02 - Không có HttpContext -> không throw, vẫn Success")]
        public async Task Logout_UTCID02_NoHttpContext()
        {
            _accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            var svc = BuildService();

            var result = await svc.LogoutAsync(7, "jti-1");

            Assert.True(result.IsSuccess);
        }

        // ---------- ChangePasswordFirstLoginAsync ----------

        [Fact(DisplayName = "ChangePasswordFirstLoginAsync - UTCID01 - User không tồn tại -> UserNotFound")]
        public async Task ChangeFirst_UTCID01_NotFound()
        {
            _authRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);
            var svc = BuildService();

            var result = await svc.ChangePasswordFirstLoginAsync(1, new ChangePasswordFirstLoginRequest { CurrentPassword = "c", NewPassword = "n" });

            Assert.Equal(AuthErrors.UserNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ChangePasswordFirstLoginAsync - UTCID02 - PasswordHash rỗng -> UserNotFound")]
        public async Task ChangeFirst_UTCID02_EmptyHash()
        {
            _authRepo.Setup(r => r.GetUserByIdAsync(1))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = "", RoleId = 2 });
            var svc = BuildService();

            var result = await svc.ChangePasswordFirstLoginAsync(1, new ChangePasswordFirstLoginRequest { CurrentPassword = "c", NewPassword = "n" });

            Assert.Equal(AuthErrors.UserNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ChangePasswordFirstLoginAsync - UTCID03 - Mật khẩu hiện tại sai -> CurrentPasswordWrong")]
        public async Task ChangeFirst_UTCID03_WrongCurrent()
        {
            _authRepo.Setup(r => r.GetUserByIdAsync(1))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("real"), RoleId = 2 });
            var svc = BuildService();

            var result = await svc.ChangePasswordFirstLoginAsync(1, new ChangePasswordFirstLoginRequest { CurrentPassword = "wrong", NewPassword = "newpass" });

            Assert.Equal(AuthErrors.CurrentPasswordWrong.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ChangePasswordFirstLoginAsync - UTCID04 - Mật khẩu mới trùng cũ -> NewPasswordSameAsOld")]
        public async Task ChangeFirst_UTCID04_SameAsOld()
        {
            _authRepo.Setup(r => r.GetUserByIdAsync(1))
                .ReturnsAsync(new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("same"), RoleId = 2 });
            var svc = BuildService();

            var result = await svc.ChangePasswordFirstLoginAsync(1, new ChangePasswordFirstLoginRequest { CurrentPassword = "same", NewPassword = "same" });

            Assert.Equal(AuthErrors.NewPasswordSameAsOld.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ChangePasswordFirstLoginAsync - UTCID05 - Hợp lệ -> đổi mật khẩu, build login response")]
        public async Task ChangeFirst_UTCID05_Success()
        {
            var user = new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("old"), RoleId = 2, MustChangePassword = true };
            _authRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _authRepo.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);
            SetupJwtAndRefreshOk();
            var svc = BuildService();

            var result = await svc.ChangePasswordFirstLoginAsync(1, new ChangePasswordFirstLoginRequest { CurrentPassword = "old", NewPassword = "newpass" });

            Assert.True(result.IsSuccess);
            Assert.False(user.MustChangePassword);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", user.PasswordHash));
            _refresh.Verify(r => r.RevokeAllAsync(1, default), Times.Once);
        }

        [Fact(DisplayName = "ChangePasswordFirstLoginAsync - UTCID06 - Role không hợp lệ -> UnknownRole")]
        public async Task ChangeFirst_UTCID06_UnknownRole()
        {
            var user = new User { UserId = 1, Email = "x@x", PasswordHash = BCrypt.Net.BCrypt.HashPassword("old"), RoleId = 99, MustChangePassword = true };
            _authRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _authRepo.Setup(r => r.UpdateUserAsync(user)).ReturnsAsync(user);
            var svc = BuildService();

            var result = await svc.ChangePasswordFirstLoginAsync(1, new ChangePasswordFirstLoginRequest { CurrentPassword = "old", NewPassword = "newpass" });

            Assert.Equal(AuthErrors.UnknownRole.Code, result.Error.Code);
        }
    }
}
