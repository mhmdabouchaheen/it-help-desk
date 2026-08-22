using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a domain user of the help desk application.
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC date and time when the user was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the user was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the user was deactivated, or null if active.
    /// </summary>
    public DateTime? DeactivatedAtUtc { get; set; }

    /// <summary>Gets or sets the active Manager responsible for this direct report.</summary>
    public Guid? ManagerUserId { get; set; }

    /// <summary>Gets or sets the Manager responsible for this direct report.</summary>
    public User? ManagerUser { get; set; }
}
