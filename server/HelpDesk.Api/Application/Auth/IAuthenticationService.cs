using HelpDesk.Api.Contracts.Auth;

namespace HelpDesk.Api.Application.Auth;

/// <summary>
/// Defines authentication application operations independently of HTTP transport concerns.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>Registers a user and issues their initial credentials.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Validates credentials and issues new credentials.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Rotates a refresh token and issues a new access token.</summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Revokes the refresh token for one session.</summary>
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Gets safe application identity details for a user.</summary>
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}
