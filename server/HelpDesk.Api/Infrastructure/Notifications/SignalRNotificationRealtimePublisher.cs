using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Contracts.Notifications;
using HelpDesk.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HelpDesk.Api.Infrastructure.Notifications;

/// <summary>Sends notification invalidations only to the recipient's controlled private group.</summary>
public sealed class SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hubContext)
    : INotificationRealtimePublisher
{
    public Task PublishCreatedAsync(Guid recipientUserId, NotificationRealtimeEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationEvent);
        var group = NotificationUserGroup.For(recipientUserId);
        return hubContext.Clients.Group(group).SendAsync(
            NotificationHubEvents.NotificationCreated, notificationEvent, cancellationToken);
    }
}
