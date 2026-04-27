using System;
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

// F41 - SendOtpAsync
// Source: AuthService.cs:223-276
// Branches:
//   1. existingUserByEmail != null && Email matches -> throw EmailAlreadyRegistered
//   2. fullName empty/whitespace                    -> throw FullNameRequired
//   3. fullName regex fail                          -> throw FullNameInvalid
//   4. phone provided + regex fail                  -> throw PhoneNumberInvalid
//   5. RoleId == 2 + studentId empty                -> throw StudentIdRequiredForStudent
//   6. RoleId == 2 + studentId regex fail           -> throw StudentIdInvalid
//   7. Success                                      -> generate OTP, cache OTP_{email}, send email
public class F41_SendOtpAsync_Tests
{
    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public F41_SendOtpAsync_Tests()
    {
        _repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new AuthService(_repoMock.Object, _config, _emailMock.Object, _cache);
    }

    private RegisterRequest BaseRequest(string email = "new@test.com", int roleId = 1) => new()
    {
        Email = email,
        FullName = "Nguyen Van A",
        Password = "Pa$$word1",
        PhoneNumber = "0123456789",
        StudentId = roleId == 2 ? "HE172047" : null,
        RoleId = roleId
    };

    [Fact(DisplayName = "SendOtpAsync - UTCID01 - Non-student valid -> cache OTP và gửi email")]
    [TestType("N")]
    public async Task SendOtpAsync_UTCID01_NonStudentValid_ShouldCacheAndSend()
    {
        var req = BaseRequest();
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);
        _emailMock.Setup(e => e.SendEmailAsync(req.Email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.SendOtpAsync(req);

        Assert.True(_cache.TryGetValue($"OTP_{req.Email}", out _));
        _emailMock.Verify(e => e.SendEmailAsync(req.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID02 - Student valid + studentId hợp lệ -> success")]
    [TestType("N")]
    public async Task SendOtpAsync_UTCID02_StudentValid_ShouldCacheAndSend()
    {
        var req = BaseRequest("student@test.com", roleId: 2);
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);
        _emailMock.Setup(e => e.SendEmailAsync(req.Email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.SendOtpAsync(req);

        Assert.True(_cache.TryGetValue($"OTP_{req.Email}", out _));
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID03 - PhoneNumber null + phone valid không apply -> success (boundary)")]
    [TestType("B")]
    public async Task SendOtpAsync_UTCID03_NullPhone_ShouldSucceed()
    {
        var req = BaseRequest();
        req.PhoneNumber = null;
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);
        _emailMock.Setup(e => e.SendEmailAsync(req.Email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.SendOtpAsync(req);

        Assert.True(_cache.TryGetValue($"OTP_{req.Email}", out _));
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID04 - Email đã tồn tại -> InvalidOperationException EmailAlreadyRegistered")]
    [TestType("A")]
    public async Task SendOtpAsync_UTCID04_EmailExists_ShouldThrow()
    {
        var req = BaseRequest("dup@test.com");
        var existing = UserBuilder.New().WithEmail(req.Email).Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));

        Assert.Equal(ErrorMessages.EmailAlreadyRegistered, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID05 - FullName whitespace -> FullNameRequired")]
    [TestType("A")]
    public async Task SendOtpAsync_UTCID05_FullNameWhitespace_ShouldThrow()
    {
        var req = BaseRequest();
        req.FullName = "   ";
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));
        Assert.Equal(ValidationMessages.FullNameRequired, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID06 - FullName chứa số -> FullNameInvalid")]
    [TestType("A")]
    public async Task SendOtpAsync_UTCID06_FullNameInvalid_ShouldThrow()
    {
        var req = BaseRequest();
        req.FullName = "Nguyen Van 1";
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));
        Assert.Equal(ValidationMessages.FullNameInvalid, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID07 - Phone không bắt đầu bằng 0 -> PhoneNumberInvalid")]
    [TestType("A")]
    public async Task SendOtpAsync_UTCID07_PhoneInvalid_ShouldThrow()
    {
        var req = BaseRequest();
        req.PhoneNumber = "1234567890";
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));
        Assert.Equal(ValidationMessages.PhoneNumberInvalid, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID08 - Student + studentId null -> StudentIdRequiredForStudent")]
    [TestType("A")]
    public async Task SendOtpAsync_UTCID08_StudentMissingId_ShouldThrow()
    {
        var req = BaseRequest("student2@test.com", roleId: 2);
        req.StudentId = null;
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));
        Assert.Equal(ValidationMessages.StudentIdRequiredForStudent, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID09 - Student + studentId sai định dạng -> StudentIdInvalid")]
    [TestType("A")]
    public async Task SendOtpAsync_UTCID09_StudentInvalidId_ShouldThrow()
    {
        var req = BaseRequest("student3@test.com", roleId: 2);
        req.StudentId = "X1";
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));
        Assert.Equal(ValidationMessages.StudentIdInvalid, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID11 - FullName = null -> throw FullNameRequired (boundary cho ??)")]
    [TestType("B")]
    public async Task SendOtpAsync_UTCID11_FullNameNull_ShouldThrowFullNameRequired()
    {
        var req = BaseRequest();
        req.FullName = null!;
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendOtpAsync(req));
        Assert.Equal(ValidationMessages.FullNameRequired, ex.Message);
    }

    [Fact(DisplayName = "SendOtpAsync - UTCID10 - Repo trả user nhưng Email khác -> qua kiểm tra (boundary cho &&)")]
    [TestType("B")]
    public async Task SendOtpAsync_UTCID10_RepoReturnsDifferentEmail_ShouldProceed()
    {
        // Defensive branch: existingUserByEmail != null nhưng .Email != request.Email -> không throw, tiếp tục.
        // Cần case này để cover nhánh && phía sau (path "true && false").
        var req = BaseRequest("target@test.com");
        var someoneElse = UserBuilder.New().WithEmail("different@test.com").Build();
        _repoMock.Setup(r => r.GetUserByEmailAsync(req.Email)).ReturnsAsync(someoneElse);
        _emailMock.Setup(e => e.SendEmailAsync(req.Email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _service.SendOtpAsync(req);

        Assert.True(_cache.TryGetValue($"OTP_{req.Email}", out _));
    }
}
