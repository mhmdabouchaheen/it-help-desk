using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Defines a future ticket status transition.</summary>
public sealed class ChangeTicketStatusRequest
{
    /// <summary>Gets the target status identifier.</summary>
    [Range(1, short.MaxValue)]
    public short StatusId { get; init; }
    /// <summary>Gets an optional transition reason.</summary>
    [MaxLength(1000)]
    public string? Note { get; init; }
}
