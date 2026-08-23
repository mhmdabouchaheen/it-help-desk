using System.Net;
using System.Net.Mail;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Auth;

public sealed class SmtpPasswordResetEmailSender(
    IOptions<PasswordResetEmailOptions> options,
    ILogger<SmtpPasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.Host) || string.IsNullOrWhiteSpace(value.FromAddress) ||
            string.IsNullOrWhiteSpace(value.FrontendBaseUrl))
        {
            logger.LogWarning("Password-reset email delivery is not configured.");
            return;
        }

        try
        {
            var link = $"{value.FrontendBaseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            using var message = new MailMessage(value.FromAddress, email)
            {
                Subject = "Reset your IT Help Desk password",
                Body = $"Use this link to reset your password: {link}",
                IsBodyHtml = false
            };
            using var client = new SmtpClient(value.Host, value.Port)
            {
                EnableSsl = value.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(value.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(value.Username, value.Password)
            };
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Password-reset SMTP delivery failed. ExceptionType: {ExceptionType}. Message: {ExceptionMessage}. SMTP status: {SmtpStatus}. Host: {SmtpHost}. Port: {SmtpPort}. UseSsl: {UseSsl}. FromAddress: {FromAddress}.",
                exception.GetType().Name,
                SanitizeMessage(exception.Message, email, token, value.Username, value.Password),
                exception is SmtpException smtpException ? smtpException.StatusCode.ToString() : "Unavailable",
                value.Host,
                value.Port,
                value.UseSsl,
                value.FromAddress);
            throw;
        }
    }

    private static string SanitizeMessage(string message, params string[] sensitiveValues)
    {
        var sanitized = message;
        foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrEmpty(value)))
        {
            sanitized = sanitized.Replace(sensitiveValue, "[REDACTED]", StringComparison.Ordinal);
        }

        sanitized = string.Concat(sanitized.Select(character => char.IsControl(character) ? ' ' : character));
        return sanitized.Length <= 512 ? sanitized : sanitized[..512];
    }
}
