using System;
using System.Text;
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

// F10 - AcceptInvitationAsync
// Source: CourseService.cs:185-218
// Branches:
//   1. parts.Length != 2                                  -> throw "Token không hợp lệ."
//   2. !int.TryParse(parts[0], out classId)               -> throw "Token không hợp lệ."
//   3. course != null && Status == Closed                 -> throw "Lớp học đã bị đóng, không thể tham gia."
//   4. AcceptEmailInvitationAsync returns 0               -> throw "Link mời không hợp lệ hoặc đã hết hạn."
//   5. Success                                            -> no exception
//   6. course == null                                     -> proceed (no closed-check)
//
// Token format: URL-safe base64 of "{classId}:{stampBase64-urlSafe}"
public class F10_AcceptInvitationAsync_Tests
{
    private readonly Mock<ICourseRepo> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IEmailService> _emailMock = new(MockBehavior.Strict);
    private readonly IConfiguration _config = TestConfigBuilder.Default();
    private readonly CourseService _service;

    public F10_AcceptInvitationAsync_Tests()
    {
        _service = new CourseService(_repoMock.Object, _emailMock.Object, _config);
    }

    private static string BuildToken(int classId, byte[] stamp)
    {
        var stampBase64 = Convert.ToBase64String(stamp).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var plain = $"{classId}:{stampBase64}";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
        return b64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string BuildBadToken(string plain)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
        return b64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID01 - Token hợp lệ + lớp Active -> chấp nhận thành công")]
    [TestType("N")]
    public async Task AcceptInvitationAsync_UTCID01_ValidToken_ShouldAccept()
    {
        var stamp = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var token = BuildToken(10, stamp);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.AcceptEmailInvitationAsync(10, 5, It.Is<byte[]>(b => b.SequenceEqual(stamp)))).ReturnsAsync(1);

        await _service.AcceptInvitationAsync(5, token);

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID02 - Token sai format (không có ':') -> throw 'Token không hợp lệ'")]
    [TestType("A")]
    public async Task AcceptInvitationAsync_UTCID02_BadFormatNoSeparator_ShouldThrow()
    {
        var token = BuildBadToken("nosemicolonhere");

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.AcceptInvitationAsync(5, token));
        Assert.Equal("Token không hợp lệ.", ex.Message);
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID03 - parts[0] không phải số -> throw 'Token không hợp lệ'")]
    [TestType("A")]
    public async Task AcceptInvitationAsync_UTCID03_BadFormatNonNumeric_ShouldThrow()
    {
        var token = BuildBadToken("notanumber:abc");

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.AcceptInvitationAsync(5, token));
        Assert.Equal("Token không hợp lệ.", ex.Message);
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID04 - Lớp đã Closed -> throw")]
    [TestType("A")]
    public async Task AcceptInvitationAsync_UTCID04_ClassClosed_ShouldThrow()
    {
        var stamp = new byte[] { 0x01 };
        var token = BuildToken(10, stamp);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Closed });

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.AcceptInvitationAsync(5, token));
        Assert.Equal("Lớp học đã bị đóng, không thể tham gia.", ex.Message);
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID05 - AcceptEmailInvitationAsync trả 0 (concurrency mismatch) -> throw")]
    [TestType("A")]
    public async Task AcceptInvitationAsync_UTCID05_RowsZero_ShouldThrow()
    {
        var stamp = new byte[] { 0x05, 0x06 };
        var token = BuildToken(10, stamp);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.AcceptEmailInvitationAsync(10, 5, It.IsAny<byte[]>())).ReturnsAsync(0);

        var ex = await Assert.ThrowsAsync<Exception>(() => _service.AcceptInvitationAsync(5, token));
        Assert.Equal("Link mời không hợp lệ hoặc đã hết hạn.", ex.Message);
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID07 - Stamp 3 bytes -> stampBase64.Length % 4 = 0 (default switch path)")]
    [TestType("B")]
    public async Task AcceptInvitationAsync_UTCID07_StampMod0_ShouldHitDefaultBranch()
    {
        // 3-byte stamp encodes to 4-char base64 with no padding -> trim leaves length 4 -> mod 4 = 0 (default switch case).
        var stamp = new byte[] { 0x01, 0x02, 0x03 };
        var token = BuildToken(10, stamp);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new CourseDTO { ClassId = 10, Status = ClassStatus.Active });
        _repoMock.Setup(r => r.AcceptEmailInvitationAsync(10, 5, It.Is<byte[]>(b => b.SequenceEqual(stamp)))).ReturnsAsync(1);

        await _service.AcceptInvitationAsync(5, token);

        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "AcceptInvitationAsync - UTCID06 - course == null -> bỏ qua kiểm tra status, vẫn chấp nhận (boundary)")]
    [TestType("B")]
    public async Task AcceptInvitationAsync_UTCID06_CourseNull_ShouldStillAccept()
    {
        var stamp = new byte[] { 0x07 };
        var token = BuildToken(10, stamp);
        _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((CourseDTO?)null);
        _repoMock.Setup(r => r.AcceptEmailInvitationAsync(10, 5, It.IsAny<byte[]>())).ReturnsAsync(1);

        await _service.AcceptInvitationAsync(5, token);

        _repoMock.VerifyAll();
    }
}
