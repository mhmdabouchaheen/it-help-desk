namespace HelpDesk.Api.Application.Tickets;

/// <summary>Defines the visibility values enforced by the ticket-comment model.</summary>
public static class TicketCommentVisibilities
{
    /// <summary>A comment visible to users with ticket access.</summary>
    public const string Public = "Public";
    /// <summary>A support-only internal comment.</summary>
    public const string Internal = "Internal";
}
