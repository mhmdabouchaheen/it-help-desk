using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Reports;
using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Infrastructure.Authorization;

namespace HelpDesk.Api.Infrastructure.Reports;

public sealed class ReportService(ApplicationDbContext db, TimeProvider timeProvider) : IReportService
{
    public async Task<TicketReportResponse> GetTicketReportAsync(TicketReportRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateLookupsAsync(request, cancellationToken);

        IQueryable<Ticket> tickets = TicketReadScope.Apply(db.Tickets.AsNoTracking(), db, accessContext);
        if (request.FromUtc.HasValue) tickets = tickets.Where(x => x.CreatedAtUtc >= request.FromUtc.Value);
        if (request.ToUtc.HasValue) tickets = tickets.Where(x => x.CreatedAtUtc <= request.ToUtc.Value);
        if (request.CategoryId.HasValue) tickets = tickets.Where(x => x.CategoryId == request.CategoryId.Value);
        if (request.PriorityId.HasValue) tickets = tickets.Where(x => x.PriorityId == request.PriorityId.Value);
        if (request.StatusId.HasValue) tickets = tickets.Where(x => x.StatusId == request.StatusId.Value);
        if (request.AssignedToUserId.HasValue) tickets = tickets.Where(x => x.AssignedToUserId == request.AssignedToUserId.Value);

        var statusCounts = await tickets.GroupBy(x => x.StatusId).Select(x => new { Id=x.Key, Count=x.Count() }).ToDictionaryAsync(x=>x.Id,x=>x.Count,cancellationToken);
        var priorityCounts = await tickets.GroupBy(x => x.PriorityId).Select(x => new { Id=x.Key, Count=x.Count() }).ToDictionaryAsync(x=>x.Id,x=>x.Count,cancellationToken);
        var categoryCounts = await tickets.GroupBy(x => x.CategoryId).Select(x => new { Id=x.Key, Count=x.Count() }).ToDictionaryAsync(x=>x.Id,x=>x.Count,cancellationToken);
        var statuses = await db.Statuses.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.SortOrder).Select(x=>new{x.Id,x.Name,x.IsTerminal}).ToListAsync(cancellationToken);
        var priorities = await db.Priorities.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Rank).Select(x=>new{x.Id,x.Name}).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.SortOrder).Select(x=>new{x.Id,x.Name}).ToListAsync(cancellationToken);
        var aggregate = await tickets.GroupBy(_=>1).Select(g=>new{Total=g.Count(),Cancelled=g.Count(x=>x.CancelledAtUtc!=null),Assigned=g.Count(x=>x.AssignedToUserId!=null),Unassigned=g.Count(x=>x.AssignedToUserId==null)}).SingleOrDefaultAsync(cancellationToken);
        var resolutionDurations = tickets
            .Where(x => x.ResolvedAtUtc != null && x.CancelledAtUtc == null && x.ResolvedAtUtc >= x.CreatedAtUtc)
            .Select(x => (x.ResolvedAtUtc!.Value - x.CreatedAtUtc).TotalMinutes);
        double? averageResolutionMinutes;
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            averageResolutionMinutes = await resolutionDurations.AnyAsync(cancellationToken)
                ? await resolutionDurations.AverageAsync(cancellationToken)
                : null;
        }
        else
        {
            var resolutionTimestamps = await tickets
                .Where(x => x.ResolvedAtUtc != null && x.CancelledAtUtc == null && x.ResolvedAtUtc >= x.CreatedAtUtc)
                .Select(x => new { x.CreatedAtUtc, ResolvedAtUtc = x.ResolvedAtUtc!.Value })
                .ToListAsync(cancellationToken);
            averageResolutionMinutes = resolutionTimestamps.Count == 0
                ? null
                : resolutionTimestamps.Average(x => (x.ResolvedAtUtc - x.CreatedAtUtc).TotalMinutes);
        }
        var terminalIds=statuses.Where(x=>x.IsTerminal).Select(x=>x.Id).ToArray();
        var terminal=terminalIds.Sum(id=>statusCounts.GetValueOrDefault(id));

        var now=timeProvider.GetUtcNow().UtcDateTime;
        var from=(request.FromUtc??now.Date.AddDays(-29)).Date;
        var to=(request.ToUtc??now).Date;
        if(to<from)to=from;
        var createdDates=await tickets.Where(x=>x.CreatedAtUtc>=from&&x.CreatedAtUtc<to.AddDays(1)).Select(x=>x.CreatedAtUtc).ToListAsync(cancellationToken);
        var closedDates=await tickets.Where(x=>x.ClosedAtUtc>=from&&x.ClosedAtUtc<to.AddDays(1)).Select(x=>x.ClosedAtUtc!.Value).ToListAsync(cancellationToken);
        var trend=Enumerable.Range(0,(to-from).Days+1).Select(offset=>from.AddDays(offset)).Select(day=>new TicketReportTrendResponse{PeriodStartUtc=DateTime.SpecifyKind(day,DateTimeKind.Utc),CreatedCount=createdDates.Count(x=>x.Date==day),ClosedCount=closedDates.Count(x=>x.Date==day)}).ToArray();

        var supportRoleNames=AppRoles.SupportStaff;
        var agentsQuery=from user in db.Users.AsNoTracking()
            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where user.IsActive && supportRoleNames.Contains(role.Name!)
            select new {user.Id,user.DisplayName};
        if (!TicketReadScope.IsSupportWide(accessContext))
        {
            var scopedAssigneeIds = tickets.Where(x => x.AssignedToUserId != null).Select(x => x.AssignedToUserId!.Value);
            agentsQuery = agentsQuery.Where(agent => scopedAssigneeIds.Contains(agent.Id));
        }
        var agents=await agentsQuery.Distinct().OrderBy(x=>x.DisplayName).ToListAsync(cancellationToken);
        var activeCounts=await tickets.Where(x=>x.AssignedToUserId!=null&&!terminalIds.Contains(x.StatusId)).GroupBy(x=>x.AssignedToUserId!.Value).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count,cancellationToken);

        static TicketReportBreakdownResponse Item(short id,string name,IReadOnlyDictionary<short,int> counts)=>new(){Id=id,Name=name,Count=counts.GetValueOrDefault(id)};
        return new TicketReportResponse{
            Summary=new(){TotalTickets=aggregate?.Total??0,OpenTickets=(aggregate?.Total??0)-terminal,TerminalTickets=terminal,CancelledTickets=aggregate?.Cancelled??0,AssignedTickets=aggregate?.Assigned??0,UnassignedTickets=aggregate?.Unassigned??0,AverageResolutionMinutes=averageResolutionMinutes},
            StatusBreakdown=statuses.Select(x=>Item(x.Id,x.Name,statusCounts)).ToArray(),PriorityBreakdown=priorities.Select(x=>Item(x.Id,x.Name,priorityCounts)).ToArray(),CategoryBreakdown=categories.Select(x=>Item(x.Id,x.Name,categoryCounts)).ToArray(),Trend=trend,
            AgentWorkload=agents.Select(x=>new AgentWorkloadResponse{UserId=x.Id,DisplayName=x.DisplayName,ActiveTicketCount=activeCounts.GetValueOrDefault(x.Id)}).ToArray()};
    }

    private async Task ValidateLookupsAsync(TicketReportRequest request,CancellationToken cancellationToken)
    {
        if(request.CategoryId.HasValue&&!await db.Categories.AsNoTracking().AnyAsync(x=>x.Id==request.CategoryId,cancellationToken))throw new TicketValidationException();
        if(request.PriorityId.HasValue&&!await db.Priorities.AsNoTracking().AnyAsync(x=>x.Id==request.PriorityId,cancellationToken))throw new TicketValidationException();
        if(request.StatusId.HasValue&&!await db.Statuses.AsNoTracking().AnyAsync(x=>x.Id==request.StatusId,cancellationToken))throw new TicketValidationException();
        if(request.AssignedToUserId.HasValue&&!await db.Users.AsNoTracking().AnyAsync(x=>x.Id==request.AssignedToUserId&&x.IsActive,cancellationToken))throw new TicketValidationException();
    }
}
