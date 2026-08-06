using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Notifications;

namespace HelpDesk.Api.Application.Notifications;

/// <summary>Creates and manages recipient-scoped persistent notifications.</summary>
public interface INotificationService
{
    Task CreateAsync(Guid recipientUserId, Guid? ticketId, string type, string title, string message,
        DateTime? expiresAtUtc = null, CancellationToken cancellationToken = default);
    Task<PagedResponse<NotificationResponse>> GetPagedAsync(Guid currentUserId, NotificationListRequest request,
        CancellationToken cancellationToken = default);
    Task<NotificationUnreadCountResponse> GetUnreadCountAsync(Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid currentUserId, Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
