using System.Net;

namespace HelpDesk.Api.Tests;

public sealed class ProductionReadinessEndpointTests
{
    [Fact]public async Task Health_IsAnonymousAndMinimal(){await using var factory=new AuthApiFactory();var response=await factory.CreateClient().GetAsync("/healthz");Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal("Healthy",await response.Content.ReadAsStringAsync());}
    [Fact]public async Task Responses_IncludeDefensiveHeaders(){await using var factory=new AuthApiFactory();var response=await factory.CreateClient().GetAsync("/healthz");Assert.Equal("nosniff",response.Headers.GetValues("X-Content-Type-Options").Single());Assert.Equal("DENY",response.Headers.GetValues("X-Frame-Options").Single());Assert.Equal("no-referrer",response.Headers.GetValues("Referrer-Policy").Single());Assert.Contains("camera=()",response.Headers.GetValues("Permissions-Policy").Single());}
}
