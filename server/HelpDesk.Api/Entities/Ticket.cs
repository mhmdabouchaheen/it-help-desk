using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a help desk ticket and its current workflow state.
/// </summary>
public class Ticket
{
    /// <summary>
    /// Gets or sets the unique identifier for the ticket.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique human-readable reference number for the ticket.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the concise title of the ticket.
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the issue or request.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the ticket's category.
    /// </summary>
    public short CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ticket's priority.
    /// </summary>
    public short PriorityId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ticket's current status.
    /// </summary>
    public short StatusId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the ticket.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the currently assigned support agent, or null when unassigned.
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the ticket was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the ticket was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the ticket was resolved, or null if unresolved.
    /// </summary>
    public DateTime? ResolvedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the ticket was closed, or null if not closed.
    /// </summary>
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the ticket was cancelled, or null if not cancelled.
    /// </summary>
    public DateTime? CancelledAtUtc { get; set; }
}
