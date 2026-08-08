using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Application.Common.Exceptions;

namespace HelpDesk.Api.Hubs;

/// <summary>Resolves controlled private SignalR group names from validated JWT principals.</summary>
internal static class NotificationUserGroup
{
    internal static string For(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A non-empty user ID is required.", nameof(userId));
        return $"user:{userId:D}";
    }

    internal static string FromPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
            throw new InvalidAuthenticatedPrincipalException();
        return For(userId);
    }
}
