using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

/// <summary>Provides the restricted directory of valid ticket assignees.</summary>
[ApiController]
[Route("api/support-users")]
[Authorize(Policy = AppPolicies.SupportStaff)]
public sealed class SupportUsersController(ISupportUserDirectoryService directoryService) : ControllerBase
{
    /// <summary>Gets active users eligible to receive ticket assignments.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupportUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SupportUserResponse>>> GetAsync(
        CancellationToken cancellationToken) =>
        Ok(await directoryService.GetEligibleSupportUsersAsync(cancellationToken));
}
