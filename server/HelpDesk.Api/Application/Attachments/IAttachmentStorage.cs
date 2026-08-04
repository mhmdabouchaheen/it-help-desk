namespace HelpDesk.Api.Application.Attachments;

/// <summary>Stores attachment bytes behind opaque keys.</summary>
public interface IAttachmentStorage
{
    /// <summary>Saves content using a validated extension.</summary>
    Task<StoredAttachmentResult> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    /// <summary>Opens stored content for asynchronous read access.</summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    /// <summary>Deletes stored content idempotently.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

/// <summary>Describes privately stored attachment content.</summary>
public sealed record StoredAttachmentResult(string StorageProvider, string StorageKey, long SizeBytes, string ContentHash);
