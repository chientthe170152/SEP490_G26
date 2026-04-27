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

// F11 - GetPendingStudentsAsync
// Source: CourseService.cs:220-223 — pure delegation. No branches.
public class F11_GetPendingStudentsAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F11_GetPendingStudentsAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "GetPendingStudentsAsync - UTCID01 - Có học sinh chờ duyệt -> trả list")]
    [TestType("N")]
    public async Task GetPendingStudentsAsync_UTCID01_HasPending_ShouldReturnList()
    {
        var data = new List<StudentInClassDTO> { new() { StudentId = 1, FullName = "A", Email = "a@test.com" } };
        _repoMock.Setup(r => r.GetPendingStudentsAsync(10)).ReturnsAsync(data);

        var result = await _service.GetPendingStudentsAsync(10);

        Assert.Single(result);
    }

    [Fact(DisplayName = "GetPendingStudentsAsync - UTCID02 - Không có học sinh chờ -> trả list rỗng")]
    [TestType("B")]
    public async Task GetPendingStudentsAsync_UTCID02_None_ShouldReturnEmpty()
    {
        _repoMock.Setup(r => r.GetPendingStudentsAsync(10)).ReturnsAsync(new List<StudentInClassDTO>());

        var result = await _service.GetPendingStudentsAsync(10);

        Assert.Empty(result);
    }
}
