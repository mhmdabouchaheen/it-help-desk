namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents safe priority lookup data.</summary>
public sealed class TicketPriorityResponse
{
    /// <summary>Gets the priority identifier.</summary>
    public short Id { get; init; }
    /// <summary>Gets the priority name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the urgency rank.</summary>
    public short Rank { get; init; }
    /// <summary>Gets whether the priority is selectable.</summary>
    public bool IsActive { get; init; }
}
