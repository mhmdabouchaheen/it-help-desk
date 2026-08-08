using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using HelpDesk.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HelpDesk.Api.Tests;

public sealed class NotificationHubTests
{
    [Fact] public void Hub_RequiresAuthorization() => Assert.NotNull(typeof(NotificationHub).GetCustomAttribute<AuthorizeAttribute>());
    [Fact] public void Subject_ProducesControlledPrivateGroup() { var id=Guid.NewGuid(); Assert.Equal($"user:{id:D}",NotificationUserGroup.FromPrincipal(Principal(new Claim(JwtRegisteredClaimNames.Sub,id.ToString())))); }
    [Fact] public void NameIdentifierFallback_IsSupported() { var id=Guid.NewGuid(); Assert.Equal($"user:{id:D}",NotificationUserGroup.FromPrincipal(Principal(new Claim(ClaimTypes.NameIdentifier,id.ToString())))); }
    [Theory] [InlineData(null)] [InlineData("not-a-guid")] [InlineData("00000000-0000-0000-0000-000000000000")]
    public void MissingMalformedOrEmptySubject_IsRejected(string? value) { Claim[] claims=value is null?Array.Empty<Claim>():[new Claim(JwtRegisteredClaimNames.Sub,value)]; Assert.ThrowsAny<Exception>((Action)(()=>NotificationUserGroup.FromPrincipal(Principal(claims)))); }
    [Fact] public void EmailAndRole_AreNeverUsedForGrouping() => Assert.ThrowsAny<Exception>((Action)(()=>NotificationUserGroup.FromPrincipal(Principal(new Claim(ClaimTypes.Email,"a@example.test"),new Claim(ClaimTypes.Role,"Admin")))));
    [Fact] public void Hub_HasNoClientCallableMethodsOrDataDependencies() { var methods=typeof(NotificationHub).GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly);Assert.Single(methods);Assert.Equal(nameof(NotificationHub.OnConnectedAsync),methods[0].Name);var constructors=typeof(NotificationHub).GetConstructors();Assert.Single(constructors);Assert.Empty(constructors[0].GetParameters());Assert.True(typeof(Hub).IsAssignableFrom(typeof(NotificationHub))); }
    private static ClaimsPrincipal Principal(params Claim[] claims)=>new(new ClaimsIdentity(claims,"Bearer"));
}
