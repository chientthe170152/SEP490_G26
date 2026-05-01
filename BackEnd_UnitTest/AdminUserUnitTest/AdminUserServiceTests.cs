using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Common;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.Admin;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Backend_UnitTest.AdminUserUnitTest
{
    public class AdminUserServiceTests
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Mock<IAdminUserRepository> _repo = new();
        private readonly Mock<IRefreshTokenStore> _tokens = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly AdminUserService _service;

        public AdminUserServiceTests()
        {
            _service = new AdminUserService(_repo.Object, _tokens.Object, _email.Object, new FakeTimeProvider(FixedNow));
        }

        // ---------- ListAsync ----------

        [Fact(DisplayName = "ListAsync - UTCID01 - Trả về danh sách + map đúng")]
        public async Task ListAsync_UTCID01_ShouldMapResponse()
        {
            var query = new AdminUserListQuery { Page = 2, PageSize = 30 };
            var users = new List<User>
            {
                new() { UserId = 1, Email = "a@a", FullName = "A", RoleId = RoleIds.TeacherInt, Status = UserStatus.Active, MustChangePassword = false }
            };
            _repo.Setup(r => r.ListAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((users, 100));

            var result = await _service.ListAsync(query);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal(100, result.Value.Total);
            Assert.Equal(2, result.Value.Page);
            Assert.Equal(30, result.Value.PageSize);
            Assert.Equal("Teacher", result.Value.Items[0].RoleName);
        }

        [Fact(DisplayName = "ListAsync - UTCID02 - Page<1 -> clamp về 1")]
        public async Task ListAsync_UTCID02_PageLessThanOne_ClampsToOne()
        {
            var query = new AdminUserListQuery { Page = 0, PageSize = 20 };
            _repo.Setup(r => r.ListAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<User>(), 0));

            var result = await _service.ListAsync(query);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.Page);
        }

        [Fact(DisplayName = "ListAsync - UTCID03 - PageSize>100 -> clamp về 100")]
        public async Task ListAsync_UTCID03_PageSizeGtMax_Clamp100()
        {
            var query = new AdminUserListQuery { Page = 1, PageSize = 500 };
            _repo.Setup(r => r.ListAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<User>(), 0));

            var result = await _service.ListAsync(query);

            Assert.Equal(100, result.Value.PageSize);
        }

        [Fact(DisplayName = "ListAsync - UTCID04 - User có Role nav -> RoleName từ entity")]
        public async Task ListAsync_UTCID04_UserHasNavRole_ShouldUseEntityName()
        {
            var query = new AdminUserListQuery();
            var users = new List<User>
            {
                new()
                {
                    UserId = 1, Email = "a@a", FullName = "A",
                    RoleId = RoleIds.StudentInt, Role = new Role { Name = "Sinh viên", RoleId = RoleIds.StudentInt },
                    Status = UserStatus.Active
                }
            };
            _repo.Setup(r => r.ListAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync((users, 1));

            var result = await _service.ListAsync(query);

            Assert.Equal("Sinh viên", result.Value.Items[0].RoleName);
        }

        // ---------- GetAsync ----------

        [Fact(DisplayName = "GetAsync - UTCID01 - User tồn tại -> map item")]
        public async Task GetAsync_UTCID01_UserExists_ShouldReturnItem()
        {
            var u = new User { UserId = 5, Email = "z@z", FullName = "Z", RoleId = RoleIds.AdminInt, Status = UserStatus.Active };
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(u);

            var result = await _service.GetAsync(5);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value.UserId);
            Assert.Equal("Admin", result.Value.RoleName);
        }

        [Fact(DisplayName = "GetAsync - UTCID02 - User không tồn tại -> NotFound")]
        public async Task GetAsync_UTCID02_NotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

            var result = await _service.GetAsync(99);

            Assert.True(result.IsFailure);
            Assert.Equal(AdminUserErrors.NotFound.Code, result.Error.Code);
        }

        // ---------- CreateAsync ----------

        [Fact(DisplayName = "CreateAsync - UTCID01 - RoleId không hợp lệ (Admin) -> InvalidRole")]
        public async Task CreateAsync_UTCID01_InvalidRole_AdminId()
        {
            var req = new CreateUserRequest { Email = "x@x", FullName = "X", RoleId = RoleIds.AdminInt };

            var result = await _service.CreateAsync(req);

            Assert.True(result.IsFailure);
            Assert.Equal(AdminUserErrors.InvalidRole.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAsync - UTCID02 - RoleId null -> InvalidRole")]
        public async Task CreateAsync_UTCID02_NullRole()
        {
            var req = new CreateUserRequest { Email = "x@x", FullName = "X", RoleId = null };

            var result = await _service.CreateAsync(req);

            Assert.Equal(AdminUserErrors.InvalidRole.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAsync - UTCID03 - Email đã tồn tại -> EmailExists")]
        public async Task CreateAsync_UTCID03_EmailExists()
        {
            var req = new CreateUserRequest { Email = "x@x", FullName = "X", RoleId = RoleIds.TeacherInt };
            _repo.Setup(r => r.EmailExistsAsync("x@x", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _service.CreateAsync(req);

            Assert.Equal(AdminUserErrors.EmailExists.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAsync - UTCID04 - Hợp lệ + email send OK -> trả về CreatedResponse")]
        public async Task CreateAsync_UTCID04_Success()
        {
            var req = new CreateUserRequest
            {
                Email = "new@x",
                FullName = "  New User  ",
                RoleId = RoleIds.TeacherInt,
                PhoneNumber = "  0123  ",
                StudentId = "  S1  "
            };
            _repo.Setup(r => r.EmailExistsAsync("new@x", It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            User? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((u, _) => { u.UserId = 99; added = u; })
                .ReturnsAsync((User u, CancellationToken _) => u);

            var result = await _service.CreateAsync(req);

            Assert.True(result.IsSuccess);
            Assert.Equal(99, result.Value.UserId);
            Assert.Equal("new@x", result.Value.Email);
            Assert.NotNull(added);
            Assert.Equal("New User", added!.FullName);
            Assert.Equal("0123", added.PhoneNumber);
            Assert.Equal("S1", added.StudentId);
            Assert.True(added.MustChangePassword);
            Assert.Equal(UserStatus.Active, added.Status);
            Assert.False(string.IsNullOrEmpty(added.PasswordHash));
        }

        [Fact(DisplayName = "CreateAsync - UTCID05 - PhoneNumber/StudentId rỗng -> set null")]
        public async Task CreateAsync_UTCID05_BlankOptionalFields_ShouldBeNull()
        {
            var req = new CreateUserRequest
            {
                Email = "n@x",
                FullName = "N",
                RoleId = RoleIds.StudentInt,
                PhoneNumber = "  ",
                StudentId = ""
            };
            _repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            User? added = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((u, _) => added = u)
                .ReturnsAsync((User u, CancellationToken _) => u);

            await _service.CreateAsync(req);

            Assert.NotNull(added);
            Assert.Null(added!.PhoneNumber);
            Assert.Null(added.StudentId);
        }

        [Fact(DisplayName = "CreateAsync - UTCID06 - Email send fail -> EmailSendFailed, không Add user")]
        public async Task CreateAsync_UTCID06_EmailFail_ShouldReturnError()
        {
            var req = new CreateUserRequest { Email = "fail@x", FullName = "F", RoleId = RoleIds.TeacherInt };
            _repo.Setup(r => r.EmailExistsAsync("fail@x", It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("smtp"));

            var result = await _service.CreateAsync(req);

            Assert.Equal(AdminUserErrors.EmailSendFailed.Code, result.Error.Code);
            _repo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- LockAsync ----------

        [Fact(DisplayName = "LockAsync - UTCID01 - Lock chính mình -> CannotLockSelf")]
        public async Task LockAsync_UTCID01_LockSelf()
        {
            var result = await _service.LockAsync(1, 1);

            Assert.Equal(AdminUserErrors.CannotLockSelf.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LockAsync - UTCID02 - Không tìm thấy user -> NotFound")]
        public async Task LockAsync_UTCID02_NotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

            var result = await _service.LockAsync(2, 1);

            Assert.Equal(AdminUserErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LockAsync - UTCID03 - User là admin -> CannotModifyAdmin")]
        public async Task LockAsync_UTCID03_TargetIsAdmin()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { UserId = 2, Email = "a@a", RoleId = RoleIds.AdminInt });

            var result = await _service.LockAsync(2, 1);

            Assert.Equal(AdminUserErrors.CannotModifyAdmin.Code, result.Error.Code);
        }

        [Fact(DisplayName = "LockAsync - UTCID04 - Hợp lệ -> update status + revoke tokens")]
        public async Task LockAsync_UTCID04_Success()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { UserId = 2, Email = "x@x", RoleId = RoleIds.TeacherInt });

            var result = await _service.LockAsync(2, 1);

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.UpdateStatusAsync(2, UserStatus.Locked, It.IsAny<CancellationToken>()), Times.Once);
            _tokens.Verify(t => t.RevokeAllAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------- UnlockAsync ----------

        [Fact(DisplayName = "UnlockAsync - UTCID01 - Không tìm thấy -> NotFound")]
        public async Task UnlockAsync_UTCID01_NotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

            var result = await _service.UnlockAsync(2);

            Assert.Equal(AdminUserErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "UnlockAsync - UTCID02 - Là admin -> CannotModifyAdmin")]
        public async Task UnlockAsync_UTCID02_AdminTarget()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { RoleId = RoleIds.AdminInt, Email = "" });

            var result = await _service.UnlockAsync(2);

            Assert.Equal(AdminUserErrors.CannotModifyAdmin.Code, result.Error.Code);
        }

        [Fact(DisplayName = "UnlockAsync - UTCID03 - Hợp lệ -> update status active")]
        public async Task UnlockAsync_UTCID03_Success()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { UserId = 2, RoleId = RoleIds.StudentInt, Email = "x@x" });

            var result = await _service.UnlockAsync(2);

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.UpdateStatusAsync(2, UserStatus.Active, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------- ResetPasswordAsync ----------

        [Fact(DisplayName = "ResetPasswordAsync - UTCID01 - Không tìm thấy -> NotFound")]
        public async Task ResetPassword_UTCID01_NotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

            var result = await _service.ResetPasswordAsync(2);

            Assert.Equal(AdminUserErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResetPasswordAsync - UTCID02 - Là admin -> CannotModifyAdmin")]
        public async Task ResetPassword_UTCID02_AdminTarget()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { RoleId = RoleIds.AdminInt, Email = "" });

            var result = await _service.ResetPasswordAsync(2);

            Assert.Equal(AdminUserErrors.CannotModifyAdmin.Code, result.Error.Code);
        }

        [Fact(DisplayName = "ResetPasswordAsync - UTCID03 - Email send fail -> EmailSendFailed, không update password")]
        public async Task ResetPassword_UTCID03_EmailFail()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { UserId = 2, RoleId = RoleIds.StudentInt, Email = "x@x" });
            _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("smtp"));

            var result = await _service.ResetPasswordAsync(2);

            Assert.Equal(AdminUserErrors.EmailSendFailed.Code, result.Error.Code);
            _repo.Verify(r => r.UpdatePasswordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "ResetPasswordAsync - UTCID04 - Hợp lệ -> update password + revoke tokens")]
        public async Task ResetPassword_UTCID04_Success()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { UserId = 2, RoleId = RoleIds.TeacherInt, Email = "x@x" });
            _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _service.ResetPasswordAsync(2);

            Assert.True(result.IsSuccess);
            Assert.Equal("x@x", result.Value.Email);
            _repo.Verify(r => r.UpdatePasswordAsync(2, It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
            _tokens.Verify(t => t.RevokeAllAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;
            public FakeTimeProvider(DateTimeOffset now) => _now = now;
            public override DateTimeOffset GetUtcNow() => _now;
        }
    }
}
