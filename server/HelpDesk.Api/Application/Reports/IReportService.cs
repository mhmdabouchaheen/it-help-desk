using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Application.Tickets;

namespace HelpDesk.Api.Application.Reports;

public interface IReportService
{
    Task<TicketReportResponse> GetTicketReportAsync(TicketReportRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
}
