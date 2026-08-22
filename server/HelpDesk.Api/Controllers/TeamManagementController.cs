using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/admin/team-members"), Authorize(Policy = AppPolicies.AdminOnly)]
public sealed class TeamManagementController(IUserTeamManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamMemberResponse>>> GetAsync(CancellationToken cancellationToken) =>
        Ok(await service.GetUsersAsync(cancellationToken));

    [HttpPut("{userId:guid}/manager")]
    public async Task<ActionResult<TeamMemberResponse>> UpdateManagerAsync(
        Guid userId,
        [FromBody] UpdateUserManagerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateManagerAsync(userId, request, cancellationToken));
}
