namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents safe attachment metadata without storage details.</summary>
public sealed class TicketAttachmentResponse
{
    /// <summary>Gets the attachment identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets the ticket identifier.</summary>
    public Guid TicketId { get; init; }
    /// <summary>Gets the optional introducing comment identifier.</summary>
    public Guid? CommentId { get; init; }
    /// <summary>Gets the original display filename.</summary>
    public string OriginalFileName { get; init; } = string.Empty;
    /// <summary>Gets the media type.</summary>
    public string ContentType { get; init; } = string.Empty;
    /// <summary>Gets the file size in bytes.</summary>
    public long SizeBytes { get; init; }
    /// <summary>Gets the uploader identifier.</summary>
    public Guid UploadedByUserId { get; init; }
    /// <summary>Gets the uploader display name.</summary>
    public string UploadedByDisplayName { get; init; } = string.Empty;
    /// <summary>Gets the metadata creation time.</summary>
    public DateTime CreatedAtUtc { get; init; }
}
