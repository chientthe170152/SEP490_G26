using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.ExamBlueprint;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest;

// F51 - GetBlueprintsAsync (ExamBlueprintService)
// Source: ExamBlueprintService.cs:40-61
// Branches:
//   1. currentUserId <= 0                            -> throw UnauthorizedAccessException
//   2. query.Page < 1                                -> reset to 1
//   3. query.PageSize <= 0                           -> reset to 10
//   4. query.PageSize > 100                          -> clamp to 100
//   5. totalCount == 0                               -> totalPages = 0
//   6. totalCount > 0                                -> totalPages = ceiling(totalCount/pageSize)
public class F51_GetBlueprintsAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F51_GetBlueprintsAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    [Fact(DisplayName = "GetBlueprintsAsync - UTCID01 - Query hợp lệ + có dữ liệu -> trả paged response")]
    [TestType("N")]
    public async Task GetBlueprintsAsync_UTCID01_Valid_ShouldReturnPaged()
    {
        var query = new BlueprintListQueryDto { Page = 1, PageSize = 10 };
        _repoMock.Setup(r => r.GetBlueprintsAsync(query, 1)).ReturnsAsync((new List<BlueprintListItemDto> { new() { ExamBlueprintId = 5 } }, 25));

        var result = await _service.GetBlueprintsAsync(query, 1);

        Assert.Equal(25, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact(DisplayName = "GetBlueprintsAsync - UTCID02 - currentUserId = 0 -> UnauthorizedAccessException (boundary)")]
    [TestType("B")]
    public async Task GetBlueprintsAsync_UTCID02_ZeroUserId_ShouldThrow()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetBlueprintsAsync(new BlueprintListQueryDto(), 0));
    }

    [Fact(DisplayName = "GetBlueprintsAsync - UTCID03 - Page = 0 + PageSize = 0 -> reset Page=1, PageSize=10 (boundary)")]
    [TestType("B")]
    public async Task GetBlueprintsAsync_UTCID03_BadPagination_ShouldNormalize()
    {
        var query = new BlueprintListQueryDto { Page = 0, PageSize = 0 };
        _repoMock.Setup(r => r.GetBlueprintsAsync(query, 1)).ReturnsAsync((new List<BlueprintListItemDto>(), 0));

        var result = await _service.GetBlueprintsAsync(query, 1);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact(DisplayName = "GetBlueprintsAsync - UTCID04 - PageSize = 200 -> clamp xuống 100 (boundary)")]
    [TestType("B")]
    public async Task GetBlueprintsAsync_UTCID04_PageSizeTooLarge_ShouldClamp()
    {
        var query = new BlueprintListQueryDto { Page = 1, PageSize = 200 };
        _repoMock.Setup(r => r.GetBlueprintsAsync(query, 1)).ReturnsAsync((new List<BlueprintListItemDto>(), 50));

        var result = await _service.GetBlueprintsAsync(query, 1);

        Assert.Equal(100, result.PageSize);
        Assert.Equal(1, result.TotalPages);
    }
}
