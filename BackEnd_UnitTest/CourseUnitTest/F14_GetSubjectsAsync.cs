using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.ExamBlueprint;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F14 - GetSubjectsAsync (CourseService)
// Source: CourseService.cs:267-270 — pure delegation. No branches.
public class F14_GetSubjectsAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F14_GetSubjectsAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "GetSubjectsAsync - UTCID01 - DB có môn học -> trả list")]
    [TestType("N")]
    public async Task GetSubjectsAsync_UTCID01_HasSubjects_ShouldReturnList()
    {
        var data = new List<SubjectOptionDto> { new() { SubjectId = 1, Code = "MAE", Name = "Toán" } };
        _repoMock.Setup(r => r.GetSubjectsAsync()).ReturnsAsync(data);

        var result = await _service.GetSubjectsAsync();

        Assert.Single(result);
    }

    [Fact(DisplayName = "GetSubjectsAsync - UTCID02 - DB không có môn học -> trả list rỗng")]
    [TestType("B")]
    public async Task GetSubjectsAsync_UTCID02_NoSubjects_ShouldReturnEmpty()
    {
        _repoMock.Setup(r => r.GetSubjectsAsync()).ReturnsAsync(new List<SubjectOptionDto>());

        var result = await _service.GetSubjectsAsync();

        Assert.Empty(result);
    }
}
