using System.Security.Claims;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Notifications;
using HelpDesk.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class NotificationsControllerTests
{
    [Fact] public async Task List_UsesPrincipalForUserForwardsQueryAndTokenAndReturns200(){var x=Build();var request=new NotificationListRequest{PageNumber=2,PageSize=10,UnreadOnly=true};var response=new PagedResponse<NotificationResponse>();x.Service.Setup(s=>s.GetPagedAsync(x.UserId,request,x.Token)).ReturnsAsync(response);var result=await x.Controller.GetAsync(request,x.Token);Assert.Same(response,Assert.IsType<OkObjectResult>(result.Result).Value);x.Factory.Verify(f=>f.Create(x.Principal),Times.Once);x.Service.VerifyAll();}
    [Fact] public async Task UnreadCount_DelegatesOnceAndReturns200(){var x=Build();var response=new NotificationUnreadCountResponse{UnreadCount=3};x.Service.Setup(s=>s.GetUnreadCountAsync(x.UserId,x.Token)).ReturnsAsync(response);var result=await x.Controller.GetUnreadCountAsync(x.Token);Assert.Same(response,Assert.IsType<OkObjectResult>(result.Result).Value);x.Service.VerifyAll();}
    [Fact] public async Task MarkRead_DelegatesOnceAndReturns204(){var x=Build();var id=Guid.NewGuid();Assert.IsType<NoContentResult>(await x.Controller.MarkAsReadAsync(id,x.Token));x.Service.Verify(s=>s.MarkAsReadAsync(x.UserId,id,x.Token),Times.Once);}
    [Fact] public async Task MarkAll_DelegatesOnceAndReturns204(){var x=Build();Assert.IsType<NoContentResult>(await x.Controller.MarkAllAsReadAsync(x.Token));x.Service.Verify(s=>s.MarkAllAsReadAsync(x.UserId,x.Token),Times.Once);}
    [Fact] public void Shape_HasNoDatabaseUserManagerOrRecipientInput(){var constructor=typeof(NotificationsController).GetConstructors().Single();Assert.Equal([typeof(INotificationService),typeof(ITicketAccessContextFactory)],constructor.GetParameters().Select(x=>x.ParameterType));Assert.All(typeof(NotificationsController).GetMethods().Where(x=>x.DeclaringType==typeof(NotificationsController)),m=>Assert.DoesNotContain(m.GetParameters(),p=>p.Name?.Contains("recipient",StringComparison.OrdinalIgnoreCase)==true));Assert.Equal(4,typeof(NotificationsController).GetMethods().Count(x=>x.GetCustomAttributes(typeof(HttpMethodAttribute),true).Any()));}
    private static Context Build(){var service=new Mock<INotificationService>();var factory=new Mock<ITicketAccessContextFactory>();var principal=new ClaimsPrincipal();var id=Guid.NewGuid();factory.Setup(x=>x.Create(principal)).Returns(new TicketAccessContext{UserId=id,Roles=[AppRoles.Employee]});var controller=new NotificationsController(service.Object,factory.Object){ControllerContext=new(){HttpContext=new DefaultHttpContext{User=principal}}};return new(controller,service,factory,principal,id,new CancellationTokenSource().Token);}
    private sealed record Context(NotificationsController Controller,Mock<INotificationService> Service,Mock<ITicketAccessContextFactory> Factory,ClaimsPrincipal Principal,Guid UserId,CancellationToken Token);
}
