using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Application.Tickets;

namespace HelpDesk.Api.Application.Reports;

public sealed record ReportExportResult(byte[] Content,string ContentType,string FileName);

public interface IReportExportService
{
    Task<ReportExportResult> ExportTicketReportPdfAsync(TicketReportRequest request,TicketAccessContext accessContext,CancellationToken cancellationToken=default);
    Task<ReportExportResult> ExportTicketReportExcelAsync(TicketReportRequest request,TicketAccessContext accessContext,CancellationToken cancellationToken=default);
}
