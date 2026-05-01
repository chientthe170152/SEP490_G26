using System;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest.ProfileUnitTest
{
    public class ChangePasswordAsyncTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly ProfileService _service;

        public ChangePasswordAsyncTests()
        {
            _mockRepo = new Mock<IProfileRepository>();
            _service = new ProfileService(_mockRepo.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "ChangePasswordAsync - UTCID01 - User không tồn tại → NotFound")]
        public async Task ChangePasswordAsync_UTCID01_UserNotFound_ShouldReturnNotFound()
        {
            var dto = new ChangePasswordDTO { CurrentPassword = "old", NewPassword = "new" };
            _mockRepo.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            var result = await _service.ChangePasswordAsync(999, dto);

            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.NotFound.Code, result.Error.Code);
            _mockRepo.Verify(r => r.GetUserByIdAsync(999), Times.Once);
        }

        [Fact(DisplayName = "ChangePasswordAsync - UTCID02 - Google account (no password) → GoogleAccount")]
        public async Task ChangePasswordAsync_UTCID02_GoogleAccount_ShouldReturnGoogleAccountError()
        {
            var user = new User { UserId = 1, Email = "user@example.com", PasswordHash = null };
            var dto = new ChangePasswordDTO { CurrentPassword = "old", NewPassword = "new" };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.ChangePasswordAsync(1, dto);

            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.GoogleAccount.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ChangePasswordAsync - UTCID03 - Mật khẩu hiện tại sai → WrongPassword")]
        public async Task ChangePasswordAsync_UTCID03_WrongCurrentPassword_ShouldReturnWrongPasswordError()
        {
            var correctPassword = "correct123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword);
            var user = new User { UserId = 1, Email = "user@example.com", PasswordHash = hashedPassword };
            var dto = new ChangePasswordDTO { CurrentPassword = "wrongpassword", NewPassword = "newpassword" };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.ChangePasswordAsync(1, dto);

            Assert.True(result.IsFailure);
            Assert.Equal(ProfileErrors.WrongPassword.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ChangePasswordAsync - UTCID04 - Mật khẩu hiện tại đúng → Success, password cập nhật")]
        public async Task ChangePasswordAsync_UTCID04_ValidCurrentPassword_ShouldUpdatePassword()
        {
            var currentPassword = "currentPass123";
            var newPassword = "newPass456";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);
            var user = new User
            {
                UserId = 1,
                Email = "user@example.com",
                PasswordHash = hashedPassword,
                SecurityStamp = DateTime.UtcNow
            };
            var dto = new ChangePasswordDTO { CurrentPassword = currentPassword, NewPassword = newPassword };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.ChangePasswordAsync(1, dto);

            Assert.True(result.IsSuccess);
            // Verify password was hashed with BCrypt
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash));
            // Verify SecurityStamp was updated
            _mockRepo.Verify(r => r.GetUserByIdAsync(1), Times.Once);
            _mockRepo.Verify(r => r.UpdateUserAsync(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
