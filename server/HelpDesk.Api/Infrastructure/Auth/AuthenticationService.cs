using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Audit;
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
    private const string PasswordResetReason = "Password reset";
    private const string PasswordChangeReason = "Password changed";

    private readonly UserManager<User> _userManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IActivityLogService? _activityLogs;
    private readonly IPasswordResetEmailSender? _passwordResetEmailSender;

    /// <summary>Initializes a new authentication application service.</summary>
    public AuthenticationService(
        UserManager<User> userManager,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthenticationService> logger,
        IActivityLogService? activityLogs = null,
        IPasswordResetEmailSender? passwordResetEmailSender = null)
    {
        _userManager = userManager;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
        _activityLogs = activityLogs;
        _passwordResetEmailSender = passwordResetEmailSender;
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
        var response=await IssueCredentialsAsync(user, roles, ipAddress, cancellationToken);
        await TryAuditAsync(user.Id,ActivityActions.UserRegistered,cancellationToken);
        return response;
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
        var response=await IssueCredentialsAsync(user, roles, ipAddress, cancellationToken);
        await TryAuditAsync(user.Id,ActivityActions.UserLoggedIn,cancellationToken);
        return response;
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

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || _passwordResetEmailSender is null) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        try
        {
            await _passwordResetEmailSender.SendPasswordResetAsync(user.Email ?? request.Email.Trim(), token, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            _logger.LogError("Password-reset email delivery failed for user {UserId}.", user.Id);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive) throw new InvalidPasswordResetException();

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            LogIdentityFailure("reset password", user.Id, result.Errors);
            throw new InvalidPasswordResetException();
        }

        await _refreshTokenService.RevokeAllForUserAsync(user.Id, ipAddress, PasswordResetReason, cancellationToken);
    }

    public async Task<CurrentUserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindUserAsync(userId, cancellationToken);
        var displayName = request.DisplayName.Trim();
        if (displayName.Length == 0) throw new ProfileValidationException("Display name is required.");
        user.DisplayName = displayName;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            LogIdentityFailure("update profile", user.Id, result.Errors);
            throw new ProfileValidationException();
        }
        return await GetCurrentUserAsync(user.Id, cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindUserAsync(userId, cancellationToken);
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            LogIdentityFailure("change password", user.Id, result.Errors);
            throw new ProfileValidationException("The current password is incorrect or the new password is invalid.");
        }
        await _refreshTokenService.RevokeAllForUserAsync(user.Id, ipAddress, PasswordChangeReason, cancellationToken);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user identifier is required.", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();
        return await _userManager.FindByIdAsync(userId.ToString()) ?? throw new UserNotFoundException();
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

    private async Task TryAuditAsync(Guid userId,string action,CancellationToken token)
    {
        if(_activityLogs is null)return;
        try{await _activityLogs.WriteAsync(userId,action,ActivityEntityTypes.User,userId.ToString(),
            new Dictionary<string,string?>{{"userId",userId.ToString()}},token);}
        catch(Exception exception){_logger.LogWarning(exception,"Activity logging failed after authentication action {Action} for user {UserId}.",action,userId);}
    }
}
