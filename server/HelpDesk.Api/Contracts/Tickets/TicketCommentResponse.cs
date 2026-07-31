namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents a safe ticket comment projection.</summary>
public sealed class TicketCommentResponse
{
    /// <summary>Gets the comment identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the ticket identifier.</summary>
    public Guid TicketId { get; init; }
    /// <summary>Gets the author identifier.</summary>
    public Guid AuthorUserId { get; init; }
    /// <summary>Gets the author display name.</summary>
    public string AuthorDisplayName { get; init; } = string.Empty;
    /// <summary>Gets the comment body.</summary>
    public string Body { get; init; } = string.Empty;
    /// <summary>Gets the domain visibility value.</summary>
    public string Visibility { get; init; } = string.Empty;
    /// <summary>Gets the creation time.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>Gets the optional last update time.</summary>
    public DateTime? UpdatedAtUtc { get; init; }
}
