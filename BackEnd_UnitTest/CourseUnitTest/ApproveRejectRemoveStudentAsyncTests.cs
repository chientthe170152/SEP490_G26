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
    // F11 - ApproveStudentAsync
    public class ApproveStudentAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public ApproveStudentAsyncTests()
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
        [Fact(DisplayName = "ApproveStudentAsync - UTCID01 - Lớp không tồn tại → NotFound")]
        public async Task ApproveStudentAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CourseDTO?)null);
            var result = await _service.ApproveStudentAsync(999, 5);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - lớp đã đóng → Closed
        [Fact(DisplayName = "ApproveStudentAsync - UTCID02 - Lớp đã đóng → Closed")]
        public async Task ApproveStudentAsync_UTCID02_ClassClosed_ShouldReturnClosed()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Closed });
            var result = await _service.ApproveStudentAsync(5, 5);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - student không là thành viên chờ (repo trả false) → NotMember
        [Fact(DisplayName = "ApproveStudentAsync - UTCID03 - Student không tồn tại trong lớp → NotMember")]
        public async Task ApproveStudentAsync_UTCID03_StudentNotMember_ShouldReturnNotMember()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Active });
            _mockRepo.Setup(r => r.ApproveStudentAsync(5, 10)).ReturnsAsync(false);
            var result = await _service.ApproveStudentAsync(5, 10);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotMember.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - hợp lệ → duyệt thành công
        [Fact(DisplayName = "ApproveStudentAsync - UTCID04 - Hợp lệ → duyệt thành công")]
        public async Task ApproveStudentAsync_UTCID04_ValidRequest_ShouldApproveSuccessfully()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Active });
            _mockRepo.Setup(r => r.ApproveStudentAsync(5, 10)).ReturnsAsync(true);
            var result = await _service.ApproveStudentAsync(5, 10);
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }

    // F12 - RejectStudentAsync
    public class RejectStudentAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public RejectStudentAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            var mockEmail = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new CourseService(
                _mockRepo.Object, mockEmail.Object, mockConfig.Object,
                mockCurrentUser.Object, TimeProvider.System);
        }

        // UTCID01 - lớp không tồn tại → NotFound
        [Fact(DisplayName = "RejectStudentAsync - UTCID01 - Lớp không tồn tại → NotFound")]
        public async Task RejectStudentAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CourseDTO?)null);
            var result = await _service.RejectStudentAsync(999, 5);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - lớp đã đóng → Closed
        [Fact(DisplayName = "RejectStudentAsync - UTCID02 - Lớp đã đóng → Closed")]
        public async Task RejectStudentAsync_UTCID02_ClassClosed_ShouldReturnClosed()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Closed });
            var result = await _service.RejectStudentAsync(5, 5);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - student không là thành viên → NotMember
        [Fact(DisplayName = "RejectStudentAsync - UTCID03 - Student không là thành viên → NotMember")]
        public async Task RejectStudentAsync_UTCID03_StudentNotMember_ShouldReturnNotMember()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Active });
            _mockRepo.Setup(r => r.RejectStudentAsync(5, 10)).ReturnsAsync(false);
            var result = await _service.RejectStudentAsync(5, 10);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotMember.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - hợp lệ → từ chối thành công
        [Fact(DisplayName = "RejectStudentAsync - UTCID04 - Hợp lệ → từ chối thành công")]
        public async Task RejectStudentAsync_UTCID04_ValidRequest_ShouldRejectSuccessfully()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Active });
            _mockRepo.Setup(r => r.RejectStudentAsync(5, 10)).ReturnsAsync(true);
            var result = await _service.RejectStudentAsync(5, 10);
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }

    // F13 - RemoveStudentAsync
    public class RemoveStudentAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public RemoveStudentAsyncTests()
        {
            _mockRepo = new Mock<ICourseRepository>(MockBehavior.Strict);
            var mockEmail = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();
            var mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new CourseService(
                _mockRepo.Object, mockEmail.Object, mockConfig.Object,
                mockCurrentUser.Object, TimeProvider.System);
        }

        // UTCID01 - lớp không tồn tại → NotFound
        [Fact(DisplayName = "RemoveStudentAsync - UTCID01 - Lớp không tồn tại → NotFound")]
        public async Task RemoveStudentAsync_UTCID01_ClassNotFound_ShouldReturnNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CourseDTO?)null);
            var result = await _service.RemoveStudentAsync(999, 5);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - lớp đã đóng → Closed
        [Fact(DisplayName = "RemoveStudentAsync - UTCID02 - Lớp đã đóng → Closed")]
        public async Task RemoveStudentAsync_UTCID02_ClassClosed_ShouldReturnClosed()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Closed });
            var result = await _service.RemoveStudentAsync(5, 5);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Closed.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - student không là thành viên → NotMember
        [Fact(DisplayName = "RemoveStudentAsync - UTCID03 - Student không là thành viên → NotMember")]
        public async Task RemoveStudentAsync_UTCID03_StudentNotMember_ShouldReturnNotMember()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Active });
            _mockRepo.Setup(r => r.RemoveStudentAsync(5, 10)).ReturnsAsync(false);
            var result = await _service.RemoveStudentAsync(5, 10);
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotMember.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID04 - hợp lệ → xóa thành viên thành công
        [Fact(DisplayName = "RemoveStudentAsync - UTCID04 - Hợp lệ → xóa thành viên thành công")]
        public async Task RemoveStudentAsync_UTCID04_ValidRequest_ShouldRemoveSuccessfully()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new CourseDTO { ClassId = 5, Status = ClassStatus.Active });
            _mockRepo.Setup(r => r.RemoveStudentAsync(5, 10)).ReturnsAsync(true);
            var result = await _service.RemoveStudentAsync(5, 10);
            Assert.True(result.IsSuccess);
            _mockRepo.VerifyAll();
        }
    }
}
