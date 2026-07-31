namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that a selected ticket status was not found.</summary>
public sealed class StatusNotFoundException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public StatusNotFoundException() : base("The selected ticket status was not found.") { }
}
