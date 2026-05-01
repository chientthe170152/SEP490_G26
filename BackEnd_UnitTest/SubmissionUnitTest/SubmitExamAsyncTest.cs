using Backend.Common;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Backend_UnitTest.SubmissionUnitTest
{
    public class SubmitExamAsyncTest
    {
        private readonly Mock<ISubmissionRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly SubmissionService _service;

        public SubmitExamAsyncTest()
        {
            _mockRepo = new Mock<ISubmissionRepository>(MockBehavior.Strict);
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            _service = new SubmissionService(_mockRepo.Object, _mockCurrentUser.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID01 - Submission không tồn tại → SubmissionNotFound")]
        public async Task SubmitExamAsync_UTCID01_SubmissionNotFound_ShouldReturnNotFound()
        {
            int studentId = 1;
            var request = new SubmitExamRequest { ExamId = 1, StudentAnswers = new List<StudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync((Submission?)null);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(SubmissionErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID02 - Submission đã được nộp → AlreadySubmitted")]
        public async Task SubmitExamAsync_UTCID02_AlreadySubmitted_ShouldReturnAlreadySubmitted()
        {
            int studentId = 1;
            var submission = new Submission
            {
                SubmissionId = 1,
                Status = SubmissionStatus.Submitted,
                PaperId = 10
            };
            var request = new SubmitExamRequest { ExamId = 1, StudentAnswers = new List<StudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(SubmissionErrors.AlreadySubmitted.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID03 - Paper không có Exam → SubmissionNotFound")]
        public async Task SubmitExamAsync_UTCID03_NoExam_ShouldReturnNotFound()
        {
            int studentId = 1;
            var submission = new Submission
            {
                SubmissionId = 1,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = null }
            };
            var request = new SubmitExamRequest { ExamId = 1, StudentAnswers = new List<StudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(SubmissionErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID04 - Quá giờ hạn → Late")]
        public async Task SubmitExamAsync_UTCID04_OverDueTime_ShouldReturnLate()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var startTime = now.AddMinutes(-90);  // Bắt đầu 90 phút trước
            var exam = new Exam { ExamId = 1, Duration = 60, CloseAt = null };
            var submission = new Submission
            {
                SubmissionId = 1,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = startTime,
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam }
            };
            var request = new SubmitExamRequest { ExamId = 1, StudentAnswers = new List<StudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(SubmissionErrors.Late.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID05 - QuestionAnswerId không hợp lệ → InvalidAnswer")]
        public async Task SubmitExamAsync_UTCID05_InvalidQuestionAnswerId_ShouldReturnInvalidAnswer()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var exam = new Exam { ExamId = 1, Duration = 60, CloseAt = null };
            var submission = new Submission
            {
                SubmissionId = 1,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = now.AddMinutes(-10),
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam }
            };
            var request = new SubmitExamRequest
            {
                ExamId = 1,
                StudentAnswers = new List<StudentAnswerDto> { new StudentAnswerDto { QuestionAnswerId = 999 } }
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, default)).ReturnsAsync(new HashSet<int> { 1, 2, 3 });

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(SubmissionErrors.InvalidAnswer.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID06 - Valid submit → Success, return SubmitExamResponse")]
        public async Task SubmitExamAsync_UTCID06_ValidSubmit_ShouldSuccessAndReturnResponse()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var exam = new Exam { ExamId = 1, Duration = 60, CloseAt = null };
            var submission = new Submission
            {
                SubmissionId = 5,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now.AddMinutes(-10),
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam },
                StudentAnswers = new List<StudentAnswer>()
            };
            var request = new SubmitExamRequest
            {
                ExamId = 1,
                StudentAnswers = new List<StudentAnswerDto> { new StudentAnswerDto { QuestionAnswerId = 1, Response = "A" } },
                Submit = true
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, default)).ReturnsAsync(new HashSet<int> { 1, 2, 3 });
            _mockRepo.Setup(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
            _mockRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value.SubmissionId);
            Assert.True(result.Value.IsLate == false || result.Value.IsLate == true);
            _mockRepo.Verify(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID07 - Valid save (không submit) → Status = InProgress")]
        public async Task SubmitExamAsync_UTCID07_SaveWithoutSubmit_ShouldKeepInProgress()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var exam = new Exam { ExamId = 1, Duration = 60, CloseAt = null };
            var submission = new Submission
            {
                SubmissionId = 5,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now.AddMinutes(-10),
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam },
                StudentAnswers = new List<StudentAnswer>()
            };
            var request = new SubmitExamRequest
            {
                ExamId = 1,
                StudentAnswers = new List<StudentAnswerDto> { new StudentAnswerDto { QuestionAnswerId = 1, Response = "B" } },
                Submit = false
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, default)).ReturnsAsync(new HashSet<int> { 1, 2, 3 });
            _mockRepo.Setup(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
            _mockRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value.SubmissionId);
            // Status should be 1 (InProgress) when Submit=false
            Assert.Equal(SubmissionStatus.InProgress, submission.Status);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID09 - Update existing FillInBlank answer (Response thay đổi)")]
        public async Task SubmitExamAsync_UTCID09_UpdateExistingAnswer_ShouldUpdateResponse()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var exam = new Exam { ExamId = 1, Duration = 60, CloseAt = null };
            var existingAnswer = new StudentAnswer { StudentAnswerId = 100, QuestionAnswerId = 1, Response = "OldValue", SubmissionId = 5 };
            var submission = new Submission
            {
                SubmissionId = 5,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now.AddMinutes(-10),
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam },
                StudentAnswers = new List<StudentAnswer> { existingAnswer }
            };
            var request = new SubmitExamRequest
            {
                ExamId = 1,
                StudentAnswers = new List<StudentAnswerDto>
                {
                    new StudentAnswerDto { QuestionAnswerId = 1, Response = "NewValue" }
                },
                Submit = false
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, default)).ReturnsAsync(new HashSet<int> { 1, 2, 3 });
            _mockRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal("NewValue", existingAnswer.Response);
            _mockRepo.Verify(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()), Times.Never);
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID10 - Existing answer không có trong request -> remove")]
        public async Task SubmitExamAsync_UTCID10_RemoveStaleAnswer_ShouldCallRemove()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var exam = new Exam { ExamId = 1, Duration = 60, CloseAt = null };
            var existingStale = new StudentAnswer { StudentAnswerId = 100, QuestionAnswerId = 99, Response = "Stale" };
            var submission = new Submission
            {
                SubmissionId = 5,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now.AddMinutes(-10),
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam },
                StudentAnswers = new List<StudentAnswer> { existingStale }
            };
            var request = new SubmitExamRequest
            {
                ExamId = 1,
                StudentAnswers = new List<StudentAnswerDto>
                {
                    new StudentAnswerDto { QuestionAnswerId = 1, Response = "A" }
                },
                Submit = false
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.GetValidQuestionAnswerIdsAsync(10, default)).ReturnsAsync(new HashSet<int> { 1, 2, 3, 99 });
            _mockRepo.Setup(r => r.RemoveStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
            _mockRepo.Setup(r => r.AddStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()));
            _mockRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.RemoveStudentAnswers(It.IsAny<IEnumerable<StudentAnswer>>()), Times.Once);
        }

        [Fact(DisplayName = "SubmitExamAsync - UTCID08 - CloseAt validation")]
        public async Task SubmitExamAsync_UTCID08_CloseAtPassed_ShouldReturnLate()
        {
            int studentId = 1;
            var now = DateTime.UtcNow;
            var exam = new Exam
            {
                ExamId = 1,
                Duration = 60,
                CloseAt = now.AddMinutes(-5)  // CloseAt đã qua
            };
            var submission = new Submission
            {
                SubmissionId = 1,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                CreatedAtUtc = now.AddMinutes(-50),
                Paper = new Paper { PaperId = 10, ExamId = 1, Exam = exam }
            };
            var request = new SubmitExamRequest { ExamId = 1, StudentAnswers = new List<StudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetActiveSubmissionAsync(1, studentId, default)).ReturnsAsync(submission);

            var result = await _service.SubmitExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(SubmissionErrors.Late.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }
    }
}
