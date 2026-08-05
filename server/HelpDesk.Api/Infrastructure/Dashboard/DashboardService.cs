using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Dashboard;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Dashboard;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Dashboard;

/// <summary>Aggregates role-filtered dashboard data using database-side queries.</summary>
public sealed class DashboardService(ApplicationDbContext db, TimeProvider timeProvider,
    ILogger<DashboardService> logger) : IDashboardService
{
    private static readonly string[] NamedStatuses = ["Open", "In Progress", "Pending", "Resolved", "Closed"];

    public async Task<DashboardResponse> GetDashboardAsync(TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        var support = Validate(accessContext);
        IQueryable<Ticket> tickets = db.Tickets.AsNoTracking();
        if (!support) tickets = tickets.Where(x => x.CreatedByUserId == accessContext.UserId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trendStart = month.AddMonths(-5);
        var statusCounts = await tickets.GroupBy(x => x.StatusId)
            .Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        var priorityCounts = await tickets.GroupBy(x => x.PriorityId)
            .Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        var categoryCounts = await tickets.GroupBy(x => x.CategoryId)
            .Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var statuses = await db.Statuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.Name, Order = (int)x.SortOrder }).ToListAsync(cancellationToken);
        var priorities = await db.Priorities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Rank)
            .Select(x => new { x.Id, x.Name, Order = (int)x.Rank }).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.Name, Order = (int)x.SortOrder }).ToListAsync(cancellationToken);

        foreach (var name in NamedStatuses.Where(name => statuses.All(x => x.Name != name)))
            logger.LogWarning("Dashboard named status {StatusName} is missing; its KPI will be zero.", name);
        if (priorities.All(x => x.Name != "Critical"))
            logger.LogWarning("Dashboard Critical priority is missing; its KPI will be zero.");

        int Status(string name) => statuses.Where(x => x.Name == name).Sum(x => statusCounts.GetValueOrDefault(x.Id));
        var summaryAggregate = await tickets.GroupBy(_ => 1).Select(g => new
        {
            Total = g.Count(), Cancelled = g.Count(x => x.CancelledAtUtc != null),
            Assigned = g.Count(x => x.AssignedToUserId != null), Unassigned = g.Count(x => x.AssignedToUserId == null),
            CreatedMonth = g.Count(x => x.CreatedAtUtc >= month), ClosedMonth = g.Count(x => x.ClosedAtUtc >= month)
        }).SingleOrDefaultAsync(cancellationToken);
        var criticalIds = priorities.Where(x => x.Name == "Critical").Select(x => x.Id).ToArray();
        var critical = criticalIds.Length == 0 ? 0 : await tickets.CountAsync(x => criticalIds.Contains(x.PriorityId), cancellationToken);

        var createdDates = await tickets.Where(x => x.CreatedAtUtc >= trendStart && x.CreatedAtUtc < month.AddMonths(1))
            .Select(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        var closedDates = await tickets.Where(x => x.ClosedAtUtc >= trendStart && x.ClosedAtUtc < month.AddMonths(1))
            .Select(x => x.ClosedAtUtc!.Value).ToListAsync(cancellationToken);
        var cancelledDates = await tickets.Where(x => x.CancelledAtUtc >= trendStart && x.CancelledAtUtc < month.AddMonths(1))
            .Select(x => x.CancelledAtUtc!.Value).ToListAsync(cancellationToken);
        var trend = Enumerable.Range(0, 6).Select(i => trendStart.AddMonths(i)).Select(start => new DashboardTrendPointResponse
        {
            PeriodStartUtc = start, CreatedCount = createdDates.Count(x => x.Year == start.Year && x.Month == start.Month),
            ClosedCount = closedDates.Count(x => x.Year == start.Year && x.Month == start.Month),
            CancelledCount = cancelledDates.Count(x => x.Year == start.Year && x.Month == start.Month)
        }).ToArray();

        var recent = await (from ticket in tickets
            join status in db.Statuses.AsNoTracking() on ticket.StatusId equals status.Id
            join priority in db.Priorities.AsNoTracking() on ticket.PriorityId equals priority.Id
            join category in db.Categories.AsNoTracking() on ticket.CategoryId equals category.Id
            join user in db.Users.AsNoTracking() on ticket.AssignedToUserId equals user.Id into assignees
            from assignee in assignees.DefaultIfEmpty()
            orderby ticket.UpdatedAtUtc descending, ticket.Id descending
            select new DashboardRecentTicketResponse { Id=ticket.Id, ReferenceNumber=ticket.ReferenceNumber, Title=ticket.Title,
                StatusName=status.Name, PriorityName=priority.Name, CategoryName=category.Name, CreatedAtUtc=ticket.CreatedAtUtc,
                UpdatedAtUtc=ticket.UpdatedAtUtc, CancelledAtUtc=ticket.CancelledAtUtc,
                AssignedToDisplayName=assignee == null ? null : assignee.DisplayName }).Take(8).ToListAsync(cancellationToken);

        DashboardBreakdownItemResponse Map(short id, string name, int order, IReadOnlyDictionary<short,int> counts) =>
            new() { Id=id, Name=name, DisplayOrder=order, Count=counts.GetValueOrDefault(id) };
        return new DashboardResponse
        {
            Summary = new() { TotalTickets=summaryAggregate?.Total ?? 0, OpenTickets=Status("Open"), InProgressTickets=Status("In Progress"),
                PendingTickets=Status("Pending"), ResolvedTickets=Status("Resolved"), ClosedTickets=Status("Closed"),
                CancelledTickets=summaryAggregate?.Cancelled ?? 0, AssignedTickets=summaryAggregate?.Assigned ?? 0,
                UnassignedTickets=summaryAggregate?.Unassigned ?? 0, CriticalTickets=critical,
                CreatedThisMonth=summaryAggregate?.CreatedMonth ?? 0, ClosedThisMonth=summaryAggregate?.ClosedMonth ?? 0 },
            StatusBreakdown=statuses.Select(x => Map(x.Id,x.Name,x.Order,statusCounts)).ToArray(),
            PriorityBreakdown=priorities.Select(x => Map(x.Id,x.Name,x.Order,priorityCounts)).ToArray(),
            CategoryBreakdown=categories.Select(x => Map(x.Id,x.Name,x.Order,categoryCounts)).ToArray(),
            MonthlyTrend=trend, RecentTickets=recent
        };
    }

    private static bool Validate(TicketAccessContext context)
    {
        if (context is null || context.UserId == Guid.Empty || context.Roles is null ||
            !context.Roles.Any(x => AppRoles.All.Contains(x, StringComparer.Ordinal))) throw new TicketAccessDeniedException();
        return context.Roles.Any(x => AppRoles.SupportStaff.Contains(x, StringComparer.Ordinal));
    }
}
