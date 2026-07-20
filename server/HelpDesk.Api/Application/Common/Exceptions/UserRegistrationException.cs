namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Represents a registration failure that is safe to expose generically.</summary>
public sealed class UserRegistrationException : Exception
{
    /// <summary>Initializes the exception with a stable message.</summary>
    public UserRegistrationException() : base("The user could not be registered.") { }
}
