using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/activity")]
[Authorize]
public sealed class TicketActivityController(ITicketService tickets,IActivityLogService activityLogs,
    ITicketAccessContextFactory accessFactory):ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ActivityLogResponse>),StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ActivityLogResponse>>> GetAsync(
        Guid ticketId,[FromQuery] PagedRequest request,CancellationToken token)
    {
        await tickets.GetByIdAsync(ticketId,accessFactory.Create(User),token);
        return Ok(await activityLogs.GetForTicketAsync(ticketId,request,token));
    }
}
