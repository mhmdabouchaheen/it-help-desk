namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>
/// Represents detected reuse of a refresh token that was already rotated.
/// </summary>
public sealed class RefreshTokenReuseDetectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenReuseDetectedException"/> class.
    /// </summary>
    public RefreshTokenReuseDetectedException()
        : base("Refresh token reuse was detected. Active sessions have been revoked.")
    {
    }
}
