using Backend.Common.Errors;
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
    // F5 - CreateCourseAsync
    public class CreateCourseAsyncTests
    {
        private readonly Mock<ICourseRepository> _mockRepo;
        private readonly CourseService _service;

        public CreateCourseAsyncTests()
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

        private static CreateCourseRequestDTO BaseRequest() => new CreateCourseRequestDTO
        {
            ClassName = "Math 101",
            SubjectId = 1,
            Semester = "sp2026"
        };

        // UTCID01 - lớp bị trùng (duplicate) → Duplicate error
        [Fact(DisplayName = "CreateCourseAsync - UTCID01 - Lớp bị trùng → Duplicate error")]
        public async Task CreateCourseAsync_UTCID01_DuplicateClass_ShouldReturnDuplicateError()
        {
            // Arrange
            int teacherId = 1;
            var dto = BaseRequest();
            _mockRepo.Setup(r => r.GetDuplicateClassErrorAsync(teacherId, dto.ClassName!, "SP2026", dto.SubjectId!.Value))
                     .ReturnsAsync("duplicate");

            // Act
            var result = await _service.CreateCourseAsync(teacherId, dto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.Duplicate.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        // UTCID02 - hợp lệ, semester được normalize → tạo lớp thành công
        [Fact(DisplayName = "CreateCourseAsync - UTCID02 - Hợp lệ → tạo lớp thành công")]
        public async Task CreateCourseAsync_UTCID02_ValidRequest_ShouldCreateCourseSuccessfully()
        {
            // Arrange
            int teacherId = 1;
            var dto = BaseRequest();
            var createdCourse = new CourseDTO
            {
                ClassId = 10,
                ClassName = dto.ClassName!,
                Semester = "SP2026"
            };

            _mockRepo.Setup(r => r.GetDuplicateClassErrorAsync(teacherId, dto.ClassName!, "SP2026", dto.SubjectId!.Value))
                     .ReturnsAsync((string?)null);
            _mockRepo.Setup(r => r.CreateCourseAsync(It.IsAny<Class>()))
                     .ReturnsAsync(createdCourse);

            // Act
            var result = await _service.CreateCourseAsync(teacherId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value.ClassId);
            Assert.Equal("SP2026", result.Value.Semester);
            _mockRepo.VerifyAll();
        }

        // UTCID03 - semester null → normalize thành chuỗi rỗng, vẫn tạo được
        [Fact(DisplayName = "CreateCourseAsync - UTCID03 - semester null → normalize thành rỗng, tạo thành công")]
        public async Task CreateCourseAsync_UTCID03_NullSemester_ShouldNormalizeAndCreate()
        {
            // Arrange
            int teacherId = 2;
            var dto = new CreateCourseRequestDTO
            {
                ClassName = "Physics 101",
                SubjectId = 2,
                Semester = null
            };

            var createdCourse = new CourseDTO { ClassId = 20, ClassName = "Physics 101", Semester = "" };

            _mockRepo.Setup(r => r.GetDuplicateClassErrorAsync(teacherId, "Physics 101", string.Empty, 2))
                     .ReturnsAsync((string?)null);
            _mockRepo.Setup(r => r.CreateCourseAsync(It.IsAny<Class>()))
                     .ReturnsAsync(createdCourse);

            // Act
            var result = await _service.CreateCourseAsync(teacherId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value.ClassId);
            _mockRepo.VerifyAll();
        }
    }
}
