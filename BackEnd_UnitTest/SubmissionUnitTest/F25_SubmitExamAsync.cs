using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.SubmissionUnitTest;

// F25 - SubmitExamAsync
// Source: SubmissionService.cs:18-117
// Branches:
//   1. submission == null                                  -> throw KeyNotFoundException(SubmissionNotFound)
//   2. submission.Status != InProgress                     -> throw InvalidOperationException(SubmissionAlreadySubmitted)
//   3. submission.Paper?.Exam == null                      -> throw InvalidOperationException("Submission has no associated exam.")
//   4. now > deadline (duration exceeded)                  -> throw InvalidOperationException(SubmissionLateNotAllowed)
//   5. exam.CloseAt.HasValue && now > exam.CloseAt         -> same throw (alt path)
//   6. !validIds.Contains(answer.QuestionAnswerId)         -> throw ArgumentException(InvalidQuestionAnswer)
//   7. existing answer found -> update Response (FillInBlank)
//   8. new answer -> add to toAdd
//   9. answer in existing but not in incoming -> add to toRemove
//  10. toRemove.Count > 0 -> RemoveStudentAnswers called
//  11. toAdd.Count > 0    -> AddStudentAnswers called
//  12. request.Submit == true  -> Status = 2 (Submitted)
//  13. request.Submit == false -> Status = 1 (InProgress, save draft)
public class F25_SubmitExamAsync_Tests
{
    private readonly Mock<ISubmissionRepository> _repoMock;
    private readonly SubmissionService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public F25_SubmitExamAsync_Tests()
    {
        _repoMock = new Mock<ISubmissionRepository>(MockBehavior.Strict);
        _service = new SubmissionService(_repoMock.Object);
    }

    private static Submission BuildActiveSubmission(
        int submissionId = 100,
        int paperId = 10,
        int examId = 1,
        int duration = 60,
        DateTime? createdAt = null,
        int? closeAtMinutesFromNow = null,
        IEnumerable<StudentAnswer>? existingAnswers = null)
    {
        var paper = new Paper
        {
            PaperId = paperId,
            ExamId = examId,
            Code = 1,
            Exam = new Exam
            {
                ExamId = examId,
                Title = "Sample",
                Duration = duration,
                MaxAttempts = 3,
                Status = ExamStatus.Published,
                CloseAt = closeAtMinutesFromNow.HasValue ? DateTime.UtcNow.AddMinutes(closeAtMinutesFromNow.Value) : null,
                ConcurrencyStamp = Array.Empty<byte>()
            }
        };
        return new Submission
        {
            SubmissionId = submissionId,
            StudentId = 1,
            PaperId = paperId,
            Status = SubmissionStatus.InProgress,
            CreatedAtUtc = createdAt ?? DateTime.UtcNow.AddMinutes(-10),
            UpdatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Array.Empty<byte>(),
            Paper = paper,
            StudentAnswers = (existingAnswers ?? Enumerable.Empty<StudentAnswer>()).ToList()
        };
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID01 - Submit hợp lệ trong giờ -> Status=Submitted, IsLate=false")]
    [TestType("N")]
    public async Task SubmitExamAsync_UTCID01_ValidSubmit_ShouldFinalize()
    {
        var sub = BuildActiveSubmission();
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);
        _repoMock.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, _ct)).ReturnsAsync(new HashSet<int> { 100, 101 });
        _repoMock.Setup(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
        _repoMock.Setup(r => r.SaveChangesAsync(_ct)).Returns(Task.CompletedTask);

        var req = new SubmitExamRequest
        {
            ExamId = 1,
            Submit = true,
            StudentAnswers = new List<StudentAnswerDto>
            {
                new() { QuestionAnswerId = 100, Response = null },
                new() { QuestionAnswerId = 101, Response = null }
            }
        };

        var res = await _service.SubmitExamAsync(1, req, _ct);

        Assert.Equal(100, res.SubmissionId);
        Assert.False(res.IsLate);
        Assert.Equal(SubmissionStatus.Submitted, sub.Status);
        _repoMock.Verify(r => r.AddStudentAnswers(It.Is<IEnumerable<StudentAnswer>>(a => a.Count() == 2)), Times.Once);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID02 - Save draft (Submit=false) -> Status=InProgress")]
    [TestType("N")]
    public async Task SubmitExamAsync_UTCID02_SaveDraft_ShouldKeepInProgress()
    {
        var sub = BuildActiveSubmission();
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);
        _repoMock.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, _ct)).ReturnsAsync(new HashSet<int> { 100 });
        _repoMock.Setup(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
        _repoMock.Setup(r => r.SaveChangesAsync(_ct)).Returns(Task.CompletedTask);

        var req = new SubmitExamRequest
        {
            ExamId = 1,
            Submit = false,
            StudentAnswers = new List<StudentAnswerDto> { new() { QuestionAnswerId = 100 } }
        };

        await _service.SubmitExamAsync(1, req, _ct);

        Assert.Equal(SubmissionStatus.InProgress, sub.Status);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID03 - Update existing FillInBlank answer + remove orphan")]
    [TestType("N")]
    public async Task SubmitExamAsync_UTCID03_UpdateAndRemoveAnswers()
    {
        var existingKept = new StudentAnswer { StudentAnswerId = 1, QuestionAnswerId = 100, Response = "old", ConcurrencyStamp = Array.Empty<byte>() };
        var existingOrphan = new StudentAnswer { StudentAnswerId = 2, QuestionAnswerId = 999, Response = "stale", ConcurrencyStamp = Array.Empty<byte>() };
        var sub = BuildActiveSubmission(existingAnswers: new[] { existingKept, existingOrphan });
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);
        _repoMock.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, _ct)).ReturnsAsync(new HashSet<int> { 100, 101 });
        _repoMock.Setup(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
        _repoMock.Setup(r => r.RemoveStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
        _repoMock.Setup(r => r.SaveChangesAsync(_ct)).Returns(Task.CompletedTask);

        var req = new SubmitExamRequest
        {
            ExamId = 1,
            Submit = false,
            StudentAnswers = new List<StudentAnswerDto>
            {
                new() { QuestionAnswerId = 100, Response = "new" },
                new() { QuestionAnswerId = 101, Response = "added" }
            }
        };

        await _service.SubmitExamAsync(1, req, _ct);

        Assert.Equal("new", existingKept.Response);
        _repoMock.Verify(r => r.RemoveStudentAnswers(It.Is<IEnumerable<StudentAnswer>>(a => a.Any(x => x.StudentAnswerId == 2))), Times.Once);
        _repoMock.Verify(r => r.AddStudentAnswers(It.Is<IEnumerable<StudentAnswer>>(a => a.Any(x => x.QuestionAnswerId == 101))), Times.Once);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID04 - Submission không tồn tại -> KeyNotFoundException SubmissionNotFound")]
    [TestType("A")]
    public async Task SubmitExamAsync_UTCID04_SubmissionNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync((Submission?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SubmitExamAsync(1, new SubmitExamRequest { ExamId = 1 }, _ct));

        Assert.Equal(ErrorMessages.SubmissionNotFound, ex.Message);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID05 - Submission đã Submitted -> InvalidOperationException AlreadySubmitted")]
    [TestType("A")]
    public async Task SubmitExamAsync_UTCID05_AlreadySubmitted_ShouldThrow()
    {
        var sub = BuildActiveSubmission();
        sub.Status = SubmissionStatus.Submitted;
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitExamAsync(1, new SubmitExamRequest { ExamId = 1 }, _ct));

        Assert.Equal(ErrorMessages.SubmissionAlreadySubmitted, ex.Message);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID06 - Paper.Exam = null -> InvalidOperationException 'Submission has no associated exam.'")]
    [TestType("A")]
    public async Task SubmitExamAsync_UTCID06_NoExam_ShouldThrow()
    {
        var sub = BuildActiveSubmission();
        sub.Paper.Exam = null!;
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitExamAsync(1, new SubmitExamRequest { ExamId = 1 }, _ct));

        Assert.Equal("Submission has no associated exam.", ex.Message);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID10 - Paper = null -> InvalidOperationException (boundary cho ?.)")]
    [TestType("B")]
    public async Task SubmitExamAsync_UTCID10_NoPaper_ShouldThrow()
    {
        var sub = BuildActiveSubmission();
        sub.Paper = null!;
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitExamAsync(1, new SubmitExamRequest { ExamId = 1 }, _ct));

        Assert.Equal("Submission has no associated exam.", ex.Message);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID07 - Quá deadline duration -> SubmissionLateNotAllowed")]
    [TestType("A")]
    public async Task SubmitExamAsync_UTCID07_OverDuration_ShouldThrow()
    {
        var sub = BuildActiveSubmission(duration: 30, createdAt: DateTime.UtcNow.AddMinutes(-60));
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitExamAsync(1, new SubmitExamRequest { ExamId = 1 }, _ct));

        Assert.Equal(ErrorMessages.SubmissionLateNotAllowed, ex.Message);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID08 - Vượt CloseAt của exam -> SubmissionLateNotAllowed")]
    [TestType("A")]
    public async Task SubmitExamAsync_UTCID08_OverCloseAt_ShouldThrow()
    {
        var sub = BuildActiveSubmission(duration: 600, createdAt: DateTime.UtcNow.AddMinutes(-1), closeAtMinutesFromNow: -5);
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitExamAsync(1, new SubmitExamRequest { ExamId = 1 }, _ct));

        Assert.Equal(ErrorMessages.SubmissionLateNotAllowed, ex.Message);
    }

    [Fact(DisplayName = "SubmitExamAsync - UTCID09 - QuestionAnswerId không thuộc Paper -> ArgumentException InvalidQuestionAnswer")]
    [TestType("A")]
    public async Task SubmitExamAsync_UTCID09_InvalidQuestionAnswer_ShouldThrow()
    {
        var sub = BuildActiveSubmission();
        _repoMock.Setup(r => r.GetActiveSubmissionAsync(1, 1, _ct)).ReturnsAsync(sub);
        _repoMock.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, _ct)).ReturnsAsync(new HashSet<int> { 100 });

        var req = new SubmitExamRequest
        {
            ExamId = 1,
            Submit = false,
            StudentAnswers = new List<StudentAnswerDto> { new() { QuestionAnswerId = 999 } }
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SubmitExamAsync(1, req, _ct));

        Assert.Equal(ErrorMessages.InvalidQuestionAnswer, ex.Message);
    }
}
