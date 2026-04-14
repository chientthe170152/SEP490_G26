using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

public class GetQuestionByIdAsyncTest
{
    private readonly Mock<IQuestionRepository> _repoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();

    private QuestionService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    // ── TC01: Câu hỏi không tồn tại → KeyNotFoundException ──────────────
    [Fact]
    public async Task TC01_NotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>()))
                 .ReturnsAsync((Question?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService().GetQuestionByIdAsync(99, 1));

        Assert.Contains("Không tìm thấy câu hỏi", ex.Message);
    }

    // ── TC02: Câu hỏi tồn tại nhưng userId khác → KeyNotFoundException ──
    [Fact]
    public async Task TC02_WrongOwner_ThrowsKeyNotFoundException()
    {
        var entity = TestHelpers.ValidQuestionEntity(createdBy: 99);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService().GetQuestionByIdAsync(1, userId: 1));
    }

    // ── TC03: MCQ – DTO có đúng số answers, không có BlankGroups ─────────
    [Fact]
    public async Task TC03_McqQuestion_ReturnsDtoWithAnswers()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        Assert.Equal(QuestionType.Mcq, dto.QuestionType);
        Assert.Equal(2, dto.Answers.Count);
        Assert.Null(dto.BlankGroups); // MCQ không có BlankGroups
    }

    // ── TC04: FillBlank – BlankGroups được build từ GroupAnswer ──────────
    [Fact]
    public async Task TC04_FillBlankQuestion_ReturnsBlankGroups()
    {
        var group = new GroupAnswer { GroupAnswerId = 5, Name = "Nhóm A", DependsOnGroupId = null };

        var entity = new Question
        {
            QuestionId = 1,
            CreatedByUserId = 1,
            QuestionType = QuestionType.FillBlank,
            Status = QuestionStatus.Draft,
            Difficulty = 1,
            ChapterId = 1,
            QuestionContent = "{\"stem\":\"Fill\",\"frame\":\"placeholder[1]\"}",
            UpdatedAtUtc = DateTime.UtcNow,
            QuestionAnswers = new List<QuestionAnswer>
            {
                new()
                {
                    QuestionAnswerId = 20,
                    Content = "placeholder[1]",
                    CorrectAnswer = "ans",
                    Point = 100,
                    GroupAnswerId = 5,
                    GroupAnswer = group,
                    BlankInputs = new List<BlankInput>
                    {
                        new() { InputTypeId = 1 }
                    }
                }
            }
        };

        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        Assert.Equal(QuestionType.FillBlank, dto.QuestionType);
        Assert.NotNull(dto.BlankGroups);
        Assert.Single(dto.BlankGroups!);
        Assert.Equal("Nhóm A", dto.BlankGroups![0].Name);
    }

    // ── TC05: QuestionContent là JSON hợp lệ → Stem và Frame parse đúng ─
    [Fact]
    public async Task TC05_ValidJsonContent_ParsesStemAndFrame()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        entity.QuestionContent = "{\"stem\":\"Đây là đề\",\"frame\":\"frame mẫu\"}";
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        Assert.Equal("Đây là đề", dto.Stem);
        Assert.Equal("frame mẫu", dto.Frame);
    }

    // ── TC06: QuestionContent là raw text (không phải JSON) → fallback ──
    [Fact]
    public async Task TC06_RawTextContent_FallsBackToStem()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        entity.QuestionContent = "câu hỏi dạng text thuần";
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        // Fallback: stem = raw text, frame = null
        Assert.Equal("câu hỏi dạng text thuần", dto.Stem);
        Assert.Null(dto.Frame);
    }

    // ── TC07: QuestionContent null/empty → Stem và Frame đều null ────────
    [Fact]
    public async Task TC07_NullContent_StemAndFrameNull()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        entity.QuestionContent = null;
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        Assert.Null(dto.Stem);
        Assert.Null(dto.Frame);
    }

    // ── TC08: Answer có BlankInput → InputTypeId được map đúng ───────────
    [Fact]
    public async Task TC08_AnswerWithBlankInput_MapsInputTypeId()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        entity.QuestionAnswers.First().BlankInputs = new List<BlankInput> { new() { InputTypeId = 7 } };
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        Assert.Equal(7, dto.Answers[0].InputTypeId);
    }

    // ── TC09: Answer có content dạng placeholder[N] → BlankIndex map đúng
    [Fact]
    public async Task TC09_AnswerWithPlaceholder_MapsBlankIndex()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        entity.QuestionAnswers.First().Content = "placeholder[3]{some}";
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var dto = await CreateService().GetQuestionByIdAsync(1, 1);

        Assert.Equal(3, dto.Answers[0].BlankIndex);
    }
}
