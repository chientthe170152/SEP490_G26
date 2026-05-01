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
    // F7 - LeaveCourseAsync
    public class LeaveCourseAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public LeaveCourseAsyncTests()
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
        [Fact(DisplayName = "LeaveCourseAsync - UTCID01 - classId không tồn tại → NotFound")]
        public async Task LeaveCourseAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            int classId = 999;
            int userId = 5;
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _service.LeaveCourseAsync(classId, userId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - lớp đã bị đóng → Closed
        [Fact(DisplayName = "LeaveCourseAsync - UTCID02 - Lớp đã đóng → Closed")]
        public async Task LeaveCourseAsync_UTCID02_ClassClosed_ShouldReturnClosed()
        {
            // Arrange
            int classId = 5;
            int userId = 5;
            var closedCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Closed };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(closedCourse);

            // Act
            var result = await _service.LeaveCourseAsync(classId, userId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - hợp lệ → rời lớp thành công
        [Fact(DisplayName = "LeaveCourseAsync - UTCID03 - Hợp lệ → rời lớp thành công")]
        public async Task LeaveCourseAsync_UTCID03_ValidRequest_ShouldLeaveClassSuccessfully()
        {
            // Arrange
            int classId = 5;
            int userId = 5;
            var activeCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(activeCourse);
            _mockRepo.Setup(r => r.LeaveClassAsync(classId, userId)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.LeaveCourseAsync(classId, userId);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }
}
