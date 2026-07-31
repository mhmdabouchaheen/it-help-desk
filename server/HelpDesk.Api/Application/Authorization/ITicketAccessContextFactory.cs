using System.Security.Claims;
using HelpDesk.Api.Application.Tickets;

namespace HelpDesk.Api.Application.Authorization;

/// <summary>Creates ticket access data exclusively from a validated principal.</summary>
public interface ITicketAccessContextFactory
{
    TicketAccessContext Create(ClaimsPrincipal principal);
}
