using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents a public reply or internal support note associated with a ticket.
/// </summary>
public class TicketComment
{
    /// <summary>
    /// Gets or sets the unique identifier for the comment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ticket containing the comment.
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who authored the comment.
    /// </summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>
    /// Gets or sets the comment content.
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the visibility classification of the comment.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Visibility { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the comment was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the comment was last updated, or null if never updated.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the comment was soft-deleted, or null if active.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
