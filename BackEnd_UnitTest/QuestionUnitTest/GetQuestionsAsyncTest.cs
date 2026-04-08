using Backend.DTOs.Question;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

public class GetQuestionsAsyncTest
{
    private readonly Mock<IQuestionRepository> _repoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();
    private QuestionService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    private void SetupRepo(List<QuestionSummaryDto> items, int total)
    {
        _repoMock
            .Setup(r => r.GetQuestionsAsync(It.IsAny<QuestionListQueryDto>(), It.IsAny<int>()))
            .ReturnsAsync((items, total));
    }

    // ── TC01: Normal – trả về đúng paging metadata ──────────────────────
    [Fact]
    public async Task TC01_ValidQuery_ReturnsPaginatedResult()
    {
        // Arrange
        var items = Enumerable.Range(1, 5)
            .Select(i => new QuestionSummaryDto { QuestionId = i })
            .ToList();

        SetupRepo(items, total: 25);

        var query = new QuestionListQueryDto { Page = 2, PageSize = 5 };
        var svc = CreateService();

        // Act
        var result = await svc.GetQuestionsAsync(query, userId: 1);

        // Assert
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(5, result.TotalPages);    // ceil(25/5) = 5
        Assert.Equal(5, result.PageSize);
        Assert.Equal(2, result.CurrentPage);
    }

    // ── TC02: PageSize < 1 → clamp thành 1 ─────────────────────────────
    [Fact]
    public async Task TC02_PageSizeTooSmall_ClampedTo1()
    {
        SetupRepo(new List<QuestionSummaryDto>(), total: 0);

        var query = new QuestionListQueryDto { Page = 1, PageSize = 0 };
        var result = await CreateService().GetQuestionsAsync(query, 1);

        Assert.Equal(1, result.PageSize);
    }

    // ── TC03: PageSize > 50 → clamp thành 50 ────────────────────────────
    [Fact]
    public async Task TC03_PageSizeTooLarge_ClampedTo50()
    {
        SetupRepo(new List<QuestionSummaryDto>(), total: 0);

        var query = new QuestionListQueryDto { Page = 1, PageSize = 999 };
        var result = await CreateService().GetQuestionsAsync(query, 1);

        Assert.Equal(50, result.PageSize);
    }

    // ── TC04: Page < 1 → CurrentPage clamp thành 1 ──────────────────────
    [Fact]
    public async Task TC04_PageLessThan1_CurrentPageSetTo1()
    {
        SetupRepo(new List<QuestionSummaryDto>(), total: 0);

        var query = new QuestionListQueryDto { Page = -5, PageSize = 10 };
        var result = await CreateService().GetQuestionsAsync(query, 1);

        Assert.Equal(1, result.CurrentPage);
    }

    // ── TC05: totalCount = 0 → TotalPages = 0 ───────────────────────────
    [Fact]
    public async Task TC05_ZeroTotalCount_ZeroTotalPages()
    {
        SetupRepo(new List<QuestionSummaryDto>(), total: 0);

        var query = new QuestionListQueryDto { Page = 1, PageSize = 10 };
        var result = await CreateService().GetQuestionsAsync(query, 1);

        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }

    // ── TC06: totalCount không chia hết → TotalPages ceil đúng ──────────
    [Fact]
    public async Task TC06_TotalCountNotDivisible_CeilTotalPages()
    {
        SetupRepo(new List<QuestionSummaryDto>(), total : 11);

        var query = new QuestionListQueryDto { Page = 1, PageSize = 5 };
        var result = await CreateService().GetQuestionsAsync(query, 1);

        Assert.Equal(3, result.TotalPages); // ceil(11/5) = 3
    }
}
