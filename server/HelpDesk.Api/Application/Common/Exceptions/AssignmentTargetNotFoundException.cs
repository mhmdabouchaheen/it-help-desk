namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that the selected assignment target was not found.</summary>
public sealed class AssignmentTargetNotFoundException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public AssignmentTargetNotFoundException() : base("The selected assignment target was not found.") { }
}
