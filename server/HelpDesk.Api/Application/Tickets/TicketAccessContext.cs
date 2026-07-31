namespace HelpDesk.Api.Application.Tickets;

/// <summary>Provides validated caller identity data to future ticket application services.</summary>
public sealed class TicketAccessContext
{
    /// <summary>Gets the authenticated user's identifier.</summary>
    public Guid UserId { get; init; }
    /// <summary>Gets roles sourced from validated JWT role claims.</summary>
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
