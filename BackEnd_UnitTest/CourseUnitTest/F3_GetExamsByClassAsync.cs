using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F3 - GetExamsByClassAsync
// Source: CourseService.cs:41-44 — pure delegation. No branches.
public class F3_GetExamsByClassAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F3_GetExamsByClassAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "GetExamsByClassAsync - UTCID01 - Student view -> forward isTeacher=false, trả list")]
    [TestType("N")]
    public async Task GetExamsByClassAsync_UTCID01_StudentView_ShouldReturnList()
    {
        var data = new List<ExamInCourseDTO> { new() { ExamId = 1, Title = "Midterm" } };
        _repoMock.Setup(r => r.GetExamsByClassAsync(10, false)).ReturnsAsync(data);

        var result = await _service.GetExamsByClassAsync(10, false);

        Assert.Single(result);
    }

    [Fact(DisplayName = "GetExamsByClassAsync - UTCID02 - Teacher view -> forward isTeacher=true")]
    [TestType("N")]
    public async Task GetExamsByClassAsync_UTCID02_TeacherView_ShouldReturnList()
    {
        _repoMock.Setup(r => r.GetExamsByClassAsync(10, true)).ReturnsAsync(new List<ExamInCourseDTO>());

        var result = await _service.GetExamsByClassAsync(10, true);

        Assert.Empty(result);
        _repoMock.Verify(r => r.GetExamsByClassAsync(10, true), Times.Once);
    }

    [Fact(DisplayName = "GetExamsByClassAsync - UTCID03 - Lớp không có bài thi -> trả list rỗng")]
    [TestType("B")]
    public async Task GetExamsByClassAsync_UTCID03_NoExams_ShouldReturnEmpty()
    {
        _repoMock.Setup(r => r.GetExamsByClassAsync(99, false)).ReturnsAsync(new List<ExamInCourseDTO>());

        var result = await _service.GetExamsByClassAsync(99, false);

        Assert.Empty(result);
    }
}
