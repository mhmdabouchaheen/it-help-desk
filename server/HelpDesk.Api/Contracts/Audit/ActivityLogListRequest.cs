using HelpDesk.Api.Contracts.Common;

namespace HelpDesk.Api.Contracts.Audit;

/// <summary>Filters and pagination for the support-only activity feed.</summary>
public sealed class ActivityLogListRequest : PagedRequest
{
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public Guid? ActorUserId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
