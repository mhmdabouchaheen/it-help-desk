using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notifications,ITicketAccessContextFactory accessFactory):ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NotificationResponse>),StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetAsync([FromQuery]NotificationListRequest request,CancellationToken token) => Ok(await notifications.GetPagedAsync(accessFactory.Create(User).UserId,request,token));
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(NotificationUnreadCountResponse),StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountResponse>> GetUnreadCountAsync(CancellationToken token) => Ok(await notifications.GetUnreadCountAsync(accessFactory.Create(User).UserId,token));
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsReadAsync(Guid notificationId,CancellationToken token){await notifications.MarkAsReadAsync(accessFactory.Create(User).UserId,notificationId,token);return NoContent();}
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken token){await notifications.MarkAllAsReadAsync(accessFactory.Create(User).UserId,token);return NoContent();}
}
