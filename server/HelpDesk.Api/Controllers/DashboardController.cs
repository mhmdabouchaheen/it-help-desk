using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Dashboard;
using HelpDesk.Api.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboardService,
    ITicketAccessContextFactory accessContextFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardResponse>> GetAsync(CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetDashboardAsync(accessContextFactory.Create(User), cancellationToken));
}
