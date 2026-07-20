namespace HelpDesk.Api.Application.Auth;

/// <summary>
/// Represents a generated JWT access token and its UTC expiration time.
/// </summary>
public sealed record AccessTokenResult
{
    /// <summary>
    /// Gets the serialized access token.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC date and time when the access token expires.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }
}
