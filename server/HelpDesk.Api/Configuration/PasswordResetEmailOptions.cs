namespace HelpDesk.Api.Configuration;

public sealed class PasswordResetEmailOptions
{
    public const string SectionName = "PasswordResetEmail";
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
}
