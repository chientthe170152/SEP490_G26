using Backend.DTOs.Question;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

public class CreateQuestionsAsyncTest
{
    private readonly Mock<IQuestionRepository> _repoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();

    private QuestionService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    /// <summary>
    /// Cài đặt repo mặc định cho happy path: chapter tồn tại, CreateQuestions trả về entities.
    /// </summary>
    private void SetupHappyPath(List<QuestionDto> request, int userId = 1)
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var createdEntities = request.Select((_, i) => new Question
        {
            QuestionId = i + 1,
            QuestionType = request[i].QuestionType,
            Status = request[i].Status,
            Difficulty = request[i].Difficulty,
            ChapterId = request[i].ChapterId,
            QuestionContent = "{}",
            UpdatedAtUtc = DateTime.UtcNow,
            QuestionAnswers = new List<QuestionAnswer>()
        }).ToList();

        _repoMock
            .Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
            .ReturnsAsync(createdEntities);

        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    // ── TC01: request = null → QuestionValidationException ──────────────
    [Fact]
    public async Task TC01_RequestNull_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, null!));

        Assert.Contains("Phải có ít nhất 1 câu hỏi.", ex.Errors);
    }

    // ── TC02: request = [] → QuestionValidationException ────────────────
    [Fact]
    public async Task TC02_RequestEmpty_ThrowsValidationException()
    {
        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto>()));

        Assert.Contains("Phải có ít nhất 1 câu hỏi.", ex.Errors);
    }

    // ── TC03: QuestionType không hợp lệ → lỗi sớm (early return) ────────
    [Fact]
    public async Task TC03_InvalidQuestionType_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.QuestionType = "INVALID_TYPE";

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Loại câu hỏi không hợp lệ"));
    }

    // ── TC04: Stem trống → lỗi validation ───────────────────────────────
    [Fact]
    public async Task TC04_EmptyStem_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Stem = "  ";

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Đề bài không được để trống"));
    }

    // ── TC05: Difficulty ngoài khoảng [1,4] → lỗi validation ────────────
    [Fact]
    public async Task TC05_InvalidDifficulty_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Difficulty = 99;

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Mức độ phải từ 1 đến 4"));
    }

    // ── TC06: Status không hợp lệ → lỗi validation ──────────────────────
    [Fact]
    public async Task TC06_InvalidStatus_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Status = "INVALID_STATUS";

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Trạng thái không hợp lệ"));
    }

    // ── TC07: Chapter không tồn tại → lỗi validation ────────────────────
    [Fact]
    public async Task TC07_ChapterNotExist_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(false);

        var dto = TestHelpers.ValidMcqDto();

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Chương không tồn tại"));
    }

    // ── TC08: MCQ không có đáp án → early return ────────────────────────
    [Fact]
    public async Task TC08_McqNoAnswers_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers = new List<AnswerDto>();

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Phải có ít nhất 1 đáp án"));
    }

    // ── TC09: MCQ ít hơn 2 lựa chọn → lỗi ──────────────────────────────
    [Fact]
    public async Task TC09_McqLessThan2Answers_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers = new List<AnswerDto>
        {
            new() { Content = "A", IsCorrect = true, Point = 100 }
        };

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("ít nhất 2 lựa chọn"));
    }

    // ── TC10: MCQ không có đáp án đúng → lỗi ────────────────────────────
    [Fact]
    public async Task TC10_McqNoCorrectAnswer_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers.ForEach(a => a.IsCorrect = false);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("ít nhất 1 đáp án đúng"));
    }

    // ── TC11: MCQ điểm ngoài [0,100] → lỗi ─────────────────────────────
    [Fact]
    public async Task TC11_McqAnswerPointOutOfRange_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers[0].Point = 150; // out of range

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Điểm phải từ 0 đến 100"));
    }

    // ── TC12: Tổng điểm ≠ 100 → lỗi ────────────────────────────────────
    [Fact]
    public async Task TC12_TotalPointNot100_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers[0].Point = 50; // tổng = 50, không phải 100

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Tổng điểm phải bằng 100%"));
    }

    // ── TC13: FillBlank thiếu Frame → lỗi ───────────────────────────────
    [Fact]
    public async Task TC13_FillBlankEmptyFrame_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidFillBlankDto();
        dto.Frame = "";

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Khung trả lời không được để trống"));
    }

    // ── TC14: FillBlank thiếu InputTypeId → lỗi ─────────────────────────
    [Fact]
    public async Task TC14_FillBlankMissingInputTypeId_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidFillBlankDto();
        dto.Answers[0].InputTypeId = 0; // <= 0 → invalid

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Thiếu loại giới hạn nhập liệu"));
    }

    // ── TC15: FillBlank điểm ô trống ngoài [0,100] → lỗi ────────────────
    [Fact]
    public async Task TC15_FillBlankPointOutOfRange_ThrowsValidationException()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto = TestHelpers.ValidFillBlankDto();
        dto.Answers[0].Point = -1;

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto }));

        Assert.Contains(ex.Errors, e => e.Contains("Điểm phải từ 0 đến 100"));
    }

    // ── TC16: Nhiều câu hỏi, 1 lỗi → errors chứa đúng prefix câu hỏi ───
    [Fact]
    public async Task TC16_MultipleQuestions_ErrorPrefixCorrect()
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        var dto1 = TestHelpers.ValidMcqDto();
        var dto2 = TestHelpers.ValidMcqDto();
        dto2.Stem = ""; // câu hỏi #2 lỗi

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto1, dto2 }));

        Assert.Contains(ex.Errors, e => e.Contains("Câu hỏi #2"));
    }

    // ── TC17: Happy path MCQ – trả về list đúng số lượng ────────────────
    [Fact]
    public async Task TC17_ValidMcq_ReturnsCreatedList()
    {
        var dto = TestHelpers.ValidMcqDto();
        SetupHappyPath(new List<QuestionDto> { dto });

        var result = await CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto });

        Assert.Single(result);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── TC18: Happy path FillBlank – CreateQuestions và SaveChanges được gọi
    [Fact]
    public async Task TC18_ValidFillBlank_CreatesAndSaves()
    {
        var dto = TestHelpers.ValidFillBlankDto();
        SetupHappyPath(new List<QuestionDto> { dto });

        var result = await CreateService().CreateQuestionsAsync(1, new List<QuestionDto> { dto });

        Assert.Single(result);
        _repoMock.Verify(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
