using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Exceptions;
using Backend.Models;
using Backend.UnitTest;
using Moq;
using System.Text;
using Xunit;

namespace Backend_UnitTest.CourseUnitTest
{
    public class InviteStudentByEmailTests : CourseTestBase
    {
        private readonly string _studentEmail = "student@test.com";
        private readonly int _classId = 10;
        private readonly int _teacherId = 1;

        // 1. Test Config BaseUrl null hoặc trống (Dòng 115)
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task InviteStudent_ConfigMissing_ShouldThrowException(string baseUrl)
        {
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns(baseUrl);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Lỗi khi thêm học sinh vào lớp.", ex.Message);
        }

        // 2. Test Học sinh không tồn tại (Dòng 121)
        [Fact]
        public async Task InviteStudent_UserNotFound_ShouldThrowException()
        {
            SetupConfig();
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(_studentEmail)).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Học sinh chưa có tài khoản trong hệ thống.", ex.Message);
        }

        // 3. Test Người dùng tìm thấy không phải Role Student (Dòng 126)
        [Fact]
        public async Task InviteStudent_NotStudentRole_ShouldThrowException()
        {
            SetupConfig();
            var teacherUser = new User { Role = new Role { Name = "Teacher" } };
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(_studentEmail)).ReturnsAsync(teacherUser);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Chỉ có thể mời người dùng có vai trò là học sinh tham gia lớp học.", ex.Message);
        }

        [Fact]
        public async Task InviteStudent_ClassNotFound_ShouldThrowException()
        {
            // Arrange
            SetupConfig();
            SetupValidStudent();

            // SỬA TẠI ĐÂY: Trả về null để kích hoạt nhánh throw Exception
            _mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((CourseDTO)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Không tìm thấy lớp học.", ex.Message);
        }

        // 5. Test Học sinh đã là Active Member (Dòng 137)
        [Fact]
        public async Task InviteStudent_AlreadyActive_ShouldThrowException()
        {
            SetupConfig();
            var student = SetupValidStudent();
            SetupValidClass();
            var membership = new ClassMember { MemberStatus = MemberStatus.Active };
            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync(membership);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Học sinh này đã tham gia lớp học.", ex.Message);
        }

        // 6. Test Học sinh đã được gửi lời mời (Dòng 141)
        [Fact]
        public async Task InviteStudent_AlreadyInvited_ShouldThrowException()
        {
            SetupConfig();
            var student = SetupValidStudent();
            SetupValidClass();
            var membership = new ClassMember { MemberStatus = MemberStatus.Invited };
            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync(membership);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Học sinh này đã được gửi lời mời trước đó.", ex.Message);
        }

        // 7. Test Học sinh đang chờ duyệt -> Auto Approve (Dòng 145)
        [Fact]
        public async Task InviteStudent_PendingStatus_ShouldAutoApproveAndThrowSpecialException()
        {
            SetupConfig();
            var student = SetupValidStudent();
            SetupValidClass();
            var membership = new ClassMember { MemberStatus = MemberStatus.Pending };
            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync(membership);

            // Kiểm tra xem có ném đúng AutoApprovePendingException không
            await Assert.ThrowsAsync<AutoApprovePendingException>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            // Kiểm tra xem UpdateClassMemberStatusAsync có được gọi không
            _mockRepo.Verify(r => r.UpdateClassMemberStatusAsync(_classId, student.UserId, MemberStatus.Active), Times.Once);
        }

        // 8. Test Không thể tạo membership mới (Dòng 154)
        [Fact]
        public async Task InviteStudent_CreateInviteFailed_ShouldThrowException()
        {
            SetupConfig();
            var student = SetupValidStudent();
            SetupValidClass();
            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync((ClassMember?)null);
            _mockRepo.Setup(r => r.InviteStudentAsync(_classId, student.UserId)).ReturnsAsync((ClassMember?)null);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Không thể tạo lời mời.", ex.Message);
        }

        // 9. Happy Path: Mời thành công và gửi Email (Dòng 172)
        // 1. Khai báo Mock Email trong class (nếu CourseTestBase chưa có)
        // private readonly Mock<IEmailService> _mockEmailService = new Mock<IEmailService>();

        [Fact]
        public async Task InviteStudent_ValidRequest_ShouldSendEmailAndReturnToken()
        {
            // Arrange
            SetupConfig();

            // Giả sử UserId của bạn tên là Id hoặc StudentId, hãy đổi cho đúng
            var student = new User { UserId = 100, Role = new Role { Name = "Student" } };
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(_studentEmail)).ReturnsAsync(student);

            // Giả sử ClassName của bạn tên là Name
            var course = new Class { ClassId = _classId, Name = "Lớp 10A" };
            _mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
               .ReturnsAsync(new CourseDTO { ClassId = 1, ClassName = "DotNet" });

            var stamp = Encoding.UTF8.GetBytes("test-stamp");
            // Sửa UserId thành StudentId nếu model của bạn dùng StudentId
            var membership = new ClassMember
            {
                StudentId = student.UserId, // Kiểm tra lại tên thuộc tính này trong file ClassMember.cs
                ClassId = _classId,
                ConcurrencyStamp = stamp
            };

            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync((ClassMember?)null);
            _mockRepo.Setup(r => r.InviteStudentAsync(_classId, student.UserId)).ReturnsAsync(membership);

            // Act
            var result = await _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail);

            // Assert
            Assert.NotNull(result);
            // Đảm bảo _mockEmailService đã được truyền vào Constructor của CourseService trong setup
            _mockEmail.Verify(e => e.SendEmailAsync(
                _studentEmail,
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Once);
        }

        // --- Helper Methods để tái sử dụng code ---
        private void SetupConfig() =>
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns("http://frontend.com");

        private User SetupValidStudent()
        {
            var user = new User { UserId = 100, Role = new Role { Name = "Student" } };
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(_studentEmail)).ReturnsAsync(user);
            return user;
        }

        private Class SetupValidClass()
        {
            var course = new Class { ClassId = _classId, Name = "Lớp 10A" };
            _mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
               .ReturnsAsync(new CourseDTO { ClassId = 1, ClassName = "DotNet" });
            return course;
        }
        [Fact]
        public async Task InviteStudent_StatusIsUnknown_ShouldProceedToInvite()
        {
            // Arrange
            SetupConfig();
            var student = SetupValidStudent();
            SetupValidClass();

            // Giả lập một trạng thái không nằm trong bộ (0, 1, 2) - ví dụ: 99 (Removed/Rejected)
            var membership = new ClassMember { MemberStatus = 99 };
            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync(membership);

            // Mock cho hành động tiếp theo sau khi thoát khỏi khối IF (dòng 154)
            var newInvite = new ClassMember { ConcurrencyStamp = Encoding.UTF8.GetBytes("new-stamp") };
            _mockRepo.Setup(r => r.InviteStudentAsync(_classId, student.UserId)).ReturnsAsync(newInvite);

            // Act
            var result = await _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail);

            // Assert
            Assert.NotNull(result);
            // Xác nhận là code đã chạy xuyên qua khối IF và gọi hàm Invite ở dưới
            _mockRepo.Verify(r => r.InviteStudentAsync(_classId, student.UserId), Times.Once);
        }
        [Fact]
        public async Task InviteStudent_UserRoleIsNull_ShouldThrowException()
        {
            // Arrange
            SetupConfig();
            // Tạo user nhưng không gán Role (Role sẽ là null)
            var userNoRole = new User { UserId = 100, Role = null };
            _mockRepo.Setup(r => r.GetUserWithRoleByEmailAsync(_studentEmail)).ReturnsAsync(userNoRole);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            Assert.Equal("Chỉ có thể mời người dùng có vai trò là học sinh tham gia lớp học.", ex.Message);
        }
        [Fact]
        public async Task InviteStudent_PendingStatus_ShouldAutoApprove()
        {
            // Arrange
            SetupConfig();
            var student = SetupValidStudent();
            SetupValidClass();

            // MemberStatus.Pending = 0
            var membership = new ClassMember { MemberStatus = MemberStatus.Pending };
            _mockRepo.Setup(r => r.GetClassMemberAsync(_classId, student.UserId)).ReturnsAsync(membership);

            // Act & Assert
            await Assert.ThrowsAsync<AutoApprovePendingException>(() =>
                _courseService.InviteStudentByEmailAsync(_teacherId, _classId, _studentEmail));

            _mockRepo.Verify(r => r.UpdateClassMemberStatusAsync(_classId, student.UserId, MemberStatus.Active), Times.Once);
        }
    }
}