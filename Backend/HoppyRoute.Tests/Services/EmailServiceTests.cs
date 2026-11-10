using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using HoppyRoute.Application.Models;
using HoppyRoute.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoppyRoute.Tests.Services
{
    /// <summary>
    /// Deterministic: never touches a real mail server. "Unreachable" uses a closed port on
    /// 127.0.0.1, so the result does not depend on the network the tests run on.
    /// </summary>
    public class EmailServiceTests
    {
        private static EmailService Service(EmailSettings s) => new(Options.Create(s), NullLogger<EmailService>.Instance);

        private static int ClosedLocalPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private static EmailSettings Unreachable() => new()
        {
            SmtpHost = "127.0.0.1",
            SmtpPort = ClosedLocalPort(),
            SmtpUsername = "test-only-user",
            SmtpPassword = "test-only-password",
            FromEmail = "noreply@test.invalid",
            FromName = "Hoppy Test",
            EnableSsl = false,
            TimeoutSeconds = 5
        };

        [Fact]
        public async Task TemporaryPasswordEmail_WithoutSmtpConfigured_OnlyLogs_AndReportsSuccess()
        {
            var service = Service(new EmailSettings { SmtpHost = "", SmtpUsername = "" });

            var ok = await service.SendTemporaryPasswordEmailAsync("a@test.invalid", "A", "B", "a.b", "Temp123!abcd");

            ok.Should().BeTrue();
        }

        [Fact]
        public async Task TemporaryPasswordEmail_SmtpUnreachable_ReturnsFalse_WithoutThrowing()
        {
            var ok = await Service(Unreachable()).SendTemporaryPasswordEmailAsync("a@test.invalid", "A", "B", "a.b", "Temp123!abcd");
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task WelcomeEmail_SmtpUnreachable_ReturnsFalse_WithoutThrowing()
        {
            var ok = await Service(Unreachable()).SendWelcomeEmailAsync("a@test.invalid", "A", "B");
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task PasswordResetEmail_SmtpUnreachable_ReturnsFalse_WithoutThrowing()
        {
            var ok = await Service(Unreachable()).SendPasswordResetEmailAsync("a@test.invalid", "A", "https://example.invalid/reset?token=test");
            ok.Should().BeFalse();
        }
    }
}
