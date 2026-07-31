namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that a requested ticket could not be found.</summary>
public sealed class TicketNotFoundException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public TicketNotFoundException() : base("The requested ticket was not found.") { }
}
