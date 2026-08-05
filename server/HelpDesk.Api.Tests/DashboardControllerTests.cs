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
}
