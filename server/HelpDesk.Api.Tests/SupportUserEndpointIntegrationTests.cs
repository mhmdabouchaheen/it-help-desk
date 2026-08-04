using System.Net;
using System.Text.Json;
using HelpDesk.Api.Application.Authorization;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class SupportUserEndpointIntegrationTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact] public async Task Anonymous_Is401_WithoutServiceInvocation() { factory.SupportUserDirectoryService.Invocations.Clear(); var r=await factory.CreateClient().GetAsync("/api/support-users"); Assert.Equal(HttpStatusCode.Unauthorized,r.StatusCode); Assert.Empty(factory.SupportUserDirectoryService.Invocations); }
    [Theory] [InlineData(AppRoles.Employee)] [InlineData(AppRoles.Manager)] [InlineData("Unknown")]
    public async Task UnsupportedRole_Is403_WithoutServiceInvocation(string role) { factory.SupportUserDirectoryService.Invocations.Clear(); var r=await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/support-users"); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode); Assert.Empty(factory.SupportUserDirectoryService.Invocations); }
    [Theory] [InlineData(AppRoles.Admin)] [InlineData(AppRoles.ItSupportAgent)]
    public async Task SupportRole_ReturnsSafeJson(string role) { var r=await factory.CreateAuthorizedClient(Guid.NewGuid(),role).GetAsync("/api/support-users"); Assert.Equal(HttpStatusCode.OK,r.StatusCode); var json=JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement[0]; Assert.True(json.TryGetProperty("id",out _)); Assert.True(json.TryGetProperty("displayName",out _)); Assert.True(json.TryGetProperty("roles",out _)); Assert.False(json.TryGetProperty("email",out _)); Assert.False(json.TryGetProperty("passwordHash",out _)); Assert.False(json.TryGetProperty("securityStamp",out _)); }
    [Fact] public async Task MixedEmployeeAndSupportRole_IsAllowed() { var r=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee,AppRoles.ItSupportAgent).GetAsync("/api/support-users"); Assert.Equal(HttpStatusCode.OK,r.StatusCode); }
    [Fact] public async Task RoleHeaderCannotBypassBearerAuthorization() { using var request=new HttpRequestMessage(HttpMethod.Get,"/api/support-users"); request.Headers.Add("X-Role",AppRoles.Admin); var r=await factory.CreateClient().SendAsync(request); Assert.Equal(HttpStatusCode.Unauthorized,r.StatusCode); }
}
