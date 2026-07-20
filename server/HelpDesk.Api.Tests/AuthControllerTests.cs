using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HelpDesk.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_CallsServiceAndReturns201()
    {
        var fixture = new Fixture();
        var request = new RegisterRequest();

        var result = await fixture.Controller.RegisterAsync(request, default);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Same(fixture.AuthResponse, objectResult.Value);
        fixture.Service.Verify(service => service.RegisterAsync(
            request, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_CallsServiceAndReturns200()
    {
        var fixture = new Fixture();
        var request = new LoginRequest();

        var result = await fixture.Controller.LoginAsync(request, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(fixture.AuthResponse, ok.Value);
        fixture.Service.Verify(service => service.LoginAsync(
            request, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_CallsServiceAndReturns200()
    {
        var fixture = new Fixture();
        var request = new RefreshTokenRequest();

        var result = await fixture.Controller.RefreshAsync(request, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(fixture.AuthResponse, ok.Value);
        fixture.Service.Verify(service => service.RefreshAsync(
            request, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_CallsServiceAndReturns204()
    {
        var fixture = new Fixture();
        var request = new LogoutRequest();

        var result = await fixture.Controller.LogoutAsync(request, default);

        Assert.IsType<NoContentResult>(result);
        fixture.Service.Verify(service => service.LogoutAsync(
            request, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Me_ReadsSubjectAndCallsService()
    {
        var userId = Guid.NewGuid();
        var fixture = new Fixture(userId.ToString());

        var result = await fixture.Controller.GetCurrentUserAsync(default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(fixture.CurrentUserResponse, ok.Value);
        fixture.Service.Verify(service => service.GetCurrentUserAsync(
            userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Me_MissingSubjectIsRejected()
    {
        var fixture = new Fixture(subject: null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.Controller.GetCurrentUserAsync(default));
    }

    [Fact]
    public async Task Me_InvalidSubjectIsRejected()
    {
        var fixture = new Fixture("not-a-guid");

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.Controller.GetCurrentUserAsync(default));
    }

    [Fact]
    public async Task IPv4MappedAddress_IsNormalizedAndForwarded()
    {
        var fixture = new Fixture();
        fixture.Controller.HttpContext.Connection.RemoteIpAddress =
            IPAddress.Parse("::ffff:192.0.2.10");
        var request = new LoginRequest();

        await fixture.Controller.LoginAsync(request, default);

        fixture.Service.Verify(service => service.LoginAsync(
            request, "192.0.2.10", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Controller_PublicActionsExposeOnlyContractResults()
    {
        var actionReturnTypes = typeof(AuthController).GetMethods()
            .Where(method => method.DeclaringType == typeof(AuthController) && method.IsPublic)
            .Select(method => method.ReturnType.ToString());

        Assert.DoesNotContain(actionReturnTypes, type =>
            type.Contains("Entities.User", StringComparison.Ordinal) ||
            type.Contains("Entities.RefreshToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CancellationToken_IsForwarded()
    {
        var fixture = new Fixture();
        using var source = new CancellationTokenSource();
        var request = new RefreshTokenRequest();

        await fixture.Controller.RefreshAsync(request, source.Token);

        fixture.Service.Verify(service => service.RefreshAsync(
            request, It.IsAny<string?>(), source.Token), Times.Once);
    }

    private sealed class Fixture
    {
        public Fixture(string? subject = null)
        {
            Service = new Mock<IAuthenticationService>();
            AuthResponse = new AuthResponse { UserId = Guid.NewGuid() };
            CurrentUserResponse = new CurrentUserResponse { UserId = Guid.NewGuid() };
            Service.Setup(service => service.RegisterAsync(
                    It.IsAny<RegisterRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResponse);
            Service.Setup(service => service.LoginAsync(
                    It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResponse);
            Service.Setup(service => service.RefreshAsync(
                    It.IsAny<RefreshTokenRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResponse);
            Service.Setup(service => service.LogoutAsync(
                    It.IsAny<LogoutRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Service.Setup(service => service.GetCurrentUserAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CurrentUserResponse);

            var claims = subject is null
                ? Array.Empty<Claim>()
                : [new Claim(JwtRegisteredClaimNames.Sub, subject)];
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };
            Controller = new AuthController(Service.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = context }
            };
        }

        public Mock<IAuthenticationService> Service { get; }
        public AuthResponse AuthResponse { get; }
        public CurrentUserResponse CurrentUserResponse { get; }
        public AuthController Controller { get; }
    }
}
