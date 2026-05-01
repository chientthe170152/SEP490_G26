using System;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest
{
    public class UpdateProfileAsyncTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly ProfileService _service;

        public UpdateProfileAsyncTests()
        {
            _mockRepo = new Mock<IProfileRepository>(MockBehavior.Strict);
            _service = new ProfileService(_mockRepo.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID01 - User not found -> NotFound error")]
        public async Task UpdateProfileAsync_UTCID01_UserNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            int userId = 999;
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = null };
            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.NotFound.Code, result.Error.Code);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID02 - Valid non-student update -> Success")]
        public async Task UpdateProfileAsync_UTCID02_ValidNonStudentUpdate_ShouldReturnSuccess()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserId = userId,
                Email = "teacher@example.com",
                RoleId = 1,
                Status = 1,
                FullName = "Old Name",
                PhoneNumber = "0999999999",
                StudentId = "SE000001"
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "  Nguyen Van A  ",
                PhoneNumber = "   ",
                StudentId = null
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Nguyen Van A", user.FullName);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.StudentId);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID03 - Student role + whitespace StudentId -> StudentIdRequired error")]
        public async Task UpdateProfileAsync_UTCID03_StudentRoleWithWhitespaceStudentId_ShouldReturnStudentIdRequiredError()
        {
            // Arrange
            int userId = 5;
            var user = new User { UserId = userId, Email = "s@y.com", RoleId = 2, Status = 1 };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = "   " };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.StudentIdRequired.Code, result.Error.Code);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID04 - Student role + null StudentId -> StudentIdRequired error")]
        public async Task UpdateProfileAsync_UTCID04_StudentRoleWithNullStudentId_ShouldReturnStudentIdRequiredError()
        {
            // Arrange
            int userId = 6;
            var user = new User
            {
                UserId = userId,
                Email = "student@y.com",
                RoleId = 2,
                Status = 1,
                FullName = "Old Name",
                PhoneNumber = "0123456789",
                StudentId = "HE172047"
            };
            var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = null, StudentId = null };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.StudentIdRequired.Code, result.Error.Code);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID05 - Valid student update -> Success")]
        public async Task UpdateProfileAsync_UTCID05_ValidStudentUpdate_ShouldReturnSuccess()
        {
            // Arrange
            int userId = 7;
            var user = new User
            {
                UserId = userId,
                Email = "student@y.com",
                RoleId = 2,
                Status = 1,
                FullName = "Old Name",
                PhoneNumber = null,
                StudentId = null
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "Nguyen Van B",
                PhoneNumber = "0123456789",
                StudentId = "  HE172047 "
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Nguyen Van B", user.FullName);
            Assert.Equal("0123456789", user.PhoneNumber);
            Assert.Equal("HE172047", user.StudentId);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID06 - Non-student with StudentId provided -> trimmed and saved successfully")]
        public async Task UpdateProfileAsync_UTCID06_NonStudentWithStudentId_ShouldTrimAndSaveSuccessfully()
        {
            // Arrange
            int userId = 8;
            var user = new User
            {
                UserId = userId,
                Email = "t@y.com",
                RoleId = 1,
                Status = 1,
                FullName = "Old",
                PhoneNumber = null,
                StudentId = null
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "Nguyen Van A",
                PhoneNumber = " 0123456789 ",
                StudentId = "  HE172047 "
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProfileAsync(userId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Nguyen Van A", user.FullName);
            Assert.Equal("0123456789", user.PhoneNumber);
            Assert.Equal("HE172047", user.StudentId);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.VerifyAll();
        }
    }
}
