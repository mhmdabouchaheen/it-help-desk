using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Tickets;

namespace HelpDesk.Api.Application.Attachments;

/// <summary>Manages protected ticket attachments.</summary>
public interface ITicketAttachmentService
{
    Task<TicketAttachmentResponse> UploadAsync(Guid ticketId, Stream content, string originalFileName, string contentType, long sizeBytes, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    Task<AttachmentDownloadResult> DownloadAsync(Guid ticketId, Guid attachmentId, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid ticketId, Guid attachmentId, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
}
