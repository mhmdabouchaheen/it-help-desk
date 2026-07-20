using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents an authorization role that can be assigned to users.
/// </summary>
public class Role : IdentityRole<Guid>
{
    /// <summary>
    /// Gets or sets an optional administrative description of the role.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the role is available for assignment.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC date and time when the role was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the role was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}
