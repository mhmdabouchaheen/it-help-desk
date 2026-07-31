using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Tickets;

/// <summary>Defines a future operation that adds a ticket comment.</summary>
public sealed class AddTicketCommentRequest
{
    /// <summary>Gets the comment body.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Content { get; init; } = string.Empty;
    /// <summary>Gets whether the future service should use the domain's Internal visibility.</summary>
    public bool IsInternal { get; init; }
}
