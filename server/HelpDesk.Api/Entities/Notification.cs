using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents an in-application notification belonging to a recipient user.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the unique identifier for the notification.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who receives the notification.
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the related ticket, or null when not ticket-specific.
    /// </summary>
    public Guid? TicketId { get; set; }

    /// <summary>
    /// Gets or sets the stable application event type for the notification.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short display title of the notification.
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message intended for the recipient.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the notification was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the notification was first read, or null while unread.
    /// </summary>
    public DateTime? ReadAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional UTC date and time when the notification expires.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }
}
