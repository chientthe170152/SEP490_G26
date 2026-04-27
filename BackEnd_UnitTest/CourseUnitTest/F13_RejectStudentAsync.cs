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

// F13 - RejectStudentAsync
// Source: CourseService.cs:235-243
// Branches:
//   1. course != null && Status == Closed       -> throw "Lớp học đã bị đóng."
//   2. RejectStudentAsync repo returns false    -> throw "Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt."
//   3. Success                                  -> no exception
//   4. course == null                           -> proceed
public class F13_RejectStudentAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F13_RejectStudentAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "RejectStudentAsync - UTCID01 - Lớp Active + repo trả true -> success")]
    [TestType("N")]
    public async Task RejectStudentAsync_UTCID01_Valid_ShouldSucceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.RejectStudentAsync(10, 5)).ReturnsAsync(true);

        await _service.RejectStudentAsync(10, 5);
    }

    [Fact(DisplayName = "RejectStudentAsync - UTCID02 - Lớp Closed -> throw")]
    [TestType("A")]
    public async Task RejectStudentAsync_UTCID02_ClassClosed_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Closed });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RejectStudentAsync(10, 5));
        Assert.Equal("Lớp học đã bị đóng.", ex.Message);
    }

    [Fact(DisplayName = "RejectStudentAsync - UTCID03 - Repo trả false -> throw")]
    [TestType("A")]
    public async Task RejectStudentAsync_UTCID03_RepoReturnsFalse_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.RejectStudentAsync(10, 5)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RejectStudentAsync(10, 5));
        Assert.Equal("Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt.", ex.Message);
    }

    [Fact(DisplayName = "RejectStudentAsync - UTCID04 - course == null -> proceed (boundary)")]
    [TestType("B")]
    public async Task RejectStudentAsync_UTCID04_CourseNull_ShouldProceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((CourseDTO?)null);
        _repoMock.Setup(r => r.RejectStudentAsync(10, 5)).ReturnsAsync(true);

        await _service.RejectStudentAsync(10, 5);
    }
}
