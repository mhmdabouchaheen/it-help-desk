using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController, Authorize, Route("api/profile")]
public sealed class ProfileController(IAuthenticationService authenticationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserResponse>> GetAsync(CancellationToken cancellationToken) =>
        Ok(await authenticationService.GetCurrentUserAsync(UserId(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<CurrentUserResponse>> UpdateAsync(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await authenticationService.UpdateProfileAsync(UserId(), request, cancellationToken));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authenticationService.ChangePasswordAsync(UserId(), request, ClientIp(), cancellationToken);
        return NoContent();
    }

    private Guid UserId()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(subject, out var userId) ? userId : throw new AuthenticationFailedException();
    }

    private string? ClientIp()
    {
        var address = HttpContext.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();
        return address?.ToString();
    }
}
