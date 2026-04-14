using Backend.UnitTest;
using Moq;
using System.Text;

public class AcceptInvitationAsyncTests : CourseTestBase
{
    // 1. Phủ nhánh: Token không đúng định dạng (Thiếu dấu :)
    [Fact]
    public async Task Accept_NoColon_ShouldThrowException()
    {
        // "abc" -> Base64 là "YWJj" (độ dài 4, không cần padding)
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("InvalidFormat"));

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _courseService.AcceptInvitationAsync(1, token));
        Assert.Equal("Token không hợp lệ.", ex.Message);
    }

    // 2. Phủ nhánh: ClassId không phải là số (int.TryParse fails)
    [Fact]
    public async Task Accept_ClassIdNotInt_ShouldThrowException()
    {
        // "notInt:stamp"
        var plain = "abc:c3RhbXA=";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _courseService.AcceptInvitationAsync(1, token));
        Assert.Equal("Token không hợp lệ.", ex.Message);
    }

    // 3. Phủ nhánh: Padding Base64 case 2 và case 3 (Dòng 5-9 và 17-21)
    // Để coverage dòng này, ta cần tạo chuỗi Base64 thiếu padding '='
    [Theory]
    [InlineData("10:c3RhbXA", 2)] // Độ dài 10 % 4 = 2 -> Cần padding "=="
    [InlineData("100:c3RhbXA", 3)] // Độ dài 11 % 4 = 3 -> Cần padding "="
    public async Task Accept_PaddingLogic_ShouldWork(string plainToken, int studentId)
    {
        // Tạo URL safe base64 thủ công (xóa padding và thay ký tự)
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainToken))
            .Replace("=", "").Replace("+", "-").Replace("/", "_");

        _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                 .ReturnsAsync(1);

        // Act
        await _courseService.AcceptInvitationAsync(studentId, base64);

        // Assert
        _mockRepo.Verify(r => r.AcceptEmailInvitationAsync(It.IsAny<int>(), studentId, It.IsAny<byte[]>()), Times.Once);
    }

    // 4. Phủ nhánh: Link hết hạn / Repo trả về 0 (Dòng 26-30)
    [Fact]
    public async Task Accept_RepoReturnsZero_ShouldThrowException()
    {
        var plain = "10:c3RhbXA=";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));

        _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                 .ReturnsAsync(0);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _courseService.AcceptInvitationAsync(1, token));
        Assert.Equal("Link mời không hợp lệ hoặc đã hết hạn.", ex.Message);
    }

    // 5. Phủ nhánh: Thành công (Happy Path)
    [Fact]
    public async Task Accept_Success_ShouldExecuteCorrectly()
    {
        // Arrange
        int classId = 99;
        int studentId = 1;
        byte[] stamp = { 0x01, 0x02 };
        string stampBase64 = Convert.ToBase64String(stamp);
        string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{classId}:{stampBase64}"));

        _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(classId, studentId, It.IsAny<byte[]>()))
                 .ReturnsAsync(1);

        // Act
        await _courseService.AcceptInvitationAsync(studentId, token);

        // Assert
        _mockRepo.Verify(r => r.AcceptEmailInvitationAsync(classId, studentId, It.Is<byte[]>(b => b.SequenceEqual(stamp))), Times.Once);
    }
    [Theory]
    // Test Case phủ case 2 của stampBase64 (Độ dài phần sau dấu : chia 4 dư 2)
    [InlineData(1, "10:c3RhbXA")]
    // Test Case phủ case 3 của stampBase64 (Độ dài phần sau dấu : chia 4 dư 3)
    [InlineData(1, "100:c3RhbXBh")]
    public async Task Accept_FullPaddingCoverage_ShouldWork(int studentId, string plainToken)
    {
        // 1. Arrange
        // Tạo URL safe base64 thủ công: Xóa padding '=', thay '+' bằng '-', '/' bằng '_'
        var base64Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainToken))
            .Replace("=", "").Replace("+", "-").Replace("/", "_");

        _mockRepo.Setup(r => r.AcceptEmailInvitationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                 .ReturnsAsync(1);

        // 2. Act
        await _courseService.AcceptInvitationAsync(studentId, base64Token);

        // 3. Assert
        _mockRepo.Verify(r => r.AcceptEmailInvitationAsync(It.IsAny<int>(), studentId, It.IsAny<byte[]>()), Times.Once);
    }
}