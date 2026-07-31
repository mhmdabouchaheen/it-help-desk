namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents safe status lookup data.</summary>
public sealed class TicketStatusResponse
{
    /// <summary>Gets the status identifier.</summary>
    public short Id { get; init; }
    /// <summary>Gets the status name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the display order.</summary>
    public short SortOrder { get; init; }
    /// <summary>Gets whether the status is terminal.</summary>
    public bool IsTerminal { get; init; }
    /// <summary>Gets whether the status is selectable.</summary>
    public bool IsActive { get; init; }
}
