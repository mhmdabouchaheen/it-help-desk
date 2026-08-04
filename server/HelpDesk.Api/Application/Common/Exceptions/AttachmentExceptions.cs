namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that an attachment was not found or was intentionally hidden.</summary>
public sealed class AttachmentNotFoundException() : Exception("The attachment was not found.");
/// <summary>Indicates that attachment access was denied.</summary>
public sealed class AttachmentAccessDeniedException() : Exception("Access to the attachment was denied.");
/// <summary>Indicates that attachment metadata or content validation failed.</summary>
public sealed class AttachmentValidationException() : Exception("The attachment did not pass validation.");
/// <summary>Indicates that an attachment exceeds the configured maximum size.</summary>
public sealed class AttachmentTooLargeException() : Exception("The attachment exceeds the maximum allowed size.");
/// <summary>Indicates that stored attachment content is temporarily unavailable.</summary>
public sealed class AttachmentUnavailableException() : Exception("The attachment content is unavailable.");
