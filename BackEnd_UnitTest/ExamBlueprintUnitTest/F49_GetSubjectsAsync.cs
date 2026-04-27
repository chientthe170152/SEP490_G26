using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.ExamBlueprint;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest;

// F49 - GetSubjectsAsync (ExamBlueprintService)
// Source: ExamBlueprintService.cs:19-22 — pure delegation. No branches.
public class F49_GetSubjectsAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F49_GetSubjectsAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    [Fact(DisplayName = "GetSubjectsAsync - UTCID01 - Có môn học -> trả list")]
    [TestType("N")]
    public async Task GetSubjectsAsync_UTCID01_HasSubjects_ShouldReturnList()
    {
        _repoMock.Setup(r => r.GetSubjectsAsync()).ReturnsAsync(new List<SubjectOptionDto> { new() { SubjectId = 1, Name = "Toán" } });
        var result = await _service.GetSubjectsAsync();
        Assert.Single(result);
    }

    [Fact(DisplayName = "GetSubjectsAsync - UTCID02 - Không có môn -> list rỗng")]
    [TestType("B")]
    public async Task GetSubjectsAsync_UTCID02_None_ShouldReturnEmpty()
    {
        _repoMock.Setup(r => r.GetSubjectsAsync()).ReturnsAsync(new List<SubjectOptionDto>());
        var result = await _service.GetSubjectsAsync();
        Assert.Empty(result);
    }
}
