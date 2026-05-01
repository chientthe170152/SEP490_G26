using Backend.Common.Errors;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Backend_UnitTest.CourseUnitTests
{
    // F6 - JoinCourseAsync
    public class JoinCourseAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public JoinCourseAsyncTests()
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

        // UTCID01 - inviteCode null → InviteCodeInvalid
        [Fact(DisplayName = "JoinCourseAsync - UTCID01 - inviteCode null → InviteCodeInvalid")]
        public async Task JoinCourseAsync_UTCID01_NullInviteCode_ShouldReturnInviteCodeInvalid()
        {
            // Arrange
            int studentId = 5;

            // Act
            var result = await _service.JoinCourseAsync(studentId, null);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteCodeInvalid.Code, result.Error.Code);
        }

        // UTCID02 - inviteCode rỗng/whitespace (boundary) → InviteCodeInvalid
        [Fact(DisplayName = "JoinCourseAsync - UTCID02 - inviteCode rỗng (boundary) → InviteCodeInvalid")]
        public async Task JoinCourseAsync_UTCID02_EmptyInviteCode_ShouldReturnInviteCodeInvalid()
        {
            // Arrange
            int studentId = 5;

            // Act
            var result = await _service.JoinCourseAsync(studentId, "   ");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteCodeInvalid.Code, result.Error.Code);
        }

        // UTCID03 - inviteCode không tìm thấy lớp → InviteCodeInvalid
        [Fact(DisplayName = "JoinCourseAsync - UTCID03 - inviteCode không tìm thấy lớp → InviteCodeInvalid")]
        public async Task JoinCourseAsync_UTCID03_InviteCodeNotFound_ShouldReturnInviteCodeInvalid()
        {
            // Arrange
            int studentId = 5;
            string inviteCode = "INVALID-CODE";
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync(inviteCode)).ReturnsAsync((Class?)null);

            // Act
            var result = await _service.JoinCourseAsync(studentId, inviteCode);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.InviteCodeInvalid.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        private static Class MakeClass(int classId = 10) => new Class
        {
            ClassId = classId,
            TeacherId = 1,
            Name = "Math 101",
            Semester = "SP2026",
            InvitationCode = "CODE",
            SubjectId = 1,
            Status = 1,
            ConcurrencyStamp = Array.Empty<byte>()
        };

        // UTCID04 - student đã là thành viên lớp → AlreadyMember
        [Fact(DisplayName = "JoinCourseAsync - UTCID04 - Student đã là thành viên → AlreadyMember")]
        public async Task JoinCourseAsync_UTCID04_AlreadyMember_ShouldReturnAlreadyMember()
        {
            // Arrange
            int studentId = 5;
            string inviteCode = "VALID-CODE";
            var course = MakeClass();
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync(inviteCode)).ReturnsAsync(course);
            _mockRepo.Setup(r => r.IsUserInClassAsync(course.ClassId, studentId)).ReturnsAsync(true);

            // Act
            var result = await _service.JoinCourseAsync(studentId, inviteCode);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AlreadyMember.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID05 - hợp lệ → tham gia lớp thành công
        [Fact(DisplayName = "JoinCourseAsync - UTCID05 - Hợp lệ → tham gia lớp thành công")]
        public async Task JoinCourseAsync_UTCID05_ValidRequest_ShouldJoinClassSuccessfully()
        {
            // Arrange
            int studentId = 5;
            string inviteCode = "VALID-CODE";
            var course = MakeClass();
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync(inviteCode)).ReturnsAsync(course);
            _mockRepo.Setup(r => r.IsUserInClassAsync(course.ClassId, studentId)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.JoinClassAsync(course.ClassId, studentId)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.JoinCourseAsync(studentId, inviteCode);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }
}
