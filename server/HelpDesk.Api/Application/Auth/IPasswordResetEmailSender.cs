namespace HelpDesk.Api.Application.Auth;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
}
