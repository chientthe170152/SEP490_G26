using Backend.Constants;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

public class UpdateQuestionStatusAsyncTest
{
    private readonly Mock<IQuestionRepository> _repoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();

    private QuestionService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    // ── TC01: Status không hợp lệ → QuestionValidationException ──────────
    [Fact]
    public async Task TC01_InvalidStatus_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().UpdateQuestionStatusAsync(new List<int> { 1 }, 1, "INVALID"));

        Assert.Contains(ex.Errors, e => e.Contains("Trạng thái không hợp lệ"));
    }

    // ── TC02: Không có câu hỏi nào thuộc userId → updatedCount = 0, không Save
    [Fact]
    public async Task TC02_NoOwnedQuestions_ReturnsZeroNoSave()
    {
        var questions = new List<Question>
        {
            TestHelpers.ValidQuestionEntity(questionId: 1, createdBy: 99) // không phải userId=1
        };

        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(questions);

        var count = await CreateService().UpdateQuestionStatusAsync(new List<int> { 1 }, userId: 1, QuestionStatus.Active);

        Assert.Equal(0, count);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ── TC03: Tất cả câu hỏi thuộc userId → update và save ──────────────
    [Fact]
    public async Task TC03_AllOwned_UpdatesAllAndSaves()
    {
        var questions = new List<Question>
        {
            TestHelpers.ValidQuestionEntity(questionId: 1, createdBy: 1),
            TestHelpers.ValidQuestionEntity(questionId: 2, createdBy: 1)
        };

        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(questions);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var count = await CreateService().UpdateQuestionStatusAsync(
            new List<int> { 1, 2 }, userId: 1, QuestionStatus.Active);

        Assert.Equal(2, count);
        Assert.All(questions, q => Assert.Equal(QuestionStatus.Active, q.Status));
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── TC04: Mix owner – chỉ update câu hỏi thuộc userId ────────────────
    [Fact]
    public async Task TC04_MixedOwnership_OnlyUpdatesOwned()
    {
        var q1 = TestHelpers.ValidQuestionEntity(questionId: 1, createdBy: 1);
        var q2 = TestHelpers.ValidQuestionEntity(questionId: 2, createdBy: 99); // khác owner

        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Question> { q1, q2 });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var count = await CreateService().UpdateQuestionStatusAsync(
            new List<int> { 1, 2 }, userId: 1, QuestionStatus.Archive);

        Assert.Equal(1, count);
        Assert.Equal(QuestionStatus.Archive, q1.Status);
        Assert.NotEqual(QuestionStatus.Archive, q2.Status); // q2 không bị đổi
    }

    // ── TC05: updatedCount > 0 → Log được gọi ────────────────────────────
    [Fact]
    public async Task TC05_UpdatedCountGt0_LogsInformation()
    {
        var questions = new List<Question>
        {
            TestHelpers.ValidQuestionEntity(questionId: 1, createdBy: 1)
        };

        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(questions);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await CreateService().UpdateQuestionStatusAsync(new List<int> { 1 }, 1, QuestionStatus.Active);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Updated status")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── TC06: questionIds rỗng → repo trả về rỗng, count = 0 ─────────────
    [Fact]
    public async Task TC06_EmptyIds_ReturnsZero()
    {
        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>()))
                 .ReturnsAsync(new List<Question>());

        var count = await CreateService().UpdateQuestionStatusAsync(new List<int>(), 1, QuestionStatus.Draft);

        Assert.Equal(0, count);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ── TC07: UpdatedAtUtc được gán ≈ UtcNow ─────────────────────────────
    [Fact]
    public async Task TC07_UpdatedAtUtc_SetToNow()
    {
        var q = TestHelpers.ValidQuestionEntity(createdBy: 1);
        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Question> { q });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        await CreateService().UpdateQuestionStatusAsync(new List<int> { 1 }, 1, QuestionStatus.Active);
        var after = DateTime.UtcNow;

        Assert.InRange(q.UpdatedAtUtc, before, after);
    }
}
