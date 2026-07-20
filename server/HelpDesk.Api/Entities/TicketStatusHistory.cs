using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a historical status transition for a help desk ticket.
/// </summary>
public class TicketStatusHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the status history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ticket whose status changed.
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Gets or sets the previous status identifier, or null for the initial creation event.
    /// </summary>
    public short? FromStatusId { get; set; }

    /// <summary>
    /// Gets or sets the new status identifier.
    /// </summary>
    public short ToStatusId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who changed the status, or null for a system transition.
    /// </summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the status changed.
    /// </summary>
    public DateTime ChangedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets an optional explanation for the status transition.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }
}
