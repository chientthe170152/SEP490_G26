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

// F57 - RemoveStudentAsync (registry calls it RemoveStudentFromClassAsync; source method is RemoveStudentAsync)
// Source: CourseService.cs:245-253
// Branches:
//   1. course != null && Status == Closed       -> throw "Lớp học đã bị đóng, không thể xóa học sinh."
//   2. RemoveStudentAsync repo returns false    -> throw "Học sinh không tồn tại trong lớp ."
//   3. Success                                  -> no exception
//   4. course == null                           -> proceed
public class F57_RemoveStudentAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F57_RemoveStudentAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "RemoveStudentAsync - UTCID01 - Lớp Active + repo trả true -> success")]
    [TestType("N")]
    public async Task RemoveStudentAsync_UTCID01_Valid_ShouldSucceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.RemoveStudentAsync(10, 5)).ReturnsAsync(true);

        await _service.RemoveStudentAsync(10, 5);
    }

    [Fact(DisplayName = "RemoveStudentAsync - UTCID02 - Lớp Closed -> throw")]
    [TestType("A")]
    public async Task RemoveStudentAsync_UTCID02_ClassClosed_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Closed });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RemoveStudentAsync(10, 5));
        Assert.Equal("Lớp học đã bị đóng, không thể xóa học sinh.", ex.Message);
    }

    [Fact(DisplayName = "RemoveStudentAsync - UTCID03 - Repo trả false -> throw")]
    [TestType("A")]
    public async Task RemoveStudentAsync_UTCID03_RepoReturnsFalse_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.RemoveStudentAsync(10, 5)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RemoveStudentAsync(10, 5));
        Assert.Equal("Học sinh không tồn tại trong lớp .", ex.Message);
    }

    [Fact(DisplayName = "RemoveStudentAsync - UTCID04 - course == null -> proceed (boundary)")]
    [TestType("B")]
    public async Task RemoveStudentAsync_UTCID04_CourseNull_ShouldProceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((CourseDTO?)null);
        _repoMock.Setup(r => r.RemoveStudentAsync(10, 5)).ReturnsAsync(true);

        await _service.RemoveStudentAsync(10, 5);
    }
}
