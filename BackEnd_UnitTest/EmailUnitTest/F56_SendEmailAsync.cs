using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BackEnd_UnitTest.EmailUnitTest;

// F56 - SendEmailAsync
// Source: EmailService.cs:16-51
// Branches:
//   1. SmtpServer null            -> fallback "smtp.gmail.com"  (?? branch)
//   2. PortString null            -> fallback "587"             (?? branch)
//   3. int.TryParse fails         -> port = 587 (boundary)
//   4. int.TryParse succeeds      -> port = parsed
//   5. senderEmail null/empty     -> isPlaceholder = true       (Dev mode)
//   6. senderPassword null/empty  -> isPlaceholder = true
//   7. senderEmail contains YOUR_GMAIL_HERE   -> isPlaceholder = true
//   8. senderPassword contains YOUR_APP_PASSWORD_HERE -> isPlaceholder = true
//   9. isPlaceholder == true      -> Console log + return (no SMTP)
//  10. isPlaceholder == false     -> create MailMessage + SmtpClient.SendMailAsync (real SMTP)
//
// The real-send path (#10) cannot be unit-tested without a real SMTP server. We trigger it
// against a non-routable host and expect an exception, which both proves the branch was reached
// and catches any flakiness deterministically (the SmtpClient cannot connect -> throws).
public class F56_SendEmailAsync_Tests
{
    private static EmailService BuildService(params (string Key, string? Value)[] settings)
        => new(TestConfigBuilder.Override(settings));

    [Fact(DisplayName = "SendEmailAsync - UTCID01 - SenderEmail null -> Dev mode log + return không lỗi")]
    [TestType("N")]
    public async Task SendEmailAsync_UTCID01_NullSenderEmail_ShouldHitDevMode()
    {
        var service = BuildService(
            ("EmailSettings:SmtpServer", "smtp.gmail.com"),
            ("EmailSettings:Port", "587"),
            ("EmailSettings:SenderEmail", null),
            ("EmailSettings:SenderPassword", "irrelevant"));

        // Capture stdout to confirm the dev-mode branch ran.
        var oldOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try { await service.SendEmailAsync("to@test.com", "Sub", "<b>body</b>"); }
        finally { Console.SetOut(oldOut); }

        Assert.Contains("[DEV MODE] Email to to@test.com", sw.ToString());
    }

    [Fact(DisplayName = "SendEmailAsync - UTCID02 - SenderEmail empty -> Dev mode (boundary IsNullOrEmpty)")]
    [TestType("B")]
    public async Task SendEmailAsync_UTCID02_EmptySenderEmail_ShouldHitDevMode()
    {
        var service = BuildService(
            ("EmailSettings:SenderEmail", ""),
            ("EmailSettings:SenderPassword", "p"));

        var sw = new StringWriter();
        var oldOut = Console.Out;
        Console.SetOut(sw);
        try { await service.SendEmailAsync("to@test.com", "Sub", "body"); }
        finally { Console.SetOut(oldOut); }

        Assert.Contains("[DEV MODE]", sw.ToString());
    }

    [Fact(DisplayName = "SendEmailAsync - UTCID03 - SenderPassword null -> Dev mode")]
    [TestType("A")]
    public async Task SendEmailAsync_UTCID03_NullSenderPassword_ShouldHitDevMode()
    {
        var service = BuildService(
            ("EmailSettings:SenderEmail", "real@gmail.com"),
            ("EmailSettings:SenderPassword", null));

        var sw = new StringWriter();
        var oldOut = Console.Out;
        Console.SetOut(sw);
        try { await service.SendEmailAsync("to@test.com", "Sub", "body"); }
        finally { Console.SetOut(oldOut); }

        Assert.Contains("[DEV MODE]", sw.ToString());
    }

    [Fact(DisplayName = "SendEmailAsync - UTCID04 - SenderEmail = 'YOUR_GMAIL_HERE...' -> Dev mode")]
    [TestType("A")]
    public async Task SendEmailAsync_UTCID04_PlaceholderEmail_ShouldHitDevMode()
    {
        var service = BuildService(
            ("EmailSettings:SenderEmail", "YOUR_GMAIL_HERE@gmail.com"),
            ("EmailSettings:SenderPassword", "abc"));

        var sw = new StringWriter();
        var oldOut = Console.Out;
        Console.SetOut(sw);
        try { await service.SendEmailAsync("to@test.com", "Sub", "body"); }
        finally { Console.SetOut(oldOut); }

        Assert.Contains("[DEV MODE]", sw.ToString());
    }

    [Fact(DisplayName = "SendEmailAsync - UTCID05 - SenderPassword chứa YOUR_APP_PASSWORD_HERE -> Dev mode")]
    [TestType("A")]
    public async Task SendEmailAsync_UTCID05_PlaceholderPassword_ShouldHitDevMode()
    {
        var service = BuildService(
            ("EmailSettings:SenderEmail", "real@gmail.com"),
            ("EmailSettings:SenderPassword", "YOUR_APP_PASSWORD_HERE_xyz"));

        var sw = new StringWriter();
        var oldOut = Console.Out;
        Console.SetOut(sw);
        try { await service.SendEmailAsync("to@test.com", "Sub", "body"); }
        finally { Console.SetOut(oldOut); }

        Assert.Contains("[DEV MODE]", sw.ToString());
    }

    [Fact(DisplayName = "SendEmailAsync - UTCID06 - SmtpServer null + Port không parse được -> fallback giá trị mặc định (boundary)")]
    [TestType("B")]
    public async Task SendEmailAsync_UTCID06_NullServerUnparsablePort_ShouldFallback()
    {
        // Vẫn rơi vào dev mode (do thiếu credentials), nhưng đi qua nhánh ?? cho server và TryParse fail cho port.
        var service = BuildService(
            ("EmailSettings:SmtpServer", null),
            ("EmailSettings:Port", "not-a-number"),
            ("EmailSettings:SenderEmail", null),
            ("EmailSettings:SenderPassword", null));

        var sw = new StringWriter();
        var oldOut = Console.Out;
        Console.SetOut(sw);
        try { await service.SendEmailAsync("to@test.com", "Sub", "body"); }
        finally { Console.SetOut(oldOut); }

        Assert.Contains("[DEV MODE]", sw.ToString());
    }

    [Fact(DisplayName = "SendEmailAsync - UTCID07 - Cấu hình SMTP đầy đủ + non-routable host -> ném exception (real-send path)")]
    [TestType("A")]
    public async Task SendEmailAsync_UTCID07_RealSendPath_ShouldAttemptSmtp()
    {
        // Cover real-send branch (isPlaceholder == false). Use 127.0.0.1:1 (no SMTP listener) → connect fails → SmtpException/SocketException.
        var service = BuildService(
            ("EmailSettings:SmtpServer", "127.0.0.1"),
            ("EmailSettings:Port", "1"),
            ("EmailSettings:SenderEmail", "sender@gmail.com"),
            ("EmailSettings:SenderPassword", "real-pass"));

        // Any exception is acceptable — the goal is to prove the real-send branch was entered.
        await Assert.ThrowsAnyAsync<Exception>(
            () => service.SendEmailAsync("to@test.com", "Sub", "body"));
    }
}
