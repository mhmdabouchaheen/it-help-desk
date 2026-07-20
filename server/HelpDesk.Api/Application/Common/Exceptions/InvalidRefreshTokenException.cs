namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>
/// Represents a refresh token that cannot be accepted for the requested operation.
/// </summary>
public sealed class InvalidRefreshTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRefreshTokenException"/> class.
    /// </summary>
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid or no longer usable.")
    {
    }
}
