using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Notifications;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Notifications;

/// <summary>EF-backed, recipient-scoped persistent notification operations.</summary>
public sealed class NotificationService(ApplicationDbContext db, TimeProvider timeProvider,
    INotificationRealtimePublisher realtimePublisher, ILogger<NotificationService> logger,
    IActivityLogService? activityLogs = null) : INotificationService
{
    public async Task CreateAsync(Guid recipientUserId, Guid? ticketId, string type, string title, string message,
        DateTime? expiresAtUtc = null, CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty || string.IsNullOrWhiteSpace(type) ||
            string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            throw new NotificationValidationException();
        var notification = new Notification { Id = Guid.NewGuid(), RecipientUserId = recipientUserId,
            TicketId = ticketId, Type = type.Trim(), Title = title.Trim(), Message = message.Trim(),
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime, ExpiresAtUtc = expiresAtUtc };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created notification type {NotificationType} for recipient {RecipientUserId}.", type.Trim(), recipientUserId);
        try
        {
            await realtimePublisher.PublishCreatedAsync(recipientUserId, new NotificationRealtimeEvent
            {
                NotificationId = notification.Id, TicketId = notification.TicketId,
                Type = notification.Type, CreatedAtUtc = notification.CreatedAtUtc
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception,
                "Real-time notification delivery failed for notification {NotificationId} and recipient {RecipientUserId}.",
                notification.Id, recipientUserId);
        }
    }

    public async Task<PagedResponse<NotificationResponse>> GetPagedAsync(Guid currentUserId,
        NotificationListRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUser(currentUserId); ArgumentNullException.ThrowIfNull(request);
        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100) throw new NotificationValidationException();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == currentUserId &&
            (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now));
        if (request.UnreadOnly) query = query.Where(x => x.ReadAtUtc == null);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip(checked((request.PageNumber - 1) * request.PageSize)).Take(request.PageSize)
            .Select(x => new NotificationResponse { Id=x.Id, TicketId=x.TicketId, Type=x.Type, Title=x.Title,
                Message=x.Message, CreatedAtUtc=x.CreatedAtUtc, ReadAtUtc=x.ReadAtUtc, IsRead=x.ReadAtUtc != null })
            .ToListAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize);
        return new PagedResponse<NotificationResponse> { Items=items, PageNumber=request.PageNumber,
            PageSize=request.PageSize, TotalCount=total, TotalPages=pages,
            HasPreviousPage=request.PageNumber > 1 && pages > 0, HasNextPage=request.PageNumber < pages };
    }

    public async Task<NotificationUnreadCountResponse> GetUnreadCountAsync(Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUser(currentUserId); var now=timeProvider.GetUtcNow().UtcDateTime;
        return new() { UnreadCount = await db.Notifications.AsNoTracking().CountAsync(x =>
            x.RecipientUserId == currentUserId && x.ReadAtUtc == null &&
            (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now), cancellationToken) };
    }

    public async Task MarkAsReadAsync(Guid currentUserId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        ValidateUser(currentUserId); if(notificationId==Guid.Empty) throw new NotificationValidationException();
        var item=await db.Notifications.SingleOrDefaultAsync(x=>x.Id==notificationId&&x.RecipientUserId==currentUserId,cancellationToken)
            ?? throw new NotificationNotFoundException();
        if(item.ReadAtUtc is null){item.ReadAtUtc=timeProvider.GetUtcNow().UtcDateTime;await db.SaveChangesAsync(cancellationToken);await TryAuditAsync(currentUserId,ActivityActions.NotificationMarkedRead,item.Id.ToString(),new Dictionary<string,string?>{{"notificationId",item.Id.ToString()}},cancellationToken);}
    }

    public async Task MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        ValidateUser(currentUserId);var items=await db.Notifications.Where(x=>x.RecipientUserId==currentUserId&&x.ReadAtUtc==null).ToListAsync(cancellationToken);
        if(items.Count==0)return;var now=timeProvider.GetUtcNow().UtcDateTime;foreach(var item in items)item.ReadAtUtc=now;await db.SaveChangesAsync(cancellationToken);await TryAuditAsync(currentUserId,ActivityActions.NotificationMarkedAllRead,currentUserId.ToString(),new Dictionary<string,string?>{{"count",items.Count.ToString()}},cancellationToken);
    }
    private async Task TryAuditAsync(Guid actor,string action,string id,IReadOnlyDictionary<string,string?> metadata,CancellationToken token){if(activityLogs is null)return;try{await activityLogs.WriteAsync(actor,action,ActivityEntityTypes.Notification,id,metadata,token);}catch(Exception exception){logger.LogWarning(exception,"Activity logging failed after notification operation {Action}.",action);}}
    private static void ValidateUser(Guid id){if(id==Guid.Empty)throw new NotificationValidationException();}
}
