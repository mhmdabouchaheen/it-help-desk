using HelpDesk.Api.Contracts.Tickets;

namespace HelpDesk.Api.Application.Tickets;

/// <summary>Defines the application boundary for public ticket lookup data.</summary>
public interface ITicketLookupService
{
    /// <summary>Gets ticket categories.</summary>
    Task<IReadOnlyList<TicketCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets ticket priorities.</summary>
    Task<IReadOnlyList<TicketPriorityResponse>> GetPrioritiesAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets ticket statuses.</summary>
    Task<IReadOnlyList<TicketStatusResponse>> GetStatusesAsync(CancellationToken cancellationToken = default);
}
