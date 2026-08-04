using System.Net;
using System.Net.Http.Json;
using HelpDesk.Api.Application.Authorization;

namespace HelpDesk.Api.Tests;

public sealed class TicketCancellationEndpointIntegrationTests(AuthApiFactory factory):IClassFixture<AuthApiFactory>
{
    [Fact] public async Task AnonymousCancelReturns401(){var response=await factory.CreateClient().PostAsJsonAsync($"/api/tickets/{AuthApiFactory.TicketId}/cancel",new{reason="x"});Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);}
    [Fact] public async Task AuthenticatedCancelReturns200AndReachesService(){factory.TicketService.Invocations.Clear();var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee).PostAsJsonAsync($"/api/tickets/{AuthApiFactory.TicketId}/cancel",new{reason="x",actingUserId=Guid.NewGuid(),statusId=5,cancelledAtUtc=DateTime.UtcNow});Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Single(factory.TicketService.Invocations,x=>x.Method.Name=="CancelAsync");}
    [Fact] public async Task ReasonOver500ReturnsValidationProblem(){var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee).PostAsJsonAsync($"/api/tickets/{AuthApiFactory.TicketId}/cancel",new{reason=new string('x',501)});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Contains("validation_failed",await response.Content.ReadAsStringAsync());}
}
