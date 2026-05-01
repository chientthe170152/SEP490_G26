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
    // F4 - GetClassSettingsAsync
    public class GetClassSettingsAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public GetClassSettingsAsyncTests()
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
        [Fact(DisplayName = "GetClassSettingsAsync - UTCID01 - classId không tồn tại → NotFound")]
        public async Task GetClassSettingsAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            int classId = 999;
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _service.GetClassSettingsAsync(classId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - classId hợp lệ → trả về thông tin lớp học
        [Fact(DisplayName = "GetClassSettingsAsync - UTCID02 - classId hợp lệ → trả về CourseDTO")]
        public async Task GetClassSettingsAsync_UTCID02_ClassExists_ShouldReturnCourseSettings()
        {
            // Arrange
            int classId = 5;
            var course = new CourseDTO
            {
                ClassId = classId,
                ClassName = "Advanced Math",
                Semester = "SP2026",
                InvitationCodeStatus = 1
            };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(course);

            // Act
            var result = await _service.GetClassSettingsAsync(classId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(classId, result.Value.ClassId);
            Assert.Equal("Advanced Math", result.Value.ClassName);
            _mockRepo.VerifyAll();
        }
    }
}
