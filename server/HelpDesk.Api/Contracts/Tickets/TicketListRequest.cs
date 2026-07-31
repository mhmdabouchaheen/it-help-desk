using System.ComponentModel.DataAnnotations;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Common;

namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Defines filtering, sorting, and pagination for ticket lists.</summary>
public sealed class TicketListRequest : PagedRequest
{
    /// <summary>Gets optional free-text search input.</summary>
    [MaxLength(200)]
    public string? Search { get; init; }
    /// <summary>Gets an optional category filter.</summary>
    public short? CategoryId { get; init; }
    /// <summary>Gets an optional priority filter.</summary>
    public short? PriorityId { get; init; }
    /// <summary>Gets an optional status filter.</summary>
    public short? StatusId { get; init; }
    /// <summary>Gets an optional creator filter.</summary>
    public Guid? CreatedByUserId { get; init; }
    /// <summary>Gets an optional assignee filter.</summary>
    public Guid? AssignedToUserId { get; init; }
    /// <summary>Gets the optional inclusive creation-time lower bound.</summary>
    public DateTime? CreatedFromUtc { get; init; }
    /// <summary>Gets the optional inclusive creation-time upper bound.</summary>
    public DateTime? CreatedToUtc { get; init; }
    /// <summary>Gets the selected stable sort field.</summary>
    [Common.Validation.AllowedValues(TicketSortFields.CreatedAtUtc, TicketSortFields.UpdatedAtUtc,
        TicketSortFields.TicketNumber, TicketSortFields.Priority, TicketSortFields.Status,
        TicketSortFields.Title)]
    public string SortBy { get; init; } = TicketSortFields.CreatedAtUtc;
    /// <summary>Gets the sort direction.</summary>
    [Common.Validation.AllowedValues(SortDirections.Ascending, SortDirections.Descending)]
    public string SortDirection { get; init; } = SortDirections.Descending;
}
