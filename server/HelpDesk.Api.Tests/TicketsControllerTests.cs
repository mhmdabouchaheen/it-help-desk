using System.Security.Claims;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Controllers;
using HelpDesk.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HelpDesk.Api.Tests;

public class TicketsControllerTests
{
    [Fact]
    public async Task Create_DelegatesOnceAndReturnsCreatedAtAction()
    {
        var (controller, service, factory, principal, access) = TicketController();
        var request = new CreateTicketRequest { Title = "Title", Description = "Description", CategoryId = 1, PriorityId = 1 };
        var token = new CancellationTokenSource().Token;
        service.Setup(x => x.CreateAsync(request, access, token)).ReturnsAsync(Detail());
        var result = await controller.CreateAsync(request, token);
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(AuthApiFactory.TicketId, created.RouteValues!["ticketId"]);
        service.Verify(x => x.CreateAsync(request, access, token), Times.Once);
        factory.Verify(x => x.Create(principal), Times.Once);
    }

    [Fact]
    public async Task List_ForwardsSameRequestAndCancellationToken()
    {
        var (controller, service, _, _, access) = TicketController();
        var request = new TicketListRequest { Search = "exact" };
        var token = new CancellationTokenSource().Token;
        service.Setup(x => x.GetPagedAsync(request, access, token))
            .ReturnsAsync(new PagedResponse<TicketSummaryResponse>());
        Assert.IsType<OkObjectResult>((await controller.GetPagedAsync(request, token)).Result);
        service.Verify(x => x.GetPagedAsync(request, access, token), Times.Once);
    }

    [Fact]
    public async Task Comment_ForwardsRouteAndRequestAndReturnsStableCreatedLocation()
    {
        var (controller, service, _, _, access) = TicketController();
        var request = new AddTicketCommentRequest { Content = "hello" };
        service.Setup(x => x.AddCommentAsync(AuthApiFactory.TicketId, request, access, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketCommentResponse { Id = AuthApiFactory.CommentId });
        var created = Assert.IsType<CreatedResult>((await controller.AddCommentAsync(
            AuthApiFactory.TicketId, request, default)).Result);
        Assert.Equal($"/api/tickets/{AuthApiFactory.TicketId}/comments/{AuthApiFactory.CommentId}", created.Location);
    }

    [Fact]
    public async Task Cancel_DelegatesOnce_ForwardsContextAndReturnsOk()
    { var(controller,service,factory,principal,access)=TicketController();var request=new CancelTicketRequest{Reason="reason"};var token=new CancellationTokenSource().Token;service.Setup(x=>x.CancelAsync(AuthApiFactory.TicketId,request,access,token)).ReturnsAsync(Detail());var result=await controller.CancelAsync(AuthApiFactory.TicketId,request,token);Assert.IsType<OkObjectResult>(result.Result);service.Verify(x=>x.CancelAsync(AuthApiFactory.TicketId,request,access,token),Times.Once);factory.Verify(x=>x.Create(principal),Times.Once); }

    [Fact]
    public async Task LookupController_DelegatesEachOperation()
    {
        var service = new Mock<ITicketLookupService>();
        service.Setup(x => x.GetCategoriesAsync(default)).ReturnsAsync(Array.Empty<TicketCategoryResponse>());
        service.Setup(x => x.GetPrioritiesAsync(default)).ReturnsAsync(Array.Empty<TicketPriorityResponse>());
        service.Setup(x => x.GetStatusesAsync(default)).ReturnsAsync(Array.Empty<TicketStatusResponse>());
        var controller = new TicketLookupsController(service.Object);
        Assert.IsType<OkObjectResult>((await controller.GetCategoriesAsync(default)).Result);
        Assert.IsType<OkObjectResult>((await controller.GetPrioritiesAsync(default)).Result);
        Assert.IsType<OkObjectResult>((await controller.GetStatusesAsync(default)).Result);
        service.VerifyAll();
    }

    [Fact]
    public void Controllers_HaveNoDataOrIdentityDependencies()
    {
        var parameters = typeof(TicketsController).GetConstructors().Single().GetParameters()
            .Concat(typeof(TicketLookupsController).GetConstructors().Single().GetParameters()).ToArray();
        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(ApplicationDbContext));
        Assert.DoesNotContain(parameters, p => p.ParameterType.Name.StartsWith("UserManager", StringComparison.Ordinal));
        Assert.All(typeof(TicketsController).GetMethods().Where(m => m.DeclaringType == typeof(TicketsController)), method =>
        {
            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(TicketAccessContext));
            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(ClaimsPrincipal));
        });
    }

    private static (TicketsController Controller, Mock<ITicketService> Service,
        Mock<ITicketAccessContextFactory> Factory, ClaimsPrincipal Principal, TicketAccessContext Access) TicketController()
    {
        var service = new Mock<ITicketService>();
        var factory = new Mock<ITicketAccessContextFactory>();
        var access = new TicketAccessContext { UserId = Guid.NewGuid(), Roles = ["Employee"] };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, access.UserId.ToString())], "test"));
        factory.Setup(x => x.Create(principal)).Returns(access);
        var controller = new TicketsController(service.Object, factory.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } }
        };
        return (controller, service, factory, principal, access);
    }

    private static TicketDetailResponse Detail() => new() { Id = AuthApiFactory.TicketId };
}
