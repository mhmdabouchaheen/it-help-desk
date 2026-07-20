using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using HelpDesk.Api.Application.Authorization;

namespace HelpDesk.Api.Tests;

public class AuthorizationEndpointIntegrationTests
{
    [Theory]
    [InlineData("authenticated")]
    [InlineData("admin")]
    [InlineData("support")]
    [InlineData("management")]
    public async Task UnauthenticatedRequests_Return401(string action)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().GetAsync(Route(action));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedWithoutRole_CanAccessAuthenticatedEndpoint()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid()).GetAsync(Route("authenticated"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("support")]
    [InlineData("management")]
    public async Task AuthenticatedWithoutRole_CannotAccessRolePolicies(string action)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid()).GetAsync(Route(action));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("support")]
    [InlineData("management")]
    public async Task Admin_CanAccessEveryRolePolicy(string action)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Admin).GetAsync(Route(action));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ItSupportAgent_CanAccessSupport()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.ItSupportAgent).GetAsync(Route("support"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("management")]
    public async Task ItSupportAgent_CannotAccessOtherRestrictedPolicies(string action)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.ItSupportAgent).GetAsync(Route(action));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CanAccessAuthenticatedEndpoint()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.Employee).GetAsync(Route("authenticated"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("support")]
    [InlineData("management")]
    public async Task Employee_CannotAccessRolePolicies(string action)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.Employee).GetAsync(Route(action));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CanAccessManagement()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.Manager).GetAsync(Route("management"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("support")]
    public async Task Manager_CannotAccessOtherRolePolicies(string action)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.Manager).GetAsync(Route(action));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EmployeeAndSupportAgent_CanAccessSupport()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.Employee, AppRoles.ItSupportAgent).GetAsync(Route("support"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EmployeeAndSupportAgent_CannotAccessManagement()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(
            Guid.NewGuid(), AppRoles.Employee, AppRoles.ItSupportAgent).GetAsync(Route("management"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedFailure_UsesAuthenticationRequiredCode()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().GetAsync(Route("authenticated"));
        Assert.Equal("authentication_required", await ProblemCodeAsync(response));
    }

    [Fact]
    public async Task ForbiddenFailure_UsesAccessForbiddenCode()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid()).GetAsync(Route("admin"));
        Assert.Equal("access_forbidden", await ProblemCodeAsync(response));
    }

    [Fact]
    public async Task AuthenticationFailure_DoesNotExposeTokenValidationDetails()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed-token");

        var body = await (await client.GetAsync(Route("authenticated"))).Content.ReadAsStringAsync();
        Assert.DoesNotContain("IDX", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("signature", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizationFailure_ContainsTraceId()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid()).GetAsync(Route("admin"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private static string Route(string action) => $"/api/authorization-probe/{action}";

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("code").GetString();
    }
}
