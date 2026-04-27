using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F12 - ApproveStudentAsync
// Source: CourseService.cs:225-233
// Branches:
//   1. course != null && Status == Closed       -> throw "Lớp học đã bị đóng, không thể phê duyệt học sinh."
//   2. ApproveStudentAsync repo returns false   -> throw "Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt."
//   3. Success                                  -> no exception
//   4. course == null                           -> proceed
public class F12_ApproveStudentAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F12_ApproveStudentAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "ApproveStudentAsync - UTCID01 - Lớp Active + repo trả true -> success")]
    [TestType("N")]
    public async Task ApproveStudentAsync_UTCID01_Valid_ShouldSucceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.ApproveStudentAsync(10, 5)).ReturnsAsync(true);

        await _service.ApproveStudentAsync(10, 5);

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ApproveStudentAsync - UTCID02 - Lớp Closed -> throw")]
    [TestType("A")]
    public async Task ApproveStudentAsync_UTCID02_ClassClosed_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Closed });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.ApproveStudentAsync(10, 5));
        Assert.Equal("Lớp học đã bị đóng, không thể phê duyệt học sinh.", ex.Message);
    }

    [Fact(DisplayName = "ApproveStudentAsync - UTCID03 - Repo trả false -> throw")]
    [TestType("A")]
    public async Task ApproveStudentAsync_UTCID03_RepoReturnsFalse_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.ApproveStudentAsync(10, 5)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.ApproveStudentAsync(10, 5));
        Assert.Equal("Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt.", ex.Message);
    }

    [Fact(DisplayName = "ApproveStudentAsync - UTCID04 - course == null -> proceed (boundary)")]
    [TestType("B")]
    public async Task ApproveStudentAsync_UTCID04_CourseNull_ShouldProceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((CourseDTO?)null);
        _repoMock.Setup(r => r.ApproveStudentAsync(10, 5)).ReturnsAsync(true);

        await _service.ApproveStudentAsync(10, 5);

        _repoMock.VerifyAll();
    }
}
