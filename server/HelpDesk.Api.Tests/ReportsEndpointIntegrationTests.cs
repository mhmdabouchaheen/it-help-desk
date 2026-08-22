using System.Net;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Reports;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class ReportsEndpointIntegrationTests
{
    [Theory][InlineData("pdf")][InlineData("excel")]
    public async Task AnonymousExports_AreUnauthorized(string format){await using var factory=new AuthApiFactory();Assert.Equal(HttpStatusCode.Unauthorized,(await factory.CreateClient().GetAsync($"/api/reports/tickets/export/{format}")).StatusCode);}
    [Theory][InlineData(AppRoles.Employee,"pdf")][InlineData(AppRoles.Employee,"excel")]
    public async Task OrdinaryRoles_CannotExport(string role,string format){await using var factory=new AuthApiFactory();Assert.Equal(HttpStatusCode.Forbidden,(await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync($"/api/reports/tickets/export/{format}")).StatusCode);}
    [Theory][InlineData("pdf")][InlineData("excel")]
    public async Task HeadersAndQueryIdentity_CannotBypassSupportPolicy(string format){await using var factory=new AuthApiFactory();var client=factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee);client.DefaultRequestHeaders.Add("X-Role",AppRoles.Admin);client.DefaultRequestHeaders.Add("X-User-Id",Guid.NewGuid().ToString());Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/reports/tickets/export/{format}?userId={Guid.NewGuid()}&role=Admin")).StatusCode);}
    [Theory][InlineData(AppRoles.Admin,"pdf","application/pdf",".pdf")][InlineData(AppRoles.Admin,"excel","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",".xlsx")][InlineData(AppRoles.ItSupportAgent,"pdf","application/pdf",".pdf")][InlineData(AppRoles.ItSupportAgent,"excel","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",".xlsx")]
    public async Task SupportExports_ReturnAttachmentsAndBindFilters(string role,string format,string contentType,string extension){await using var factory=new AuthApiFactory();var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync($"/api/reports/tickets/export/{format}?categoryId=1");Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal(contentType,response.Content.Headers.ContentType?.MediaType);Assert.True(response.Content.Headers.ContentDisposition?.FileNameStar?.EndsWith(extension)??response.Content.Headers.ContentDisposition?.FileName?.EndsWith(extension));if(format=="pdf")factory.ReportExportService.Verify(x=>x.ExportTicketReportPdfAsync(It.Is<TicketReportRequest>(r=>r.CategoryId==1),It.Is<TicketAccessContext>(a=>a.Roles.Contains(role)),It.IsAny<CancellationToken>()));else factory.ReportExportService.Verify(x=>x.ExportTicketReportExcelAsync(It.Is<TicketReportRequest>(r=>r.CategoryId==1),It.Is<TicketAccessContext>(a=>a.Roles.Contains(role)),It.IsAny<CancellationToken>()));}
    [Theory][InlineData("pdf")][InlineData("excel")]
    public async Task ExportInvalidFilters_ReturnSafeBadRequest(string format){await using var factory=new AuthApiFactory();var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Admin).GetAsync($"/api/reports/tickets/export/{format}?categoryId=0");Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Contains("validation_failed",await response.Content.ReadAsStringAsync());}
    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    public async Task SupportStaff_CanReadReportsAndFiltersBind(string role)
    {
        await using var factory=new AuthApiFactory();var agent=Guid.NewGuid();
        var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync($"/api/reports/tickets?fromUtc=2026-08-01T00:00:00Z&toUtc=2026-08-31T23:59:59Z&categoryId=1&priorityId=2&statusId=3&assignedToUserId={agent}");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        factory.ReportService.Verify(x=>x.GetTicketReportAsync(It.Is<TicketReportRequest>(r=>r.CategoryId==1&&r.PriorityId==2&&r.StatusId==3&&r.AssignedToUserId==agent&&r.FromUtc.HasValue&&r.ToUtc.HasValue),It.Is<TicketAccessContext>(a=>a.Roles.Contains(role)),It.IsAny<CancellationToken>()));
    }

    [Theory][InlineData(AppRoles.Employee)]
    public async Task OrdinaryRoles_AreForbidden(string role){await using var factory=new AuthApiFactory();Assert.Equal(HttpStatusCode.Forbidden,(await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/reports/tickets")).StatusCode);factory.ReportService.VerifyNoOtherCalls();}

    [Fact]public async Task Manager_CanReadTeamScopedReports(){await using var factory=new AuthApiFactory();var manager=Guid.NewGuid();Assert.Equal(HttpStatusCode.OK,(await factory.CreateAuthorizedClient(manager,AppRoles.Manager).GetAsync("/api/reports/tickets")).StatusCode);factory.ReportService.Verify(x=>x.GetTicketReportAsync(It.IsAny<TicketReportRequest>(),It.Is<TicketAccessContext>(a=>a.UserId==manager&&a.Roles.Contains(AppRoles.Manager)),It.IsAny<CancellationToken>()));}

    [Fact]public async Task Anonymous_IsUnauthorized(){await using var factory=new AuthApiFactory();Assert.Equal(HttpStatusCode.Unauthorized,(await factory.CreateClient().GetAsync("/api/reports/tickets")).StatusCode);}
    [Theory][InlineData("?fromUtc=2026-09-01T00:00:00Z&toUtc=2026-08-01T00:00:00Z")][InlineData("?categoryId=0")][InlineData("?assignedToUserId=00000000-0000-0000-0000-000000000000")]
    public async Task InvalidFilters_ReturnBadRequest(string query){await using var factory=new AuthApiFactory();Assert.Equal(HttpStatusCode.BadRequest,(await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Admin).GetAsync("/api/reports/tickets"+query)).StatusCode);factory.ReportService.VerifyNoOtherCalls();}
}
