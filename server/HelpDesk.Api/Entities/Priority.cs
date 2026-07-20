using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a configurable urgency level for help desk tickets.
/// </summary>
public class Priority
{
    /// <summary>
    /// Gets or sets the unique identifier for the priority.
    /// </summary>
    public short Id { get; set; }

    /// <summary>
    /// Gets or sets the priority name.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative urgency rank, where larger values indicate greater urgency.
    /// </summary>
    public short Rank { get; set; }

    /// <summary>
    /// Gets or sets an optional administrative description of the priority.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the priority can be selected.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC date and time when the priority was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the priority was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}
