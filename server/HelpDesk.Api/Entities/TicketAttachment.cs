using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents metadata and an external storage location for a ticket attachment.
/// </summary>
public class TicketAttachment
{
    /// <summary>
    /// Gets or sets the unique identifier for the attachment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ticket that owns the attachment.
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the comment that introduced the attachment, or null if none.
    /// </summary>
    public Guid? CommentId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who uploaded the attachment.
    /// </summary>
    public Guid UploadedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the original display filename of the attachment.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media type of the attachment.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attachment size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the external storage provider.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string StorageProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque key used to locate the attachment in external storage.
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional content digest used for integrity verification or deduplication.
    /// </summary>
    [MaxLength(128)]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the attachment metadata was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the attachment was soft-deleted, or null if active.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
