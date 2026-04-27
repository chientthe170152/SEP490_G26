using System;
using System.Threading.Tasks;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F5 - JoinCourseAsync
// Source: CourseService.cs:71-91
// Branches:
//   1. inviteCode null/whitespace -> throw "Mã mời không thể trống."
//   2. course == null             -> throw "Mã mời không chính xác hoặc lớp học đã bị đóng."
//   3. alreadyJoined              -> throw "Bạn đã ở trong lớp học này rồi."
//   4. Success                    -> JoinClassAsync called
public class F5_JoinCourseAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F5_JoinCourseAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "JoinCourseAsync - UTCID01 - Mã mời hợp lệ + chưa ở lớp -> join thành công")]
    [TestType("N")]
    public async Task JoinCourseAsync_UTCID01_ValidInvite_ShouldJoin()
    {
        var clazz = new Class { ClassId = 10, Name = "Toán 12", TeacherId = 1, SubjectId = 1, Semester = "FA2026", InvitationCode = "ABC123", ConcurrencyStamp = Array.Empty<byte>() };
        _repoMock.Setup(r => r.GetClassByInviteCodeAsync("ABC123")).ReturnsAsync(clazz);
        _repoMock.Setup(r => r.IsUserInClassAsync(10, 5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.JoinClassAsync(10, 5)).Returns(Task.CompletedTask);

        await _service.JoinCourseAsync(5, "ABC123");

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "JoinCourseAsync - UTCID02 - inviteCode null -> throw")]
    [TestType("A")]
    public async Task JoinCourseAsync_UTCID02_NullInviteCode_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.JoinCourseAsync(5, null!));
        Assert.Equal("Mã mời không thể trống.", ex.Message);
    }

    [Fact(DisplayName = "JoinCourseAsync - UTCID03 - inviteCode whitespace -> throw (boundary)")]
    [TestType("B")]
    public async Task JoinCourseAsync_UTCID03_WhitespaceInviteCode_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.JoinCourseAsync(5, "   "));
        Assert.Equal("Mã mời không thể trống.", ex.Message);
    }

    [Fact(DisplayName = "JoinCourseAsync - UTCID04 - Mã mời không tồn tại -> throw")]
    [TestType("A")]
    public async Task JoinCourseAsync_UTCID04_InviteCodeNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetClassByInviteCodeAsync("XXX")).ReturnsAsync((Class?)null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.JoinCourseAsync(5, "XXX"));
        Assert.Equal("Mã mời không chính xác hoặc lớp học đã bị đóng.", ex.Message);
    }

    [Fact(DisplayName = "JoinCourseAsync - UTCID05 - Đã ở trong lớp -> throw")]
    [TestType("A")]
    public async Task JoinCourseAsync_UTCID05_AlreadyJoined_ShouldThrow()
    {
        var clazz = new Class { ClassId = 10, Name = "X", TeacherId = 1, SubjectId = 1, Semester = "FA2026", InvitationCode = "ABC", ConcurrencyStamp = Array.Empty<byte>() };
        _repoMock.Setup(r => r.GetClassByInviteCodeAsync("ABC")).ReturnsAsync(clazz);
        _repoMock.Setup(r => r.IsUserInClassAsync(10, 5)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.JoinCourseAsync(5, "ABC"));
        Assert.Equal("Bạn đã ở trong lớp học này rồi.", ex.Message);
        _repoMock.Verify(r => r.JoinClassAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
