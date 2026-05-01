using Backend.Common.Errors;
using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Backend_UnitTest.CourseUnitTests
{
    // F2 - GetExamsForCurrentUserAsync
    public class GetExamsForCurrentUserAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly CourseService _service;

        public GetExamsForCurrentUserAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            var mockEmail = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();

            _service = new CourseService(
                _mockRepo.Object,
                mockEmail.Object,
                mockConfig.Object,
                _mockCurrentUser.Object,
                TimeProvider.System
            );
        }

        // UTCID01 - currentUser không thuộc lớp nào → AccessDenied
        [Fact(DisplayName = "GetExamsForCurrentUserAsync - UTCID01 - currentUser không thuộc lớp nào → AccessDenied")]
        public async Task GetExamsForCurrentUserAsync_UTCID01_UserNotInAnyClass_ShouldReturnAccessDenied()
        {
            // Arrange
            int classId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _service.GetExamsForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AccessDenied.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - currentUser có role Pending trong lớp → AccessDenied
        [Fact(DisplayName = "GetExamsForCurrentUserAsync - UTCID02 - currentUser có role Pending → AccessDenied")]
        public async Task GetExamsForCurrentUserAsync_UTCID02_UserHasPendingRole_ShouldReturnAccessDenied()
        {
            // Arrange
            int classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>
            {
                new CourseDTO { ClassId = classId, Role = "Pending" }
            });

            // Act
            var result = await _service.GetExamsForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AccessDenied.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - currentUser là Teacher → gọi repo với isTeacher=true, trả danh sách exam
        [Fact(DisplayName = "GetExamsForCurrentUserAsync - UTCID03 - currentUser là Teacher → trả exam list (isTeacher=true)")]
        public async Task GetExamsForCurrentUserAsync_UTCID03_UserIsTeacher_ShouldReturnExamsWithTeacherFlag()
        {
            // Arrange
            int classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>
            {
                new CourseDTO { ClassId = classId, Role = "Teacher" }
            });

            var expectedExams = new List<ExamInCourseDTO>
            {
                new ExamInCourseDTO { ExamId = 1, Title = "Midterm" },
                new ExamInCourseDTO { ExamId = 2, Title = "Final" }
            };
            _mockRepo.Setup(r => r.GetExamsByClassAsync(classId, true)).ReturnsAsync(expectedExams);

            // Act
            var result = await _service.GetExamsForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - currentUser là Student → gọi repo với isTeacher=false, trả danh sách exam
        [Fact(DisplayName = "GetExamsForCurrentUserAsync - UTCID04 - currentUser là Student → trả exam list (isTeacher=false)")]
        public async Task GetExamsForCurrentUserAsync_UTCID04_UserIsStudent_ShouldReturnExamsWithStudentFlag()
        {
            // Arrange
            int classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(20);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(20)).ReturnsAsync(new List<CourseDTO>
            {
                new CourseDTO { ClassId = classId, Role = "Student" }
            });

            var expectedExams = new List<ExamInCourseDTO>
            {
                new ExamInCourseDTO { ExamId = 3, Title = "Quiz 1" }
            };
            _mockRepo.Setup(r => r.GetExamsByClassAsync(classId, false)).ReturnsAsync(expectedExams);

            // Act
            var result = await _service.GetExamsForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            _mockRepo.VerifyAll();
        }
    }
}
