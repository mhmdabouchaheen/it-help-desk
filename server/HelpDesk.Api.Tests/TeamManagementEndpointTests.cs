using System.Net;
using System.Net.Http.Json;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Contracts.Users;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class TeamManagementEndpointTests
{
    [Theory][InlineData(AppRoles.Employee)][InlineData(AppRoles.Manager)][InlineData(AppRoles.ItSupportAgent)]
    public async Task NonAdminCannotManageTeams(string role){await using var f=new AuthApiFactory();Assert.Equal(HttpStatusCode.Forbidden,(await f.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/admin/team-members")).StatusCode);}
    [Fact]public async Task AdminCanAssignAndRemoveManager(){await using var f=new AuthApiFactory();var user=Guid.NewGuid();var manager=Guid.NewGuid();var client=f.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Admin);Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/admin/team-members/{user}/manager",new UpdateUserManagerRequest{ManagerUserId=manager})).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/admin/team-members/{user}/manager",new UpdateUserManagerRequest{ManagerUserId=null})).StatusCode);f.UserTeamManagementService.Verify(x=>x.UpdateManagerAsync(user,It.IsAny<UpdateUserManagerRequest>(),It.IsAny<CancellationToken>()),Times.Exactly(2));}
}
