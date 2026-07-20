namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Represents a login attempt with invalid credentials.</summary>
public sealed class AuthenticationFailedException : Exception
{
    /// <summary>Initializes the exception with a stable, non-disclosing message.</summary>
    public AuthenticationFailedException() : base("The supplied credentials are invalid.") { }
}
