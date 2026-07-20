namespace HelpDesk.Api.Configuration;

/// <summary>
/// Represents configuration values used for JSON Web Token issuance and refresh lifetimes.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Gets or sets the expected token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the intended token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key used by future token-signing infrastructure.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access-token lifetime in minutes.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the refresh-token lifetime in days.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; }
}
