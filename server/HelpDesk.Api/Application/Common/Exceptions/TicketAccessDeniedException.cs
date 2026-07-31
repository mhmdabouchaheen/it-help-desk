namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that the caller may not perform a ticket operation.</summary>
public sealed class TicketAccessDeniedException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public TicketAccessDeniedException() : base("Access to the requested ticket operation was denied.") { }
}
