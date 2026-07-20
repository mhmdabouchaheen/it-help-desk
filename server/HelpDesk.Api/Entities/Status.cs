using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a configurable workflow state for help desk tickets.
/// </summary>
public class Status
{
    /// <summary>
    /// Gets or sets the unique identifier for the status.
    /// </summary>
    public short Id { get; set; }

    /// <summary>
    /// Gets or sets the status name.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional administrative description of the status.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the status's display order.
    /// </summary>
    public short SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status normally ends active ticket work.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status can be selected for transitions.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC date and time when the status was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the status was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}
