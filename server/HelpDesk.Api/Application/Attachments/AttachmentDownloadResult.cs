namespace HelpDesk.Api.Application.Attachments;

/// <summary>Provides a caller-owned download stream and safe response metadata.</summary>
public sealed record AttachmentDownloadResult(Stream Content, string ContentType, string DownloadFileName, long SizeBytes);
