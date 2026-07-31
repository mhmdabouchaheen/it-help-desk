namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that a selected ticket category was not found.</summary>
public sealed class CategoryNotFoundException : Exception
{
    /// <summary>Initializes the exception with a stable, non-sensitive message.</summary>
    public CategoryNotFoundException() : base("The selected ticket category was not found.") { }
}
