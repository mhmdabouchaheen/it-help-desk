using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents an append-only audit record for an important system action.
/// </summary>
public class ActivityLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the activity record.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the acting user, or null for a system or anonymous action.
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Gets or sets the stable action key describing the activity.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical type of entity affected by the activity.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the textual identifier of the entity affected by the activity.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the activity occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets optional structured, non-sensitive metadata for later JSONB mapping.
    /// </summary>
    public string? Metadata { get; set; }
}
