using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Auth;

/// <summary>
/// Represents a request to exchange a refresh token for new authentication credentials.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token supplied by the client.
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
