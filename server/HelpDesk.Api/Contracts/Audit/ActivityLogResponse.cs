namespace HelpDesk.Api.Contracts.Audit;

/// <summary>A safe, read-only representation of one activity record.</summary>
public sealed class ActivityLogResponse
{
    public long Id { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? ActorDisplayName { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityIdentifier { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public IReadOnlyDictionary<string, string?> Metadata { get; init; } = new Dictionary<string, string?>();
}
