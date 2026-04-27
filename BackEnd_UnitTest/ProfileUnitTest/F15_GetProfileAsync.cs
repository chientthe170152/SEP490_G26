using System.Threading.Tasks;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ProfileUnitTest;

// F15 - GetProfileAsync
// Source: ProfileService.cs:18-34
// Branches:
//   1. user == null              -> return null
//   2. user found                -> map to UserProfileDTO and return
public class F15_GetProfileAsync_Tests
{
    private readonly Mock<IProfileRepository> _repoMock;
    private readonly ProfileService _service;

    public F15_GetProfileAsync_Tests()
    {
        _repoMock = new Mock<IProfileRepository>(MockBehavior.Strict);
        _service = new ProfileService(_repoMock.Object);
    }

    [Fact(DisplayName = "GetProfileAsync - UTCID01 - User tồn tại -> trả UserProfileDTO map đúng")]
    [TestType("N")]
    public async Task GetProfileAsync_UTCID01_UserExists_ShouldReturnMappedDto()
    {
        var user = UserBuilder.New()
            .WithId(1)
            .WithEmail("test@example.com")
            .WithFullName("Nguyen Van A")
            .WithPhone("0123456789")
            .WithStudentId("HE172047")
            .WithRoleId(2)
            .WithStatus(1)
            .Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

        var result = await _service.GetProfileAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Nguyen Van A", result.FullName);
        Assert.Equal("0123456789", result.PhoneNumber);
        Assert.Equal("HE172047", result.StudentId);
        Assert.Equal(2, result.RoleId);
        Assert.Equal(1, result.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetProfileAsync - UTCID02 - User không tồn tại -> trả null")]
    [TestType("A")]
    public async Task GetProfileAsync_UTCID02_UserNotFound_ShouldReturnNull()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _service.GetProfileAsync(999);

        Assert.Null(result);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetProfileAsync - UTCID03 - User có optional fields = null -> DTO giữ null")]
    [TestType("B")]
    public async Task GetProfileAsync_UTCID03_UserWithNullOptionalFields_ShouldReturnDtoWithNulls()
    {
        var user = UserBuilder.New()
            .WithId(2)
            .WithEmail("only-email@example.com")
            .WithFullName(null)
            .WithPhone(null)
            .WithStudentId(null)
            .WithRoleId(1)
            .WithStatus(1)
            .Build();
        _repoMock.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(user);

        var result = await _service.GetProfileAsync(2);

        Assert.NotNull(result);
        Assert.Equal(2, result!.UserId);
        Assert.Equal("only-email@example.com", result.Email);
        Assert.Null(result.FullName);
        Assert.Null(result.PhoneNumber);
        Assert.Null(result.StudentId);
        _repoMock.VerifyAll();
    }
}
