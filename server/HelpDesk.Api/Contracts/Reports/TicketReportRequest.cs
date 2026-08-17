using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Reports;

public sealed class TicketReportRequest : IValidatableObject
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    [Range(1, short.MaxValue)] public short? CategoryId { get; init; }
    [Range(1, short.MaxValue)] public short? PriorityId { get; init; }
    [Range(1, short.MaxValue)] public short? StatusId { get; init; }
    public Guid? AssignedToUserId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromUtc is { Kind: DateTimeKind.Local } || ToUtc is { Kind: DateTimeKind.Local })
            yield return new ValidationResult("Report dates must be UTC.", [nameof(FromUtc), nameof(ToUtc)]);
        if (FromUtc.HasValue && ToUtc.HasValue && FromUtc.Value > ToUtc.Value)
            yield return new ValidationResult("FromUtc must be earlier than or equal to ToUtc.", [nameof(FromUtc), nameof(ToUtc)]);
        if (FromUtc.HasValue && ToUtc.HasValue && ToUtc.Value - FromUtc.Value > TimeSpan.FromDays(366))
            yield return new ValidationResult("The report date range cannot exceed 366 days.", [nameof(FromUtc), nameof(ToUtc)]);
        if (AssignedToUserId == Guid.Empty)
            yield return new ValidationResult("AssignedToUserId must be a valid identifier.", [nameof(AssignedToUserId)]);
    }
}
