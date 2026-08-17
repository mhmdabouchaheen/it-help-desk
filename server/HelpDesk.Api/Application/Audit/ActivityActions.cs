namespace HelpDesk.Api.Application.Audit;

/// <summary>Stable action keys written to the append-only activity log.</summary>
public static class ActivityActions
{
    public const string UserRegistered = "user.registered";
    public const string UserLoggedIn = "user.logged_in";
    public const string UserLoggedOut = "user.logged_out";
    public const string TicketCreated = "ticket.created";
    public const string TicketUpdated = "ticket.updated";
    public const string TicketCancelled = "ticket.cancelled";
    public const string TicketAssigned = "ticket.assigned";
    public const string TicketStatusChanged = "ticket.status_changed";
    public const string TicketCommentAdded = "ticket.comment_added";
    public const string TicketInternalCommentAdded = "ticket.internal_comment_added";
    public const string TicketAttachmentUploaded = "ticket.attachment_uploaded";
    public const string TicketAttachmentDeleted = "ticket.attachment_deleted";
    public const string NotificationMarkedRead = "notification.marked_read";
    public const string NotificationMarkedAllRead = "notification.marked_all_read";
}
