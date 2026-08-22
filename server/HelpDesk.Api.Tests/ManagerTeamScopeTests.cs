using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Authorization;
using HelpDesk.Api.Infrastructure.Dashboard;
using HelpDesk.Api.Infrastructure.Reports;
using HelpDesk.Api.Infrastructure.Tickets;
using HelpDesk.Api.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class ManagerTeamScopeTests
{
    [Fact]
    public async Task CentralScope_EnforcesTeamEmployeeSupportAndMultiRolePrecedence()
    {
        await using var f=await Fixture.Create();
        Assert.Equal(["A1","A2","MA"],await f.Visible(f.ManagerA,AppRoles.Manager));
        Assert.Equal(["B1","MB"],await f.Visible(f.ManagerB,AppRoles.Manager));
        Assert.Equal(["A1"],await f.Visible(f.EmployeeA1,AppRoles.Employee));
        Assert.Equal(6,(await f.Visible(f.Support,AppRoles.ItSupportAgent)).Count);
        Assert.Equal(6,(await f.Visible(f.Admin,AppRoles.Admin)).Count);
        Assert.Equal(["A1","A2","MA"],await f.Visible(f.ManagerA,AppRoles.Employee,AppRoles.Manager));
        Assert.Equal(6,(await f.Visible(f.ManagerA,AppRoles.Manager,AppRoles.ItSupportAgent)).Count);
    }

    [Fact]
    public async Task DashboardAndReports_UseIdenticalManagerScopeAndFiltersCannotEscape()
    {
        await using var f=await Fixture.Create();var access=f.Access(f.ManagerA,AppRoles.Manager);
        var dashboard=await new DashboardService(f.Db,TimeProvider.System,NullLogger<DashboardService>.Instance).GetDashboardAsync(access);
        var reports=new ReportService(f.Db,TimeProvider.System);
        var report=await reports.GetTicketReportAsync(new TicketReportRequest(),access);
        var escaped=await reports.GetTicketReportAsync(new TicketReportRequest{AssignedToUserId=f.Support},access);
        Assert.Equal(3,dashboard.Summary.TotalTickets);Assert.Equal(3,report.Summary.TotalTickets);
        Assert.All(dashboard.RecentTickets,x=>Assert.Contains(x.ReferenceNumber,new[]{"MA","A1","A2"}));
        Assert.Equal(3,escaped.Summary.TotalTickets);
    }

    [Fact]
    public async Task Detail_ShowsTeamPublicCommentsButNotInternalAndRejectsOtherTeam()
    {
        await using var f=await Fixture.Create();var service=f.Tickets();var access=f.Access(f.ManagerA,AppRoles.Manager);
        var detail=await service.GetByIdAsync(f.TicketIds["A1"],access);
        Assert.Single(detail.Comments);Assert.Equal("Public",detail.Comments[0].Visibility);
        await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TicketNotFoundException>(()=>service.GetByIdAsync(f.TicketIds["B1"],access));
        await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TicketNotFoundException>(()=>service.UpdateAsync(f.TicketIds["A1"],new UpdateTicketRequest{Title="Changed",Description="Changed",CategoryId=1,PriorityId=1},access));
        await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TicketNotFoundException>(()=>service.AddCommentAsync(f.TicketIds["A1"],new AddTicketCommentRequest{Content="manager",IsInternal=false},access));
    }

    [Fact]
    public async Task AdminTeamService_ValidatesManagerRoleActiveSelfAndRemoval()
    {
        await using var f=await Fixture.Create();var service=new UserTeamManagementService(f.Db);
        var updated=await service.UpdateManagerAsync(f.Unassigned,new(){ManagerUserId=f.ManagerA});Assert.Equal(f.ManagerA,updated.ManagerUserId);
        await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TeamManagementValidationException>(()=>service.UpdateManagerAsync(f.Unassigned,new(){ManagerUserId=f.EmployeeA1}));
        await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TeamManagementValidationException>(()=>service.UpdateManagerAsync(f.ManagerA,new(){ManagerUserId=f.ManagerA}));
        var inactive=await f.Db.Users.SingleAsync(x=>x.Id==f.ManagerB);inactive.IsActive=false;await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<HelpDesk.Api.Application.Common.Exceptions.TeamManagementValidationException>(()=>service.UpdateManagerAsync(f.Unassigned,new(){ManagerUserId=f.ManagerB}));
        updated=await service.UpdateManagerAsync(f.Unassigned,new(){ManagerUserId=null});Assert.Null(updated.ManagerUserId);
    }

    private sealed class Fixture(SqliteConnection connection,ApplicationDbContext db):IAsyncDisposable
    {
        public ApplicationDbContext Db=>db;public Guid ManagerA{get;}=Guid.Parse("10000000-0000-0000-0000-000000000001");public Guid ManagerB{get;}=Guid.Parse("10000000-0000-0000-0000-000000000002");public Guid EmployeeA1{get;}=Guid.Parse("20000000-0000-0000-0000-000000000001");public Guid EmployeeA2{get;}=Guid.Parse("20000000-0000-0000-0000-000000000002");public Guid EmployeeB1{get;}=Guid.Parse("20000000-0000-0000-0000-000000000003");public Guid Unassigned{get;}=Guid.Parse("20000000-0000-0000-0000-000000000004");public Guid Support{get;}=Guid.Parse("30000000-0000-0000-0000-000000000001");public Guid Admin{get;}=Guid.Parse("30000000-0000-0000-0000-000000000002");public Dictionary<string,Guid>TicketIds{get;}=[];
        public static async Task<Fixture>Create(){var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await TicketSqliteDatabase.InitializeAsync(c);var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);var f=new Fixture(c,db);var now=DateTime.UtcNow;User U(Guid id,string name,Guid?manager=null,bool active=true)=>new(){Id=id,UserName=name,NormalizedUserName=name.ToUpperInvariant(),Email=$"{name}@test",NormalizedEmail=$"{name}@TEST",DisplayName=name,ManagerUserId=manager,IsActive=active,CreatedAtUtc=now,UpdatedAtUtc=now};db.Users.AddRange(U(f.ManagerA,"Manager A"),U(f.ManagerB,"Manager B"),U(f.EmployeeA1,"A1",f.ManagerA),U(f.EmployeeA2,"A2",f.ManagerA),U(f.EmployeeB1,"B1",f.ManagerB),U(f.Unassigned,"Unassigned"),U(f.Support,"Support"),U(f.Admin,"Admin"));db.UserRoles.AddRange(new UserRole{UserId=f.ManagerA,RoleId=Guid.Parse("44444444-4444-4444-4444-444444444444"),AssignedAtUtc=now},new UserRole{UserId=f.ManagerB,RoleId=Guid.Parse("44444444-4444-4444-4444-444444444444"),AssignedAtUtc=now},new UserRole{UserId=f.Support,RoleId=Guid.Parse("22222222-2222-2222-2222-222222222222"),AssignedAtUtc=now},new UserRole{UserId=f.EmployeeA1,RoleId=Guid.Parse("33333333-3333-3333-3333-333333333333"),AssignedAtUtc=now},new UserRole{UserId=f.EmployeeA2,RoleId=Guid.Parse("33333333-3333-3333-3333-333333333333"),AssignedAtUtc=now},new UserRole{UserId=f.EmployeeB1,RoleId=Guid.Parse("33333333-3333-3333-3333-333333333333"),AssignedAtUtc=now},new UserRole{UserId=f.Unassigned,RoleId=Guid.Parse("33333333-3333-3333-3333-333333333333"),AssignedAtUtc=now});foreach(var(name,owner)in new[]{("MA",f.ManagerA),("MB",f.ManagerB),("A1",f.EmployeeA1),("A2",f.EmployeeA2),("B1",f.EmployeeB1),("U",f.Unassigned)}){var id=Guid.NewGuid();f.TicketIds[name]=id;db.Tickets.Add(new Ticket{Id=id,ReferenceNumber=name,Title=name,Description=name,CategoryId=1,PriorityId=1,StatusId=1,CreatedByUserId=owner,AssignedToUserId=f.Support,CreatedAtUtc=now,UpdatedAtUtc=now});}db.TicketComments.AddRange(new TicketComment{Id=Guid.NewGuid(),TicketId=f.TicketIds["A1"],AuthorUserId=f.EmployeeA1,Body="public",Visibility="Public",CreatedAtUtc=now},new TicketComment{Id=Guid.NewGuid(),TicketId=f.TicketIds["A1"],AuthorUserId=f.Support,Body="secret",Visibility="Internal",CreatedAtUtc=now});await db.SaveChangesAsync();return f;}
        public TicketAccessContext Access(Guid id,params string[]roles)=>new(){UserId=id,Roles=roles};public async Task<List<string>>Visible(Guid id,params string[]roles)=>await TicketReadScope.Apply(db.Tickets.AsNoTracking(),db,Access(id,roles)).OrderBy(x=>x.ReferenceNumber).Select(x=>x.ReferenceNumber).ToListAsync();public TicketService Tickets()=>new(db,Mock.Of<ITicketNumberGenerator>(),TimeProvider.System,NullLogger<TicketService>.Instance,Mock.Of<ITicketNotificationService>());public async ValueTask DisposeAsync(){await db.DisposeAsync();await connection.DisposeAsync();}
    }
}
