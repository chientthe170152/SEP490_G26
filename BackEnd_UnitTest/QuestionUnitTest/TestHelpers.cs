using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Models;

namespace BackEnd_UnitTest.QuestionUnitTest;

/// <summary>
/// Factory methods dùng chung cho tất cả test class.
/// </summary>
public static class TestHelpers
{
    // ── QuestionDto ──────────────────────────────────────────────────────

    public static QuestionDto ValidMcqDto(string status = QuestionStatus.Draft) => new()
    {
        QuestionType = QuestionType.Mcq,
        Stem = "Câu hỏi mẫu?",
        Difficulty = 1,
        Status = status,
        ChapterId = 1,
        Answers = new List<AnswerDto>
        {
            new() { Content = "A", IsCorrect = true,  Point = 100 },
            new() { Content = "B", IsCorrect = false, Point = 0   }
        }
    };

    public static QuestionDto ValidFillBlankDto(string status = QuestionStatus.Draft) => new()
    {
        QuestionType = QuestionType.FillBlank,
        Stem = "Điền vào chỗ trống",
        Frame = "placeholder[1]",
        Difficulty = 2,
        Status = status,
        ChapterId = 1,
        Answers = new List<AnswerDto>
        {
            new() { Content = "placeholder[1]", CorrectAnswer = "đáp án", Point = 100, InputTypeId = 1 }
        }
    };

    // ── Question (entity) ────────────────────────────────────────────────

    public static Question ValidQuestionEntity(
        int questionId = 1,
        int createdBy = 1,
        string status = QuestionStatus.Draft) => new()
        {
            QuestionId = questionId,
            CreatedByUserId = createdBy,
            QuestionType = QuestionType.Mcq,
            Status = status,
            Difficulty = 1,
            ChapterId = 1,
            QuestionContent = "{\"stem\":\"Câu hỏi\",\"frame\":null}",
            UpdatedAtUtc = DateTime.UtcNow,
            QuestionAnswers = new List<QuestionAnswer>
        {
            new() { QuestionAnswerId = 10, Content = "A", IsCorrect = true,  Point = 100, BlankInputs = new List<BlankInput>() },
            new() { QuestionAnswerId = 11, Content = "B", IsCorrect = false, Point = 0,   BlankInputs = new List<BlankInput>() }
        }
        };
}
