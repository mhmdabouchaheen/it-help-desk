using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Audit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpDesk.Api.Tests;

public sealed class ActivityLogServiceTests
{
    [Fact] public async Task Write_PersistsSafeMetadataAndUsesTimeProvider(){await using var f=await Fixture.Create();await f.Service.WriteAsync(null,ActivityActions.TicketCreated,ActivityEntityTypes.Ticket,Guid.NewGuid().ToString(),new Dictionary<string,string?>{{"referenceNumber","HD-1"}});var row=await f.Db.ActivityLogs.SingleAsync();Assert.Equal(f.Now,row.OccurredAtUtc);Assert.Contains("HD-1",row.Metadata);Assert.Null(row.ActorUserId);}
    [Theory]
    [InlineData("")][InlineData(" ")]
    public async Task Write_RejectsBlankAction(string action){await using var f=await Fixture.Create();await Assert.ThrowsAsync<ActivityLogValidationException>(()=>f.Service.WriteAsync(null,action,"Ticket","id"));}
    [Fact] public async Task Write_RejectsBlankEntityType(){await using var f=await Fixture.Create();await Assert.ThrowsAsync<ActivityLogValidationException>(()=>f.Service.WriteAsync(null,ActivityActions.TicketCreated," ","id"));}
    [Fact] public async Task Write_RejectsBlankIdentifier(){await using var f=await Fixture.Create();await Assert.ThrowsAsync<ActivityLogValidationException>(()=>f.Service.WriteAsync(null,ActivityActions.TicketCreated,"Ticket"," "));}
    [Theory]
    [InlineData("password")][InlineData("accessToken")][InlineData("refreshToken")][InlineData("tokenHash")][InlineData("body")][InlineData("storageKey")]
    public async Task Write_RejectsSensitiveOrUnapprovedMetadata(string key){await using var f=await Fixture.Create();await Assert.ThrowsAsync<ActivityLogValidationException>(()=>f.Service.WriteAsync(null,ActivityActions.TicketCreated,"Ticket","id",new Dictionary<string,string?>{{key,"secret"}}));}
    [Fact] public async Task Query_FiltersPagesAndMapsDisplayNameWithoutEmail(){await using var f=await Fixture.Create();var actor=new User{Id=Guid.NewGuid(),UserName="hidden@example.test",Email="hidden@example.test",DisplayName="Support User",CreatedAtUtc=f.Now,UpdatedAtUtc=f.Now};f.Db.Users.Add(actor);await f.Db.SaveChangesAsync();await f.Service.WriteAsync(actor.Id,ActivityActions.TicketCreated,ActivityEntityTypes.Ticket,"one");f.Time.Advance();await f.Service.WriteAsync(actor.Id,ActivityActions.TicketUpdated,ActivityEntityTypes.Ticket,"one",new Dictionary<string,string?>{{"changedFields","title"}});var page=await f.Service.GetPagedAsync(new ActivityLogListRequest{Action=ActivityActions.TicketUpdated,ActorUserId=actor.Id,PageSize=1});var item=Assert.Single(page.Items);Assert.Equal("Support User",item.ActorDisplayName);Assert.Equal(ActivityActions.TicketUpdated,item.Action);Assert.DoesNotContain("example",System.Text.Json.JsonSerializer.Serialize(item),StringComparison.OrdinalIgnoreCase);}
    [Fact] public async Task TicketQuery_IsNewestFirstAndExcludesOtherTickets(){await using var f=await Fixture.Create();var wanted=Guid.NewGuid();await f.Service.WriteAsync(null,ActivityActions.TicketCreated,ActivityEntityTypes.Ticket,wanted.ToString());f.Time.Advance();await f.Service.WriteAsync(null,ActivityActions.TicketCancelled,ActivityEntityTypes.Ticket,wanted.ToString());await f.Service.WriteAsync(null,ActivityActions.TicketCreated,ActivityEntityTypes.Ticket,Guid.NewGuid().ToString());var items=await f.Service.GetForTicketAsync(wanted);Assert.Equal(2,items.Count);Assert.Equal(ActivityActions.TicketCancelled,items[0].Action);}
    [Fact] public async Task Query_RejectsReversedDateRange(){await using var f=await Fixture.Create();await Assert.ThrowsAsync<ActivityLogValidationException>(()=>f.Service.GetPagedAsync(new(){FromUtc=f.Now.AddDays(1),ToUtc=f.Now}));}

    private sealed class MutableTime(DateTime now):TimeProvider{public DateTime Value=now;public override DateTimeOffset GetUtcNow()=>new(Value,TimeSpan.Zero);public void Advance()=>Value=Value.AddMinutes(1);}
    private sealed class Fixture:IAsyncDisposable
    {private Fixture(SqliteConnection connection,ApplicationDbContext db,MutableTime time){Connection=connection;Db=db;Time=time;Service=new(db,time,NullLogger<ActivityLogService>.Instance);}public SqliteConnection Connection{get;}public ApplicationDbContext Db{get;}public MutableTime Time{get;}public DateTime Now=>Time.Value;public ActivityLogService Service{get;}public static async Task<Fixture>Create(){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await TicketSqliteDatabase.InitializeAsync(c);await using(var command=c.CreateCommand()){command.CommandText="CREATE TABLE \"ActivityLogs\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"ActorUserId\" TEXT, \"Action\" TEXT NOT NULL, \"EntityType\" TEXT NOT NULL, \"EntityIdentifier\" TEXT NOT NULL, \"OccurredAtUtc\" TEXT NOT NULL, \"Metadata\" TEXT);";await command.ExecuteNonQueryAsync();}var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);return new(c,db,new MutableTime(new DateTime(2026,8,17,12,0,0,DateTimeKind.Utc)));}public async ValueTask DisposeAsync(){await Db.DisposeAsync();await Connection.DisposeAsync();}}
}
