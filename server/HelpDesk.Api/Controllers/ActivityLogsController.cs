using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/activity-logs")]
[Authorize(Policy = AppPolicies.SupportStaff)]
public sealed class ActivityLogsController(IActivityLogService activityLogs) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ActivityLogResponse>),StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ActivityLogResponse>>> GetAsync(
        [FromQuery] ActivityLogListRequest request,CancellationToken cancellationToken) =>
        Ok(await activityLogs.GetPagedAsync(request,cancellationToken));
}
