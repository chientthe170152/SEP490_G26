using System;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.CourseUnitTest;

// F8 - UpdateClassSettingsAsync
// Source: CourseService.cs:98-109
// Branches:
//   1. course != null && Status == Closed  -> throw "Khóa học đã bị đóng, không thể thay đổi cài đặt."
//   2. newName empty/whitespace            -> throw "Tên lớp không được để trống."
//   3. UpdateClassSettingsAsync returns false -> throw "Không tìm thấy lớp học."
//   4. Success                              -> repo updates without exception
//   5. course == null                       -> proceeds to validation
public class F8_UpdateClassSettingsAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F8_UpdateClassSettingsAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID01 - Lớp Active + tên hợp lệ -> update thành công")]
    [TestType("N")]
    public async Task UpdateClassSettingsAsync_UTCID01_Valid_ShouldUpdate()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.UpdateClassSettingsAsync(10, "Toán 12 nâng cao", 1)).ReturnsAsync(true);

        await _service.UpdateClassSettingsAsync(10, "Toán 12 nâng cao", 1);

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID02 - Lớp đã Closed -> throw")]
    [TestType("A")]
    public async Task UpdateClassSettingsAsync_UTCID02_ClassClosed_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Closed });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateClassSettingsAsync(10, "X", 1));
        Assert.Equal("Khóa học đã bị đóng, không thể thay đổi cài đặt.", ex.Message);
        _repoMock.Verify(r => r.UpdateClassSettingsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID03 - newName whitespace -> throw")]
    [TestType("A")]
    public async Task UpdateClassSettingsAsync_UTCID03_EmptyName_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateClassSettingsAsync(10, "   ", 1));
        Assert.Equal("Tên lớp không được để trống.", ex.Message);
    }

    [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID04 - Repo trả false -> throw 'Không tìm thấy lớp học'")]
    [TestType("A")]
    public async Task UpdateClassSettingsAsync_UTCID04_RepoReturnsFalse_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.UpdateClassSettingsAsync(10, "X", 0)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateClassSettingsAsync(10, "X", 0));
        Assert.Equal("Không tìm thấy lớp học.", ex.Message);
    }

    [Fact(DisplayName = "UpdateClassSettingsAsync - UTCID05 - course == null -> không throw status, kiểm tra name + update")]
    [TestType("B")]
    public async Task UpdateClassSettingsAsync_UTCID05_CourseNull_ShouldProceed()
    {
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((CourseDTO?)null);
        _repoMock.Setup(r => r.UpdateClassSettingsAsync(10, "X", 1)).ReturnsAsync(true);

        await _service.UpdateClassSettingsAsync(10, "X", 1);

        _repoMock.VerifyAll();
    }
}
