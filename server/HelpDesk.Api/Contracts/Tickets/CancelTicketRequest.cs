using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Represents a request to permanently cancel a ticket.</summary>
public sealed class CancelTicketRequest
{
    /// <summary>Gets or sets optional cancellation context. The reason is not persisted in this version.</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
