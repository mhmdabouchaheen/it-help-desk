namespace HelpDesk.Api.Application.Auth;

/// <summary>
/// Defines secure creation, rotation, and revocation operations for refresh tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a refresh token for a user.
    /// </summary>
    Task<RefreshTokenResult> CreateAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token and returns its single-use replacement.
    /// </summary>
    Task<RefreshTokenResult> RotateAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token if it is currently active.
    /// </summary>
    Task RevokeAsync(
        string refreshToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every active, unexpired refresh token belonging to a user.
    /// </summary>
    Task RevokeAllForUserAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default);
}
