using System.Text;
using ClosedXML.Excel;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Reports;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Infrastructure.Reports;
using Moq;
using PdfSharp.Pdf.IO;

namespace HelpDesk.Api.Tests;

public sealed class ReportExportServiceTests
{
    public ReportExportServiceTests() => ReportPdfFontConfiguration.Configure();

    [Fact]
    public async Task Pdf_UsesEmbeddedRegularAndBoldFonts_AndProducesSafeNonEmptyFile()
    {
        var context=Create();using var cts=new CancellationTokenSource();
        var result=await context.Service.ExportTicketReportPdfAsync(context.Request,context.Access,cts.Token);
        Assert.Equal(ReportExportService.PdfContentType,result.ContentType);
        Assert.StartsWith("%PDF",Encoding.ASCII.GetString(result.Content,0,4));
        Assert.EndsWith(".pdf",result.FileName);Assert.DoesNotContain(result.FileName,Path.GetInvalidFileNameChars());
        context.Reports.Verify(x=>x.GetTicketReportAsync(context.Request,context.Access,cts.Token),Times.Once);
        var binary=Encoding.Latin1.GetString(result.Content);Assert.Contains("IT Help Desk",binary);Assert.Contains("Average Resolution Time",binary);Assert.Contains("2h 15m",binary);
        Assert.DoesNotContain("agent@example.test",binary);Assert.DoesNotContain("secret-token",binary);
        Assert.DoesNotContain("internal note",binary);Assert.DoesNotContain("storage/key",binary);
        var resources=typeof(ReportPdfFontConfiguration).Assembly.GetManifestResourceNames();
        Assert.Contains("HelpDesk.Api.Assets.Fonts.DejaVuSans.ttf",resources);
        Assert.Contains("HelpDesk.Api.Assets.Fonts.DejaVuSans-Bold.ttf",resources);
    }

    [Fact]
    public async Task Pdf_AcceptsRepresentativeArabicAndLatinDisplayNames()
    {
        var context=Create(agentDisplayName:"محمد أبو شاهين — Élodie");
        var result=await context.Service.ExportTicketReportPdfAsync(context.Request,context.Access);
        Assert.StartsWith("%PDF",Encoding.ASCII.GetString(result.Content,0,4));
        using var document=PdfReader.Open(new MemoryStream(result.Content));Assert.True(document.PageCount>=1);
    }

    [Fact]
    public async Task Pdf_LargeReport_ProducesMultiplePages()
    {
        var trend=Enumerable.Range(0,90).Select(day=>new TicketReportTrendResponse
        {PeriodStartUtc=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc).AddDays(day),CreatedCount=day%5,ClosedCount=day%3}).ToArray();
        var context=Create(trend:trend);var result=await context.Service.ExportTicketReportPdfAsync(context.Request,context.Access);
        using var document=PdfReader.Open(new MemoryStream(result.Content));Assert.True(document.PageCount>1);
    }

    [Fact]
    public async Task Excel_HasRequiredSheetsAndAuthoritativeValues()
    {
        var context=Create();var result=await context.Service.ExportTicketReportExcelAsync(context.Request,context.Access);
        Assert.Equal(ReportExportService.ExcelContentType,result.ContentType);Assert.EndsWith(".xlsx",result.FileName);
        using var workbook=new XLWorkbook(new MemoryStream(result.Content));
        Assert.Equal(["Summary","Status","Priority","Categories","Trend","Agent Workload"],workbook.Worksheets.Select(x=>x.Name));
        Assert.Equal("IT Help Desk - Ticket Report",workbook.Worksheet("Summary").Cell("A1").GetString());
        Assert.Contains(workbook.Worksheet("Summary").CellsUsed(),x=>x.GetString()=="7");Assert.Contains(workbook.Worksheet("Summary").CellsUsed(),x=>x.GetString()=="2h 15m");
        Assert.Contains(workbook.Worksheet("Status").CellsUsed(),x=>x.GetString()=="Open");
        Assert.Contains(workbook.Worksheet("Agent Workload").CellsUsed(),x=>x.GetString()=="Safe Agent");
        Assert.DoesNotContain(workbook.Worksheets.SelectMany(x=>x.CellsUsed()),x=>x.GetString().Contains("example.test")||x.GetString().Contains("token")||x.GetString().Contains("internal note")||x.GetString().Contains("storage/key"));
        context.Reports.Verify(x=>x.GetTicketReportAsync(context.Request,context.Access,It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task EmptyReport_ProducesValidPdfAndWorkbook()
    {
        var access=AdminAccess();var reports=new Mock<IReportService>();reports.Setup(x=>x.GetTicketReportAsync(It.IsAny<TicketReportRequest>(),access,It.IsAny<CancellationToken>())).ReturnsAsync(new TicketReportResponse());
        var service=new ReportExportService(reports.Object,new FixedTime());var pdf=await service.ExportTicketReportPdfAsync(new(),access);
        Assert.StartsWith("%PDF",Encoding.ASCII.GetString(pdf.Content,0,4));using var pdfDocument=PdfReader.Open(new MemoryStream(pdf.Content));Assert.True(pdfDocument.PageCount>=1);
        var excel=await service.ExportTicketReportExcelAsync(new(),access);using var workbook=new XLWorkbook(new MemoryStream(excel.Content));Assert.Equal(6,workbook.Worksheets.Count);
    }

    private static Context Create(string agentDisplayName="Safe Agent",IReadOnlyList<TicketReportTrendResponse>? trend=null)
    {
        var request=new TicketReportRequest{FromUtc=new DateTime(2026,8,1,0,0,0,DateTimeKind.Utc),CategoryId=1};
        var data=new TicketReportResponse{Summary=new(){TotalTickets=7,OpenTickets=4,TerminalTickets=3,CancelledTickets=1,AssignedTickets=5,UnassignedTickets=2,AverageResolutionMinutes=135},StatusBreakdown=[new(){Id=1,Name="Open",Count=4}],PriorityBreakdown=[new(){Id=2,Name="Critical",Count=2}],CategoryBreakdown=[new(){Id=1,Name="Hardware",Count=7}],Trend=trend??[new(){PeriodStartUtc=new DateTime(2026,8,1,0,0,0,DateTimeKind.Utc),CreatedCount=2,ClosedCount=1}],AgentWorkload=[new(){UserId=Guid.NewGuid(),DisplayName=agentDisplayName,ActiveTicketCount=3}]};
        var access=AdminAccess();var reports=new Mock<IReportService>();reports.Setup(x=>x.GetTicketReportAsync(request,access,It.IsAny<CancellationToken>())).ReturnsAsync(data);
        return new(request,access,reports,new ReportExportService(reports.Object,new FixedTime()));
    }
    private static TicketAccessContext AdminAccess()=>new(){UserId=Guid.NewGuid(),Roles=[AppRoles.Admin]};
    private sealed class FixedTime:TimeProvider{public override DateTimeOffset GetUtcNow()=>new(2026,8,17,14,30,0,TimeSpan.Zero);}
    private sealed record Context(TicketReportRequest Request,TicketAccessContext Access,Mock<IReportService> Reports,ReportExportService Service);
}
