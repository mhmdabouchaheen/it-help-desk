using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController(
    ITicketService ticketService,
    ITicketAccessContextFactory accessContextFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketDetailResponse>> CreateAsync(
        [FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await ticketService.CreateAsync(request, Access(), cancellationToken);
        return CreatedAtAction("GetById", new { ticketId = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TicketSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TicketSummaryResponse>>> GetPagedAsync(
        [FromQuery] TicketListRequest request, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetPagedAsync(request, Access(), cancellationToken));

    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDetailResponse>> GetByIdAsync(
        Guid ticketId, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetByIdAsync(ticketId, Access(), cancellationToken));

    [HttpPut("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDetailResponse>> UpdateAsync(
        Guid ticketId, [FromBody] UpdateTicketRequest request, CancellationToken cancellationToken) =>
        Ok(await ticketService.UpdateAsync(ticketId, request, Access(), cancellationToken));

    [HttpPost("{ticketId:guid}/assignment")]
    [Authorize(Policy = AppPolicies.SupportStaff)]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDetailResponse>> AssignAsync(
        Guid ticketId, [FromBody] AssignTicketRequest request, CancellationToken cancellationToken) =>
        Ok(await ticketService.AssignAsync(ticketId, request, Access(), cancellationToken));

    [HttpPost("{ticketId:guid}/status")]
    [Authorize(Policy = AppPolicies.SupportStaff)]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDetailResponse>> ChangeStatusAsync(
        Guid ticketId, [FromBody] ChangeTicketStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await ticketService.ChangeStatusAsync(ticketId, request, Access(), cancellationToken));

    [HttpPost("{ticketId:guid}/comments")]
    [ProducesResponseType(typeof(TicketCommentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketCommentResponse>> AddCommentAsync(
        Guid ticketId, [FromBody] AddTicketCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await ticketService.AddCommentAsync(ticketId, request, Access(), cancellationToken);
        return Created($"/api/tickets/{ticketId}/comments/{comment.Id}", comment);
    }

    [HttpPost("{ticketId:guid}/cancel")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketDetailResponse>> CancelAsync(Guid ticketId, [FromBody] CancelTicketRequest request, CancellationToken cancellationToken) =>
        Ok(await ticketService.CancelAsync(ticketId, request, Access(), cancellationToken));

    private TicketAccessContext Access() => accessContextFactory.Create(User);
}
