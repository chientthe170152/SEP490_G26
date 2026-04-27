using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Exceptions;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest;

// F55 - UpdateBlueprintStatusAsync
// Source: ExamBlueprintService.cs:418-429
// Branches:
//   1. status != Archived                          -> throw ExamBlueprintValidationException
//   2. ids filtered (id > 0, distinct).Count == 0  -> return 0 (no save)
//   3. Otherwise                                   -> repo update
public class F55_UpdateBlueprintStatusAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F55_UpdateBlueprintStatusAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID01 - Archive valid ids -> repo trả số updated")]
    [TestType("N")]
    public async Task UpdateBlueprintStatusAsync_UTCID01_Valid_ShouldUpdate()
    {
        var ids = new List<int> { 1, 2, 3 };
        _repoMock.Setup(r => r.UpdateBlueprintStatusAsync(It.Is<IEnumerable<int>>(x => x.SequenceEqual(ids)), 100, ExamBlueprintStatus.Archived)).ReturnsAsync(3);

        var count = await _service.UpdateBlueprintStatusAsync(ids, 100, ExamBlueprintStatus.Archived);

        Assert.Equal(3, count);
    }

    [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID02 - Status không phải Archived -> ValidationException")]
    [TestType("A")]
    public async Task UpdateBlueprintStatusAsync_UTCID02_NonArchived_ShouldThrow()
    {
        await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintStatusAsync(new List<int> { 1 }, 100, ExamBlueprintStatus.Active));
    }

    [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID03 - ids rỗng sau filter -> return 0 (boundary)")]
    [TestType("B")]
    public async Task UpdateBlueprintStatusAsync_UTCID03_EmptyAfterFilter_ShouldReturnZero()
    {
        // Tất cả id <= 0 sau Where(>0) → list rỗng → service trả 0 mà không gọi repo.
        var count = await _service.UpdateBlueprintStatusAsync(new List<int> { 0, -1, -5 }, 100, ExamBlueprintStatus.Archived);

        Assert.Equal(0, count);
        _repoMock.Verify(r => r.UpdateBlueprintStatusAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID04 - ids có trùng -> distinct trước khi gọi repo")]
    [TestType("B")]
    public async Task UpdateBlueprintStatusAsync_UTCID04_DuplicateIds_ShouldDistinct()
    {
        _repoMock.Setup(r => r.UpdateBlueprintStatusAsync(It.Is<IEnumerable<int>>(x => x.Count() == 2 && x.Contains(1) && x.Contains(2)), 100, ExamBlueprintStatus.Archived)).ReturnsAsync(2);

        var count = await _service.UpdateBlueprintStatusAsync(new List<int> { 1, 1, 2, 2 }, 100, ExamBlueprintStatus.Archived);

        Assert.Equal(2, count);
    }
}
