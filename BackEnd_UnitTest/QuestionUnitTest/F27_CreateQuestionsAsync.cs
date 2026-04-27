using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

// F27 - CreateQuestionsAsync
// Source: QuestionService.cs:47-63 + ValidateQuestionItemAsync (344-374) + ValidatePointRange (376-379) + HandleBlankGroupsAsync
// Branches in CreateQuestionsAsync + ValidateQuestionItemAsync:
//   1. request null/empty                            -> throw QuestionValidationException
//   2. Loop validate -> errors.Any() true            -> throw QuestionValidationException
//   3. Loop validate -> errors empty                 -> CreateQuestionsAsync + HandleBlankGroups + Save
//   4. Validate: invalid QuestionType                -> early-return error
//   5. Validate: empty Stem                          -> add error
//   6. Validate: invalid Difficulty                  -> add error
//   7. Validate: invalid Status                      -> add error
//   8. Validate: invalid QuestionPurpose             -> add error
//   9. Validate: chapter doesn't exist               -> add error
//  10. Validate: empty Answers                       -> early-return error
//  11. Validate FillBlank: empty Frame               -> add error
//  12. Validate FillBlank: InputTypeId <= 0          -> add error
//  13. Validate MCQ: Answers.Count < 2               -> add error
//  14. Validate MCQ: no IsCorrect                    -> add error
//  15. Validate: Sum points != 100                   -> add error
//  16. ValidatePointRange: Point < 0                 -> add error
//  17. ValidatePointRange: Point > 100               -> add error
public class F27_CreateQuestionsAsync_Tests
{
    private readonly Mock<IQuestionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly QuestionService _service;

    public F27_CreateQuestionsAsync_Tests()
    {
        _service = new QuestionService(_repoMock.Object, new Mock<ILogger<QuestionService>>().Object);
    }

    private static QuestionDto ValidMcq() => new()
    {
        QuestionType = QuestionType.Mcq,
        Stem = "1+1=?",
        ChapterId = 1,
        Difficulty = 2,
        Status = QuestionStatus.Active,
        QuestionPurpose = Backend.Constants.QuestionPurpose.Exam,
        Answers = new List<AnswerDto>
        {
            new() { Content = "A", CorrectAnswer = "A", IsCorrect = true, Point = 50 },
            new() { Content = "B", CorrectAnswer = "B", IsCorrect = false, Point = 50 }
        }
    };

    private static QuestionDto ValidFillBlank() => new()
    {
        QuestionType = QuestionType.FillBlank,
        Stem = "Fill the blank",
        Frame = "X is placeholder[1]",
        ChapterId = 1,
        Difficulty = 2,
        Status = QuestionStatus.Active,
        QuestionPurpose = Backend.Constants.QuestionPurpose.Practice,
        Answers = new List<AnswerDto>
        {
            new() { Content = "placeholder[1]", CorrectAnswer = "ans", InputTypeId = 1, BlankIndex = 1, Point = 100 }
        }
    };

    private void SetupChapterExists(int chapterId, bool exists = true)
        => _repoMock.Setup(r => r.ChapterExistsAsync(chapterId)).ReturnsAsync(exists);

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID01 - MCQ valid -> tạo thành công")]
    [TestType("N")]
    public async Task CreateQuestionsAsync_UTCID01_McqValid_ShouldCreate()
    {
        SetupChapterExists(1);
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => qs.Select((q, i) => { q.QuestionId = i + 1; return q; }).ToList());
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateQuestionsAsync(100, new List<QuestionDto> { ValidMcq() });

        Assert.Single(result);
        Assert.Equal(1, result[0].QuestionId);
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID02 - FillBlank valid -> tạo thành công + HandleBlankGroups (no groups)")]
    [TestType("N")]
    public async Task CreateQuestionsAsync_UTCID02_FillBlankValid_ShouldCreate()
    {
        SetupChapterExists(1);
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => qs.Select((q, i) => { q.QuestionId = i + 10; return q; }).ToList());
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateQuestionsAsync(100, new List<QuestionDto> { ValidFillBlank() });

        Assert.Single(result);
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID03 - request null -> QuestionValidationException")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID03_NullRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, null!));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID04 - request rỗng -> QuestionValidationException (boundary)")]
    [TestType("B")]
    public async Task CreateQuestionsAsync_UTCID04_EmptyRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto>()));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID05 - QuestionType không hợp lệ -> early-return error")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID05_InvalidType_ShouldThrow()
    {
        var q = ValidMcq();
        q.QuestionType = "BadType";

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("Loại câu hỏi không hợp lệ"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID06 - Stem empty + Difficulty/Status/Purpose invalid + chapter not exists -> nhiều lỗi gộp")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID06_MultipleInvalid_ShouldCollectErrors()
    {
        var q = ValidMcq();
        q.Stem = "   ";
        q.Difficulty = 99;
        q.Status = "BAD";
        q.QuestionPurpose = 9;
        q.ChapterId = 99999;
        SetupChapterExists(99999, false);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));

        Assert.Contains(ex.Errors, e => e.Contains("Đề bài không được để trống"));
        Assert.Contains(ex.Errors, e => e.Contains("Mức độ phải"));
        Assert.Contains(ex.Errors, e => e.Contains("Trạng thái không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("Mục đích câu hỏi không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("Chương không tồn tại"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID07 - Answers rỗng -> early-return 'Phải có ít nhất 1 đáp án'")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID07_NoAnswers_ShouldThrow()
    {
        var q = ValidMcq();
        q.Answers = new List<AnswerDto>();
        SetupChapterExists(1);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("Phải có ít nhất 1 đáp án"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID08 - MCQ chỉ 1 answer -> 'ít nhất 2 lựa chọn'")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID08_McqOneAnswer_ShouldThrow()
    {
        var q = ValidMcq();
        q.Answers = new List<AnswerDto> { new() { Content = "A", CorrectAnswer = "A", IsCorrect = true, Point = 100 } };
        SetupChapterExists(1);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("ít nhất 2 lựa chọn"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID09 - MCQ không có IsCorrect -> 'ít nhất 1 đáp án đúng'")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID09_McqNoCorrect_ShouldThrow()
    {
        var q = ValidMcq();
        q.Answers = new List<AnswerDto>
        {
            new() { Content = "A", CorrectAnswer = "A", IsCorrect = false, Point = 50 },
            new() { Content = "B", CorrectAnswer = "B", IsCorrect = false, Point = 50 }
        };
        SetupChapterExists(1);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("ít nhất 1 đáp án đúng"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID10 - FillBlank: Frame empty + InputTypeId 0 -> nhiều lỗi")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID10_FillBlankInvalid_ShouldThrow()
    {
        var q = ValidFillBlank();
        q.Frame = "  ";
        q.Answers[0].InputTypeId = 0;
        SetupChapterExists(1);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("Khung trả lời không được để trống"));
        Assert.Contains(ex.Errors, e => e.Contains("Thiếu loại giới hạn nhập liệu"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID11 - Tổng điểm != 100 -> 'Tổng điểm phải bằng 100%'")]
    [TestType("A")]
    public async Task CreateQuestionsAsync_UTCID11_PointSumWrong_ShouldThrow()
    {
        var q = ValidMcq();
        q.Answers[0].Point = 30;
        q.Answers[1].Point = 30;
        SetupChapterExists(1);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("Tổng điểm phải bằng 100%"));
    }

    [Fact(DisplayName = "CreateQuestionsAsync - UTCID12 - Point < 0 -> ValidatePointRange error (boundary)")]
    [TestType("B")]
    public async Task CreateQuestionsAsync_UTCID12_PointBelowMin_ShouldThrow()
    {
        var q = ValidMcq();
        q.Answers[0].Point = -10;
        q.Answers[1].Point = 110; // Force > 100 too — both ValidatePointRange branches in one shot
        SetupChapterExists(1);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.CreateQuestionsAsync(100, new List<QuestionDto> { q }));
        Assert.Contains(ex.Errors, e => e.Contains("Điểm phải từ"));
    }
}
