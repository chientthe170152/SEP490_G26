using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Backend.Services.Implements;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Backend_UnitTest.EmailUnitTest
{
    public class EmailServiceTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        [Fact(DisplayName = "SendEmailAsync - UTCID01 - SenderEmail rỗng -> Dev mode (không send SMTP)")]
        public async Task SendEmailAsync_UTCID01_EmptySender_ShouldUseDevMode()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["EmailSettings:SmtpServer"] = "smtp.gmail.com",
                ["EmailSettings:Port"] = "587",
                ["EmailSettings:SenderEmail"] = "",
                ["EmailSettings:SenderPassword"] = ""
            });
            var service = new EmailService(config);

            var sw = new StringWriter();
            var origOut = Console.Out;
            Console.SetOut(sw);
            try
            {
                await service.SendEmailAsync("user@test.com", "subject", "body");
            }
            finally
            {
                Console.SetOut(origOut);
            }

            var output = sw.ToString();
            Assert.Contains("[DEV MODE]", output);
            Assert.Contains("user@test.com", output);
            Assert.Contains("subject", output);
            Assert.Contains("body", output);
        }

        [Fact(DisplayName = "SendEmailAsync - UTCID02 - SenderEmail chứa placeholder -> Dev mode")]
        public async Task SendEmailAsync_UTCID02_PlaceholderSender_ShouldUseDevMode()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE",
                ["EmailSettings:SenderPassword"] = "anything"
            });
            var service = new EmailService(config);

            var sw = new StringWriter();
            var origOut = Console.Out;
            Console.SetOut(sw);
            try
            {
                await service.SendEmailAsync("any@test.com", "s", "b");
            }
            finally
            {
                Console.SetOut(origOut);
            }

            Assert.Contains("[DEV MODE]", sw.ToString());
        }

        [Fact(DisplayName = "SendEmailAsync - UTCID03 - SenderPassword chứa placeholder -> Dev mode")]
        public async Task SendEmailAsync_UTCID03_PlaceholderPassword_ShouldUseDevMode()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["EmailSettings:SenderEmail"] = "real@test.com",
                ["EmailSettings:SenderPassword"] = "YOUR_APP_PASSWORD_HERE"
            });
            var service = new EmailService(config);

            var sw = new StringWriter();
            var origOut = Console.Out;
            Console.SetOut(sw);
            try
            {
                await service.SendEmailAsync("any@test.com", "s", "b");
            }
            finally
            {
                Console.SetOut(origOut);
            }

            Assert.Contains("[DEV MODE]", sw.ToString());
        }

        [Fact(DisplayName = "SendEmailAsync - UTCID04 - Port không phải số -> dùng default 587 và vào Dev mode")]
        public async Task SendEmailAsync_UTCID04_InvalidPort_ShouldFallbackTo587()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["EmailSettings:Port"] = "not-a-number",
                ["EmailSettings:SenderEmail"] = "",
                ["EmailSettings:SenderPassword"] = ""
            });
            var service = new EmailService(config);

            var sw = new StringWriter();
            var origOut = Console.Out;
            Console.SetOut(sw);
            try
            {
                await service.SendEmailAsync("u@t.com", "s", "b");
            }
            finally
            {
                Console.SetOut(origOut);
            }

            Assert.Contains("[DEV MODE]", sw.ToString());
        }

        [Fact(DisplayName = "SendEmailAsync - UTCID05 - Credentials thật nhưng host không tồn tại -> ném exception (đi qua SMTP path)")]
        public async Task SendEmailAsync_UTCID05_RealCredentialsButInvalidHost_ShouldThrow()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["EmailSettings:SmtpServer"] = "invalid-host-that-does-not-exist.localhost.invalid",
                ["EmailSettings:Port"] = "2525",
                ["EmailSettings:SenderEmail"] = "real-sender@test.com",
                ["EmailSettings:SenderPassword"] = "real-password"
            });
            var service = new EmailService(config);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.SendEmailAsync("user@test.com", "subject", "body"));
        }
    }
}
