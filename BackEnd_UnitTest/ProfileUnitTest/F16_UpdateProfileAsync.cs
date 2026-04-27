using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ProfileUnitTest;

// F16 - UpdateProfileAsync
// Source: ProfileService.cs:36-79
// Branches:
//   1. user == null                                            -> return false
//   2. fullName empty/whitespace                               -> throw ValidationMessages.FullNameRequired
//   3. fullName regex fail                                     -> throw ValidationMessages.FullNameInvalid
//   4. phone provided + regex fail                             -> throw ValidationMessages.PhoneNumberInvalid
//   5. phone empty/whitespace -> user.PhoneNumber = null       (else: trimmed phone)
//   6. RoleId == 2 (Student):
//      6a. studentId empty                                     -> throw StudentIdRequiredForStudent
//      6b. studentId regex fail                                -> throw StudentIdInvalid
//      6c. studentId valid                                     -> user.StudentId = trimmed
//   7. RoleId != 2 (non-student):
//      7a. raw empty                                           -> studentId = null
//      7b. raw provided + regex fail                           -> throw StudentIdInvalid
//      7c. raw provided + valid                                -> studentId = trimmed
public class F16_UpdateProfileAsync_Tests
{
    private readonly Mock<IProfileRepository> _repoMock;
    private readonly ProfileService _service;

    public F16_UpdateProfileAsync_Tests()
    {
        _repoMock = new Mock<IProfileRepository>(MockBehavior.Strict);
        _service = new ProfileService(_repoMock.Object);
    }

    private static User StudentUser(int userId = 1) => UserBuilder.New()
        .WithId(userId)
        .WithEmail("student@test.com")
        .WithRoleId(2)
        .WithFullName("Old Name")
        .WithPhone("0999999999")
        .WithStudentId("HE000001")
        .Build();

    private static User TeacherUser(int userId = 1) => UserBuilder.New()
        .WithId(userId)
        .WithEmail("teacher@test.com")
        .WithRoleId(1)
        .WithFullName("Old Name")
        .WithPhone(null)
        .WithStudentId(null)
        .Build();

    private void SetupUpdateSavePath()
    {
        _repoMock.Setup(r => r.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID01 - Student valid + phone valid + studentId valid -> update + save thành công")]
    [TestType("N")]
    public async Task UpdateProfileAsync_UTCID01_StudentValidFullUpdate_ShouldReturnTrue()
    {
        var user = StudentUser(1);
        var dto = new UpdateProfileDTO
        {
            FullName = "Nguyen Van A",
            PhoneNumber = "0123456789",
            StudentId = "HE172047"
        };
        _repoMock.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
        SetupUpdateSavePath();

        var ok = await _service.UpdateProfileAsync(1, dto);

        Assert.True(ok);
        Assert.Equal("Nguyen Van A", user.FullName);
        Assert.Equal("0123456789", user.PhoneNumber);
        Assert.Equal("HE172047", user.StudentId);
        _repoMock.Verify(r => r.UpdateUserAsync(user), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID02 - Non-student + phone empty + studentId null -> phone/studentId set null, return true")]
    [TestType("N")]
    public async Task UpdateProfileAsync_UTCID02_NonStudentNullPhoneNullStudentId_ShouldClearAndReturnTrue()
    {
        var user = TeacherUser(2);
        user.PhoneNumber = "0999999999";
        user.StudentId = "HE000001";
        var dto = new UpdateProfileDTO
        {
            FullName = "  Nguyen Van A  ",
            PhoneNumber = "   ",
            StudentId = null
        };
        _repoMock.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(user);
        SetupUpdateSavePath();

        var ok = await _service.UpdateProfileAsync(2, dto);

        Assert.True(ok);
        Assert.Equal("Nguyen Van A", user.FullName);
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.StudentId);
        _repoMock.Verify(r => r.UpdateUserAsync(user), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID03 - Non-student + studentId valid (có khoảng trắng) -> trim + cập nhật")]
    [TestType("N")]
    public async Task UpdateProfileAsync_UTCID03_NonStudentValidStudentId_ShouldTrimAndUpdate()
    {
        var user = TeacherUser(3);
        var dto = new UpdateProfileDTO
        {
            FullName = "Nguyen Van A",
            PhoneNumber = " 0123456789 ",
            StudentId = "  HE172047 "
        };
        _repoMock.Setup(r => r.GetUserByIdAsync(3)).ReturnsAsync(user);
        SetupUpdateSavePath();

        var ok = await _service.UpdateProfileAsync(3, dto);

        Assert.True(ok);
        Assert.Equal("Nguyen Van A", user.FullName);
        Assert.Equal("0123456789", user.PhoneNumber);
        Assert.Equal("HE172047", user.StudentId);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID04 - User không tồn tại -> return false, không save")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID04_UserNotFound_ShouldReturnFalse()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789" };

        var ok = await _service.UpdateProfileAsync(999, dto);

        Assert.False(ok);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID05 - FullName whitespace -> throw FullNameRequired")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID05_FullNameWhitespace_ShouldThrowFullNameRequired()
    {
        var user = TeacherUser(5);
        _repoMock.Setup(r => r.GetUserByIdAsync(5)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = "   ", PhoneNumber = "0123456789" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(5, dto));

        Assert.Equal(ValidationMessages.FullNameRequired, ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID06 - FullName chứa số -> throw FullNameInvalid")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID06_FullNameInvalidChars_ShouldThrowFullNameInvalid()
    {
        var user = TeacherUser(6);
        _repoMock.Setup(r => r.GetUserByIdAsync(6)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A1", PhoneNumber = "0123456789" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(6, dto));

        Assert.Equal(ValidationMessages.FullNameInvalid, ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID07 - Phone không bắt đầu bằng 0 -> throw PhoneNumberInvalid")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID07_PhoneInvalid_ShouldThrowPhoneNumberInvalid()
    {
        var user = TeacherUser(7);
        _repoMock.Setup(r => r.GetUserByIdAsync(7)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "1234567890" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(7, dto));

        Assert.Equal(ValidationMessages.PhoneNumberInvalid, ex.Message);
        _repoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID08 - Student + studentId null -> throw StudentIdRequiredForStudent")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID08_StudentMissingStudentId_ShouldThrowStudentIdRequired()
    {
        var user = StudentUser(8);
        _repoMock.Setup(r => r.GetUserByIdAsync(8)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = null };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(8, dto));

        Assert.Equal(ValidationMessages.StudentIdRequiredForStudent, ex.Message);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID09 - Student + studentId sai định dạng -> throw StudentIdInvalid")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID09_StudentInvalidStudentId_ShouldThrowStudentIdInvalid()
    {
        var user = StudentUser(9);
        _repoMock.Setup(r => r.GetUserByIdAsync(9)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = "H172047" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(9, dto));

        Assert.Equal(ValidationMessages.StudentIdInvalid, ex.Message);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID10 - Non-student + studentId provided invalid -> throw StudentIdInvalid")]
    [TestType("A")]
    public async Task UpdateProfileAsync_UTCID10_NonStudentInvalidStudentId_ShouldThrowStudentIdInvalid()
    {
        var user = TeacherUser(10);
        _repoMock.Setup(r => r.GetUserByIdAsync(10)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = "0123456789", StudentId = "H172047" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(10, dto));

        Assert.Equal(ValidationMessages.StudentIdInvalid, ex.Message);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID11 - FullName = null -> throw FullNameRequired (boundary cho ??)")]
    [TestType("B")]
    public async Task UpdateProfileAsync_UTCID11_FullNameNull_ShouldThrowFullNameRequired()
    {
        var user = TeacherUser(11);
        _repoMock.Setup(r => r.GetUserByIdAsync(11)).ReturnsAsync(user);
        var dto = new UpdateProfileDTO { FullName = null, PhoneNumber = "0123456789" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(11, dto));

        Assert.Equal(ValidationMessages.FullNameRequired, ex.Message);
    }

    [Fact(DisplayName = "UpdateProfileAsync - UTCID12 - PhoneNumber = null + valid -> success (boundary cho ??)")]
    [TestType("B")]
    public async Task UpdateProfileAsync_UTCID12_PhoneNumberNull_ShouldClearPhoneAndReturnTrue()
    {
        var user = TeacherUser(12);
        _repoMock.Setup(r => r.GetUserByIdAsync(12)).ReturnsAsync(user);
        SetupUpdateSavePath();
        var dto = new UpdateProfileDTO { FullName = "Nguyen Van A", PhoneNumber = null, StudentId = null };

        var ok = await _service.UpdateProfileAsync(12, dto);

        Assert.True(ok);
        Assert.Null(user.PhoneNumber);
    }
}
