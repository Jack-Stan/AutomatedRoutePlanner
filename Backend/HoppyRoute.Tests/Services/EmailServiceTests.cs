using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using HoppyRoute.Application.Models;
using HoppyRoute.Application.Services;
using Microsoft.Extensions.Logging;
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

        // ---- Credentials never reach the logs ----

        private static EmailSettings NoSmtp() => new() { SmtpHost = "", SmtpUsername = "" };

        [Fact]
        public async Task TemporaryPasswordEmail_NeverLogsThePassword_ButDoesLogTheUsername()
        {
            const string password = "test-only-Temp-Pw-7x9";
            var logger = new CapturingLogger<EmailService>();
            var service = new EmailService(Options.Create(NoSmtp()), logger);

            (await service.SendTemporaryPasswordEmailAsync("a@test.invalid", "A", "B", "a.b", password)).Should().BeTrue();

            logger.Messages.Should().NotBeEmpty();
            logger.Messages.Should().NotContain(m => m.Contains(password));
            logger.Messages.Should().Contain(m => m.Contains("a.b"));
        }

        [Fact]
        public async Task TemporaryPasswordEmail_SmtpFailure_NeverLogsThePassword()
        {
            const string password = "test-only-Temp-Pw-7x9";
            var logger = new CapturingLogger<EmailService>();
            var service = new EmailService(Options.Create(Unreachable()), logger);

            (await service.SendTemporaryPasswordEmailAsync("a@test.invalid", "A", "B", "a.b", password)).Should().BeFalse();

            logger.Messages.Should().NotContain(m => m.Contains(password));
        }

        [Fact]
        public async Task PasswordResetEmail_NeverLogsTheResetToken()
        {
            const string token = "test-only-reset-token-abc123";
            var logger = new CapturingLogger<EmailService>();
            var service = new EmailService(Options.Create(NoSmtp()), logger);

            (await service.SendPasswordResetEmailAsync("a@test.invalid", "A", $"https://example.invalid/reset?token={token}")).Should().BeTrue();

            logger.Messages.Should().NotBeEmpty();
            logger.Messages.Should().NotContain(m => m.Contains(token));
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Messages.Add(formatter(state, exception) + (exception != null ? " " + exception : ""));
        }
    }
}
