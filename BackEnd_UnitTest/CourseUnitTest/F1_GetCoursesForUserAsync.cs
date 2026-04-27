using System;
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

// F1 - GetCoursesForUserAsync
// Branches in source (CourseService.cs:23-28):
//   - Pure delegation to repo. No conditional branches inside service.
// What we cover:
//   - Normal: repo returns non-empty list -> service returns same list
//   - Boundary: repo returns empty list -> service returns empty list
//   - Boundary: userId = 0 -> still forwarded to repo (no validation in service)
//   - Abnormal: userId < 0 -> still forwarded to repo (no validation in service)
//   - Abnormal: repo throws -> service propagates
public class F1_GetCoursesForUserAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IConfiguration _config;
    private readonly CourseService _service;

    public F1_GetCoursesForUserAsync_Tests()
    {
        _repoMock = new Mock<ICourseRepo>(MockBehavior.Strict);
        _emailMock = new Mock<IEmailService>(MockBehavior.Strict);
        _config = TestConfigBuilder.Default();
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "GetCoursesForUserAsync - UTCID01 - User có khoá học -> trả full list")]
    [TestType("N")]
    public async Task GetCoursesForUserAsync_UTCID01_UserHasCourses_ShouldReturnList()
    {
        const int userId = 1;
        var data = new List<CourseDTO>
        {
            new() { ClassId = 10, ClassName = "Toan 12", Role = "Student" },
            new() { ClassId = 11, ClassName = "Ly 11",   Role = "Student" }
        };
        _repoMock.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(data);

        var result = await _service.GetCoursesForUserAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].ClassId);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetCoursesForUserAsync - UTCID02 - User không có khoá học -> trả list rỗng")]
    [TestType("B")]
    public async Task GetCoursesForUserAsync_UTCID02_UserHasNoCourse_ShouldReturnEmpty()
    {
        const int userId = 2;
        _repoMock.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());

        var result = await _service.GetCoursesForUserAsync(userId);

        Assert.NotNull(result);
        Assert.Empty(result);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetCoursesForUserAsync - UTCID03 - userId = 0 -> vẫn forward repo, repo trả rỗng")]
    [TestType("B")]
    public async Task GetCoursesForUserAsync_UTCID03_UserIdZero_ShouldForwardToRepo()
    {
        const int userId = 0;
        _repoMock.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());

        var result = await _service.GetCoursesForUserAsync(userId);

        Assert.Empty(result);
        _repoMock.Verify(r => r.GetCoursesForUserAsync(0), Times.Once);
    }

    [Fact(DisplayName = "GetCoursesForUserAsync - UTCID04 - userId âm -> vẫn forward repo, repo trả rỗng")]
    [TestType("A")]
    public async Task GetCoursesForUserAsync_UTCID04_UserIdNegative_ShouldForwardToRepo()
    {
        const int userId = -1;
        _repoMock.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());

        var result = await _service.GetCoursesForUserAsync(userId);

        Assert.Empty(result);
        _repoMock.Verify(r => r.GetCoursesForUserAsync(-1), Times.Once);
    }

    [Fact(DisplayName = "GetCoursesForUserAsync - UTCID05 - Repo ném exception -> service propagate")]
    [TestType("A")]
    public async Task GetCoursesForUserAsync_UTCID05_RepoThrows_ShouldPropagate()
    {
        const int userId = 1;
        _repoMock.Setup(r => r.GetCoursesForUserAsync(userId))
                 .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetCoursesForUserAsync(userId));

        Assert.Equal("DB connection lost", ex.Message);
        _repoMock.VerifyAll();
    }
}
