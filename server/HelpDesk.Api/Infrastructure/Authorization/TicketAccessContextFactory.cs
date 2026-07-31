using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;

namespace HelpDesk.Api.Infrastructure.Authorization;

/// <summary>Reads actor and roles from validated JWT claims without external state.</summary>
public sealed class TicketAccessContextFactory : ITicketAccessContextFactory
{
    public TicketAccessContext Create(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated is not true)
            throw new InvalidAuthenticatedPrincipalException();

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
            throw new InvalidAuthenticatedPrincipalException();

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return new TicketAccessContext { UserId = userId, Roles = roles };
    }
}
