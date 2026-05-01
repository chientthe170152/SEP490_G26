using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Backend_UnitTest.CourseUnitTests
{
    public class CourseServiceMissingCoverageTests
    {
        private readonly Mock<ICourseRepository> _repo = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly IConfiguration _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FrontendSettings:BaseUrl"] = "https://app" })
            .Build();

        private CourseService BuildService() =>
            new(_repo.Object, _email.Object, _config, _currentUser.Object, TimeProvider.System);

        [Fact(DisplayName = "GetStudentsInClassAsync - UTCID01 - Delegate repo")]
        public async Task GetStudentsInClass_DelegatesToRepo()
        {
            var list = new List<StudentInClassDTO>();
            _repo.Setup(r => r.GetStudentsInClassAsync(5)).ReturnsAsync(list);

            var result = await BuildService().GetStudentsInClassAsync(5);

            Assert.Same(list, result);
        }

        [Fact(DisplayName = "GetPendingStudentsAsync - UTCID01 - Delegate repo")]
        public async Task GetPendingStudents_DelegatesToRepo()
        {
            var list = new List<StudentInClassDTO>();
            _repo.Setup(r => r.GetPendingStudentsAsync(5)).ReturnsAsync(list);

            var result = await BuildService().GetPendingStudentsAsync(5);

            Assert.Same(list, result);
        }

        [Fact(DisplayName = "GetSubjectsAsync - UTCID01 - Delegate repo")]
        public async Task GetSubjects_DelegatesToRepo()
        {
            var list = new List<SubjectOptionDto>();
            _repo.Setup(r => r.GetSubjectsAsync()).ReturnsAsync(list);

            var result = await BuildService().GetSubjectsAsync();

            Assert.Same(list, result);
        }

        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID-UnknownStatus - Existing với status không xác định -> rơi qua InviteStudent")]
        public async Task Invite_UnknownStatus_ShouldFallThrough()
        {
            _repo.Setup(r => r.GetUserWithRoleByEmailAsync("s@x"))
                .ReturnsAsync(new User { UserId = 5, Email = "s@x", RoleId = int.Parse(RoleIds.Student) });
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new CourseDTO { ClassId = 10, Status = 1, ClassName = "C" });
            _repo.Setup(r => r.GetClassMemberAsync(10, 5))
                .ReturnsAsync(new ClassMember { ClassId = 10, StudentId = 5, MemberStatus = 99 });
            _repo.Setup(r => r.InviteStudentAsync(10, 5))
                .ReturnsAsync(new ClassMember { ClassId = 10, StudentId = 5, ConcurrencyStamp = new byte[] { 1, 2, 3 } });
            _email.Setup(e => e.SendEmailAsync("s@x", It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await BuildService().InviteStudentByEmailAsync(1, 10, "s@x");

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.AutoApproved);
        }

        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID-Pending - Existing pending -> auto-approve")]
        public async Task Invite_PendingMember_ShouldAutoApprove()
        {
            _repo.Setup(r => r.GetUserWithRoleByEmailAsync("s@x"))
                .ReturnsAsync(new User { UserId = 5, Email = "s@x", RoleId = int.Parse(RoleIds.Student) });
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new CourseDTO { ClassId = 10, Status = 1, ClassName = "C" });
            _repo.Setup(r => r.GetClassMemberAsync(10, 5))
                .ReturnsAsync(new ClassMember { ClassId = 10, StudentId = 5, MemberStatus = MemberStatus.Pending });
            _repo.Setup(r => r.UpdateClassMemberStatusAsync(10, 5, MemberStatus.Active)).Returns(Task.CompletedTask);

            var result = await BuildService().InviteStudentByEmailAsync(1, 10, "s@x");

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.AutoApproved);
        }

        [Fact(DisplayName = "InviteStudentByEmailAsync - UTCID-NotFoundOnInvite - InviteStudentAsync trả null -> NotFound")]
        public async Task Invite_RepoReturnsNull_NotFound()
        {
            _repo.Setup(r => r.GetUserWithRoleByEmailAsync("s@x"))
                .ReturnsAsync(new User { UserId = 5, Email = "s@x", RoleId = int.Parse(RoleIds.Student) });
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new CourseDTO { ClassId = 10, Status = 1, ClassName = "C" });
            _repo.Setup(r => r.GetClassMemberAsync(10, 5)).ReturnsAsync((ClassMember?)null);
            _repo.Setup(r => r.InviteStudentAsync(10, 5)).ReturnsAsync((ClassMember?)null);

            var result = await BuildService().InviteStudentByEmailAsync(1, 10, "s@x");

            Assert.True(result.IsFailure);
            Assert.Equal(CourseErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "AcceptInvitationAsync - UTCID-Garbage - Token base64 garbage -> InviteTokenInvalid (catch)")]
        public async Task Accept_GarbageToken_ShouldTriggerCatch()
        {
            var result = await BuildService().AcceptInvitationAsync(1, "@@invalid base64@@");

            Assert.Equal(CourseErrors.InviteTokenInvalid.Code, result.Error.Code);
        }

        [Fact(DisplayName = "AcceptInvitationAsync - UTCID-WrongFormat - Token decode được nhưng sai format -> InviteTokenInvalid")]
        public async Task Accept_WrongFormatPlain_ShouldFailDecode()
        {
            // Encode "abc" (no colon)
            var noColon = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("abcwithoutcolon"))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var result = await BuildService().AcceptInvitationAsync(1, noColon);

            Assert.Equal(CourseErrors.InviteTokenInvalid.Code, result.Error.Code);
        }
    }
}
