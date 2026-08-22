using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Users;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class UserRoleManagementServiceTests
{
    [Theory] [InlineData("Unknown",false)] [InlineData("Employee",true)]
    public async Task RejectsUnknownAndDuplicateRoles(string role,bool duplicate)
    {await using var f=await Fixture.Create();var roles=duplicate?new[]{role,role}:new[]{role};await Assert.ThrowsAsync<RoleManagementValidationException>(()=>f.Service.UpdateRolesAsync(f.Target,new(){Roles=roles},f.Admin,null));}

    [Fact] public async Task RejectsRemovingLastActiveAdmin()
    {await using var f=await Fixture.Create(targetIsAdmin:true);await Assert.ThrowsAsync<RoleManagementValidationException>(()=>f.Service.UpdateRolesAsync(f.Target,new(){Roles=[AppRoles.Employee]},f.Admin,null));}

    [Fact] public async Task AllowsAdminRemovalWhenAnotherActiveAdminExists()
    {await using var f=await Fixture.Create(targetIsAdmin:true,anotherActiveAdmin:true);await f.Service.UpdateRolesAsync(f.Target,new(){Roles=[AppRoles.Employee]},f.Admin,null);f.Users.Verify(x=>x.RemoveFromRolesAsync(It.IsAny<User>(),It.Is<IEnumerable<string>>(r=>r.SequenceEqual(new[]{AppRoles.Admin}))));}

    [Fact] public async Task RejectsManagerRemovalWhileDirectReportsExist()
    {await using var f=await Fixture.Create(targetIsManager:true,directReport:true);await Assert.ThrowsAsync<RoleManagementValidationException>(()=>f.Service.UpdateRolesAsync(f.Target,new(){Roles=[AppRoles.Employee]},f.Admin,null));}

    [Fact] public async Task AllowsManagerRemovalAfterDirectReportsAreCleared()
    {await using var f=await Fixture.Create(targetIsManager:true);await f.Service.UpdateRolesAsync(f.Target,new(){Roles=[AppRoles.Employee]},f.Admin,null);f.Users.Verify(x=>x.RemoveFromRolesAsync(It.IsAny<User>(),It.Is<IEnumerable<string>>(r=>r.SequenceEqual(new[]{AppRoles.Manager}))));}

    [Fact] public async Task AddsManagerWithoutChangingRelationshipAndRevokesSessions()
    {await using var f=await Fixture.Create();var before=(await f.Db.Users.FindAsync(f.Target))!.ManagerUserId;var result=await f.Service.UpdateRolesAsync(f.Target,new(){Roles=[AppRoles.Employee,AppRoles.Manager]},f.Admin,"127.0.0.1");Assert.Contains(AppRoles.Manager,result.Roles);Assert.Equal(before,(await f.Db.Users.FindAsync(f.Target))!.ManagerUserId);f.Refresh.Verify(x=>x.RevokeAllForUserAsync(f.Target,"127.0.0.1","Roles changed",It.IsAny<CancellationToken>()));f.Users.Verify(x=>x.AddToRolesAsync(It.IsAny<User>(),It.Is<IEnumerable<string>>(r=>r.SequenceEqual(new[]{AppRoles.Manager}))));}

    private sealed class Fixture(SqliteConnection connection,ApplicationDbContext db,Mock<UserManager<User>> users,
        Mock<IRefreshTokenService> refresh,UserRoleManagementService service):IAsyncDisposable
    {
        public Guid Target{get;}=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");public Guid Admin{get;}=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");public ApplicationDbContext Db=>db;public Mock<UserManager<User>> Users=>users;public Mock<IRefreshTokenService> Refresh=>refresh;public UserRoleManagementService Service=>service;
        public static async Task<Fixture>Create(bool targetIsAdmin=false,bool targetIsManager=false,bool directReport=false,bool anotherActiveAdmin=false)
        {var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();await TicketSqliteDatabase.InitializeAsync(connection);var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);var target=new User{Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),UserName="target",Email="target@test",DisplayName="Target",IsActive=true,CreatedAtUtc=DateTime.UtcNow,UpdatedAtUtc=DateTime.UtcNow};var admin=new User{Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),UserName="admin",Email="admin@test",DisplayName="Admin",IsActive=anotherActiveAdmin,CreatedAtUtc=DateTime.UtcNow,UpdatedAtUtc=DateTime.UtcNow};db.Users.AddRange(target,admin);if(anotherActiveAdmin)db.UserRoles.Add(new(){UserId=admin.Id,RoleId=Guid.Parse("11111111-1111-1111-1111-111111111111"),AssignedAtUtc=DateTime.UtcNow});if(directReport)db.Users.Add(new User{Id=Guid.NewGuid(),UserName="report",Email="report@test",DisplayName="Report",IsActive=true,ManagerUserId=target.Id,CreatedAtUtc=DateTime.UtcNow,UpdatedAtUtc=DateTime.UtcNow});if(targetIsAdmin)db.UserRoles.Add(new(){UserId=target.Id,RoleId=Guid.Parse("11111111-1111-1111-1111-111111111111"),AssignedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();var store=Mock.Of<IUserStore<User>>();var users=new Mock<UserManager<User>>(store,null!,Mock.Of<IPasswordHasher<User>>(),Array.Empty<IUserValidator<User>>(),Array.Empty<IPasswordValidator<User>>(),Mock.Of<ILookupNormalizer>(),new IdentityErrorDescriber(),null!,Mock.Of<ILogger<UserManager<User>>>());var initial=new List<string>{AppRoles.Employee};if(targetIsAdmin)initial.Add(AppRoles.Admin);if(targetIsManager)initial.Add(AppRoles.Manager);users.Setup(x=>x.FindByIdAsync(target.Id.ToString())).ReturnsAsync(target);users.SetupSequence(x=>x.GetRolesAsync(target)).ReturnsAsync(initial).ReturnsAsync(initial.Append(AppRoles.Manager).Distinct().ToList());users.Setup(x=>x.AddToRolesAsync(target,It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);users.Setup(x=>x.RemoveFromRolesAsync(target,It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);var refresh=new Mock<IRefreshTokenService>();refresh.Setup(x=>x.RevokeAllForUserAsync(It.IsAny<Guid>(),It.IsAny<string?>(),It.IsAny<string>(),It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);var audit=new Mock<IActivityLogService>();audit.Setup(x=>x.WriteAsync(It.IsAny<Guid?>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<IReadOnlyDictionary<string,string?>?>(),It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);return new(connection,db,users,refresh,new(users.Object,db,refresh.Object,audit.Object,Mock.Of<ILogger<UserRoleManagementService>>()));}
        public async ValueTask DisposeAsync(){await db.DisposeAsync();await connection.DisposeAsync();}
    }
}
