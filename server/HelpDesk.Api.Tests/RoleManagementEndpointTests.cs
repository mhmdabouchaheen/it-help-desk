using System.Net;
using System.Net.Http.Json;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class RoleManagementEndpointTests
{
    [Fact]
    public async Task AdminCanListAndUpdateUsingTrustedActor()
    {
        await using var factory=new AuthApiFactory();var actor=Guid.NewGuid();var target=Guid.NewGuid();
        factory.UserRoleManagementService.Setup(x=>x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new RoleManagedUserResponse[]{new(){UserId=target,DisplayName="Employee",Email="safe@test",Roles=[AppRoles.Employee]}});
        factory.UserRoleManagementService.Setup(x=>x.UpdateRolesAsync(target,It.IsAny<UpdateUserRolesRequest>(),actor,It.IsAny<string?>(),It.IsAny<CancellationToken>())).ReturnsAsync(new RoleManagedUserResponse{UserId=target,DisplayName="Employee",Email="safe@test",Roles=[AppRoles.Employee,AppRoles.Manager]});
        var client=factory.CreateAuthorizedClient(actor,AppRoles.Admin);
        var listed=await client.GetFromJsonAsync<RoleManagedUserResponse[]>("/api/admin/role-management");
        Assert.Single(listed!);Assert.DoesNotContain("password",await client.GetStringAsync("/api/admin/role-management"),StringComparison.OrdinalIgnoreCase);
        var response=await client.PutAsJsonAsync($"/api/admin/role-management/{target}/roles",new UpdateUserRolesRequest{Roles=[AppRoles.Employee,AppRoles.Manager]});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        factory.UserRoleManagementService.Verify(x=>x.UpdateRolesAsync(target,It.Is<UpdateUserRolesRequest>(r=>r.Roles!.Contains(AppRoles.Manager)),actor,It.IsAny<string?>(),It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData(AppRoles.Employee)] [InlineData(AppRoles.Manager)] [InlineData(AppRoles.ItSupportAgent)]
    public async Task NonAdminIsForbidden(string role)
    {
        await using var factory=new AuthApiFactory();
        Assert.Equal(HttpStatusCode.Forbidden,(await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/admin/role-management")).StatusCode);
    }
}
