namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that an operation conflicts with the ticket's current state.</summary>
public sealed class TicketStateConflictException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public TicketStateConflictException() : base("The ticket operation conflicts with its current state.") { }
}
