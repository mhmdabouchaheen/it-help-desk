using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Infrastructure.Authorization;

namespace HelpDesk.Api.Tests;

public class TicketAccessContextFactoryTests
{
    private readonly TicketAccessContextFactory _factory = new();

    [Fact]
    public void Create_MapsSubjectAndExactDistinctRoles()
    {
        var id = Guid.NewGuid();
        var principal = Principal(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
            new Claim(ClaimTypes.Role, "Employee"), new Claim(ClaimTypes.Role, "Employee"),
            new Claim(ClaimTypes.Role, "unknown-role"));
        var result = _factory.Create(principal);
        Assert.Equal(id, result.UserId);
        Assert.Equal(["Employee", "unknown-role"], result.Roles);
    }

    [Fact]
    public void Create_SupportsMappedNameIdentifierWithoutEmailOrName()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, _factory.Create(Principal(new Claim(ClaimTypes.NameIdentifier, id.ToString()))).UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_RejectsMissingMalformedOrEmptySubject(string? subject)
    {
        var claims = subject is null ? Array.Empty<Claim>() : [new Claim(JwtRegisteredClaimNames.Sub, subject)];
        Assert.Throws<InvalidAuthenticatedPrincipalException>(() => _factory.Create(Principal(claims)));
    }

    [Fact]
    public void Create_RejectsUnauthenticatedIdentity()
    {
        Assert.Throws<InvalidAuthenticatedPrincipalException>(() =>
            _factory.Create(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void Create_DoesNotMutatePrincipal()
    {
        var principal = Principal(new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()));
        var before = principal.Claims.ToArray();
        _factory.Create(principal);
        Assert.Equal(before, principal.Claims);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));
}
