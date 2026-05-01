using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common;
using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Backend_UnitTest.StudentExamUnitTest
{
    public class GetAllSubmissionHistoryAsyncTest
    {
        private readonly Mock<IStudentExamRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly StudentExamService _service;

        public GetAllSubmissionHistoryAsyncTest()
        {
            _mockRepo = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            _service = new StudentExamService(_mockRepo.Object, _mockCurrentUser.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetAllSubmissionHistoryAsync - UTCID01 - Lấy lịch sử tất cả bài nộp")]
        public async Task GetAllSubmissionHistoryAsync_UTCID01_AllSubmissions_ShouldReturnList()
        {
            var studentId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);

            var rawHistory = new List<SubmissionHistoryRaw>
            {
                new SubmissionHistoryRaw
                {
                    SubmissionId = 1,
                    IsExam = true,
                    Title = "Exam 1",
                    ClassName = "Class A",
                    SubjectName = "Math",
                    TotalQuestions = 20,
                    Status = SubmissionStatus.Submitted,
                    TotalPoints = 18m,
                    CreatedAtUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                    ExamId = 1
                },
                new SubmissionHistoryRaw
                {
                    SubmissionId = 2,
                    IsExam = false,
                    Title = "Practice 1",
                    ClassName = "Class A",
                    SubjectName = "Math",
                    TotalQuestions = 15,
                    Status = SubmissionStatus.InProgress,
                    TotalPoints = null,
                    CreatedAtUtc = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2026, 4, 2, 10, 30, 0, DateTimeKind.Utc),
                    ExamId = null
                }
            };

            _mockRepo.Setup(r => r.GetSubmissionHistoryRawAsync(studentId, null)).ReturnsAsync(rawHistory);

            var result = await _service.GetAllSubmissionHistoryAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);

            // Check first (Exam)
            Assert.Equal(1, result.Value[0].SubmissionId);
            Assert.Equal("Kiểm tra", result.Value[0].Type);
            Assert.Equal("Exam 1", result.Value[0].Title);
            Assert.Equal("Class A", result.Value[0].ClassName);
            Assert.Equal("Math", result.Value[0].SubjectName);
            Assert.Equal(20, result.Value[0].TotalQuestions);
            Assert.Equal("Đã nộp", result.Value[0].Status);
            Assert.Equal(18m, result.Value[0].TotalPoints);
            Assert.NotNull(result.Value[0].CompletedAtUtc);

            // Check second (Practice)
            Assert.Equal(2, result.Value[1].SubmissionId);
            Assert.Equal("Luyện tập", result.Value[1].Type);
            Assert.Equal("Practice 1", result.Value[1].Title);
            Assert.Equal("Đang làm", result.Value[1].Status);
            Assert.Null(result.Value[1].CompletedAtUtc);

            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetAllSubmissionHistoryAsync - UTCID02 - Lịch sử trống")]
        public async Task GetAllSubmissionHistoryAsync_UTCID02_EmptyHistory_ShouldReturnEmptyList()
        {
            var studentId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);
            _mockRepo.Setup(r => r.GetSubmissionHistoryRawAsync(studentId, null)).ReturnsAsync(new List<SubmissionHistoryRaw>());

            var result = await _service.GetAllSubmissionHistoryAsync();

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetAllSubmissionHistoryAsync - UTCID03 - Lọc theo lớp")]
        public async Task GetAllSubmissionHistoryAsync_UTCID03_FilterByClass_ShouldReturnFilteredList()
        {
            var studentId = 1;
            var classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);

            var rawHistory = new List<SubmissionHistoryRaw>
            {
                new SubmissionHistoryRaw
                {
                    SubmissionId = 1,
                    IsExam = true,
                    Title = "Exam 1",
                    ClassName = "Class A",
                    SubjectName = "Math",
                    TotalQuestions = 20,
                    Status = SubmissionStatus.Submitted,
                    TotalPoints = 18m,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    ExamId = 1
                }
            };

            _mockRepo.Setup(r => r.GetSubmissionHistoryRawAsync(studentId, classId)).ReturnsAsync(rawHistory);

            var result = await _service.GetAllSubmissionHistoryAsync(classId);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(1, result.Value[0].SubmissionId);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetAllSubmissionHistoryAsync - UTCID04 - Chuyển trạng thái submit đúng")]
        public async Task GetAllSubmissionHistoryAsync_UTCID04_StatusTranslation_ShouldMapCorrectly()
        {
            var studentId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(studentId);

            var rawHistory = new List<SubmissionHistoryRaw>
            {
                new SubmissionHistoryRaw
                {
                    SubmissionId = 1,
                    IsExam = true,
                    Title = "Test",
                    ClassName = "A",
                    SubjectName = "S",
                    TotalQuestions = 10,
                    Status = SubmissionStatus.Submitted,
                    TotalPoints = 5m,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    ExamId = 1
                },
                new SubmissionHistoryRaw
                {
                    SubmissionId = 2,
                    IsExam = false,
                    Title = "Practice",
                    ClassName = "A",
                    SubjectName = "S",
                    TotalQuestions = 5,
                    Status = SubmissionStatus.InProgress,
                    TotalPoints = null,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    ExamId = null
                }
            };

            _mockRepo.Setup(r => r.GetSubmissionHistoryRawAsync(studentId, null)).ReturnsAsync(rawHistory);

            var result = await _service.GetAllSubmissionHistoryAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal("Đã nộp", result.Value[0].Status);
            Assert.Equal("Đang làm", result.Value[1].Status);
            _mockRepo.VerifyAll();
        }
    }
}
