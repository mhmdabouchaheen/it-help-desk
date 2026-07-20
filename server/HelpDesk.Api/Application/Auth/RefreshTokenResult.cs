namespace HelpDesk.Api.Application.Auth;

/// <summary>
/// Represents a newly created plaintext refresh token and its persistence metadata.
/// </summary>
public sealed record RefreshTokenResult
{
    /// <summary>
    /// Gets the plaintext refresh token returned once to the caller.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets the identifier of the persisted refresh-token record.
    /// </summary>
    public Guid TokenId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who owns the refresh token.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the UTC date and time when the refresh token expires.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }
}
