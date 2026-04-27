using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.Question;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

// F26 - GetQuestionsAsync
// Source: QuestionService.cs:32-45
// Effective branches:
//   1. PageSize within (1..50)        -> use as-is
//   2. PageSize > 50                  -> clamp to 50
//   3. PageSize < 1                   -> clamp to 1
//   4. Page >= 1                      -> CurrentPage = Page
//   5. Page < 1                       -> CurrentPage = 1 (Math.Max boundary)
//   6. totalCount % pageSize == 0     -> ceiling = totalCount/pageSize
//   7. totalCount % pageSize > 0      -> ceiling = floor + 1
public class F26_GetQuestionsAsync_Tests
{
    private readonly Mock<IQuestionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly QuestionService _service;

    public F26_GetQuestionsAsync_Tests()
    {
        _service = new QuestionService(_repoMock.Object, new Mock<ILogger<QuestionService>>().Object);
    }

    [Fact(DisplayName = "GetQuestionsAsync - UTCID01 - Query hợp lệ -> trả paged result")]
    [TestType("N")]
    public async Task GetQuestionsAsync_UTCID01_ValidQuery_ShouldReturnPaged()
    {
        var query = new QuestionListQueryDto { Page = 2, PageSize = 10 };
        _repoMock.Setup(r => r.GetQuestionsAsync(query, 1)).ReturnsAsync((new List<QuestionSummaryDto> { new() { QuestionId = 5 } }, 25));

        var result = await _service.GetQuestionsAsync(query, 1);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages); // ceil(25/10)
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact(DisplayName = "GetQuestionsAsync - UTCID02 - PageSize > 50 -> clamp xuống 50 (boundary)")]
    [TestType("B")]
    public async Task GetQuestionsAsync_UTCID02_PageSizeOver50_ShouldClamp()
    {
        var query = new QuestionListQueryDto { Page = 1, PageSize = 200 };
        _repoMock.Setup(r => r.GetQuestionsAsync(query, 1)).ReturnsAsync((new List<QuestionSummaryDto>(), 100));

        var result = await _service.GetQuestionsAsync(query, 1);

        Assert.Equal(50, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact(DisplayName = "GetQuestionsAsync - UTCID03 - PageSize = 0 -> clamp lên 1 (boundary)")]
    [TestType("B")]
    public async Task GetQuestionsAsync_UTCID03_PageSizeZero_ShouldClampToOne()
    {
        var query = new QuestionListQueryDto { Page = 1, PageSize = 0 };
        _repoMock.Setup(r => r.GetQuestionsAsync(query, 1)).ReturnsAsync((new List<QuestionSummaryDto>(), 5));

        var result = await _service.GetQuestionsAsync(query, 1);

        Assert.Equal(1, result.PageSize);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact(DisplayName = "GetQuestionsAsync - UTCID04 - Page = 0 -> CurrentPage = 1 (boundary cho Math.Max)")]
    [TestType("B")]
    public async Task GetQuestionsAsync_UTCID04_PageZero_ShouldClampToOne()
    {
        var query = new QuestionListQueryDto { Page = 0, PageSize = 10 };
        _repoMock.Setup(r => r.GetQuestionsAsync(query, 1)).ReturnsAsync((new List<QuestionSummaryDto>(), 0));

        var result = await _service.GetQuestionsAsync(query, 1);

        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(0, result.TotalPages);
    }
}
