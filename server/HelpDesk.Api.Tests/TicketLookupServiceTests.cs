using HelpDesk.Api.Data;
using HelpDesk.Api.Infrastructure.Tickets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Tests;

public sealed class TicketLookupServiceTests
{
    [Fact]
    public async Task Lookups_ReturnOnlyActiveEntriesInConfiguredOrderWithoutTracking()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await TicketSqliteDatabase.InitializeAsync(connection);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        db.Categories.Single(x => x.Id == 1).IsActive = false;
        db.Priorities.Single(x => x.Id == 1).IsActive = false;
        db.Statuses.Single(x => x.Id == 1).IsActive = false;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new TicketLookupService(db);
        var categories = await service.GetCategoriesAsync();
        var priorities = await service.GetPrioritiesAsync();
        var statuses = await service.GetStatusesAsync();

        Assert.DoesNotContain(categories, x => x.Id == 1);
        Assert.DoesNotContain(priorities, x => x.Id == 1);
        Assert.DoesNotContain(statuses, x => x.Id == 1);
        Assert.Equal(categories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => x.Id), categories.Select(x => x.Id));
        Assert.Equal(priorities.OrderBy(x => x.Rank).ThenBy(x => x.Name).Select(x => x.Id), priorities.Select(x => x.Id));
        Assert.Equal(statuses.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => x.Id), statuses.Select(x => x.Id));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Lookups_ForwardCancellation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        var service = new TicketLookupService(db);
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetCategoriesAsync(source.Token));
    }
}
