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

// F28 - UpdateQuestionAsync
// Source: QuestionService.cs:65-96
// Branches:
//   1. existing == null                                              -> throw KeyNotFoundException
//   2. existing.CreatedByUserId != userId                            -> throw KeyNotFoundException
//   3. validation errors                                             -> throw QuestionValidationException
//   4. existing.Status == Inprogress -> archive+clone path
//   5. existing.Status == Archive    -> archive+clone path
//   6. isUsed == true                -> archive+clone path
//   7. else (Active + not used)      -> in-place update
//   8. archive+clone + request.Status == Inprogress  -> reset to Active
//   9. archive+clone + request.Status == Archive     -> reset to Active
//  10. in-place + toRemove.Any() true                -> DeleteQuestionAnswersAsync
//  11. in-place + toRemove.Any() false               -> skip delete
public class F28_UpdateQuestionAsync_Tests
{
    private readonly Mock<IQuestionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly QuestionService _service;

    public F28_UpdateQuestionAsync_Tests()
    {
        _service = new QuestionService(_repoMock.Object, new Mock<ILogger<QuestionService>>().Object);
    }

    private static Question MakeExisting(string status = QuestionStatus.Active, int qid = 5, int ownerId = 100)
        => new()
        {
            QuestionId = qid,
            CreatedByUserId = ownerId,
            QuestionType = QuestionType.Mcq,
            QuestionContent = "{\"stem\":\"Old\"}",
            ChapterId = 1,
            Difficulty = 2,
            UpdatedAtUtc = DateTime.UtcNow,
            Status = status,
            ConcurrencyStamp = Array.Empty<byte>(),
            QuestionAnswers = new List<QuestionAnswer>
            {
                new() { QuestionAnswerId = 50, QuestionId = qid, Content = "A", CorrectAnswer = "A", IsCorrect = true, Point = 50, ConcurrencyStamp = Array.Empty<byte>() },
                new() { QuestionAnswerId = 51, QuestionId = qid, Content = "B", CorrectAnswer = "B", IsCorrect = false, Point = 50, ConcurrencyStamp = Array.Empty<byte>() }
            }
        };

    private static QuestionDto ValidRequest(string status = QuestionStatus.Active) => new()
    {
        QuestionType = QuestionType.Mcq,
        Stem = "1+1=?",
        ChapterId = 1,
        Difficulty = 2,
        Status = status,
        QuestionPurpose = Backend.Constants.QuestionPurpose.Exam,
        Answers = new List<AnswerDto>
        {
            new() { AnswerId = 50, Content = "A", CorrectAnswer = "A", IsCorrect = true, Point = 50 },
            new() { AnswerId = 51, Content = "B-updated", CorrectAnswer = "B", IsCorrect = false, Point = 50 }
        }
    };

    private void SetupChapterExists() => _repoMock.Setup(r => r.ChapterExistsAsync(1)).ReturnsAsync(true);

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID01 - In-place update (Active + chưa dùng + không xóa answer) -> success")]
    [TestType("N")]
    public async Task UpdateQuestionAsync_UTCID01_InPlaceUpdate_ShouldSucceed()
    {
        var existing = MakeExisting();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await _service.UpdateQuestionAsync(5, 100, ValidRequest());

        Assert.Equal(5, dto.QuestionId);
        // No clone — original question updated in place; status stays Active
        Assert.Equal(QuestionStatus.Active, existing.Status);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID02 - Archive+clone (existing.Status = Inprogress)")]
    [TestType("N")]
    public async Task UpdateQuestionAsync_UTCID02_ExistingInprogress_ShouldArchiveAndClone()
    {
        var existing = MakeExisting(status: QuestionStatus.Inprogress);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => { qs[0].QuestionId = 999; return qs; });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await _service.UpdateQuestionAsync(5, 100, ValidRequest());

        Assert.Equal(999, dto.QuestionId);
        Assert.Equal(QuestionStatus.Archive, existing.Status);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID03 - Archive+clone (existing.Status = Archive)")]
    [TestType("N")]
    public async Task UpdateQuestionAsync_UTCID03_ExistingArchive_ShouldClone()
    {
        var existing = MakeExisting(status: QuestionStatus.Archive);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => { qs[0].QuestionId = 1000; return qs; });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await _service.UpdateQuestionAsync(5, 100, ValidRequest());

        Assert.Equal(1000, dto.QuestionId);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID04 - Archive+clone (Active nhưng đã được dùng)")]
    [TestType("B")]
    public async Task UpdateQuestionAsync_UTCID04_UsedQuestion_ShouldClone()
    {
        var existing = MakeExisting();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(true);
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => { qs[0].QuestionId = 1001; return qs; });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await _service.UpdateQuestionAsync(5, 100, ValidRequest());

        Assert.Equal(1001, dto.QuestionId);
        Assert.Equal(QuestionStatus.Archive, existing.Status);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID05 - Clone path + request.Status = Inprogress -> reset Active")]
    [TestType("B")]
    public async Task UpdateQuestionAsync_UTCID05_CloneInprogressRequest_ShouldResetToActive()
    {
        var existing = MakeExisting(status: QuestionStatus.Inprogress);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(false);
        Question? captured = null;
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => { captured = qs[0]; qs[0].QuestionId = 1002; return qs; });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var req = ValidRequest(status: QuestionStatus.Inprogress);
        await _service.UpdateQuestionAsync(5, 100, req);

        Assert.NotNull(captured);
        Assert.Equal(QuestionStatus.Active, captured!.Status); // service reset Inprogress → Active
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID06 - Clone path + request.Status = Archive -> reset Active")]
    [TestType("B")]
    public async Task UpdateQuestionAsync_UTCID06_CloneArchiveRequest_ShouldResetToActive()
    {
        var existing = MakeExisting(status: QuestionStatus.Inprogress);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(false);
        Question? captured = null;
        _repoMock.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                 .ReturnsAsync((List<Question> qs) => { captured = qs[0]; qs[0].QuestionId = 1003; return qs; });
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var req = ValidRequest(status: QuestionStatus.Archive);
        await _service.UpdateQuestionAsync(5, 100, req);

        Assert.NotNull(captured);
        Assert.Equal(QuestionStatus.Active, captured!.Status);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID07 - existing == null -> KeyNotFoundException")]
    [TestType("A")]
    public async Task UpdateQuestionAsync_UTCID07_NotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(99)).ReturnsAsync((Question?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateQuestionAsync(99, 100, ValidRequest()));
        Assert.Equal("Không tìm thấy câu hỏi hoặc bạn không có quyền sửa.", ex.Message);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID08 - userId không phải owner -> KeyNotFoundException")]
    [TestType("A")]
    public async Task UpdateQuestionAsync_UTCID08_NotOwner_ShouldThrow()
    {
        var existing = MakeExisting();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateQuestionAsync(5, 999, ValidRequest()));
        Assert.Equal("Không tìm thấy câu hỏi hoặc bạn không có quyền sửa.", ex.Message);
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID09 - Validation errors -> QuestionValidationException")]
    [TestType("A")]
    public async Task UpdateQuestionAsync_UTCID09_InvalidPayload_ShouldThrow()
    {
        var existing = MakeExisting();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        var req = ValidRequest();
        req.Stem = "   "; // empty stem

        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.UpdateQuestionAsync(5, 100, req));
    }

    [Fact(DisplayName = "UpdateQuestionAsync - UTCID10 - In-place update + toRemove.Any() true -> DeleteQuestionAnswersAsync")]
    [TestType("B")]
    public async Task UpdateQuestionAsync_UTCID10_InPlaceWithRemove_ShouldDeleteAnswers()
    {
        var existing = MakeExisting();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(existing);
        SetupChapterExists();
        _repoMock.Setup(r => r.IsQuestionUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.DeleteQuestionAnswersAsync(It.IsAny<IEnumerable<QuestionAnswer>>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Request keeps only AnswerId=50 plus a fresh new answer; AnswerId=51 should be removed.
        var req = new QuestionDto
        {
            QuestionType = QuestionType.Mcq,
            Stem = "1+1=?",
            ChapterId = 1,
            Difficulty = 2,
            Status = QuestionStatus.Active,
            QuestionPurpose = Backend.Constants.QuestionPurpose.Exam,
            Answers = new List<AnswerDto>
            {
                new() { AnswerId = 50, Content = "A", CorrectAnswer = "A", IsCorrect = true, Point = 50 },
                new() { AnswerId = null, Content = "C-new", CorrectAnswer = "C", IsCorrect = false, Point = 50 }
            }
        };

        await _service.UpdateQuestionAsync(5, 100, req);

        _repoMock.Verify(r => r.DeleteQuestionAnswersAsync(
            It.Is<IEnumerable<QuestionAnswer>>(a => a.Any(x => x.QuestionAnswerId == 51))),
            Times.Once);
    }
}
