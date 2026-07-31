namespace HelpDesk.Api.Application.Tickets;

/// <summary>Defines stable ticket-list sort field names.</summary>
public static class TicketSortFields
{
    /// <summary>Sorts by creation time.</summary>
    public const string CreatedAtUtc = nameof(CreatedAtUtc);
    /// <summary>Sorts by update time.</summary>
    public const string UpdatedAtUtc = nameof(UpdatedAtUtc);
    /// <summary>Sorts by the public ticket number.</summary>
    public const string TicketNumber = nameof(TicketNumber);
    /// <summary>Sorts by priority.</summary>
    public const string Priority = nameof(Priority);
    /// <summary>Sorts by status.</summary>
    public const string Status = nameof(Status);
    /// <summary>Sorts by title.</summary>
    public const string Title = nameof(Title);
}
