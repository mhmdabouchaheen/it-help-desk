namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Represents a requested current user that no longer exists.</summary>
public sealed class UserNotFoundException : Exception
{
    /// <summary>Initializes the exception with a stable message.</summary>
    public UserNotFoundException() : base("The requested user was not found.") { }
}
