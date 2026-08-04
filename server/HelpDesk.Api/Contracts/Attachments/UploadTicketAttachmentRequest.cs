using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HelpDesk.Api.Contracts.Attachments;

/// <summary>Represents a multipart ticket attachment upload.</summary>
public sealed class UploadTicketAttachmentRequest
{
    /// <summary>Gets or sets the uploaded file.</summary>
    [Required]
    public IFormFile File { get; set; } = null!;
}
