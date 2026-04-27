using System;
using System.Threading.Tasks;
using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F4 - CreateCourseAsync
// Source: CourseService.cs:45-69
// Branches:
//   1. duplicateError != null -> throw Exception(duplicateError)
//   2. duplicateError == null -> CreateCourseAsync, return DTO
//   3. dto.Semester == null   -> normalizedSemester = "" (?? branch)
//   4. dto.Semester != null   -> normalizedSemester = trimmed upper
public class F4_CreateCourseAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F4_CreateCourseAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "CreateCourseAsync - UTCID01 - Không trùng + dto đầy đủ -> tạo course thành công")]
    [TestType("N")]
    public async Task CreateCourseAsync_UTCID01_NoDuplicate_ShouldCreate()
    {
        var dto = new CreateCourseRequestDTO { ClassName = "Toán 12", Semester = "fa2026", SubjectId = 7 };
        _repoMock.Setup(r => r.GetDuplicateClassErrorAsync(1, "Toán 12", "FA2026", 7)).ReturnsAsync((string?)null);
        Class? created = null;
        _repoMock.Setup(r => r.CreateCourseAsync(It.IsAny<Class>()))
                 .ReturnsAsync((Class c) => { created = c; return new CourseDTO { ClassId = 100, ClassName = c.Name }; });

        var result = await _service.CreateCourseAsync(1, dto);

        Assert.Equal(100, result.ClassId);
        Assert.NotNull(created);
        Assert.Equal("FA2026", created!.Semester);
        Assert.Equal(1, created.Status);
        Assert.Equal(1, created.InvitationCodeStatus);
    }

    [Fact(DisplayName = "CreateCourseAsync - UTCID02 - Trùng class trong cùng học kỳ -> throw")]
    [TestType("A")]
    public async Task CreateCourseAsync_UTCID02_Duplicate_ShouldThrow()
    {
        var dto = new CreateCourseRequestDTO { ClassName = "Toán 12", Semester = "FA2026", SubjectId = 7 };
        _repoMock.Setup(r => r.GetDuplicateClassErrorAsync(1, "Toán 12", "FA2026", 7))
                 .ReturnsAsync("Lớp đã tồn tại trong học kỳ này.");

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateCourseAsync(1, dto));
        Assert.Equal("Lớp đã tồn tại trong học kỳ này.", ex.Message);
        _repoMock.Verify(r => r.CreateCourseAsync(It.IsAny<Class>()), Times.Never);
    }

    [Fact(DisplayName = "CreateCourseAsync - UTCID03 - dto.Semester = null -> normalizedSemester rỗng (boundary cho ??)")]
    [TestType("B")]
    public async Task CreateCourseAsync_UTCID03_NullSemester_ShouldNormalizeToEmpty()
    {
        var dto = new CreateCourseRequestDTO { ClassName = "Toán 12", Semester = null!, SubjectId = 7 };
        _repoMock.Setup(r => r.GetDuplicateClassErrorAsync(1, "Toán 12", "", 7)).ReturnsAsync((string?)null);
        Class? created = null;
        _repoMock.Setup(r => r.CreateCourseAsync(It.IsAny<Class>()))
                 .ReturnsAsync((Class c) => { created = c; return new CourseDTO { ClassId = 101 }; });

        await _service.CreateCourseAsync(1, dto);

        Assert.Equal(string.Empty, created!.Semester);
    }
}
