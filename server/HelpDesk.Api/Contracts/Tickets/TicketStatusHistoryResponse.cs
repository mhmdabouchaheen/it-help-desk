namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents a safe ticket status transition projection.</summary>
public sealed class TicketStatusHistoryResponse
{
    /// <summary>Gets the history identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the ticket identifier.</summary>
    public Guid TicketId { get; init; }
    /// <summary>Gets the previous status identifier.</summary>
    public short? FromStatusId { get; init; }
    /// <summary>Gets the previous status name.</summary>
    public string? FromStatusName { get; init; }
    /// <summary>Gets the new status identifier.</summary>
    public short ToStatusId { get; init; }
    /// <summary>Gets the new status name.</summary>
    public string ToStatusName { get; init; } = string.Empty;
    /// <summary>Gets the user who changed status.</summary>
    public Guid? ChangedByUserId { get; init; }
    /// <summary>Gets that user's display name.</summary>
    public string? ChangedByDisplayName { get; init; }
    /// <summary>Gets the transition time.</summary>
    public DateTime ChangedAtUtc { get; init; }
    /// <summary>Gets the optional transition reason.</summary>
    public string? Reason { get; init; }
}
