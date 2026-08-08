namespace HelpDesk.Api.Contracts.Notifications;

/// <summary>Safe notification-created invalidation metadata.</summary>
public sealed class NotificationRealtimeEvent
{
    public required Guid NotificationId { get; init; }
    public Guid? TicketId { get; init; }
    public required string Type { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
