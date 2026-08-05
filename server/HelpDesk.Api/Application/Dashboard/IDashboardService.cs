using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Dashboard;

namespace HelpDesk.Api.Application.Dashboard;

/// <summary>Builds analytics from tickets visible to a validated access context.</summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(TicketAccessContext accessContext,
        CancellationToken cancellationToken = default);
}
