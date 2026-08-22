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
}
