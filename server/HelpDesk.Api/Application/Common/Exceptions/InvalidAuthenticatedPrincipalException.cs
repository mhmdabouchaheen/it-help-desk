namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that an authenticated principal lacks valid identity data.</summary>
public sealed class InvalidAuthenticatedPrincipalException : Exception
{
    public InvalidAuthenticatedPrincipalException()
        : base("The authenticated identity is invalid.") { }
}
