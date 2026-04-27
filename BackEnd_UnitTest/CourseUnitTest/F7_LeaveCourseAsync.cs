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

// F7 - LeaveCourseAsync
// Source: CourseService.cs:111-118
// Branches:
//   1. course != null && Status == Closed   -> throw "Lớp học đã bị đóng, không thể rời lớp."
//   2. course == null                       -> proceed (no throw, repo handles missing class)
//   3. course != null && Status != Closed   -> proceed
public class F7_LeaveCourseAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F7_LeaveCourseAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "LeaveCourseAsync - UTCID01 - Lớp Active -> rời lớp thành công")]
    [TestType("N")]
    public async Task LeaveCourseAsync_UTCID01_ActiveClass_ShouldLeave()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.LeaveClassAsync(10, 5)).Returns(Task.CompletedTask);

        await _service.LeaveCourseAsync(10, 5);

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "LeaveCourseAsync - UTCID02 - Lớp đã Closed -> throw")]
    [TestType("A")]
    public async Task LeaveCourseAsync_UTCID02_ClosedClass_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Closed });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.LeaveCourseAsync(10, 5));
        Assert.Equal("Lớp học đã bị đóng, không thể rời lớp.", ex.Message);
        _repoMock.Verify(r => r.LeaveClassAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact(DisplayName = "LeaveCourseAsync - UTCID03 - course == null -> không throw, vẫn gọi repo (boundary)")]
    [TestType("B")]
    public async Task LeaveCourseAsync_UTCID03_CourseNull_ShouldStillCallRepo()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((CourseDTO?)null);
        _repoMock.Setup(r => r.LeaveClassAsync(10, 5)).Returns(Task.CompletedTask);

        await _service.LeaveCourseAsync(10, 5);

        _repoMock.Verify(r => r.LeaveClassAsync(10, 5), Times.Once);
    }
}
