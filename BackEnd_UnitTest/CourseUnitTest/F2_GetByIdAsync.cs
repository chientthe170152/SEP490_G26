using System;
using System.Threading.Tasks;
using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F2 - GetByIdAsync
// Source: CourseService.cs:35-38 — pure delegation to repo. No conditional branches.
public class F2_GetByIdAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F2_GetByIdAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "GetByIdAsync - UTCID01 - Class tồn tại -> trả CourseDTO")]
    [TestType("N")]
    public async Task GetByIdAsync_UTCID01_ClassExists_ShouldReturnDto()
    {
        var dto = new CourseDTO { ClassId = 5, ClassName = "Toán 12" };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(dto);

        var result = await _service.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.ClassId);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetByIdAsync - UTCID02 - Class không tồn tại -> trả null")]
    [TestType("A")]
    public async Task GetByIdAsync_UTCID02_ClassNotFound_ShouldReturnNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CourseDTO?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetByIdAsync - UTCID03 - Repo throw -> propagate")]
    [TestType("A")]
    public async Task GetByIdAsync_UTCID03_RepoThrows_ShouldPropagate()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ThrowsAsync(new InvalidOperationException("DB"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetByIdAsync(1));
    }
}
