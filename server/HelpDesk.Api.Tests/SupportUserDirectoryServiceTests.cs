using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Tests;

public sealed class SupportUserDirectoryServiceTests
{
    [Fact]
    public async Task ReturnsOnlyActiveEligibleUsers_Once_WithRelevantRoles_InDeterministicOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await TicketSqliteDatabase.InitializeAsync(connection);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        var sameNameLow = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var sameNameHigh = Guid.Parse("20000000-0000-0000-0000-000000000000");
        var employee = Guid.NewGuid(); var inactive = Guid.NewGuid();
        db.Users.AddRange(User(sameNameHigh, "Same", true, "secret@test"), User(sameNameLow, "Same", true), User(employee, "Employee", true), User(inactive, "Inactive", false));
        db.UserRoles.AddRange(Membership(sameNameLow, 1), Membership(sameNameLow, 2), Membership(sameNameLow, 3), Membership(sameNameHigh, 1), Membership(employee, 3), Membership(inactive, 2));
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await new SupportUserDirectoryService(db).GetEligibleSupportUsersAsync();
        Assert.Equal([sameNameLow, sameNameHigh], result.Select(x => x.Id));
        Assert.Equal([AppRoles.Admin, AppRoles.ItSupportAgent], result[0].Roles);
        Assert.All(result, x => Assert.Equal("Same", x.DisplayName));
        Assert.DoesNotContain(result, x => x.Id == employee || x.Id == inactive);
        Assert.All(db.ChangeTracker.Entries(), x => Assert.Equal(EntityState.Detached, x.State));
    }

    [Fact]
    public async Task EmptyDirectory_ReturnsEmptyCollection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await TicketSqliteDatabase.InitializeAsync(connection);
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        Assert.Empty(await new SupportUserDirectoryService(db).GetEligibleSupportUsersAsync());
    }

    [Fact]
    public void Contract_DoesNotExposeIdentitySecurityData()
    {
        var names = typeof(HelpDesk.Api.Contracts.Users.SupportUserResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(["Id", "DisplayName", "Roles"], names);
        Assert.DoesNotContain("Email", names); Assert.DoesNotContain("PasswordHash", names); Assert.DoesNotContain("SecurityStamp", names);
    }

    private static User User(Guid id, string name, bool active, string? email = null) => new() { Id = id, UserName = id.ToString(), DisplayName = name, IsActive = active, Email = email, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
    private static UserRole Membership(Guid user, int role) => new() { UserId = user, RoleId = role switch { 1 => Guid.Parse("11111111-1111-1111-1111-111111111111"), 2 => Guid.Parse("22222222-2222-2222-2222-222222222222"), 3 => Guid.Parse("33333333-3333-3333-3333-333333333333"), _ => throw new ArgumentOutOfRangeException(nameof(role)) }, AssignedAtUtc = DateTime.UtcNow };
}
