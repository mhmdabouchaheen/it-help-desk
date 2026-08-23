using System.Net.Mail;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Infrastructure.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Tests;

public sealed class SmtpPasswordResetEmailSenderTests
{
    [Fact]
    public async Task SendPasswordResetAsync_LogsSafeSmtpDiagnosticsWithoutSecrets()
    {
        const string resetToken = "reset-token-that-must-not-be-logged";
        const string smtpPassword = "smtp-password-that-must-not-be-logged";
        var logger = new RecordingLogger<SmtpPasswordResetEmailSender>();
        var sender = new SmtpPasswordResetEmailSender(
            Options.Create(new PasswordResetEmailOptions
            {
                FrontendBaseUrl = "https://client.example.test",
                Host = "127.0.0.1",
                Port = 1,
                UseSsl = true,
                Username = "sender@example.test",
                Password = smtpPassword,
                FromAddress = "sender@example.test"
            }),
            logger);

        await Assert.ThrowsAnyAsync<Exception>(() => sender.SendPasswordResetAsync(
            "recipient@example.test",
            resetToken));

        var log = Assert.Single(logger.Messages);
        Assert.Contains("ExceptionType", log, StringComparison.Ordinal);
        Assert.Contains("SMTP status", log, StringComparison.Ordinal);
        Assert.Contains("Host: 127.0.0.1", log, StringComparison.Ordinal);
        Assert.Contains("Port: 1", log, StringComparison.Ordinal);
        Assert.Contains("UseSsl: True", log, StringComparison.Ordinal);
        Assert.DoesNotContain(resetToken, log, StringComparison.Ordinal);
        Assert.DoesNotContain(smtpPassword, log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendPasswordResetAsync_LogsFailuresDuringMessageSetup()
    {
        var logger = new RecordingLogger<SmtpPasswordResetEmailSender>();
        var sender = new SmtpPasswordResetEmailSender(
            Options.Create(new PasswordResetEmailOptions
            {
                FrontendBaseUrl = "https://client.example.test",
                Host = "smtp.gmail.com",
                Port = 587,
                UseSsl = true,
                FromAddress = "not-an-email-address"
            }),
            logger);

        await Assert.ThrowsAnyAsync<Exception>(() => sender.SendPasswordResetAsync(
            "recipient@example.test",
            "reset-token"));

        var log = Assert.Single(logger.Messages);
        Assert.Contains("Password-reset SMTP delivery failed", log, StringComparison.Ordinal);
        Assert.Contains("ExceptionType", log, StringComparison.Ordinal);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NoopScope : IDisposable
        {
            public static NoopScope Instance { get; } = new();

            public void Dispose() { }
        }
    }
}