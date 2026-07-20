using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Auth;

/// <summary>
/// Represents the credentials required to authenticate a user.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
