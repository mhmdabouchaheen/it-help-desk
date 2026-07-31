using System.ComponentModel.DataAnnotations;
using HelpDesk.Api.Contracts.Common.Validation;

namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Defines a future ticket assignment operation.</summary>
public sealed class AssignTicketRequest
{
    /// <summary>Gets the target user's identifier.</summary>
    [NotEmptyGuid]
    public Guid AssignedToUserId { get; init; }
    /// <summary>Gets optional assignment context supported by the domain model.</summary>
    [MaxLength(500)]
    public string? Note { get; init; }
}
