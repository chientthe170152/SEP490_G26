using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.AssignExamUnitTest;

// F24 - ApproveExamAsync
// Source: AssignExamService.cs:406-420
// Branches:
//   1. GetExamByIdAsync returns null              -> throw KeyNotFoundException("Exam not found.")
//   2. GetExamByIdAsync returns exam              -> proceed with status update + question/blueprint update + Hangfire schedule
//   3. UpdateExamStatusAsync (or any subsequent repo call) throws -> propagate
public class F24_ApproveExamAsync_Tests
{
    private readonly Mock<IAssignExamRepository> _repoMock;
    private readonly AssignExamService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public F24_ApproveExamAsync_Tests()
    {
        HangfireTestSetup.EnsureInitialized();
        _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
        _service = new AssignExamService(_repoMock.Object);
    }

    private static Exam BuildExam(int examId = 1, DateTime? openAt = null, DateTime? closeAt = null)
        => new()
        {
            ExamId = examId,
            Title = "Sample Exam",
            OpenAt = openAt,
            CloseAt = closeAt,
            Status = ExamStatus.Ready,
            ConcurrencyStamp = Array.Empty<byte>()
        };

    private void SetupSuccessPath(int examId, Exam exam, IEnumerable<int>? questionIds = null)
    {
        questionIds ??= new List<int> { 10, 11, 12 };
        _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct)).ReturnsAsync(exam);
        _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetAllQuestionIdsInExamAsync(examId, _ct)).ReturnsAsync(questionIds.ToList());
        _repoMock.Setup(r => r.UpdateQuestionsToInprogressAsync(It.IsAny<IEnumerable<int>>(), _ct)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateBlueprintToInprogressAsync(examId, _ct)).Returns(Task.CompletedTask);
    }

    [Fact(DisplayName = "ApproveExamAsync - UTCID01 - Exam tồn tại + dates đã past -> publish thành công, không lập Hangfire")]
    [TestType("N")]
    public async Task ApproveExamAsync_UTCID01_ValidExamPastDates_ShouldPublishSuccessfully()
    {
        const int examId = 1;
        var past = DateTime.UtcNow.AddDays(-1);
        var exam = BuildExam(examId, openAt: past, closeAt: past.AddHours(1));
        SetupSuccessPath(examId, exam);

        await _service.ApproveExamAsync(examId, _ct);

        _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
        _repoMock.Verify(r => r.UpdateQuestionsToInprogressAsync(It.IsAny<IEnumerable<int>>(), _ct), Times.Once);
        _repoMock.Verify(r => r.UpdateBlueprintToInprogressAsync(examId, _ct), Times.Once);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ApproveExamAsync - UTCID02 - GetExamByIdAsync trả null -> KeyNotFoundException")]
    [TestType("A")]
    public async Task ApproveExamAsync_UTCID02_ExamNotFound_ShouldThrowKeyNotFound()
    {
        const int examId = 999;
        _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct)).ReturnsAsync((Exam?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ApproveExamAsync(examId, _ct));

        Assert.Equal("Exam not found.", ex.Message);
        _repoMock.Verify(r => r.UpdateExamStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ApproveExamAsync - UTCID03 - examId = 0 nhưng exam tồn tại -> vẫn forward repo")]
    [TestType("B")]
    public async Task ApproveExamAsync_UTCID03_ExamIdZero_ShouldStillProcess()
    {
        const int examId = 0;
        var past = DateTime.UtcNow.AddDays(-1);
        var exam = BuildExam(examId, openAt: past, closeAt: past.AddHours(1));
        SetupSuccessPath(examId, exam);

        await _service.ApproveExamAsync(examId, _ct);

        _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ApproveExamAsync - UTCID04 - UpdateExamStatusAsync ném exception -> propagate")]
    [TestType("A")]
    public async Task ApproveExamAsync_UTCID04_RepositoryThrows_ShouldPropagate()
    {
        const int examId = 2;
        var exam = BuildExam(examId, openAt: DateTime.UtcNow.AddDays(-1), closeAt: DateTime.UtcNow.AddDays(-1));
        _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct)).ReturnsAsync(exam);
        _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
                 .ThrowsAsync(new InvalidOperationException("Database error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveExamAsync(examId, _ct));

        Assert.Equal("Database error", ex.Message);
        _repoMock.Verify(r => r.GetAllQuestionIdsInExamAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "ApproveExamAsync - UTCID05 - Dates trong tương lai -> Hangfire schedule không lỗi")]
    [TestType("N")]
    public async Task ApproveExamAsync_UTCID05_FutureDates_ShouldScheduleHangfireJobs()
    {
        const int examId = 3;
        var future = DateTime.UtcNow.AddHours(2);
        var exam = BuildExam(examId, openAt: future, closeAt: future.AddHours(2));
        SetupSuccessPath(examId, exam);

        await _service.ApproveExamAsync(examId, _ct);

        _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
        _repoMock.VerifyAll();
    }
}
