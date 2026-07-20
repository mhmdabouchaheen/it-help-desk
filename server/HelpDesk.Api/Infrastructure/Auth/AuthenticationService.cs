using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace HelpDesk.Api.Infrastructure.Auth;

/// <summary>
/// Orchestrates Identity user operations and token services without HTTP dependencies.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private const string LogoutReason = "User logout";

    private readonly UserManager<User> _userManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthenticationService> _logger;

    /// <summary>Initializes a new authentication application service.</summary>
    public AuthenticationService(
        UserManager<User> userManager,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new UserRegistrationException();
        }

        var email = request.Email.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new EmailAlreadyRegisteredException();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var creationResult = await _userManager.CreateAsync(user, request.Password);

        if (!creationResult.Succeeded)
        {
            LogIdentityFailure("create user", user.Id, creationResult.Errors);

            if (creationResult.Errors.Any(error =>
                    error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                throw new EmailAlreadyRegisteredException();
            }

            throw new UserRegistrationException();
        }

        IdentityResult roleResult;

        try
        {
            roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Employee);
        }
        catch (Exception)
        {
            _logger.LogWarning(
                "Identity operation {Operation} failed for user {UserId}.",
                "assign default role",
                user.Id);
            await CompensateRegistrationAsync(user);
            throw new UserRegistrationException();
        }

        if (!roleResult.Succeeded)
        {
            LogIdentityFailure("assign default role", user.Id, roleResult.Errors);
            await CompensateRegistrationAsync(user);
            throw new UserRegistrationException();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var roles = await _userManager.GetRolesAsync(user);
        return await IssueCredentialsAsync(user, roles, ipAddress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AuthenticationFailedException();
        }

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var roles = await _userManager.GetRolesAsync(user);
        return await IssueCredentialsAsync(user, roles, ipAddress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = await _refreshTokenService.RotateAsync(
            request.RefreshToken,
            ipAddress,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());

        if (user is null)
        {
            throw new UserNotFoundException();
        }

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        var roles = await _userManager.GetRolesAsync(user);

        try
        {
            var accessToken = await _accessTokenService.CreateAccessTokenAsync(
                user,
                roles.ToArray(),
                cancellationToken);
            return MapResponse(user, roles, accessToken, refreshToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Access-token issuance failed after refresh-token rotation for user {UserId}.",
                user.Id);
            throw new AuthenticationTokenIssuanceException(exception);
        }
    }

    /// <inheritdoc />
    public Task LogoutAsync(
        LogoutRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return _refreshTokenService.RevokeAsync(
            request.RefreshToken,
            ipAddress,
            LogoutReason,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new UserNotFoundException();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new CurrentUserResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = [.. roles],
            IsActive = user.IsActive
        };
    }

    private async Task<AuthResponse> IssueCredentialsAsync(
        User user,
        IList<string> roles,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await _accessTokenService.CreateAccessTokenAsync(
                user,
                roles.ToArray(),
                cancellationToken);
            var refreshToken = await _refreshTokenService.CreateAsync(
                user.Id,
                ipAddress,
                cancellationToken);
            return MapResponse(user, roles, accessToken, refreshToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError("Credential issuance failed for user {UserId}.", user.Id);
            throw new AuthenticationTokenIssuanceException(exception);
        }
    }

    private static AuthResponse MapResponse(
        User user,
        IEnumerable<string> roles,
        AccessTokenResult accessToken,
        RefreshTokenResult refreshToken) => new()
    {
        AccessToken = accessToken.Token,
        ExpiresAtUtc = accessToken.ExpiresAtUtc,
        RefreshToken = refreshToken.Token,
        RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
        UserId = user.Id,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        Roles = [.. roles]
    };

    private void LogIdentityFailure(
        string operation,
        Guid userId,
        IEnumerable<IdentityError> errors)
    {
        _logger.LogWarning(
            "Identity operation {Operation} failed for user {UserId} with codes {ErrorCodes}.",
            operation,
            userId,
            string.Join(',', errors.Select(error => error.Code)));
    }

    private async Task CompensateRegistrationAsync(User user)
    {
        try
        {
            var deletionResult = await _userManager.DeleteAsync(user);

            if (!deletionResult.Succeeded)
            {
                LogIdentityFailure("compensate registration", user.Id, deletionResult.Errors);
            }
        }
        catch (Exception)
        {
            _logger.LogError(
                "Registration compensation failed for user {UserId}.",
                user.Id);
        }
    }
}
