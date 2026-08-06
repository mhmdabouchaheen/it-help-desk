using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Notifications;

/// <summary>Centralizes safe ticket-notification copy and recipient rules.</summary>
public sealed class TicketNotificationService(INotificationService notifications, ApplicationDbContext db)
    : ITicketNotificationService
{
    public Task NotifyAssignmentAsync(Guid ticketId,string referenceNumber,Guid assigneeUserId,Guid actingUserId,CancellationToken token=default) =>
        SendAsync([assigneeUserId],actingUserId,ticketId,NotificationTypes.TicketAssigned,"Ticket assigned",$"{referenceNumber} was assigned to you.",token);
    public Task NotifyStatusChangedAsync(Guid ticketId,string referenceNumber,Guid creatorUserId,Guid actingUserId,string statusName,CancellationToken token=default) =>
        SendAsync([creatorUserId],actingUserId,ticketId,NotificationTypes.TicketStatusChanged,"Ticket status changed",$"{referenceNumber} changed to {statusName}.",token);
    public async Task NotifyCommentAddedAsync(Guid ticketId,string referenceNumber,Guid creatorUserId,Guid? assigneeUserId,Guid actingUserId,bool isInternal,CancellationToken token=default)
    {
        IEnumerable<Guid> recipients;
        if(isInternal)
        {
            recipients=assigneeUserId is null?[]:[assigneeUserId.Value];
            recipients=await SupportRecipientsAsync(recipients,token);
        }
        else recipients=assigneeUserId is null?[creatorUserId]:[creatorUserId,assigneeUserId.Value];
        await SendAsync(recipients,actingUserId,ticketId,isInternal?NotificationTypes.TicketInternalCommentAdded:NotificationTypes.TicketCommentAdded,
            isInternal?"Internal ticket note added":"Ticket comment added",$"A new {(isInternal?"internal note":"comment")} was added to {referenceNumber}.",token);
    }
    public Task NotifyTicketCancelledAsync(Guid ticketId,string referenceNumber,Guid creatorUserId,Guid? previousAssigneeUserId,Guid actingUserId,CancellationToken token=default) =>
        SendAsync(previousAssigneeUserId is null?[creatorUserId]:[creatorUserId,previousAssigneeUserId.Value],actingUserId,ticketId,
            NotificationTypes.TicketCancelled,"Ticket cancelled",$"{referenceNumber} was cancelled.",token);
    public Task NotifyAttachmentAddedAsync(Guid ticketId,string referenceNumber,Guid creatorUserId,Guid? assigneeUserId,Guid actingUserId,CancellationToken token=default) =>
        SendAsync(assigneeUserId is null?[creatorUserId]:[creatorUserId,assigneeUserId.Value],actingUserId,ticketId,
            NotificationTypes.TicketAttachmentAdded,"Ticket attachment added",$"A new attachment was added to {referenceNumber}.",token);

    private async Task SendAsync(IEnumerable<Guid> recipients,Guid actor,Guid ticketId,string type,string title,string message,CancellationToken token)
    {foreach(var id in recipients.Where(x=>x!=Guid.Empty&&x!=actor).Distinct())await notifications.CreateAsync(id,ticketId,type,title,message,null,token);}
    private async Task<IReadOnlyList<Guid>> SupportRecipientsAsync(IEnumerable<Guid> recipients,CancellationToken token)
    {var ids=recipients.Distinct().ToArray();return await(from ur in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on ur.RoleId equals role.Id where ids.Contains(ur.UserId)&&(role.Name==AppRoles.Admin||role.Name==AppRoles.ItSupportAgent) select ur.UserId).Distinct().ToListAsync(token);}
}
