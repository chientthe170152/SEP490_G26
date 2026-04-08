using Backend.Models;
using Backend.UnitTest;
using Moq;

namespace Backend_UnitTest.CourseUnitTest
{
    public class JoinCourseAsyncTests : CourseTestBase
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task JoinCourseAsync_EmptyInviteCode_ShouldThrowException(string code)
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, code));
            Assert.Equal("Mã mời không thể trống.", ex.Message);
        }

        [Fact]
        public async Task JoinCourseAsync_InvalidCode_ShouldThrowException()
        {
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync("WRONG")).ReturnsAsync((Class?)null);
            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, "WRONG"));
            Assert.Equal("Mã mời không chính xác hoặc lớp học đã bị đóng.", ex.Message);
        }

        [Fact]
        public async Task JoinCourseAsync_AlreadyInClass_ShouldThrowException()
        {
            var course = new Class { ClassId = 10 };
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync("VALID")).ReturnsAsync(course);
            _mockRepo.Setup(r => r.IsUserInClassAsync(10, 1)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, "VALID"));
            Assert.Equal("Bạn đã ở trong lớp học này rồi.", ex.Message);
        }
        [Fact]
        public async Task JoinCourseAsync_ValidRequest_ShouldJoinSuccessfully()
        {
            // Arrange: Giả lập mọi điều kiện đều thỏa mãn
            int studentId = 1;
            string inviteCode = "VALID_CODE";
            var course = new Class { ClassId = 10 };

            // 1. Tìm thấy khóa học
            _mockRepo.Setup(r => r.GetClassByInviteCodeAsync(inviteCode))
                     .ReturnsAsync(course);

            // 2. QUAN TRỌNG: Trả về FALSE để đi qua câu lệnh if (để không bị chặn lại)
            _mockRepo.Setup(r => r.IsUserInClassAsync(course.ClassId, studentId))
                     .ReturnsAsync(false);

            // 3. Mock hành động Join thành công
            _mockRepo.Setup(r => r.JoinClassAsync(course.ClassId, studentId))
                     .Returns(Task.CompletedTask);

            // Act: Thực thi hàm
            await _courseService.JoinCourseAsync(studentId, inviteCode);

            // Assert: Xác nhận dòng code cuối cùng (JoinClassAsync) thực sự được gọi
            _mockRepo.Verify(r => r.JoinClassAsync(course.ClassId, studentId), Times.Once);
        }

        [Fact]
        public async Task JoinCourseAsync_WhiteSpaceInviteCode_ShouldThrowException()
        {
            // Bổ sung cho trường hợp mã mời chỉ toàn dấu cách (string.IsNullOrWhiteSpace)
            string code = "   ";

            var ex = await Assert.ThrowsAsync<Exception>(() => _courseService.JoinCourseAsync(1, code));

            Assert.Equal("Mã mời không thể trống.", ex.Message);
        }

    }
}