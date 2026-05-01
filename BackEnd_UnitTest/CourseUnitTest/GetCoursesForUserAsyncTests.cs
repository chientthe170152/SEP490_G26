using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Backend_UnitTest.CourseUnitTests
{
    // F1 - GetCoursesForUserAsync
    public class GetCoursesForUserAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public GetCoursesForUserAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            var mockEmail = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new CourseService(
                _mockRepo.Object,
                mockEmail.Object,
                mockConfig.Object,
                mockCurrentUser.Object,
                TimeProvider.System
            );
        }

        // UTCID01 - userId hợp lệ, có khóa học → trả về danh sách
        [Fact(DisplayName = "GetCoursesForUserAsync - UTCID01 - userId hợp lệ, có khóa học → trả về danh sách")]
        public async Task GetCoursesForUserAsync_UTCID01_ValidUserId_HasCourses_ShouldReturnList()
        {
            // Arrange
            int userId = 1;
            var expected = new List<CourseDTO>
            {
                new CourseDTO { ClassId = 10, ClassName = "Math 101", Role = "Student" },
                new CourseDTO { ClassId = 11, ClassName = "Physics 101", Role = "Teacher" }
            };
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(expected);

            // Act
            var result = await _service.GetCoursesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(10, result[0].ClassId);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - userId hợp lệ, không có khóa học → trả về danh sách rỗng
        [Fact(DisplayName = "GetCoursesForUserAsync - UTCID02 - userId hợp lệ, không có khóa học → danh sách rỗng")]
        public async Task GetCoursesForUserAsync_UTCID02_ValidUserId_NoCourses_ShouldReturnEmpty()
        {
            // Arrange
            int userId = 2;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _service.GetCoursesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - userId = 0 (boundary) → vẫn gọi repo và trả kết quả
        [Fact(DisplayName = "GetCoursesForUserAsync - UTCID03 - userId = 0 (boundary) → gọi repo, trả kết quả")]
        public async Task GetCoursesForUserAsync_UTCID03_UserIdBoundaryZero_ShouldDelegateToRepo()
        {
            // Arrange
            int userId = 0;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _service.GetCoursesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - userId âm (boundary) → vẫn gọi repo và trả kết quả
        [Fact(DisplayName = "GetCoursesForUserAsync - UTCID04 - userId âm (boundary) → gọi repo, trả kết quả")]
        public async Task GetCoursesForUserAsync_UTCID04_NegativeUserId_ShouldDelegateToRepo()
        {
            // Arrange
            int userId = -1;
            _mockRepo.Setup(r => r.GetCoursesForUserAsync(userId)).ReturnsAsync(new List<CourseDTO>());

            // Act
            var result = await _service.GetCoursesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            _mockRepo.VerifyAll();
        }
    }
}
