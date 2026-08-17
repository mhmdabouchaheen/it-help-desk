using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Reports;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Tests;

public sealed class ReportServiceTests
{
    [Fact]public async Task MetricsBreakdownsTrendAndWorkload_UseRealCurrentData(){await using var f=await Fixture.Create();var r=await f.Service.GetTicketReportAsync(new());Assert.Equal(3,r.Summary.TotalTickets);Assert.Equal(2,r.Summary.OpenTickets);Assert.Equal(1,r.Summary.TerminalTickets);Assert.Equal(1,r.Summary.CancelledTickets);Assert.Equal(2,r.Summary.AssignedTickets);Assert.Equal(1,r.Summary.UnassignedTickets);Assert.Equal(2,r.StatusBreakdown.Single(x=>x.Name=="Open").Count);Assert.Equal(1,r.PriorityBreakdown.Single(x=>x.Name=="Medium").Count);Assert.Equal(2,r.CategoryBreakdown.Single(x=>x.Name=="Hardware").Count);Assert.Equal(2,r.Trend.Sum(x=>x.CreatedCount));Assert.Equal(1,r.Trend.Sum(x=>x.ClosedCount));Assert.Equal(1,r.AgentWorkload.Single(x=>x.UserId==f.AgentId).ActiveTicketCount);}
    [Fact]public async Task LookupDateAndAgentFilters_AreCombined(){await using var f=await Fixture.Create();var r=await f.Service.GetTicketReportAsync(new(){FromUtc=new DateTime(2026,8,2,0,0,0,DateTimeKind.Utc),ToUtc=new DateTime(2026,8,4,23,59,59,DateTimeKind.Utc),CategoryId=1,PriorityId=1,StatusId=1,AssignedToUserId=f.AgentId});Assert.Equal(1,r.Summary.TotalTickets);Assert.Equal(1,r.Summary.AssignedTickets);Assert.Equal(1,r.CategoryBreakdown.Single(x=>x.Id==1).Count);}
    [Fact]public async Task ZeroResult_ReturnsZerosWithoutFabrication(){await using var f=await Fixture.Create();var r=await f.Service.GetTicketReportAsync(new(){FromUtc=new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc),ToUtc=new DateTime(2025,1,2,0,0,0,DateTimeKind.Utc)});Assert.Equal(0,r.Summary.TotalTickets);Assert.All(r.StatusBreakdown,x=>Assert.Equal(0,x.Count));Assert.All(r.AgentWorkload,x=>Assert.Equal(0,x.ActiveTicketCount));Assert.All(r.Trend,x=>{Assert.Equal(0,x.CreatedCount);Assert.Equal(0,x.ClosedCount);});}
    [Fact]public async Task AgentProjection_IsSafeAndInvalidLookupIsRejected(){await using var f=await Fixture.Create();var r=await f.Service.GetTicketReportAsync(new());Assert.Equal(["UserId","DisplayName","ActiveTicketCount"],typeof(AgentWorkloadResponse).GetProperties().Select(x=>x.Name));Assert.DoesNotContain("agent@example.test",System.Text.Json.JsonSerializer.Serialize(r));await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TicketValidationException>(()=>f.Service.GetTicketReportAsync(new(){CategoryId=99}));}

    private sealed class FixedTime:TimeProvider{public override DateTimeOffset GetUtcNow()=>new(2026,8,5,12,0,0,TimeSpan.Zero);}
    private sealed class Fixture(SqliteConnection connection,ApplicationDbContext db,ReportService service):IAsyncDisposable
    {public Guid AgentId{get;}=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");public ReportService Service=>service;
        public static async Task<Fixture>Create(){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await TicketSqliteDatabase.InitializeAsync(c);var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);var f=new Fixture(c,db,new ReportService(db,new FixedTime()));var now=new DateTime(2026,8,5,12,0,0,DateTimeKind.Utc);db.Users.Add(new User{Id=f.AgentId,UserName="agent",Email="agent@example.test",DisplayName="Agent Safe",CreatedAtUtc=now,UpdatedAtUtc=now});db.UserRoles.Add(new UserRole{UserId=f.AgentId,RoleId=Guid.Parse("22222222-2222-2222-2222-222222222222"),AssignedAtUtc=now});var first=Ticket("1",1,1,1,new DateTime(2026,8,1,0,0,0,DateTimeKind.Utc),null,null);first.CancelledAtUtc=now;db.Tickets.AddRange(first,Ticket("2",1,1,1,new DateTime(2026,8,3,0,0,0,DateTimeKind.Utc),f.AgentId,null),Ticket("3",2,2,5,new DateTime(2026,7,1,0,0,0,DateTimeKind.Utc),f.AgentId,now));await db.SaveChangesAsync();return f;}
        private static Ticket Ticket(string suffix,short category,short priority,short status,DateTime created,Guid? assigned,DateTime? closed)=>new(){Id=Guid.Parse($"00000000-0000-0000-0000-00000000000{suffix}"),ReferenceNumber="R-"+suffix,Title="T",Description="D",CategoryId=category,PriorityId=priority,StatusId=status,CreatedByUserId=Guid.NewGuid(),AssignedToUserId=assigned,CreatedAtUtc=created,UpdatedAtUtc=created,ClosedAtUtc=closed};
        public async ValueTask DisposeAsync(){await db.DisposeAsync();await connection.DisposeAsync();}}
}
