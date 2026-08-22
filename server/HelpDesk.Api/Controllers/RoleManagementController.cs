using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController, Route("api/admin/role-management"), Authorize(Policy = AppPolicies.AdminOnly)]
public sealed class RoleManagementController(IUserRoleManagementService service,
    ITicketAccessContextFactory accessContextFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleManagedUserResponse>>> GetAsync(CancellationToken token) =>
        Ok(await service.GetUsersAsync(token));

    [HttpPut("{userId:guid}/roles")]
    public async Task<ActionResult<RoleManagedUserResponse>> UpdateAsync(Guid userId,
        [FromBody] UpdateUserRolesRequest request, CancellationToken token) =>
        Ok(await service.UpdateRolesAsync(userId, request, accessContextFactory.Create(User).UserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), token));
}
