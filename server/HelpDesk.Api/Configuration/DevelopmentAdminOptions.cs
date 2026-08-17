namespace HelpDesk.Api.Configuration;

/// <summary>
/// Defines local-only settings for creating a development administrator account.
/// </summary>
public sealed class DevelopmentAdminOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "DevelopmentAdmin";

    /// <summary>Gets or sets whether local administrator bootstrapping is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the local administrator email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the local administrator password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the local administrator display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
