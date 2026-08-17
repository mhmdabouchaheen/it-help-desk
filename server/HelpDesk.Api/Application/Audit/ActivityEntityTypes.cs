namespace HelpDesk.Api.Application.Audit;

/// <summary>Stable entity-type keys used by activity records.</summary>
public static class ActivityEntityTypes
{
    public const string User = "User";
    public const string Ticket = "Ticket";
    public const string TicketComment = "TicketComment";
    public const string TicketAttachment = "TicketAttachment";
    public const string Notification = "Notification";
}
