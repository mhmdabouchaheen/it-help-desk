using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;

namespace HelpDesk.Api.Application.Tickets;

/// <summary>Defines the application boundary for future ticket operations.</summary>
public interface ITicketService
{
    /// <summary>Creates a ticket for the validated caller.</summary>
    Task<TicketDetailResponse> CreateAsync(CreateTicketRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Gets one authorized page of ticket summaries.</summary>
    Task<PagedResponse<TicketSummaryResponse>> GetPagedAsync(TicketListRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Gets an accessible ticket.</summary>
    Task<TicketDetailResponse> GetByIdAsync(Guid ticketId, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Updates an accessible ticket's basic details.</summary>
    Task<TicketDetailResponse> UpdateAsync(Guid ticketId, UpdateTicketRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Assigns a ticket when permitted.</summary>
    Task<TicketDetailResponse> AssignAsync(Guid ticketId, AssignTicketRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Changes ticket status when permitted.</summary>
    Task<TicketDetailResponse> ChangeStatusAsync(Guid ticketId, ChangeTicketStatusRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Adds a comment to an accessible ticket.</summary>
    Task<TicketCommentResponse> AddCommentAsync(Guid ticketId, AddTicketCommentRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
    /// <summary>Soft-cancels an accessible ticket without changing its status.</summary>
    Task<TicketDetailResponse> CancelAsync(Guid ticketId, CancelTicketRequest request, TicketAccessContext accessContext, CancellationToken cancellationToken = default);
}
