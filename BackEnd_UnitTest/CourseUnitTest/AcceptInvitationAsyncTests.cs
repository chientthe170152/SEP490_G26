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
    // F10 - AcceptInvitationAsync
    public class AcceptInvitationAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public AcceptInvitationAsyncTests()
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

        // Tạo một token hợp lệ từ classId và stamp (replicating service private logic)
        private static string BuildValidToken(int classId, byte[] stamp)
        {
            var stampBase64 = Convert.ToBase64String(stamp).Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var plain = $"{classId}:{stampBase64}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plain))
                          .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        // UTCID01 - token null → InviteTokenInvalid
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID01 - token null → InviteTokenInvalid")]
        public async Task AcceptInvitationAsync_UTCID01_NullToken_ShouldReturnInviteTokenInvalid()
        {
            // Act
            var result = await _service.AcceptInvitationAsync(5, null);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteTokenInvalid.Code, result.Error.Code);
        }

        // UTCID02 - token rỗng/whitespace (boundary) → InviteTokenInvalid
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID02 - token rỗng (boundary) → InviteTokenInvalid")]
        public async Task AcceptInvitationAsync_UTCID02_EmptyToken_ShouldReturnInviteTokenInvalid()
        {
            // Act
            var result = await _service.AcceptInvitationAsync(5, "   ");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteTokenInvalid.Code, result.Error.Code);
        }

        // UTCID03 - token sai format (không decode được) → InviteTokenInvalid
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID03 - token sai format → InviteTokenInvalid")]
        public async Task AcceptInvitationAsync_UTCID03_InvalidTokenFormat_ShouldReturnInviteTokenInvalid()
        {
            // Arrange
            // Chuỗi base64 hợp lệ nhưng sau khi decode không có dấu ':'
            string badToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("nodash"))
                                     .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            // Act
            var result = await _service.AcceptInvitationAsync(5, badToken);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteTokenInvalid.Code, result.Error.Code);
        }

        // UTCID04 - token hợp lệ nhưng lớp không tồn tại → NotFound
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID04 - Lớp không tồn tại → NotFound")]
        public async Task AcceptInvitationAsync_UTCID04_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            int classId = 10;
            var stamp = new byte[] { 1, 2, 3 };
            string token = BuildValidToken(classId, stamp);
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _service.AcceptInvitationAsync(5, token);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID05 - lớp đã đóng → Closed
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID05 - Lớp đã đóng → Closed")]
        public async Task AcceptInvitationAsync_UTCID05_ClassClosed_ShouldReturnClosed()
        {
            // Arrange
            int classId = 10;
            var stamp = new byte[] { 1, 2, 3 };
            string token = BuildValidToken(classId, stamp);
            var closedCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Closed };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(closedCourse);

            // Act
            var result = await _service.AcceptInvitationAsync(5, token);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID06 - stamp không khớp (rows=0) → InviteTokenInvalid
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID06 - Stamp không khớp → InviteTokenInvalid")]
        public async Task AcceptInvitationAsync_UTCID06_StampMismatch_ShouldReturnInviteTokenInvalid()
        {
            // Arrange
            int classId = 10;
            int studentId = 5;
            var stamp = new byte[] { 1, 2, 3 };
            string token = BuildValidToken(classId, stamp);
            var activeCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(activeCourse);
            _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(classId, studentId, It.IsAny<byte[]>()))
                     .ReturnsAsync(0);

            // Act
            var result = await _service.AcceptInvitationAsync(studentId, token);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteTokenInvalid.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID07 - hợp lệ → chấp nhận lời mời thành công
        [Fact(DisplayName = "AcceptInvitationAsync - UTCID07 - Hợp lệ → chấp nhận thành công")]
        public async Task AcceptInvitationAsync_UTCID07_ValidToken_ShouldAcceptSuccessfully()
        {
            // Arrange
            int classId = 10;
            int studentId = 5;
            var stamp = new byte[] { 1, 2, 3 };
            string token = BuildValidToken(classId, stamp);
            var activeCourse = new CourseDTO { ClassId = classId, Status = ClassStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(activeCourse);
            _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(classId, studentId, It.IsAny<byte[]>()))
                     .ReturnsAsync(1);

            // Act
            var result = await _service.AcceptInvitationAsync(studentId, token);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }
}
