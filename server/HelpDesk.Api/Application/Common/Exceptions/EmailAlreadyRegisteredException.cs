namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Represents an attempt to register an email already in use.</summary>
public sealed class EmailAlreadyRegisteredException : Exception
{
    /// <summary>Initializes the exception with a stable message.</summary>
    public EmailAlreadyRegisteredException() : base("The email address is already registered.") { }
}
