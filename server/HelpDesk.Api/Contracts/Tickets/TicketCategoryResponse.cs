namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents safe category lookup data.</summary>
public sealed class TicketCategoryResponse
{
    /// <summary>Gets the category identifier.</summary>
    public short Id { get; init; }
    /// <summary>Gets the category name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets the display order.</summary>
    public short SortOrder { get; init; }
    /// <summary>Gets whether the category is selectable.</summary>
    public bool IsActive { get; init; }
}
