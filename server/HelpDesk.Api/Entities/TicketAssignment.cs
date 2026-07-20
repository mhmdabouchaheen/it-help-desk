using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a historical assignment period for a help desk ticket.
/// </summary>
public class TicketAssignment
{
    /// <summary>
    /// Gets or sets the unique identifier for the assignment record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the assigned ticket.
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user receiving the assignment.
    /// </summary>
    public Guid AssignedToUserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the assignment, or null for a system action.
    /// </summary>
    public Guid? AssignedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the assignment began.
    /// </summary>
    public DateTime AssignedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the assignment ended, or null while active.
    /// </summary>
    public DateTime? EndedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who ended the assignment, or null when active or system-ended.
    /// </summary>
    public Guid? EndedByUserId { get; set; }

    /// <summary>
    /// Gets or sets optional context explaining the assignment or reassignment.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
