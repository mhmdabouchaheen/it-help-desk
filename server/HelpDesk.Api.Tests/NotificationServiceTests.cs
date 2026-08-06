using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Notifications;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task Create_ValidatesTrimsUsesClockAndDoesNotLogMessage()
    {
        await using var f=await Fixture.Create();var logger=new Mock<ILogger<NotificationService>>();var service=f.Service(logger.Object);
        await service.CreateAsync(f.User1,null," TicketAssigned "," Assigned "," secret body ");
        var item=await f.Db.Notifications.SingleAsync();Assert.Equal(f.User1,item.RecipientUserId);Assert.Null(item.TicketId);
        Assert.Equal("TicketAssigned",item.Type);Assert.Equal("Assigned",item.Title);Assert.Equal("secret body",item.Message);Assert.Equal(f.Now,item.CreatedAtUtc);
        Assert.DoesNotContain(logger.Invocations.SelectMany(x=>x.Arguments).Select(x=>x?.ToString()),x=>x?.Contains("secret body")==true);
    }
    [Theory]
    [InlineData("recipient")][InlineData("type")][InlineData("title")][InlineData("message")]
    public async Task Create_RejectsInvalidRequiredValues(string invalid)
    {await using var f=await Fixture.Create();var recipient=invalid=="recipient"?Guid.Empty:f.User1;var type=invalid=="type"?" ":"Type";var title=invalid=="title"?" ":"Title";var message=invalid=="message"?" ":"Message";await Assert.ThrowsAsync<NotificationValidationException>(()=>f.Service().CreateAsync(recipient,null,type,title,message));}
    [Fact]
    public async Task List_IsRecipientSafeExcludesExpiredFiltersOrdersAndMaps()
    {
        await using var f=await Fixture.Create();var tieA=Guid.Parse("10000000-0000-0000-0000-000000000001");var tieB=Guid.Parse("20000000-0000-0000-0000-000000000002");
        f.Add(tieA,f.User1,f.Now.AddMinutes(-1),null,null);f.Add(tieB,f.User1,f.Now.AddMinutes(-1),f.Now.AddSeconds(-1),Guid.NewGuid());f.Add(Guid.NewGuid(),f.User2,f.Now,null,null);f.Add(Guid.NewGuid(),f.User1,f.Now.AddHours(-2),null,null,f.Now);await f.Db.SaveChangesAsync();
        var all=await f.Service().GetPagedAsync(f.User1,new(){PageSize=10});Assert.Equal(2,all.TotalCount);Assert.Equal(tieB,all.Items[0].Id);Assert.True(all.Items[0].IsRead);Assert.NotNull(all.Items[0].TicketId);Assert.False(all.Items[1].IsRead);Assert.Null(all.Items[1].TicketId);
        var unread=await f.Service().GetPagedAsync(f.User1,new(){UnreadOnly=true});Assert.Single(unread.Items);Assert.Equal(tieA,unread.Items[0].Id);
        Assert.All(all.Items,x=>Assert.DoesNotContain("Recipient",x.GetType().GetProperties().Select(p=>p.Name)));
    }
    [Fact]
    public async Task List_PaginationAndEmptyMetadataAreCorrect()
    {await using var f=await Fixture.Create();for(var i=0;i<3;i++)f.Add(Guid.NewGuid(),f.User1,f.Now.AddMinutes(-i),null,null);await f.Db.SaveChangesAsync();var page=await f.Service().GetPagedAsync(f.User1,new(){PageNumber=2,PageSize=2});Assert.Single(page.Items);Assert.Equal(3,page.TotalCount);Assert.Equal(2,page.TotalPages);Assert.True(page.HasPreviousPage);Assert.False(page.HasNextPage);var empty=await f.Service().GetPagedAsync(Guid.NewGuid(),new());Assert.Empty(empty.Items);Assert.Equal(0,empty.TotalPages);}
    [Fact]
    public async Task UnreadCount_IsRecipientUnreadAndExpiryScoped()
    {await using var f=await Fixture.Create();f.Add(Guid.NewGuid(),f.User1,f.Now,null,null);f.Add(Guid.NewGuid(),f.User1,f.Now,f.Now,null);f.Add(Guid.NewGuid(),f.User1,f.Now,null,null,f.Now);f.Add(Guid.NewGuid(),f.User2,f.Now,null,null);await f.Db.SaveChangesAsync();Assert.Equal(1,(await f.Service().GetUnreadCountAsync(f.User1)).UnreadCount);Assert.Equal(0,(await f.Service().GetUnreadCountAsync(Guid.NewGuid())).UnreadCount);}
    [Fact]
    public async Task MarkRead_IsOwnedClockedIdempotentAndDoesNotOverwrite()
    {await using var f=await Fixture.Create();var id=Guid.NewGuid();f.Add(id,f.User1,f.Now.AddDays(-1),null,null);await f.Db.SaveChangesAsync();var service=f.Service();await service.MarkAsReadAsync(f.User1,id);Assert.Equal(f.Now,(await f.Db.Notifications.FindAsync(id))!.ReadAtUtc);await service.MarkAsReadAsync(f.User1,id);Assert.Equal(f.Now,(await f.Db.Notifications.FindAsync(id))!.ReadAtUtc);await Assert.ThrowsAsync<NotificationNotFoundException>(()=>service.MarkAsReadAsync(f.User2,id));await Assert.ThrowsAsync<NotificationNotFoundException>(()=>service.MarkAsReadAsync(f.User1,Guid.NewGuid()));}
    [Fact]
    public async Task MarkAll_OnlyChangesCurrentUserUnreadAndIsIdempotent()
    {await using var f=await Fixture.Create();var unread=Guid.NewGuid();var read=Guid.NewGuid();var other=Guid.NewGuid();var old=f.Now.AddDays(-1);f.Add(unread,f.User1,old,null,null);f.Add(read,f.User1,old,old,null);f.Add(other,f.User2,old,null,null);await f.Db.SaveChangesAsync();var service=f.Service();await service.MarkAllAsReadAsync(f.User1);await service.MarkAllAsReadAsync(f.User1);Assert.Equal(f.Now,(await f.Db.Notifications.FindAsync(unread))!.ReadAtUtc);Assert.Equal(old,(await f.Db.Notifications.FindAsync(read))!.ReadAtUtc);Assert.Null((await f.Db.Notifications.FindAsync(other))!.ReadAtUtc);}

    private sealed class FixedTime(DateTimeOffset now):TimeProvider{public override DateTimeOffset GetUtcNow()=>now;}
    private sealed class Fixture(SqliteConnection connection,ApplicationDbContext db):IAsyncDisposable
    {public Guid User1{get;}=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");public Guid User2{get;}=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");public DateTime Now{get;}=new(2026,8,6,12,0,0,DateTimeKind.Utc);public ApplicationDbContext Db=>db;public NotificationService Service(ILogger<NotificationService>? logger=null)=>new(db,new FixedTime(new DateTimeOffset(Now)),logger??Mock.Of<ILogger<NotificationService>>());public void Add(Guid id,Guid recipient,DateTime created,DateTime? read,Guid? ticket,DateTime? expires=null)=>db.Notifications.Add(new(){Id=id,RecipientUserId=recipient,TicketId=ticket,Type="Type",Title="Title",Message="Message",CreatedAtUtc=created,ReadAtUtc=read,ExpiresAtUtc=expires});public static async Task<Fixture>Create(){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await TicketSqliteDatabase.InitializeAsync(c);var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);return new(c,db);}public async ValueTask DisposeAsync(){await db.DisposeAsync();await connection.DisposeAsync();}}
}
