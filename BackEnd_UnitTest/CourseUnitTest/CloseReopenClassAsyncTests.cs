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
    // F14 - CloseClassAsync
    public class CloseClassAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public CloseClassAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            var mockEmail = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new CourseService(
                _mockRepo.Object, mockEmail.Object, mockConfig.Object,
                mockCurrentUser.Object, TimeProvider.System);
        }

        // UTCID01 - classId không tồn tại → NotFound
        [Fact(DisplayName = "CloseClassAsync - UTCID01 - classId không tồn tại → NotFound")]
        public async Task CloseClassAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.CloseClassAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _service.CloseClassAsync(999);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - hợp lệ → đóng lớp thành công
        [Fact(DisplayName = "CloseClassAsync - UTCID02 - Hợp lệ → đóng lớp thành công")]
        public async Task CloseClassAsync_UTCID02_ValidRequest_ShouldCloseClassSuccessfully()
        {
            // Arrange
            _mockRepo.Setup(r => r.CloseClassAsync(5)).ReturnsAsync(true);

            // Act
            var result = await _service.CloseClassAsync(5);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }

    // F15 - ReopenClassAsync
    public class ReopenClassAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public ReopenClassAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            var mockEmail = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new CourseService(
                _mockRepo.Object, mockEmail.Object, mockConfig.Object,
                mockCurrentUser.Object, TimeProvider.System);
        }

        // UTCID01 - classId không tồn tại → NotFound
        [Fact(DisplayName = "ReopenClassAsync - UTCID01 - classId không tồn tại → NotFound")]
        public async Task ReopenClassAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.ReopenClassAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _service.ReopenClassAsync(999);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - hợp lệ → mở lại lớp thành công
        [Fact(DisplayName = "ReopenClassAsync - UTCID02 - Hợp lệ → mở lại lớp thành công")]
        public async Task ReopenClassAsync_UTCID02_ValidRequest_ShouldReopenClassSuccessfully()
        {
            // Arrange
            _mockRepo.Setup(r => r.ReopenClassAsync(5)).ReturnsAsync(true);

            // Act
            var result = await _service.ReopenClassAsync(5);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }
}
