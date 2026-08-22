using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Reports;
using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Application.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AppPolicies.ManagementOrSupport)]
public sealed class ReportsController(IReportService reports,IReportExportService exports,
    ITicketAccessContextFactory accessContextFactory) : ControllerBase
{
    [HttpGet("tickets")]
    [ProducesResponseType(typeof(TicketReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketReportResponse>> GetTicketsAsync([FromQuery] TicketReportRequest request, CancellationToken cancellationToken) =>
        Ok(await reports.GetTicketReportAsync(request, Access(), cancellationToken));

    [HttpGet("tickets/export/pdf")]
    [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status200OK,"application/pdf")]
    public async Task<IActionResult>ExportPdfAsync([FromQuery]TicketReportRequest request,CancellationToken cancellationToken){var result=await exports.ExportTicketReportPdfAsync(request,Access(),cancellationToken);return File(result.Content,result.ContentType,result.FileName);}

    [HttpGet("tickets/export/excel")]
    [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status200OK,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult>ExportExcelAsync([FromQuery]TicketReportRequest request,CancellationToken cancellationToken){var result=await exports.ExportTicketReportExcelAsync(request,Access(),cancellationToken);return File(result.Content,result.ContentType,result.FileName);}

    private TicketAccessContext Access() => accessContextFactory.Create(User);
}
