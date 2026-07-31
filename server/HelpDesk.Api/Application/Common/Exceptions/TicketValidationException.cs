namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that ticket input violates an application rule.</summary>
public sealed class TicketValidationException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public TicketValidationException() : base("The ticket request is invalid.") { }
}
