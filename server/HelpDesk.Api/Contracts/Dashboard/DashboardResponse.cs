namespace HelpDesk.Api.Contracts.Dashboard;

/// <summary>Dashboard KPI totals for the caller's visible tickets.</summary>
public sealed class DashboardSummaryResponse
{
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public int InProgressTickets { get; init; }
    public int PendingTickets { get; init; }
    public int ResolvedTickets { get; init; }
    public int ClosedTickets { get; init; }
    public int CancelledTickets { get; init; }
    public int UnassignedTickets { get; init; }
    public int AssignedTickets { get; init; }
    public int CriticalTickets { get; init; }
    public int CreatedThisMonth { get; init; }
    public int ClosedThisMonth { get; init; }
}

/// <summary>A lookup-backed dashboard count in display order.</summary>
public sealed class DashboardBreakdownItemResponse
{
    public short Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>Ticket activity for one UTC calendar month.</summary>
public sealed class DashboardTrendPointResponse
{
    public DateTime PeriodStartUtc { get; init; }
    public int CreatedCount { get; init; }
    public int ClosedCount { get; init; }
    public int CancelledCount { get; init; }
}

/// <summary>Safe navigation fields for a recently updated ticket.</summary>
public sealed class DashboardRecentTicketResponse
{
    public Guid Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string PriorityName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public string? AssignedToDisplayName { get; init; }
}

/// <summary>Role-filtered ticket analytics for the authenticated caller.</summary>
public sealed class DashboardResponse
{
    public DashboardSummaryResponse Summary { get; init; } = new();
    public IReadOnlyList<DashboardBreakdownItemResponse> StatusBreakdown { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownItemResponse> PriorityBreakdown { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownItemResponse> CategoryBreakdown { get; init; } = [];
    public IReadOnlyList<DashboardTrendPointResponse> MonthlyTrend { get; init; } = [];
    public IReadOnlyList<DashboardRecentTicketResponse> RecentTickets { get; init; } = [];
}
