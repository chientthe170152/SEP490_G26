using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Backend_UnitTest.CourseUnitTests
{
    // F8 - UpdateClassSettingsAsync
    public class UpdateClassSettingsAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public UpdateClassSettingsAsyncTests()
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

        // UTCID01 - classId không tồn tại → NotFound
        [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID01 - classId không tồn tại → NotFound")]
        public async Task UpdateClassSettingsAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            int classId = 999;
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _service.UpdateClassSettingsAsync(classId, "New Name", 1);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - lớp đã đóng → Closed
        [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID02 - Lớp đã đóng → Closed")]
        public async Task UpdateClassSettingsAsync_UTCID02_ClassClosed_ShouldReturnClosed()
        {
            // Arrange
            int classId = 5;
            var closedCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Closed };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(closedCourse);

            // Act
            var result = await _service.UpdateClassSettingsAsync(classId, "New Name", 1);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - repo trả về false (không cập nhật được) → NotFound
        [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID03 - Repo trả false → NotFound")]
        public async Task UpdateClassSettingsAsync_UTCID03_UpdateReturnsFalse_ShouldReturnNotFound()
        {
            // Arrange
            int classId = 5;
            var activeCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(activeCourse);
            _mockRepo.Setup(r => r.UpdateClassSettingsAsync(classId, "New Name", 1)).ReturnsAsync(false);

            // Act
            var result = await _service.UpdateClassSettingsAsync(classId, "New Name", 1);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - hợp lệ → cập nhật thành công
        [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID04 - Hợp lệ → cập nhật thành công")]
        public async Task UpdateClassSettingsAsync_UTCID04_ValidRequest_ShouldUpdateSuccessfully()
        {
            // Arrange
            int classId = 5;
            var activeCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(activeCourse);
            _mockRepo.Setup(r => r.UpdateClassSettingsAsync(classId, "Updated Name", 0)).ReturnsAsync(true);

            // Act
            var result = await _service.UpdateClassSettingsAsync(classId, "Updated Name", 0);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }
}
