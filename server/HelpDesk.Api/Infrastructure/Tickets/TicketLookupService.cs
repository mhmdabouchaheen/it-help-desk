using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Tickets;

/// <summary>Provides active, ordered ticket lookup projections.</summary>
public sealed class TicketLookupService(ApplicationDbContext dbContext) : ITicketLookupService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketCategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name)
            .Select(category => new TicketCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive
            })
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketPriorityResponse>> GetPrioritiesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Priorities.AsNoTracking()
            .Where(priority => priority.IsActive)
            .OrderBy(priority => priority.Rank).ThenBy(priority => priority.Name)
            .Select(priority => new TicketPriorityResponse
            {
                Id = priority.Id,
                Name = priority.Name,
                Description = priority.Description,
                Rank = priority.Rank,
                IsActive = priority.IsActive
            })
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketStatusResponse>> GetStatusesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Statuses.AsNoTracking()
            .Where(status => status.IsActive)
            .OrderBy(status => status.SortOrder).ThenBy(status => status.Name)
            .Select(status => new TicketStatusResponse
            {
                Id = status.Id,
                Name = status.Name,
                Description = status.Description,
                SortOrder = status.SortOrder,
                IsTerminal = status.IsTerminal,
                IsActive = status.IsActive
            })
            .ToListAsync(cancellationToken);
}
