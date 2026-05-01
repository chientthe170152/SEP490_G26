using Backend.Common;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Backend_UnitTest.CourseUnitTests
{
    // F9 - InviteStudentByEmailAsync
    public class InviteStudentByEmailAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly CourseService _service;

        private const int TeacherId = 1;
        private const int ClassId = 10;
        private const string StudentEmail = "student@example.com";
        private const string FrontendBase = "https://app.example.com";

        public InviteStudentByEmailAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            _mockEmail = new Mock<IEmailService>(MockBehavior.Strict);
            _mockConfig = new Mock<IConfiguration>(MockBehavior.Strict);
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new CourseService(
                _mockRepo.Object,
                _mockEmail.Object,
                _mockConfig.Object,
                mockCurrentUser.Object,
                TimeProvider.System
            );
        }

        private User MakeStudentUser(int userId = 100) => new User
        {
            UserId = userId,
            Email = StudentEmail,
            RoleId = int.Parse(RoleIds.Student),
            Status = 1,
            ConcurrencyStamp = Array.Empty<byte>()
        };

        private CourseDTO MakeActiveCourse() => new CourseDTO
        {
            ClassId = ClassId,
            ClassName = "Math 101",
            Status = ClassStatus.Active
        };

        // UTCID01 - FrontendSettings:BaseUrl chưa cấu hình → ConfigError
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID01 - BaseUrl chưa cấu hình → ConfigError")]
        public async Task InviteStudentByEmailAsync_UTCID01_MissingConfig_ShouldReturnConfigError()
        {
            // Arrange
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns((string?)null);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.ConfigError.Code, result.Error.Code);
            _mockConfig.VerifyAll();
        }

        // UTCID02 - email không tìm thấy user → StudentNotFound
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID02 - Email không tồn tại → StudentNotFound")]
        public async Task InviteStudentByEmailAsync_UTCID02_StudentEmailNotFound_ShouldReturnStudentNotFound()
        {
            // Arrange
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.StudentNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - user tồn tại nhưng không phải student (Teacher role) → UserNotStudent
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID03 - User không phải student → UserNotStudent")]
        public async Task InviteStudentByEmailAsync_UTCID03_UserNotStudent_ShouldReturnUserNotStudent()
        {
            // Arrange
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            var teacherUser = new User
            {
                UserId = 99,
                Email = StudentEmail,
                RoleId = int.Parse(RoleIds.Teacher),
                Status = 1,
                ConcurrencyStamp = Array.Empty<byte>()
            };
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(teacherUser);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.UserNotStudent.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - lớp không tồn tại → NotFound
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID04 - Lớp không tồn tại → NotFound")]
        public async Task InviteStudentByEmailAsync_UTCID04_ClassNotFound_ShouldReturnNotFound()
        {
            // Arrange
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(MakeStudentUser());
            _mockRepo.Setup(r => r.GetByIdAsync(ClassId)).ReturnsAsync((CourseDTO?)null);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID05 - lớp đã đóng → Closed
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID05 - Lớp đã đóng → Closed")]
        public async Task InviteStudentByEmailAsync_UTCID05_ClassClosed_ShouldReturnClosed()
        {
            // Arrange
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(MakeStudentUser());
            var closedCourse = new CourseDTO { ClassId = ClassId, Status = ClassStatus.Closed };
            _mockRepo.Setup(r => r.GetByIdAsync(ClassId)).ReturnsAsync(closedCourse);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID06 - student đã là thành viên Active → AlreadyMember
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID06 - Student đã là thành viên Active → AlreadyMember")]
        public async Task InviteStudentByEmailAsync_UTCID06_StudentAlreadyActiveMember_ShouldReturnAlreadyMember()
        {
            // Arrange
            var student = MakeStudentUser();
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(student);
            _mockRepo.Setup(r => r.GetByIdAsync(ClassId)).ReturnsAsync(MakeActiveCourse());
            _mockRepo.Setup(r => r.GetClassMemberAsync(ClassId, student.UserId))
                     .ReturnsAsync(new ClassMember { ClassId = ClassId, StudentId = student.UserId, MemberStatus = MemberStatus.Active, ConcurrencyStamp = Array.Empty<byte>() });

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AlreadyMember.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID07 - student đã được mời (Invited) → AlreadyInvited
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID07 - Student đã được mời → AlreadyInvited")]
        public async Task InviteStudentByEmailAsync_UTCID07_StudentAlreadyInvited_ShouldReturnAlreadyInvited()
        {
            // Arrange
            var student = MakeStudentUser();
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(student);
            _mockRepo.Setup(r => r.GetByIdAsync(ClassId)).ReturnsAsync(MakeActiveCourse());
            _mockRepo.Setup(r => r.GetClassMemberAsync(ClassId, student.UserId))
                     .ReturnsAsync(new ClassMember { ClassId = ClassId, StudentId = student.UserId, MemberStatus = MemberStatus.Invited, ConcurrencyStamp = Array.Empty<byte>() });

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.AlreadyInvited.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID08 - student đang chờ duyệt (Pending) → tự động approve, AutoApproved=true
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID08 - Student đang Pending → AutoApproved")]
        public async Task InviteStudentByEmailAsync_UTCID08_StudentPending_ShouldAutoApprove()
        {
            // Arrange
            var student = MakeStudentUser();
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(student);
            _mockRepo.Setup(r => r.GetByIdAsync(ClassId)).ReturnsAsync(MakeActiveCourse());
            _mockRepo.Setup(r => r.GetClassMemberAsync(ClassId, student.UserId))
                     .ReturnsAsync(new ClassMember { ClassId = ClassId, StudentId = student.UserId, MemberStatus = MemberStatus.Pending, ConcurrencyStamp = Array.Empty<byte>() });
            _mockRepo.Setup(r => r.UpdateClassMemberStatusAsync(ClassId, student.UserId, MemberStatus.Active))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.AutoApproved);
            Assert.Null(result.Value.Token);
            _mockRepo.VerifyAll();
        }

        // UTCID09 - student hoàn toàn mới → gửi email, trả token
        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID09 - Student mới → gửi email mời, trả token")]
        public async Task InviteStudentByEmailAsync_UTCID09_NewStudent_ShouldSendEmailAndReturnToken()
        {
            // Arrange
            var student = MakeStudentUser();
            var concurrencyStamp = new byte[] { 1, 2, 3, 4 };
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(FrontendBase);
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(StudentEmail)).ReturnsAsync(student);
            _mockRepo.Setup(r => r.GetByIdAsync(ClassId)).ReturnsAsync(MakeActiveCourse());
            _mockRepo.Setup(r => r.GetClassMemberAsync(ClassId, student.UserId)).ReturnsAsync((ClassMember?)null);
            _mockRepo.Setup(r => r.InviteStudentAsync(ClassId, student.UserId))
                     .ReturnsAsync(new ClassMember { ClassId = ClassId, StudentId = student.UserId, MemberStatus = MemberStatus.Invited, ConcurrencyStamp = concurrencyStamp });
            _mockEmail.Setup(e => e.SendEmailAsync(StudentEmail, It.IsAny<string>(), It.IsAny<string>()))
                      .Returns(Task.CompletedTask);

            // Act
            var result = await _service.InviteStudentByEmailAsync(TeacherId, ClassId, StudentEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.Value.AutoApproved);
            Assert.NotNull(result.Value.Token);
            _mockRepo.VerifyAll();
            _mockEmail.VerifyAll();
        }
    }
}
