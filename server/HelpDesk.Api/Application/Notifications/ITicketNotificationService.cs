namespace HelpDesk.Api.Application.Notifications;

/// <summary>Formats and deduplicates notifications emitted by persisted ticket events.</summary>
public interface ITicketNotificationService
{
    Task NotifyAssignmentAsync(Guid ticketId, string referenceNumber, Guid assigneeUserId, Guid actingUserId, CancellationToken token = default);
    Task NotifyStatusChangedAsync(Guid ticketId, string referenceNumber, Guid creatorUserId, Guid actingUserId, string statusName, CancellationToken token = default);
    Task NotifyCommentAddedAsync(Guid ticketId, string referenceNumber, Guid creatorUserId, Guid? assigneeUserId, Guid actingUserId, bool isInternal, CancellationToken token = default);
    Task NotifyTicketCancelledAsync(Guid ticketId, string referenceNumber, Guid creatorUserId, Guid? previousAssigneeUserId, Guid actingUserId, CancellationToken token = default);
    Task NotifyAttachmentAddedAsync(Guid ticketId, string referenceNumber, Guid creatorUserId, Guid? assigneeUserId, Guid actingUserId, CancellationToken token = default);
}
