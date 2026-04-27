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

// F6 - GetStudentsInClassAsync
// Source: CourseService.cs:93-96 — pure delegation. No branches.
public class F6_GetStudentsInClassAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F6_GetStudentsInClassAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "GetStudentsInClassAsync - UTCID01 - Lớp có học sinh -> trả list")]
    [TestType("N")]
    public async Task GetStudentsInClassAsync_UTCID01_HasStudents_ShouldReturnList()
    {
        var data = new List<StudentInClassDTO> { new() { StudentId = 1, FullName = "A", Email = "a@test.com" } };
        _repoMock.Setup(r => r.GetStudentsInClassAsync(10)).ReturnsAsync(data);

        var result = await _service.GetStudentsInClassAsync(10);

        Assert.Single(result);
    }

    [Fact(DisplayName = "GetStudentsInClassAsync - UTCID02 - Lớp không có học sinh -> trả list rỗng")]
    [TestType("B")]
    public async Task GetStudentsInClassAsync_UTCID02_NoStudents_ShouldReturnEmpty()
    {
        _repoMock.Setup(r => r.GetStudentsInClassAsync(10)).ReturnsAsync(new List<StudentInClassDTO>());

        var result = await _service.GetStudentsInClassAsync(10);

        Assert.Empty(result);
    }
}
