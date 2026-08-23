namespace HelpDesk.Api.Contracts.Reports;

public sealed class TicketReportSummaryResponse
{
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public int TerminalTickets { get; init; }
    public int CancelledTickets { get; init; }
    public int AssignedTickets { get; init; }
    public int UnassignedTickets { get; init; }
    /// <summary>Average minutes from ticket creation to authoritative resolution, or null when no valid resolved tickets match.</summary>
    public double? AverageResolutionMinutes { get; init; }
}

public sealed class TicketReportBreakdownResponse
{
    public short Id { get; init; }
    public required string Name { get; init; }
    public int Count { get; init; }
}

public sealed class TicketReportTrendResponse
{
    public DateTime PeriodStartUtc { get; init; }
    public int CreatedCount { get; init; }
    public int ClosedCount { get; init; }
}

public sealed class AgentWorkloadResponse
{
    public Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public int ActiveTicketCount { get; init; }
}

public sealed class TicketReportResponse
{
    public TicketReportSummaryResponse Summary { get; init; } = new();
    public IReadOnlyList<TicketReportBreakdownResponse> StatusBreakdown { get; init; } = [];
    public IReadOnlyList<TicketReportBreakdownResponse> PriorityBreakdown { get; init; } = [];
    public IReadOnlyList<TicketReportBreakdownResponse> CategoryBreakdown { get; init; } = [];
    public IReadOnlyList<TicketReportTrendResponse> Trend { get; init; } = [];
    public IReadOnlyList<AgentWorkloadResponse> AgentWorkload { get; init; } = [];
}
