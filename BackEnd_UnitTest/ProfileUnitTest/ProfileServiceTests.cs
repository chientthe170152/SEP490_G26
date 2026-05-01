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
    public class ProfileServiceTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly ProfileService _service;

        public ProfileServiceTests()
        {
            _mockRepo = new Mock<IProfileRepository>();
            _service = new ProfileService(_mockRepo.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetProfileAsync - UTCID01 - User tồn tại trong DB -> trả UserProfileDTO")]
        public async Task GetProfileAsync_UTCID01_UserExists_ShouldReturnMappedDto()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                StudentId = "HE172047",
                RoleId = 2,
                Status = 1
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            var result = await _service.GetProfileAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(user.UserId, result.Value.UserId);
            Assert.Equal(user.Email, result.Value.Email);
            Assert.Equal(user.FullName, result.Value.FullName);
            Assert.Equal(user.PhoneNumber, result.Value.PhoneNumber);
            Assert.Equal(user.StudentId, result.Value.StudentId);
            Assert.Equal(user.RoleId, result.Value.RoleId);
            Assert.Equal(user.Status, result.Value.Status);
            _mockRepo.Verify(r => r.GetUserByIdAsync(1), Times.Once);
        }

        [Fact(DisplayName = "GetProfileAsync - UTCID02 - User không tồn tại trong DB -> NotFound error")]
        public async Task GetProfileAsync_UTCID02_UserNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

            // Act
            var result = await _service.GetProfileAsync(999);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.NotFound.Code, result.Error.Code);
            _mockRepo.Verify(r => r.GetUserByIdAsync(999), Times.Once);
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID01 - DTO hợp lệ + user tồn tại -> Success, DB updated")]
        public async Task UpdateProfileAsync_UTCID01_UserExists_ValidDto_ShouldUpdateFieldsAndSave()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Old Name",
                PhoneNumber = "000",
                StudentId = "OLD",
                RoleId = 2,
                Status = 1
            };

            var dto = new UpdateProfileDTO
            {
                FullName = "New Name",
                PhoneNumber = "0123456789",
                StudentId = "HE172047"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProfileAsync(1, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(dto.FullName, user.FullName);
            Assert.Equal(dto.PhoneNumber, user.PhoneNumber);
            Assert.Equal(dto.StudentId, user.StudentId);
            _mockRepo.Verify(r => r.GetUserByIdAsync(1), Times.Once);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact(DisplayName = "UpdateProfileAsync - UTCID02 - User không tồn tại -> NotFound error, không save")]
        public async Task UpdateProfileAsync_UTCID02_UserNotFound_ShouldReturnNotFoundError_AndNotSave()
        {
            // Arrange
            var dto = new UpdateProfileDTO
            {
                FullName = "New Name",
                PhoneNumber = "0123456789",
                StudentId = "HE172047"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.UpdateProfileAsync(999, dto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.NotFound.Code, result.Error.Code);
            _mockRepo.Verify(r => r.GetUserByIdAsync(999), Times.Once);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
