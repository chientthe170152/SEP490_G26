using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Backend.Jobs;
using Moq;
using Xunit;

namespace Backend_UnitTest.AssignExamUnitTest
{
    public class CancelRestoreDeleteUpdateExamTests
    {
        private readonly Mock<IAssignExamRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IExamStatusScheduler> _mockScheduler;
        private readonly AssignExamService _service;

        public CancelRestoreDeleteUpdateExamTests()
        {
            _mockRepo = new Mock<IAssignExamRepository>(MockBehavior.Strict);
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            _mockScheduler = new Mock<IExamStatusScheduler>(MockBehavior.Strict);
            _service = new AssignExamService(_mockRepo.Object, _mockCurrentUser.Object, _mockScheduler.Object, TimeProvider.System);
        }

        // ════════════════════════════════════════════════════════
        //  CancelExamAsync
        // ════════════════════════════════════════════════════════

        [Fact(DisplayName = "CancelExamAsync - UTCID01 - Exam không tồn tại → ExamNotFound")]
        public async Task CancelExamAsync_UTCID01_ExamNotFound_ShouldReturnExamNotFound()
        {
            _mockRepo.Setup(r => r.GetExamByIdAsync(999, default)).ReturnsAsync((Exam?)null);

            var result = await _service.CancelExamAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.ExamNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "CancelExamAsync - UTCID02 - Status không phải Published → InvalidStatusForCancel")]
        public async Task CancelExamAsync_UTCID02_InvalidStatus_ShouldReturnInvalidStatusForCancel()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Ready };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.CancelExamAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.InvalidStatusForCancel.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "CancelExamAsync - UTCID03 - Published + có submissions → ExamAlreadyStarted, set InProgress")]
        public async Task CancelExamAsync_UTCID03_HasSubmissions_ShouldReturnExamAlreadyStartedAndSetInProgress()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Published, ConcurrencyStamp = Array.Empty<byte>() };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.HasSubmissionsForExamAsync(1, default)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.UpdateExamStatusAsync(1, ExamStatus.InProgress, default)).Returns(Task.CompletedTask);

            var result = await _service.CancelExamAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.ExamAlreadyStarted.Code, result.Error.Code);
            _mockRepo.Verify(r => r.UpdateExamStatusAsync(1, ExamStatus.InProgress, default), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "CancelExamAsync - UTCID04 - Published + no submissions → Success, set Cancelled")]
        public async Task CancelExamAsync_UTCID04_NoSubmissions_ShouldSuccessAndSetCancelled()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Published };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.HasSubmissionsForExamAsync(1, default)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UpdateExamStatusAsync(1, ExamStatus.Cancelled, default)).Returns(Task.CompletedTask);
            _mockScheduler.Setup(s => s.CancelExamJobsAsync(1, default)).Returns(Task.CompletedTask);

            var result = await _service.CancelExamAsync(1);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.UpdateExamStatusAsync(1, ExamStatus.Cancelled, default), Times.Once);
            _mockScheduler.Verify(s => s.CancelExamJobsAsync(1, default), Times.Once);
            _mockRepo.VerifyAll();
        }

        // ════════════════════════════════════════════════════════
        //  RestoreExamAsync
        // ════════════════════════════════════════════════════════

        [Fact(DisplayName = "RestoreExamAsync - UTCID01 - Exam không tồn tại → ExamNotFound")]
        public async Task RestoreExamAsync_UTCID01_ExamNotFound_ShouldReturnExamNotFound()
        {
            _mockRepo.Setup(r => r.GetExamByIdAsync(999, default)).ReturnsAsync((Exam?)null);

            var result = await _service.RestoreExamAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.ExamNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "RestoreExamAsync - UTCID02 - Status không phải Cancelled → InvalidStatusForRestore")]
        public async Task RestoreExamAsync_UTCID02_InvalidStatus_ShouldReturnInvalidStatusForRestore()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Published };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.RestoreExamAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.InvalidStatusForRestore.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "RestoreExamAsync - UTCID03 - OpenAt đã qua → OpenTimePassed")]
        public async Task RestoreExamAsync_UTCID03_OpenAtPassed_ShouldReturnOpenTimePassed()
        {
            var pastTime = DateTime.UtcNow.AddDays(-1);
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Cancelled, OpenAt = pastTime };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.RestoreExamAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.OpenTimePassed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "RestoreExamAsync - UTCID04 - Window < Duration → DurationMismatch")]
        public async Task RestoreExamAsync_UTCID04_WindowTooSmall_ShouldReturnDurationMismatch()
        {
            var futureTime = DateTime.UtcNow.AddDays(1);
            var exam = new Exam
            {
                ExamId = 1,
                Status = ExamStatus.Cancelled,
                OpenAt = futureTime,
                CloseAt = futureTime.AddMinutes(30),
                Duration = 60
            };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.RestoreExamAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.DurationMismatch.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "RestoreExamAsync - UTCID05 - Valid restore → Success, set Published")]
        public async Task RestoreExamAsync_UTCID05_ValidRestore_ShouldSuccessAndSetPublished()
        {
            var futureTime = DateTime.UtcNow.AddDays(1);
            var exam = new Exam
            {
                ExamId = 1,
                Status = ExamStatus.Cancelled,
                OpenAt = futureTime,
                CloseAt = futureTime.AddMinutes(120),
                Duration = 60
            };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.UpdateExamStatusAsync(1, ExamStatus.Published, default)).Returns(Task.CompletedTask);
            _mockScheduler.Setup(s => s.ScheduleExamJobsAsync(1, exam.OpenAt, exam.CloseAt, default)).Returns(Task.CompletedTask);

            var result = await _service.RestoreExamAsync(1);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.UpdateExamStatusAsync(1, ExamStatus.Published, default), Times.Once);
            _mockScheduler.Verify(s => s.ScheduleExamJobsAsync(1, exam.OpenAt, exam.CloseAt, default), Times.Once);
            _mockRepo.VerifyAll();
        }

        // ════════════════════════════════════════════════════════
        //  DeleteExamAsync
        // ════════════════════════════════════════════════════════

        [Fact(DisplayName = "DeleteExamAsync - UTCID01 - Exam không tồn tại → ExamNotFound")]
        public async Task DeleteExamAsync_UTCID01_ExamNotFound_ShouldReturnExamNotFound()
        {
            _mockRepo.Setup(r => r.GetExamByIdAsync(999, default)).ReturnsAsync((Exam?)null);

            var result = await _service.DeleteExamAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.ExamNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteExamAsync - UTCID02 - Status không phải Ready/Cancelled → InvalidStatusForDelete")]
        public async Task DeleteExamAsync_UTCID02_InvalidStatus_ShouldReturnInvalidStatusForDelete()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Published };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.DeleteExamAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.InvalidStatusForDelete.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteExamAsync - UTCID03 - Status Ready → Success, hard delete")]
        public async Task DeleteExamAsync_UTCID03_ReadyStatus_ShouldSuccessAndDelete()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Ready };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.HardDeleteExamAsync(1, default)).Returns(Task.CompletedTask);

            var result = await _service.DeleteExamAsync(1);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.HardDeleteExamAsync(1, default), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteExamAsync - UTCID04 - Status Cancelled → Success, hard delete")]
        public async Task DeleteExamAsync_UTCID04_CancelledStatus_ShouldSuccessAndDelete()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Cancelled };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.HardDeleteExamAsync(1, default)).Returns(Task.CompletedTask);

            var result = await _service.DeleteExamAsync(1);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.HardDeleteExamAsync(1, default), Times.Once);
            _mockRepo.VerifyAll();
        }

        // ════════════════════════════════════════════════════════
        //  UpdateExamInfoAsync
        // ════════════════════════════════════════════════════════

        [Fact(DisplayName = "UpdateExamInfoAsync - UTCID01 - Exam không tồn tại → ExamNotFound")]
        public async Task UpdateExamInfoAsync_UTCID01_ExamNotFound_ShouldReturnExamNotFound()
        {
            var request = new UpdateExamInfoRequest { Title = "New Title" };
            _mockRepo.Setup(r => r.GetExamByIdAsync(999, default)).ReturnsAsync((Exam?)null);

            var result = await _service.UpdateExamInfoAsync(999, request);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.ExamNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateExamInfoAsync - UTCID02 - Invalid time window → InvalidTimeWindow")]
        public async Task UpdateExamInfoAsync_UTCID02_InvalidTimeWindow_ShouldReturnInvalidTimeWindow()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Ready };
            var openAt = DateTime.UtcNow.AddDays(1);
            var request = new UpdateExamInfoRequest
            {
                Title = "New",
                VisibleFrom = openAt.AddDays(1),  // VisibleFrom > OpenAt
                OpenAt = openAt,
                CloseAt = openAt.AddMinutes(60)
            };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.UpdateExamInfoAsync(1, request);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.InvalidTimeWindow.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateExamInfoAsync - UTCID03 - Status không phải Ready/Cancelled → InvalidStatusForUpdate")]
        public async Task UpdateExamInfoAsync_UTCID03_InvalidStatus_ShouldReturnInvalidStatusForUpdate()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Published };
            var request = new UpdateExamInfoRequest { Title = "New" };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);

            var result = await _service.UpdateExamInfoAsync(1, request);

            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.InvalidStatusForUpdate.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateExamInfoAsync - UTCID04 - Status Ready → Success, update title")]
        public async Task UpdateExamInfoAsync_UTCID04_ReadyStatus_ShouldSuccessAndUpdateTitle()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Ready };
            var futureTime = DateTime.UtcNow.AddDays(1);
            var request = new UpdateExamInfoRequest
            {
                Title = "Updated Title",
                VisibleFrom = futureTime,
                OpenAt = futureTime.AddMinutes(30),
                CloseAt = futureTime.AddMinutes(90)
            };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.UpdateExamInfoAsync(1, "Updated Title", futureTime, futureTime.AddMinutes(30), futureTime.AddMinutes(90), default)).Returns(Task.CompletedTask);

            var result = await _service.UpdateExamInfoAsync(1, request);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.UpdateExamInfoAsync(1, "Updated Title", futureTime, futureTime.AddMinutes(30), futureTime.AddMinutes(90), default), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateExamInfoAsync - UTCID05 - Status Cancelled → Success, title = null")]
        public async Task UpdateExamInfoAsync_UTCID05_CancelledStatus_ShouldSuccessWithNullTitle()
        {
            var exam = new Exam { ExamId = 1, Status = ExamStatus.Cancelled };
            var futureTime = DateTime.UtcNow.AddDays(1);
            var request = new UpdateExamInfoRequest
            {
                Title = "Updated Title",
                VisibleFrom = futureTime,
                OpenAt = futureTime.AddMinutes(30),
                CloseAt = futureTime.AddMinutes(90)
            };
            _mockRepo.Setup(r => r.GetExamByIdAsync(1, default)).ReturnsAsync(exam);
            _mockRepo.Setup(r => r.UpdateExamInfoAsync(1, null, futureTime, futureTime.AddMinutes(30), futureTime.AddMinutes(90), default)).Returns(Task.CompletedTask);

            var result = await _service.UpdateExamInfoAsync(1, request);

            Assert.True(result.IsSuccess);
            // Title should be null for Cancelled status
            _mockRepo.Verify(r => r.UpdateExamInfoAsync(1, null, futureTime, futureTime.AddMinutes(30), futureTime.AddMinutes(90), default), Times.Once);
            _mockRepo.VerifyAll();
        }
    }
}
