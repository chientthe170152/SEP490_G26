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
    // F3 - GetChaptersForCurrentUserAsync
    public class GetChaptersForCurrentUserAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly CourseService _service;

        public GetChaptersForCurrentUserAsyncTests()
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
        [Fact(DisplayName = "GetChaptersForCurrentUserAsync - UTCID01 - currentUser không thuộc lớp nào → AccessDenied")]
        public async Task GetChaptersForCurrentUserAsync_UTCID01_UserNotInClass_ShouldReturnAccessDenied()
        {
            // Arrange
            int classId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _service.GetChaptersForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AccessDenied.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - currentUser có role Pending → AccessDenied
        [Fact(DisplayName = "GetChaptersForCurrentUserAsync - UTCID02 - currentUser có role Pending → AccessDenied")]
        public async Task GetChaptersForCurrentUserAsync_UTCID02_UserHasPendingRole_ShouldReturnAccessDenied()
        {
            // Arrange
            int classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>
            {
                new CourseDTO { ClassId = classId, Role = "Pending" }
            });

            // Act
            var result = await _service.GetChaptersForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AccessDenied.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - currentUser có quyền nhưng lớp không tồn tại → NotFound
        [Fact(DisplayName = "GetChaptersForCurrentUserAsync - UTCID03 - Lớp không tồn tại → NotFound")]
        public async Task GetChaptersForCurrentUserAsync_UTCID03_CourseNotFound_ShouldReturnNotFound()
        {
            // Arrange
            int classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>
            {
                new CourseDTO { ClassId = classId, Role = "Student" }
            });
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _service.GetChaptersForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - hợp lệ → trả về danh sách chapter
        [Fact(DisplayName = "GetChaptersForCurrentUserAsync - UTCID04 - Hợp lệ → trả về danh sách chapter")]
        public async Task GetChaptersForCurrentUserAsync_UTCID04_ValidRequest_ShouldReturnChapters()
        {
            // Arrange
            int classId = 5;
            _mockCurrentUser.Setup(u => u.UserId).Returns(10);
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(10)).ReturnsAsync(new List<CourseDTO>
            {
                new CourseDTO { ClassId = classId, Role = "Teacher" }
            });

            var courseWithChapters = new CourseDTO
            {
                ClassId = classId,
                ClassName = "Math 101",
                Chapters = new List<ChapterDTO>
                {
                    new ChapterDTO { ChapterId = 1, Name = "Chương 1" },
                    new ChapterDTO { ChapterId = 2, Name = "Chương 2" }
                }
            };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(courseWithChapters);

            // Act
            var result = await _service.GetChaptersForCurrentUserAsync(classId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            _mockRepo.VerifyAll();
        }
    }
}
