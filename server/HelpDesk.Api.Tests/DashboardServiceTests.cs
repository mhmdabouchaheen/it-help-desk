using System.Security.Claims;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Dashboard;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Dashboard;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpDesk.Api.Tests;

public sealed class DashboardServiceTests
{
    [Theory]
    [InlineData(AppRoles.Admin,2)][InlineData(AppRoles.ItSupportAgent,2)]
    public async Task SupportRoles_SeeAllTickets(string role,int expected){await using var f=await Fixture.Create();Assert.Equal(expected,(await f.Service.GetDashboardAsync(f.Access(role))).Summary.TotalTickets);}

    [Theory]
    [InlineData(AppRoles.Employee)][InlineData(AppRoles.Manager)]
    public async Task NonSupportRoles_SeeOnlyOwnedTickets(string role){await using var f=await Fixture.Create();var result=await f.Service.GetDashboardAsync(f.Access(role));Assert.Equal(1,result.Summary.TotalTickets);Assert.Single(result.RecentTickets);Assert.Equal(f.OwnedId,result.RecentTickets[0].Id);}

    [Theory]
    [InlineData(AppRoles.Employee,AppRoles.ItSupportAgent)][InlineData(AppRoles.Manager,AppRoles.ItSupportAgent)]
    public async Task MultiRoleSupport_SeesAll(string first,string second){await using var f=await Fixture.Create();Assert.Equal(2,(await f.Service.GetDashboardAsync(f.Access(first,second))).Summary.TotalTickets);}

    [Fact] public async Task EmployeeAndManager_RemainsOwnerScoped(){await using var f=await Fixture.Create();Assert.Equal(1,(await f.Service.GetDashboardAsync(f.Access(AppRoles.Employee,AppRoles.Manager))).Summary.TotalTickets);}

    [Theory]
    [InlineData("Unknown",false)][InlineData(null,false)][InlineData(AppRoles.Employee,true)]
    public async Task InvalidAccess_IsRejected(string? role,bool emptyUser){await using var f=await Fixture.Create();var access=new TicketAccessContext{UserId=emptyUser?Guid.Empty:f.OwnerId,Roles=role is null?[]:[role]};await Assert.ThrowsAsync<TicketAccessDeniedException>(()=>f.Service.GetDashboardAsync(access));}

    [Fact]
    public async Task Summary_UsesActualFieldsAndUtcMonthBoundary()
    {
        await using var f=await Fixture.Create();var s=(await f.Service.GetDashboardAsync(f.Access(AppRoles.Admin))).Summary;
        Assert.Equal(2,s.TotalTickets);Assert.Equal(1,s.OpenTickets);Assert.Equal(1,s.ClosedTickets);Assert.Equal(0,s.InProgressTickets);Assert.Equal(0,s.PendingTickets);Assert.Equal(0,s.ResolvedTickets);
        Assert.Equal(1,s.CancelledTickets);Assert.Equal(1,s.AssignedTickets);Assert.Equal(1,s.UnassignedTickets);Assert.Equal(1,s.CriticalTickets);Assert.Equal(1,s.CreatedThisMonth);Assert.Equal(1,s.ClosedThisMonth);
    }

    [Fact]
    public async Task CancelledOpen_RemainsInActualStatusAndSeparateCancellationCount()
    {await using var f=await Fixture.Create();var r=await f.Service.GetDashboardAsync(f.Access(AppRoles.Admin));Assert.Equal(1,r.Summary.OpenTickets);Assert.Equal(1,r.Summary.CancelledTickets);Assert.DoesNotContain(r.StatusBreakdown,x=>x.Name=="Cancelled");}

    [Fact]
    public async Task Breakdowns_IncludeActiveZeroValuesAndPreserveLookupOrdering()
    {
        await using var f=await Fixture.Create();var r=await f.Service.GetDashboardAsync(f.Access(AppRoles.Admin));
        Assert.Equal(["Open","In Progress","Pending","Resolved","Closed"],r.StatusBreakdown.Select(x=>x.Name));Assert.Equal([1,0,0,0,1],r.StatusBreakdown.Select(x=>x.Count));Assert.DoesNotContain(r.StatusBreakdown,x=>x.Id==9);
        Assert.Equal(["Low","Medium","Critical"],r.PriorityBreakdown.Select(x=>x.Name));Assert.Equal([0,1,1],r.PriorityBreakdown.Select(x=>x.Count));
        Assert.Equal(["Hardware","Software","Network Operations With A Long Name"],r.CategoryBreakdown.Select(x=>x.Name));Assert.Equal([1,0,1],r.CategoryBreakdown.Select(x=>x.Count));
        Assert.Equal([1,2,7],r.CategoryBreakdown.Select(x=>x.Id));
    }

    [Fact]
    public async Task BreakdownCounts_RespectOwnership()
    {await using var f=await Fixture.Create();var r=await f.Service.GetDashboardAsync(f.Access(AppRoles.Employee));Assert.Equal(1,r.StatusBreakdown.Single(x=>x.Name=="Open").Count);Assert.Equal(0,r.StatusBreakdown.Single(x=>x.Name=="Closed").Count);Assert.Equal(1,r.PriorityBreakdown.Single(x=>x.Name=="Critical").Count);Assert.Equal(1,r.CategoryBreakdown.Single(x=>x.Id==7).Count);}

    [Fact]
    public async Task Trend_HasSixUtcChronologicalPointsWithSeparateCountsAndBoundaries()
    {
        await using var f=await Fixture.Create();var p=(await f.Service.GetDashboardAsync(f.Access(AppRoles.Admin))).MonthlyTrend;
        Assert.Equal(6,p.Count);Assert.Equal(new DateTime(2026,3,1,0,0,0,DateTimeKind.Utc),p[0].PeriodStartUtc);Assert.Equal(new DateTime(2026,8,1,0,0,0,DateTimeKind.Utc),p[5].PeriodStartUtc);Assert.True(p.Zip(p.Skip(1)).All(x=>x.First.PeriodStartUtc<x.Second.PeriodStartUtc));
        Assert.Equal(1,p.Single(x=>x.PeriodStartUtc.Month==7).CreatedCount);Assert.Equal(1,p.Single(x=>x.PeriodStartUtc.Month==8).CreatedCount);Assert.Equal(1,p.Single(x=>x.PeriodStartUtc.Month==8).ClosedCount);Assert.Equal(1,p.Single(x=>x.PeriodStartUtc.Month==8).CancelledCount);Assert.Equal(0,p.Single(x=>x.PeriodStartUtc.Month==6).CreatedCount);
    }

    [Fact]
    public async Task Trend_RespectsOwnership(){await using var f=await Fixture.Create();var p=(await f.Service.GetDashboardAsync(f.Access(AppRoles.Employee))).MonthlyTrend;Assert.Equal(1,p.Sum(x=>x.CreatedCount));Assert.Equal(0,p.Sum(x=>x.ClosedCount));Assert.Equal(1,p.Sum(x=>x.CancelledCount));}

    [Fact]
    public async Task RecentTickets_AreSafeMappedOrderedAndLimited()
    {
        await using var f=await Fixture.Create(extraTickets:9);var r=(await f.Service.GetDashboardAsync(f.Access(AppRoles.Admin))).RecentTickets;Assert.Equal(8,r.Count);Assert.True(r.Zip(r.Skip(1)).All(x=>x.First.UpdatedAtUtc>x.Second.UpdatedAtUtc||x.First.UpdatedAtUtc==x.Second.UpdatedAtUtc&&x.First.Id.CompareTo(x.Second.Id)>0));
        var owned=r.SingleOrDefault(x=>x.Id==f.OwnedId);if(owned is not null){Assert.NotNull(owned.CancelledAtUtc);Assert.Null(owned.AssignedToDisplayName);Assert.Equal("Open",owned.StatusName);Assert.Equal("Critical",owned.PriorityName);Assert.Equal("Network Operations With A Long Name",owned.CategoryName);}
        var properties=typeof(DashboardRecentTicketResponse).GetProperties().Select(x=>x.Name).ToArray();Assert.DoesNotContain(properties,x=>x.Contains("Email")||x.Contains("Description")||x.Contains("Comment")||x.Contains("Attachment"));
    }

    [Fact]
    public async Task MissingNamedLookups_ReturnZero()
    {await using var f=await Fixture.Create();var open=await f.Db.Statuses.SingleAsync(x=>x.Name=="Open");open.Name="New";var critical=await f.Db.Priorities.SingleAsync(x=>x.Name=="Critical");critical.Name="Urgent";await f.Db.SaveChangesAsync();var s=(await f.Service.GetDashboardAsync(f.Access(AppRoles.Admin))).Summary;Assert.Equal(0,s.OpenTickets);Assert.Equal(0,s.CriticalTickets);}

    [Fact]
    public void Architecture_HasNoHttpClaimsEntitiesOrQueryableExposure()
    {
        var constructor=typeof(DashboardService).GetConstructors().Single();Assert.DoesNotContain(constructor.GetParameters(),x=>x.ParameterType==typeof(HttpContext)||x.ParameterType==typeof(ClaimsPrincipal));
        var method=typeof(IDashboardService).GetMethod(nameof(IDashboardService.GetDashboardAsync))!;Assert.Equal(typeof(Task<DashboardResponse>),method.ReturnType);Assert.DoesNotContain(method.GetParameters(),x=>typeof(IQueryable).IsAssignableFrom(x.ParameterType));
        var names=typeof(DashboardResponse).Assembly.GetTypes().Where(x=>x.Namespace==typeof(DashboardResponse).Namespace).SelectMany(x=>x.GetProperties()).Select(x=>x.Name);Assert.DoesNotContain(names,x=>x.Contains("Email",StringComparison.OrdinalIgnoreCase)||x.Contains("Password",StringComparison.OrdinalIgnoreCase)||x.Contains("Security",StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now):TimeProvider{public override DateTimeOffset GetUtcNow()=>now;}
    private sealed class Fixture(SqliteConnection connection,ApplicationDbContext db,DashboardService service):IAsyncDisposable
    {
        public Guid OwnerId{get;}=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");public Guid OtherId{get;}=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");public Guid OwnedId{get;}=Guid.Parse("10000000-0000-0000-0000-000000000001");public ApplicationDbContext Db=>db;public DashboardService Service=>service;
        public TicketAccessContext Access(params string[] roles)=>new(){UserId=OwnerId,Roles=roles};
        public static async Task<Fixture>Create(int extraTickets=0){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await TicketSqliteDatabase.InitializeAsync(c);var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options;var db=new ApplicationDbContext(options);var now=new DateTime(2026,8,5,12,0,0,DateTimeKind.Utc);var f=new Fixture(c,db,new DashboardService(db,new FixedTimeProvider(new DateTimeOffset(now)),NullLogger<DashboardService>.Instance));
            db.Users.AddRange(new User{Id=f.OwnerId,UserName="owner",DisplayName="Owner",Email="owner@example.test",CreatedAtUtc=now,UpdatedAtUtc=now},new User{Id=f.OtherId,UserName="agent",DisplayName="Assigned Agent",Email="agent@example.test",CreatedAtUtc=now,UpdatedAtUtc=now});
            db.Statuses.Add(new Status{Id=9,Name="Inactive",SortOrder=9,IsActive=false,CreatedAtUtc=now,UpdatedAtUtc=now});db.Priorities.Add(new Priority{Id=3,Name="Critical",Rank=3,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now});db.Categories.Add(new Category{Id=7,Name="Network Operations With A Long Name",SortOrder=7,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now});
            db.Tickets.AddRange(new Ticket{Id=f.OwnedId,ReferenceNumber="TKT-OWN",Title="Owned",Description="secret body",CategoryId=7,PriorityId=3,StatusId=1,CreatedByUserId=f.OwnerId,CreatedAtUtc=new DateTime(2026,8,1,0,0,0,DateTimeKind.Utc),UpdatedAtUtc=now,CancelledAtUtc=now},new Ticket{Id=Guid.Parse("20000000-0000-0000-0000-000000000002"),ReferenceNumber="TKT-OTHER",Title="Other",Description="body",CategoryId=1,PriorityId=2,StatusId=5,CreatedByUserId=f.OtherId,AssignedToUserId=f.OtherId,CreatedAtUtc=new DateTime(2026,7,31,23,59,59,DateTimeKind.Utc),UpdatedAtUtc=now.AddMinutes(-1),ClosedAtUtc=now});
            for(var i=0;i<extraTickets;i++)db.Tickets.Add(new Ticket{Id=Guid.Parse($"30000000-0000-0000-0000-{i+1:000000000000}"),ReferenceNumber=$"TKT-X{i}",Title=$"Extra {i}",Description="body",CategoryId=1,PriorityId=1,StatusId=1,CreatedByUserId=f.OtherId,CreatedAtUtc=now.AddDays(-1),UpdatedAtUtc=now.AddMinutes(i+1)});await db.SaveChangesAsync();return f;}
        public async ValueTask DisposeAsync(){await db.DisposeAsync();await connection.DisposeAsync();}
    }
}
