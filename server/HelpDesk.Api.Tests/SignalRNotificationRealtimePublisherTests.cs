using HelpDesk.Api.Contracts.Notifications;
using HelpDesk.Api.Hubs;
using HelpDesk.Api.Infrastructure.Notifications;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class SignalRNotificationRealtimePublisherTests
{
    [Fact]
    public async Task Publish_SendsSafeContractToExactlyRecipientGroup()
    {
        var id=Guid.NewGuid();var proxy=new Mock<IClientProxy>();var clients=new Mock<IHubClients>();
        clients.Setup(x=>x.Group($"user:{id:D}")).Returns(proxy.Object);
        var context=new Mock<IHubContext<NotificationHub>>();context.SetupGet(x=>x.Clients).Returns(clients.Object);
        var payload=new NotificationRealtimeEvent{NotificationId=Guid.NewGuid(),TicketId=Guid.NewGuid(),Type="TicketAssigned",CreatedAtUtc=DateTime.UtcNow};
        await new SignalRNotificationRealtimePublisher(context.Object).PublishCreatedAsync(id,payload);
        clients.Verify(x=>x.Group($"user:{id:D}"),Times.Once);clients.Verify(x=>x.All,Times.Never);
        proxy.Verify(x=>x.SendCoreAsync(NotificationHubEvents.NotificationCreated,It.Is<object?[]>(a=>a.Length==1&&ReferenceEquals(a[0],payload)),It.IsAny<CancellationToken>()),Times.Once);
        Assert.DoesNotContain(payload.GetType().GetProperties(),p=>p.Name.Contains("Recipient")||p.Name.Contains("Message")||p.Name.Contains("Email")||p.Name.Contains("Token"));
    }
    [Fact] public async Task EmptyRecipient_IsRejected()=>await Assert.ThrowsAsync<ArgumentException>(()=>new SignalRNotificationRealtimePublisher(Mock.Of<IHubContext<NotificationHub>>()).PublishCreatedAsync(Guid.Empty,new(){NotificationId=Guid.NewGuid(),Type="T",CreatedAtUtc=DateTime.UtcNow}));
    [Fact] public async Task SendFailure_Propagates(){var id=Guid.NewGuid();var proxy=new Mock<IClientProxy>();proxy.Setup(x=>x.SendCoreAsync(It.IsAny<string>(),It.IsAny<object?[]>(),It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());var clients=new Mock<IHubClients>();clients.Setup(x=>x.Group(It.IsAny<string>())).Returns(proxy.Object);var context=new Mock<IHubContext<NotificationHub>>();context.SetupGet(x=>x.Clients).Returns(clients.Object);await Assert.ThrowsAsync<InvalidOperationException>(()=>new SignalRNotificationRealtimePublisher(context.Object).PublishCreatedAsync(id,new(){NotificationId=Guid.NewGuid(),Type="T",CreatedAtUtc=DateTime.UtcNow}));}
}
