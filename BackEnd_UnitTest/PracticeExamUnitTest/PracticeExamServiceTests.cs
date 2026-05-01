using Backend.Common;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.PracticeExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.PracticeExamUnitTest
{
    public class PracticeExamServiceTests
    {
        private readonly Mock<IPracticeExamRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ILogger<PracticeExamService>> _mockLogger;
        private readonly PracticeExamService _service;

        public PracticeExamServiceTests()
        {
            _mockRepo = new Mock<IPracticeExamRepository>(MockBehavior.Strict);
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<PracticeExamService>>();
            _service = new PracticeExamService(_mockRepo.Object, _mockCurrentUser.Object, _mockLogger.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetChaptersForPracticeAsync - UTCID01 - Lớp không tồn tại → ClassNotFound")]
        public async Task GetChaptersForPracticeAsync_UTCID01_ClassNotFound_ShouldReturnClassNotFound()
        {
            var studentId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetClassWithValidationAsync(999, studentId)).ReturnsAsync((Class?)null);

            var result = await _service.GetChaptersForPracticeAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.ClassNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "CreatePracticeExamAsync - UTCID01 - Lớp không tồn tại → ClassNotFound")]
        public async Task CreatePracticeExamAsync_UTCID01_ClassNotFound_ShouldReturnClassNotFound()
        {
            var studentId = 1;
            var request = new CreatePracticeExamRequest { ClassId = 999 };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetClassWithValidationAsync(999, studentId)).ReturnsAsync((Class?)null);

            var result = await _service.CreatePracticeExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.ClassNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitPracticeExamAsync - UTCID01 - Submission không tồn tại → SubmissionNotFound")]
        public async Task SubmitPracticeExamAsync_UTCID01_SubmissionNotFound_ShouldReturnSubmissionNotFound()
        {
            var studentId = 1;
            var request = new SubmitPracticeExamRequest { SubmissionId = 999, StudentAnswers = new List<PracticeStudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(999, studentId)).ReturnsAsync((Submission?)null);

            var result = await _service.SubmitPracticeExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.SubmissionNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SubmitPracticeExamAsync - UTCID02 - Submission đã được nộp → AlreadySubmitted")]
        public async Task SubmitPracticeExamAsync_UTCID02_AlreadySubmitted_ShouldReturnAlreadySubmitted()
        {
            var studentId = 1;
            var submission = new Submission { SubmissionId = 1, Status = SubmissionStatus.Submitted, PaperId = 10 };
            var request = new SubmitPracticeExamRequest { SubmissionId = 1, StudentAnswers = new List<PracticeStudentAnswerDto>() };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(1, studentId)).ReturnsAsync(submission);

            var result = await _service.SubmitPracticeExamAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.AlreadySubmitted.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "SavePracticeAnswersAsync - UTCID01 - Lưu câu trả lời thành công")]
        public async Task SavePracticeAnswersAsync_UTCID01_ValidSave_ShouldSuccess()
        {
            var studentId = 1;
            var paper = new Paper
            {
                PaperId = 10,
                Questions = new List<Question>
                {
                    new Question
                    {
                        QuestionId = 1,
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new QuestionAnswer { QuestionAnswerId = 1, Content = "A" }
                        }
                    }
                }
            };
            var submission = new Submission
            {
                SubmissionId = 1,
                Status = SubmissionStatus.InProgress,
                PaperId = 10,
                Paper = paper,
                StudentAnswers = new List<StudentAnswer>()
            };
            var request = new SubmitPracticeExamRequest
            {
                SubmissionId = 1,
                StudentAnswers = new List<PracticeStudentAnswerDto> { new PracticeStudentAnswerDto { QuestionAnswerId = 1, Response = "A" } }
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(1, studentId)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SavePracticeAnswersAsync(request);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "ResumePracticeExamAsync - UTCID01 - Submission không tồn tại → SubmissionNotFound")]
        public async Task ResumePracticeExamAsync_UTCID01_SubmissionNotFound_ShouldReturnSubmissionNotFound()
        {
            var studentId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(999, studentId)).ReturnsAsync((Submission?)null);

            var result = await _service.ResumePracticeExamAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.SubmissionNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "ResumePracticeExamAsync - UTCID02 - Resume bài luyện tập")]
        public async Task ResumePracticeExamAsync_UTCID02_ValidResume_ShouldReturnResumeData()
        {
            var studentId = 1;
            var submission = new Submission
            {
                SubmissionId = 1,
                PaperId = 10,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = DateTime.UtcNow,
                StudentAnswers = new List<StudentAnswer>()
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(1, studentId)).ReturnsAsync(submission);
            _mockRepo.Setup(r => r.GetPracticePaperWithQuestionsAsync(10)).ReturnsAsync(
                new Paper { PaperId = 10, Questions = new List<Question>() }
            );

            var result = await _service.ResumePracticeExamAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value.PaperId);
            Assert.Equal(1, result.Value.SubmissionId);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID01 - Submission không tồn tại → SubmissionNotFound")]
        public async Task GetPracticeResultAsync_UTCID01_SubmissionNotFound_ShouldReturnSubmissionNotFound()
        {
            var studentId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(999, studentId)).ReturnsAsync((Submission?)null);

            var result = await _service.GetPracticeResultAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.SubmissionNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetPracticeResultAsync - UTCID02 - Submission chưa nộp → NotSubmitted")]
        public async Task GetPracticeResultAsync_UTCID02_NotSubmitted_ShouldReturnNotSubmitted()
        {
            var studentId = 1;
            var submission = new Submission { SubmissionId = 1, Status = SubmissionStatus.InProgress };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeSubmissionFullAsync(1, studentId)).ReturnsAsync(submission);

            var result = await _service.GetPracticeResultAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(PracticeExamErrors.NotSubmitted.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetPracticeHistoryAsync - UTCID01 - Lấy lịch sử luyện tập")]
        public async Task GetPracticeHistoryAsync_UTCID01_ValidRequest_ShouldReturnHistory()
        {
            var studentId = 1;
            var rawHistory = new List<PracticeHistoryRaw>
            {
                new PracticeHistoryRaw
                {
                    SubmissionId = 1,
                    PaperId = 10,
                    SubjectName = "Math",
                    SubjectCode = "M",
                    ChapterNames = new List<string> { "Ch1", "Ch2" },
                    TotalQuestions = 10,
                    CorrectCount = 8,
                    Status = SubmissionStatus.Submitted,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetPracticeHistoryAsync(studentId, null)).ReturnsAsync(rawHistory);

            var result = await _service.GetPracticeHistoryAsync(null);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("Math", result.Value[0].SubjectName);
            _mockRepo.VerifyAll();
        }
    }
}
