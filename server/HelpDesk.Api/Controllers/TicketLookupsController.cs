using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/ticket-lookups")]
[Authorize]
public sealed class TicketLookupsController(ITicketLookupService lookupService) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketCategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        Ok(await lookupService.GetCategoriesAsync(cancellationToken));

    [HttpGet("priorities")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketPriorityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketPriorityResponse>>> GetPrioritiesAsync(CancellationToken cancellationToken) =>
        Ok(await lookupService.GetPrioritiesAsync(cancellationToken));

    [HttpGet("statuses")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketStatusResponse>>> GetStatusesAsync(CancellationToken cancellationToken) =>
        Ok(await lookupService.GetStatusesAsync(cancellationToken));
}
