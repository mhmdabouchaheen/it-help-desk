namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents safe summary information for a ticket.</summary>
public class TicketSummaryResponse
{
    /// <summary>Gets the ticket identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the public ticket number mapped from the domain reference number.</summary>
    public string TicketNumber { get; init; } = string.Empty;
    /// <summary>Gets the ticket title.</summary>
    public string Title { get; init; } = string.Empty;
    /// <summary>Gets the category identifier.</summary>
    public short CategoryId { get; init; }
    /// <summary>Gets the category display name.</summary>
    public string CategoryName { get; init; } = string.Empty;
    /// <summary>Gets the priority identifier.</summary>
    public short PriorityId { get; init; }
    /// <summary>Gets the priority display name.</summary>
    public string PriorityName { get; init; } = string.Empty;
    /// <summary>Gets the status identifier.</summary>
    public short StatusId { get; init; }
    /// <summary>Gets the status display name.</summary>
    public string StatusName { get; init; } = string.Empty;
    /// <summary>Gets the creator identifier.</summary>
    public Guid CreatedByUserId { get; init; }
    /// <summary>Gets the creator display name.</summary>
    public string CreatedByDisplayName { get; init; } = string.Empty;
    /// <summary>Gets the current assignee identifier.</summary>
    public Guid? AssignedToUserId { get; init; }
    /// <summary>Gets the current assignee display name.</summary>
    public string? AssignedToDisplayName { get; init; }
    /// <summary>Gets the creation time.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>Gets the last update time.</summary>
    public DateTime UpdatedAtUtc { get; init; }
    /// <summary>Gets the cancellation time, or null when the ticket has not been cancelled.</summary>
    public DateTime? CancelledAtUtc { get; init; }
}
