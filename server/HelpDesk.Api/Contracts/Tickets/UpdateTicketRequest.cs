using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Defines editable basic ticket details.</summary>
public sealed class UpdateTicketRequest
{
    /// <summary>Gets the concise ticket title.</summary>
    [Required(AllowEmptyStrings = false), MaxLength(250)]
    public string Title { get; init; } = string.Empty;
    /// <summary>Gets the detailed issue description.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Description { get; init; } = string.Empty;
    /// <summary>Gets the selected category identifier.</summary>
    [Range(1, short.MaxValue)]
    public short CategoryId { get; init; }
    /// <summary>Gets the selected priority identifier.</summary>
    [Range(1, short.MaxValue)]
    public short PriorityId { get; init; }
}
