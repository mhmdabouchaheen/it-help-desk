namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Represents a failure while issuing authentication credentials.</summary>
public sealed class AuthenticationTokenIssuanceException : Exception
{
    /// <summary>Initializes the exception with a stable message and internal cause.</summary>
    public AuthenticationTokenIssuanceException(Exception innerException)
        : base("Authentication credentials could not be issued.", innerException) { }
}
