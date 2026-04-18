using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
namespace Backend_UnitTest
{
    public class RemoveStudentFromClassAsync
    {
        private readonly Mock<ICourseRepo> _mockRepo;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly CourseService _courseService;
        public RemoveStudentFromClassAsync()
        {
            _mockRepo = new Mock<ICourseRepo>();
            _mockEmail = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();
            _courseService = new CourseService(
                    _mockRepo.Object,
                    _mockEmail.Object,
                    _mockConfig.Object
                );
        }

        [Fact]
        public async Task RemoveStudentFromClassAsync_TeacherNotAuthorized_ThrowsException()
        {
            // Arrange
            int teacherId = 1, classId = 1, studentId = 2;
            _mockRepo.Setup(r => r.IsTeacherOfClassAsync(classId, teacherId)).ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.RemoveStudentFromClassAsync(teacherId, classId, studentId));
            Assert.Equal("Bạn không có quyền quản lý lớp này.", exception.Message);
        }

        [Fact]
        public async Task RemoveStudentFromClassAsync_StudentNotFoundOrInactive_ThrowsException()
        {
            // Arrange
            int teacherId = 1, classId = 1, studentId = 2;
            _mockRepo.Setup(r => r.IsTeacherOfClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetClassMemberAsync(classId, studentId)).ReturnsAsync((ClassMember)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.RemoveStudentFromClassAsync(teacherId, classId, studentId));
            Assert.Equal("Học sinh không thuộc lớp hoặc không ở trạng thái đang học.", exception.Message);
        }

        [Fact]
        public async Task RemoveStudentFromClassAsync_StudentHasActiveSubmission_ThrowsException()
        {
            // Arrange
            int teacherId = 1, classId = 1, studentId = 2;
            var activeMember = new ClassMember { MemberStatus = MemberStatus.Active };

            _mockRepo.Setup(r => r.IsTeacherOfClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetClassMemberAsync(classId, studentId)).ReturnsAsync(activeMember);
            _mockRepo.Setup(r => r.StudentHasInProgressSubmissionInClassAsync(classId, studentId)).ReturnsAsync(true);
            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.RemoveStudentFromClassAsync(teacherId, classId, studentId));
            Assert.Equal("Không thể xóa học sinh đang làm dở một bài kiểm tra trong lớp.", exception.Message);
        }

        [Fact]
        public async Task RemoveStudentFromClassAsync_DeleteFailedInRepo_ThrowsException()
        {
            // Arrange
            int teacherId = 1, classId = 1, studentId = 2;
            var activeMember = new ClassMember { MemberStatus = MemberStatus.Active };

            _mockRepo.Setup(r => r.IsTeacherOfClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetClassMemberAsync(classId, studentId)).ReturnsAsync(activeMember);
            _mockRepo.Setup(r => r.StudentHasInProgressSubmissionInClassAsync(classId, studentId)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.RemoveActiveStudentFromClassAsync(classId, studentId)).ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.RemoveStudentFromClassAsync(teacherId, classId, studentId));
            Assert.Equal("Không thể xóa học sinh khỏi lớp.", exception.Message);
        }

        [Fact]
        public async Task RemoveStudentFromClassAsync_Success_CompletesTask()
        {
            // Arrange
            int teacherId = 1, classId = 1, studentId = 2;
            var activeMember = new ClassMember { MemberStatus = MemberStatus.Active };

            _mockRepo.Setup(r => r.IsTeacherOfClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetClassMemberAsync(classId, studentId)).ReturnsAsync(activeMember);
            _mockRepo.Setup(r => r.StudentHasInProgressSubmissionInClassAsync(classId, studentId)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.RemoveActiveStudentFromClassAsync(classId, studentId)).ReturnsAsync(true);

            // Act
            var exception = await Record.ExceptionAsync(() =>
                _courseService.RemoveStudentFromClassAsync(teacherId, classId, studentId));
            // Assert
            Assert.Null(exception); // Không có exception nào được ném ra
            _mockRepo.Verify(r => r.RemoveActiveStudentFromClassAsync(classId, studentId), Times.Once);
        }
        [Fact]
        public async Task RemoveStudentFromClassAsync_DatabaseConnectionError_ThrowsException()
        {
            // Arrange
            int teacherId = 1, classId = 1, studentId = 2;
            var activeMember = new ClassMember { MemberStatus = MemberStatus.Active };

            _mockRepo.Setup(r => r.IsTeacherOfClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetClassMemberAsync(classId, studentId)).ReturnsAsync(activeMember);
            _mockRepo.Setup(r => r.StudentHasInProgressSubmissionInClassAsync(classId, studentId)).ReturnsAsync(false);

            // Mô phỏng Repository ném ra lỗi Database (ví dụ: SqlException hoặc DbUpdateException)
            _mockRepo.Setup(r => r.RemoveActiveStudentFromClassAsync(classId, studentId))
                     .ThrowsAsync(new Exception("Lỗi kết nối cơ sở dữ liệu."));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _courseService.RemoveStudentFromClassAsync(teacherId, classId, studentId));

            // Kiểm tra xem Service có ném đúng thông báo lỗi từ DB ra hay không
            Assert.Equal("Lỗi kết nối cơ sở dữ liệu.", exception.Message);
        }
    }
}