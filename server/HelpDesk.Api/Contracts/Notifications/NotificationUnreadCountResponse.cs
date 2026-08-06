namespace HelpDesk.Api.Contracts.Notifications;

/// <summary>The authenticated user's non-expired unread notification count.</summary>
public sealed class NotificationUnreadCountResponse
{
    public int UnreadCount { get; init; }
}
