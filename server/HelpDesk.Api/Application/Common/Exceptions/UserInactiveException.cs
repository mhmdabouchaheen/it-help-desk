namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Represents an authentication operation rejected for an inactive user.</summary>
public sealed class UserInactiveException : Exception
{
    /// <summary>Initializes the exception with a stable message.</summary>
    public UserInactiveException() : base("The user account is inactive.") { }
}
