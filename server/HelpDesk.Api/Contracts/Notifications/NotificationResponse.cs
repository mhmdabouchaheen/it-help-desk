namespace HelpDesk.Api.Contracts.Notifications;

/// <summary>A safe notification belonging to the authenticated recipient.</summary>
public sealed class NotificationResponse
{
    public Guid Id { get; init; }
    public Guid? TicketId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public bool IsRead { get; init; }
}
