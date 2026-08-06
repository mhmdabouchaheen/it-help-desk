using System.Security.Claims;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Dashboard;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Dashboard;
using HelpDesk.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class DashboardControllerTests
{
    [Fact]
    public async Task Get_UsesPrincipalAndForwardsCancellationToken()
    {
        var service=new Mock<IDashboardService>();var factory=new Mock<ITicketAccessContextFactory>();
        var access=new TicketAccessContext{UserId=Guid.NewGuid(),Roles=[AppRoles.Employee]};var response=new DashboardResponse();
        factory.Setup(x=>x.Create(It.IsAny<ClaimsPrincipal>())).Returns(access);
        service.Setup(x=>x.GetDashboardAsync(access,It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var controller=new DashboardController(service.Object,factory.Object){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        using var source=new CancellationTokenSource();var result=await controller.GetAsync(source.Token);
        Assert.Same(response,Assert.IsType<OkObjectResult>(result.Result).Value);
        factory.Verify(x=>x.Create(controller.User),Times.Once);service.Verify(x=>x.GetDashboardAsync(access,source.Token),Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnDatabaseOrUserManager()
    {
        var types=typeof(DashboardController).GetConstructors().Single().GetParameters().Select(x=>x.ParameterType).ToArray();
        Assert.Equal([typeof(IDashboardService),typeof(ITicketAccessContextFactory)],types);
    }

    [Fact]
    public void Endpoint_RequiresAuthenticationAndDeclaresSafeResponseTypes()
    {
        var controller = typeof(DashboardController);
        Assert.NotNull(controller.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).SingleOrDefault());
        var method = controller.GetMethod(nameof(DashboardController.GetAsync))!;
        Assert.NotNull(method.GetCustomAttributes(typeof(HttpGetAttribute), true).SingleOrDefault());
        var responseTypes = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true)
            .Cast<ProducesResponseTypeAttribute>().Select(x => x.StatusCode).ToArray();
        Assert.Contains(StatusCodes.Status200OK, responseTypes);
        Assert.Contains(StatusCodes.Status401Unauthorized, responseTypes);
        Assert.Contains(StatusCodes.Status403Forbidden, responseTypes);
    }

    [Fact]
    public async Task Get_DoesNotSwallowAccessValidationFailures()
    {
        var service = new Mock<IDashboardService>();
        var factory = new Mock<ITicketAccessContextFactory>();
        var principal = new ClaimsPrincipal();
        var controller = new DashboardController(service.Object, factory.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } }
        };
        factory.Setup(x => x.Create(principal)).Throws(new InvalidOperationException("invalid identity"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetAsync(default));
        service.VerifyNoOtherCalls();
    }
}
