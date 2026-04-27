using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F9 - InviteStudentByEmailAsync
// Source: CourseService.cs:120-183
// Branches:
//   1. trustedFrontendBase null/whitespace                 -> throw "Lỗi khi thêm học sinh vào lớp."
//   2. user == null                                        -> throw "Học sinh chưa có tài khoản trong hệ thống."
//   3. user.Role.Name != "Student"                         -> throw "Chỉ có thể mời người dùng có vai trò là học sinh..."
//   4. course == null                                      -> throw "Không tìm thấy lớp học."
//   5. course.Status == Closed                             -> throw "Lớp học đã bị đóng, không thể mời thêm học sinh."
//   6. existingMembership.Active                           -> throw "Học sinh này đã tham gia lớp học."
//   7. existingMembership.Invited                          -> throw "Học sinh này đã được gửi lời mời trước đó."
//   8. existingMembership.Pending                          -> auto-approve + throw AutoApprovePendingException
//   9. InviteStudentAsync returns null                     -> throw "Không thể tạo lời mời."
//  10. Success                                             -> generate token, send email, return token
public class F9_InviteStudentByEmailAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F9_InviteStudentByEmailAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    private static User StudentUser(int userId = 5, string email = "stu@test.com")
        => UserBuilder.New().WithId(userId).WithEmail(email).WithRole(UserRoles.Student, 2).Build();

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID01 - Valid + chưa membership -> tạo invite + gửi email")]
    [TestType("N")]
    public async Task InviteStudentByEmailAsync_UTCID01_Valid_ShouldCreateInvite()
    {
        var stu = StudentUser();
        var course = new CourseDTO { ClassId = 10, ClassName = "Toán 12", Status = ClassStatus.Active };
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(course);
        _repoMock.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync((ClassMember?)null);
        _repoMock.Setup(r => r.InviteStudentAsync(10, 5)).ReturnsAsync(new ClassMember
        {
            ClassId = 10, StudentId = 5, MemberStatus = MemberStatus.Invited,
            ConcurrencyStamp = new byte[] { 0x01, 0x02, 0x03 }
        });
        _emailMock.Setup(e => e.SendEmailAsync("stu@test.com", It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var token = await _service.InviteStudentByEmailAsync(1, 10, "stu@test.com");

        Assert.False(string.IsNullOrEmpty(token));
        _emailMock.VerifyAll();
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID02 - FrontendBaseUrl trống -> throw 'Lỗi khi thêm học sinh'")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID02_NoFrontendBaseUrl_ShouldThrow()
    {
        var configWithoutBase = TestConfigBuilder.Override(("FrontendSettings:BaseUrl", null));
        var service = new CourseService(_repoMock.Object, _emailMock.Object, configWithoutBase);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.InviteStudentByEmailAsync(1, 10, "x@y.com"));
        Assert.Equal("Lỗi khi thêm học sinh vào lớp.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID03 - User không tồn tại -> throw")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID03_UserNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("ghost@test.com")).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "ghost@test.com"));
        Assert.Equal("Học sinh chưa có tài khoản trong hệ thống.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID04 - User không phải Student -> throw")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID04_NotStudentRole_ShouldThrow()
    {
        var teacher = UserBuilder.New().WithEmail("teacher@test.com").WithRole(UserRoles.Teacher, 1).Build();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("teacher@test.com")).ReturnsAsync(teacher);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "teacher@test.com"));
        Assert.Equal("Chỉ có thể mời người dùng có vai trò là học sinh tham gia lớp học.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID05 - Lớp không tồn tại -> throw 'Không tìm thấy lớp học'")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID05_ClassNotFound_ShouldThrow()
    {
        var stu = StudentUser();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CourseDTO?)null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 99, "stu@test.com"));
        Assert.Equal("Không tìm thấy lớp học.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID06 - Lớp đã Closed -> throw")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID06_ClassClosed_ShouldThrow()
    {
        var stu = StudentUser();
        var course = new CourseDTO { ClassId = 10, ClassName = "X", Status = ClassStatus.Closed };
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(course);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "stu@test.com"));
        Assert.Equal("Lớp học đã bị đóng, không thể mời thêm học sinh.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID07 - Đã là Active member -> throw")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID07_AlreadyActive_ShouldThrow()
    {
        var stu = StudentUser();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, ClassName = "X", Status = ClassStatus.Active });
        _repoMock.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync(new ClassMember { ClassId = 10, StudentId = 5, MemberStatus = MemberStatus.Active, ConcurrencyStamp = Array.Empty<byte>() });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "stu@test.com"));
        Assert.Equal("Học sinh này đã tham gia lớp học.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID08 - Đã Invited -> throw")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID08_AlreadyInvited_ShouldThrow()
    {
        var stu = StudentUser();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, ClassName = "X", Status = ClassStatus.Active });
        _repoMock.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync(new ClassMember { ClassId = 10, StudentId = 5, MemberStatus = MemberStatus.Invited, ConcurrencyStamp = Array.Empty<byte>() });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "stu@test.com"));
        Assert.Equal("Học sinh này đã được gửi lời mời trước đó.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID09 - Đang Pending -> auto-approve + throw AutoApprovePendingException")]
    [TestType("B")]
    public async Task InviteStudentByEmailAsync_UTCID09_Pending_ShouldAutoApprove()
    {
        var stu = StudentUser();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, ClassName = "X", Status = ClassStatus.Active });
        _repoMock.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync(new ClassMember { ClassId = 10, StudentId = 5, MemberStatus = MemberStatus.Pending, ConcurrencyStamp = Array.Empty<byte>() });
        _repoMock.Setup(r => r.UpdateClassMemberStatusAsync(10, 5, MemberStatus.Active)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<AutoApprovePendingException>(
            () => _service.InviteStudentByEmailAsync(1, 10, "stu@test.com"));

        _repoMock.Verify(r => r.UpdateClassMemberStatusAsync(10, 5, MemberStatus.Active), Times.Once);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID11 - User.Role = null -> throw 'Chỉ có thể mời học sinh' (boundary cho ?.)")]
    [TestType("B")]
    public async Task InviteStudentByEmailAsync_UTCID11_RoleNull_ShouldThrow()
    {
        var stu = UserBuilder.New().WithEmail("noRole@test.com").Build();
        stu.Role = null!; // no role attached
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("noRole@test.com")).ReturnsAsync(stu);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "noRole@test.com"));
        Assert.Equal("Chỉ có thể mời người dùng có vai trò là học sinh tham gia lớp học.", ex.Message);
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID12 - existingMembership có MemberStatus lạ (defensive) -> rơi xuống InviteStudentAsync")]
    [TestType("B")]
    public async Task InviteStudentByEmailAsync_UTCID12_UnknownMemberStatus_ShouldFallthroughInvite()
    {
        var stu = StudentUser();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, ClassName = "X", Status = ClassStatus.Active });
        _repoMock.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync(new ClassMember
        {
            ClassId = 10, StudentId = 5, MemberStatus = 99, // unknown status — defensive fallthrough
            ConcurrencyStamp = Array.Empty<byte>()
        });
        _repoMock.Setup(r => r.InviteStudentAsync(10, 5)).ReturnsAsync(new ClassMember
        {
            ClassId = 10, StudentId = 5, MemberStatus = MemberStatus.Invited,
            ConcurrencyStamp = new byte[] { 0x01 }
        });
        _emailMock.Setup(e => e.SendEmailAsync("stu@test.com", It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var token = await _service.InviteStudentByEmailAsync(1, 10, "stu@test.com");

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID10 - InviteStudentAsync trả null -> throw")]
    [TestType("A")]
    public async Task InviteStudentByEmailAsync_UTCID10_InviteReturnsNull_ShouldThrow()
    {
        var stu = StudentUser();
        _repoMock.Setup(r => r.GetUserWithRoleByEmailAsync("stu@test.com")).ReturnsAsync(stu);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, ClassName = "X", Status = ClassStatus.Active });
        _repoMock.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync((ClassMember?)null);
        _repoMock.Setup(r => r.InviteStudentAsync(10, 5)).ReturnsAsync((ClassMember?)null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.InviteStudentByEmailAsync(1, 10, "stu@test.com"));
        Assert.Equal("Không thể tạo lời mời.", ex.Message);
    }
}
