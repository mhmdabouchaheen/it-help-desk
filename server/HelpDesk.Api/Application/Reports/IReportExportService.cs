using HelpDesk.Api.Contracts.Reports;

namespace HelpDesk.Api.Application.Reports;

public sealed record ReportExportResult(byte[] Content,string ContentType,string FileName);

public interface IReportExportService
{
    Task<ReportExportResult> ExportTicketReportPdfAsync(TicketReportRequest request,CancellationToken cancellationToken=default);
    Task<ReportExportResult> ExportTicketReportExcelAsync(TicketReportRequest request,CancellationToken cancellationToken=default);
}
