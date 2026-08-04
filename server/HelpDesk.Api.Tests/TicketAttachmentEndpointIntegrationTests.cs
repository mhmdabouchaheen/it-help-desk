using System.Net;
using HelpDesk.Api.Application.Authorization;

namespace HelpDesk.Api.Tests;

public sealed class TicketAttachmentEndpointIntegrationTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Theory] [InlineData("POST")] [InlineData("GET")] [InlineData("DELETE")]
    public async Task AnonymousEndpointsRequireBearer(string method)
    { var id=Guid.NewGuid();using var request=new HttpRequestMessage(new HttpMethod(method),$"/api/tickets/{AuthApiFactory.TicketId}/attachments/{(method=="POST"?string.Empty:id)}");if(method=="POST")request.Content=new MultipartFormDataContent();var response=await factory.CreateClient().SendAsync(request);Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode); }
    [Fact] public async Task AuthenticatedUploadReturnsSafeCreatedMetadata()
    { using var form=new MultipartFormDataContent();form.Add(new ByteArrayContent("safe"u8.ToArray()),"file","safe.txt");var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee).PostAsync($"/api/tickets/{AuthApiFactory.TicketId}/attachments",form);Assert.Equal(HttpStatusCode.Created,response.StatusCode);var json=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("storageKey",json);Assert.DoesNotContain("contentHash",json); }
    [Fact] public async Task AuthenticatedDownloadIsPrivateAndStreamsBytes()
    { var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee).GetAsync($"/api/tickets/{AuthApiFactory.TicketId}/attachments/{Guid.NewGuid()}");Assert.True(response.Headers.CacheControl!.Private);Assert.True(response.Headers.CacheControl.NoStore);Assert.Equal("safe",await response.Content.ReadAsStringAsync()); }
    [Fact] public async Task AuthenticatedDeleteReturnsNoContent()
    { var response=await factory.CreateAuthorizedClient(Guid.NewGuid(),AppRoles.Employee).DeleteAsync($"/api/tickets/{AuthApiFactory.TicketId}/attachments/{Guid.NewGuid()}");Assert.Equal(HttpStatusCode.NoContent,response.StatusCode); }
}
