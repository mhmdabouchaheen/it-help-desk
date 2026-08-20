using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Contracts.Common;

namespace HelpDesk.Api.Application.Audit;

/// <summary>Provides safe append and read operations for application activity.</summary>
public interface IActivityLogService
{
    Task WriteAsync(Guid? actorUserId, string action, string entityType, string entityIdentifier,
        IReadOnlyDictionary<string, string?>? metadata = null, CancellationToken cancellationToken = default);
    Task<PagedResponse<ActivityLogResponse>> GetPagedAsync(ActivityLogListRequest request,
        CancellationToken cancellationToken = default);
    Task<PagedResponse<ActivityLogResponse>> GetForTicketAsync(Guid ticketId, PagedRequest request,
        CancellationToken cancellationToken = default);
}
