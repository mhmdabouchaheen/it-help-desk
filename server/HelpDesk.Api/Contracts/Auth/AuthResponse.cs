namespace HelpDesk.Api.Contracts.Auth;

/// <summary>
/// Represents authentication details returned to an authenticated client.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the access token expires.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the refresh token expires.
    /// </summary>
    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user's identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles assigned to the authenticated user.
    /// </summary>
    public List<string> Roles { get; set; } = [];
}
