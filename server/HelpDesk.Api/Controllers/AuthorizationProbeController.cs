using HelpDesk.Api.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

/// <summary>
/// Provides a temporary internal surface for verifying registered authorization policies.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/authorization-probe")]
public sealed class AuthorizationProbeController : ControllerBase
{
    /// <summary>Verifies the authenticated-user policy.</summary>
    [HttpGet("authenticated")]
    [Authorize(Policy = AppPolicies.AuthenticatedUser)]
    public IActionResult Authenticated() => Ok();

    /// <summary>Verifies the administrator-only policy.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public IActionResult Admin() => Ok();

    /// <summary>Verifies the support-staff policy.</summary>
    [HttpGet("support")]
    [Authorize(Policy = AppPolicies.SupportStaff)]
    public IActionResult Support() => Ok();

    /// <summary>Verifies the management policy.</summary>
    [HttpGet("management")]
    [Authorize(Policy = AppPolicies.Management)]
    public IActionResult Management() => Ok();
}
