using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest;

// F50 - GetChaptersBySubjectAsync
// Source: ExamBlueprintService.cs:24-38
// Branches:
//   1. subjectId <= 0                -> throw ExamBlueprintValidationException
//   2. SubjectExistsAsync false      -> throw KeyNotFoundException
//   3. Success                       -> return chapters
public class F50_GetChaptersBySubjectAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F50_GetChaptersBySubjectAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    [Fact(DisplayName = "GetChaptersBySubjectAsync - UTCID01 - Subject hợp lệ -> trả chapters")]
    [TestType("N")]
    public async Task GetChaptersBySubjectAsync_UTCID01_Valid_ShouldReturnList()
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetChaptersBySubjectAsync(1))
                 .ReturnsAsync(new List<ChapterOptionDto> { new() { ChapterId = 10, Name = "Chương 1" } });

        var result = await _service.GetChaptersBySubjectAsync(1);

        Assert.Single(result);
    }

    [Fact(DisplayName = "GetChaptersBySubjectAsync - UTCID02 - subjectId = 0 -> ValidationException (boundary)")]
    [TestType("B")]
    public async Task GetChaptersBySubjectAsync_UTCID02_ZeroSubjectId_ShouldThrow()
    {
        await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.GetChaptersBySubjectAsync(0));
    }

    [Fact(DisplayName = "GetChaptersBySubjectAsync - UTCID03 - subjectId âm -> ValidationException")]
    [TestType("A")]
    public async Task GetChaptersBySubjectAsync_UTCID03_NegativeId_ShouldThrow()
    {
        await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.GetChaptersBySubjectAsync(-5));
    }

    [Fact(DisplayName = "GetChaptersBySubjectAsync - UTCID04 - Subject không tồn tại -> KeyNotFoundException")]
    [TestType("A")]
    public async Task GetChaptersBySubjectAsync_UTCID04_SubjectNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(99)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetChaptersBySubjectAsync(99));
        Assert.Equal("Không tìm thấy môn học.", ex.Message);
    }
}
