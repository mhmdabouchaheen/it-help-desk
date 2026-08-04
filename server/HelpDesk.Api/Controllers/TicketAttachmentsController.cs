using HelpDesk.Api.Application.Attachments;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Contracts.Attachments;
using HelpDesk.Api.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

/// <summary>Provides authenticated streaming access to ticket attachments.</summary>
[ApiController]
[Authorize]
[Route("api/tickets/{ticketId:guid}/attachments")]
public sealed class TicketAttachmentsController(ITicketAttachmentService service, ITicketAccessContextFactory accessFactory) : ControllerBase
{
    /// <summary>Uploads one protected attachment.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    [ProducesResponseType(typeof(TicketAttachmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketAttachmentResponse>> UploadAsync(Guid ticketId, [FromForm] UploadTicketAttachmentRequest request, CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();
        var result = await service.UploadAsync(ticketId, content, request.File.FileName, request.File.ContentType, request.File.Length, accessFactory.Create(User), cancellationToken);
        return Created($"/api/tickets/{ticketId}/attachments/{result.Id}", result);
    }

    /// <summary>Downloads protected attachment content.</summary>
    [HttpGet("{attachmentId:guid}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAsync(Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(ticketId, attachmentId, accessFactory.Create(User), cancellationToken);
        Response.Headers.CacheControl = "private, no-store";
        return File(result.Content, result.ContentType, result.DownloadFileName, enableRangeProcessing: false);
    }

    /// <summary>Soft-deletes an attachment and schedules its stored content for removal.</summary>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
    { await service.DeleteAsync(ticketId, attachmentId, accessFactory.Create(User), cancellationToken); return NoContent(); }
}
