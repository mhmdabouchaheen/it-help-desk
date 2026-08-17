using HelpDesk.Api.Contracts.Reports;

namespace HelpDesk.Api.Application.Reports;

public interface IReportService
{
    Task<TicketReportResponse> GetTicketReportAsync(TicketReportRequest request, CancellationToken cancellationToken = default);
}
