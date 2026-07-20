using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Auth;

/// <summary>
/// Represents a request to revoke one refresh-token session.
/// </summary>
public class LogoutRequest
{
    /// <summary>Gets or sets the refresh token to revoke.</summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
