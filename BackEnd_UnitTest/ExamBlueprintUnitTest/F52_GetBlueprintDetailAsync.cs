using System;
using System.Threading.Tasks;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest;

// F52 - GetBlueprintDetailAsync (ExamBlueprintService)
// Source: ExamBlueprintService.cs:63-77
// Branches:
//   1. id <= 0                                       -> throw ExamBlueprintValidationException
//   2. detail == null                                -> throw KeyNotFoundException
//   3. Success                                       -> return detail
public class F52_GetBlueprintDetailAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F52_GetBlueprintDetailAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID01 - Id hợp lệ + tìm thấy -> trả detail")]
    [TestType("N")]
    public async Task GetBlueprintDetailAsync_UTCID01_Valid_ShouldReturnDetail()
    {
        var dto = new BlueprintDetailDto { ExamBlueprintId = 5, Name = "BP-1" };
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(dto);

        var result = await _service.GetBlueprintDetailAsync(5, 100);

        Assert.Equal(5, result.ExamBlueprintId);
    }

    [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID02 - id = 0 -> ValidationException (boundary)")]
    [TestType("B")]
    public async Task GetBlueprintDetailAsync_UTCID02_ZeroId_ShouldThrow()
    {
        await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.GetBlueprintDetailAsync(0, 100));
    }

    [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID03 - Repo trả null -> KeyNotFoundException")]
    [TestType("A")]
    public async Task GetBlueprintDetailAsync_UTCID03_NotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(99, 100)).ReturnsAsync((BlueprintDetailDto?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetBlueprintDetailAsync(99, 100));
        Assert.Equal("Không tìm thấy ma trận đề.", ex.Message);
    }
}
