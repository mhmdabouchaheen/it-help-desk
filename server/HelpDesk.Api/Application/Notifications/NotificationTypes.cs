namespace HelpDesk.Api.Application.Notifications;

/// <summary>Stable notification event type values persisted by the application.</summary>
public static class NotificationTypes
{
    public const string TicketAssigned = "TicketAssigned";
    public const string TicketStatusChanged = "TicketStatusChanged";
    public const string TicketCommentAdded = "TicketCommentAdded";
    public const string TicketInternalCommentAdded = "TicketInternalCommentAdded";
    public const string TicketCancelled = "TicketCancelled";
    public const string TicketAttachmentAdded = "TicketAttachmentAdded";
}
