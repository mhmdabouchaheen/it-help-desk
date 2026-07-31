namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents a safe ticket assignment-history projection.</summary>
public sealed class TicketAssignmentResponse
{
    /// <summary>Gets the assignment identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the ticket identifier.</summary>
    public Guid TicketId { get; init; }
    /// <summary>Gets the assigned user identifier.</summary>
    public Guid AssignedToUserId { get; init; }
    /// <summary>Gets the assigned user display name.</summary>
    public string AssignedToDisplayName { get; init; } = string.Empty;
    /// <summary>Gets the assigning user identifier, when user initiated.</summary>
    public Guid? AssignedByUserId { get; init; }
    /// <summary>Gets the assigning user display name.</summary>
    public string? AssignedByDisplayName { get; init; }
    /// <summary>Gets when the assignment began.</summary>
    public DateTime AssignedAtUtc { get; init; }
    /// <summary>Gets when the assignment ended.</summary>
    public DateTime? EndedAtUtc { get; init; }
    /// <summary>Gets the user who ended the assignment.</summary>
    public Guid? EndedByUserId { get; init; }
    /// <summary>Gets that user's display name.</summary>
    public string? EndedByDisplayName { get; init; }
    /// <summary>Gets optional assignment context.</summary>
    public string? Reason { get; init; }
}
