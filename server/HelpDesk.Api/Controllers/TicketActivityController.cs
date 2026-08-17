using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Audit;
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
    public async Task<ActionResult<IReadOnlyList<ActivityLogResponse>>> GetAsync(Guid ticketId,CancellationToken token)
    {
        await tickets.GetByIdAsync(ticketId,accessFactory.Create(User),token);
        return Ok(await activityLogs.GetForTicketAsync(ticketId,token));
    }
}
