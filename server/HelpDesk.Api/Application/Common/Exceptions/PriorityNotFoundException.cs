namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that a selected ticket priority was not found.</summary>
public sealed class PriorityNotFoundException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public PriorityNotFoundException() : base("The selected ticket priority was not found.") { }
}
