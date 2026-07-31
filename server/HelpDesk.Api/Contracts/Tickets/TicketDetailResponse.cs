namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents safe detailed ticket information.</summary>
public sealed class TicketDetailResponse : TicketSummaryResponse
{
    /// <summary>Gets the detailed issue description.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the optional resolution time.</summary>
    public DateTime? ResolvedAtUtc { get; init; }
    /// <summary>Gets the optional closure time.</summary>
    public DateTime? ClosedAtUtc { get; init; }
    /// <summary>Gets the optional cancellation time.</summary>
    public DateTime? CancelledAtUtc { get; init; }
    /// <summary>Gets ticket comments.</summary>
    public IReadOnlyList<TicketCommentResponse> Comments { get; init; } = Array.Empty<TicketCommentResponse>();
    /// <summary>Gets attachment metadata.</summary>
    public IReadOnlyList<TicketAttachmentResponse> Attachments { get; init; } = Array.Empty<TicketAttachmentResponse>();
    /// <summary>Gets assignment history.</summary>
    public IReadOnlyList<TicketAssignmentResponse> AssignmentHistory { get; init; } = Array.Empty<TicketAssignmentResponse>();
    /// <summary>Gets status history.</summary>
    public IReadOnlyList<TicketStatusHistoryResponse> StatusHistory { get; init; } = Array.Empty<TicketStatusHistoryResponse>();
}
