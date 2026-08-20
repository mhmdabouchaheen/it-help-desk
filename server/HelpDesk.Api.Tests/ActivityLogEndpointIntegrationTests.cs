using System.Net;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Contracts.Common;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class ActivityLogEndpointIntegrationTests
{
    [Fact]public async Task Global_AnonymousIsUnauthorized(){await using var f=new AuthApiFactory();Assert.Equal(HttpStatusCode.Unauthorized,(await f.CreateClient().GetAsync("/api/activity-logs")).StatusCode);}
    [Theory][InlineData(AppRoles.Employee)][InlineData(AppRoles.Manager)]public async Task Global_OrdinaryRolesAreForbidden(string role){await using var f=new AuthApiFactory();Assert.Equal(HttpStatusCode.Forbidden,(await f.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/activity-logs")).StatusCode);}
    [Theory][InlineData(AppRoles.Admin)][InlineData(AppRoles.ItSupportAgent)]public async Task Global_SupportRolesCanReadAndFiltersBind(string role){await using var f=new AuthApiFactory();var response=await f.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/activity-logs?pageNumber=2&action=ticket.created");Assert.Equal(HttpStatusCode.OK,response.StatusCode);f.ActivityLogService.Verify(x=>x.GetPagedAsync(It.Is<ActivityLogListRequest>(r=>r.PageNumber==2&&r.Action=="ticket.created"),It.IsAny<CancellationToken>()));}
    [Theory][InlineData(AppRoles.Employee)][InlineData(AppRoles.Manager)][InlineData(AppRoles.Admin)][InlineData(AppRoles.ItSupportAgent)]public async Task TicketActivity_UsesJwtVisibilityPathAndBindsPagination(string role){await using var f=new AuthApiFactory();var user=Guid.NewGuid();var attacker=Guid.NewGuid();var client=f.CreateAuthorizedClient(user,role);client.DefaultRequestHeaders.Add("X-User-Id",attacker.ToString());var response=await client.GetAsync($"/api/tickets/{AuthApiFactory.TicketId}/activity?pageNumber=2&pageSize=10&userId={attacker}");Assert.Equal(HttpStatusCode.OK,response.StatusCode);f.TicketService.Verify(x=>x.GetByIdAsync(AuthApiFactory.TicketId,It.Is<TicketAccessContext>(a=>a.UserId==user&&a.Roles.Contains(role)),It.IsAny<CancellationToken>()));f.ActivityLogService.Verify(x=>x.GetForTicketAsync(AuthApiFactory.TicketId,It.Is<PagedRequest>(r=>r.PageNumber==2&&r.PageSize==10),It.IsAny<CancellationToken>()));}
    [Theory][InlineData(AppRoles.Employee)][InlineData(AppRoles.Manager)]public async Task TicketActivity_DoesNotQueryOrExposeCountsWhenTicketIsNotVisible(string role){await using var f=new AuthApiFactory();f.TicketService.Setup(x=>x.GetByIdAsync(AuthApiFactory.TicketId,It.IsAny<TicketAccessContext>(),It.IsAny<CancellationToken>())).ThrowsAsync(new TicketNotFoundException());var response=await f.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync($"/api/tickets/{AuthApiFactory.TicketId}/activity?pageNumber=999&pageSize=100");Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);f.ActivityLogService.Verify(x=>x.GetForTicketAsync(It.IsAny<Guid>(),It.IsAny<PagedRequest>(),It.IsAny<CancellationToken>()),Times.Never);}
    [Fact]public async Task NoClientWriteEndpointExists(){await using var f=new AuthApiFactory();var response=await f.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Admin).PostAsync("/api/activity-logs",new StringContent("{}",System.Text.Encoding.UTF8,"application/json"));Assert.Equal(HttpStatusCode.MethodNotAllowed,response.StatusCode);}
}
