using HelpDesk.Api.Contracts.Notifications;

namespace HelpDesk.Api.Application.Notifications;

/// <summary>Publishes safe notification invalidations to a single recipient.</summary>
public interface INotificationRealtimePublisher
{
    Task PublishCreatedAsync(Guid recipientUserId, NotificationRealtimeEvent notificationEvent,
        CancellationToken cancellationToken = default);
}
