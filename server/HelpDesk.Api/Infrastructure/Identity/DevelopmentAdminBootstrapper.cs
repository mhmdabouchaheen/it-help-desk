using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Identity;

/// <summary>
/// Creates or promotes one explicitly configured local administrator in Development only.
/// </summary>
public sealed class DevelopmentAdminBootstrapper
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly DevelopmentAdminOptions _options;
    private readonly ILogger<DevelopmentAdminBootstrapper> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>Initializes a development administrator bootstrapper.</summary>
    public DevelopmentAdminBootstrapper(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<DevelopmentAdminOptions> options,
        ILogger<DevelopmentAdminBootstrapper> logger,
        IHostEnvironment environment)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>Applies the configured local administrator bootstrap operation once.</summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return;

        if (!_options.Enabled)
            return;

        var email = RequireValue(_options.Email, "email");
        var displayName = RequireValue(_options.DisplayName, "display name");
        var password = RequireValue(_options.Password, "password");
        cancellationToken.ThrowIfCancellationRequested();

        var passwordProbe = new User { UserName = email, Email = email };
        foreach (var validator in _userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(_userManager, passwordProbe, password);
            if (!validation.Succeeded)
                throw ConfigurationError("The configured password does not meet the current Identity password policy.");
        }

        if (!await _roleManager.RoleExistsAsync(AppRoles.Admin))
            throw ConfigurationError($"The required Identity role '{AppRoles.Admin}' does not exist. Apply the existing migrations before starting the API.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var now = DateTime.UtcNow;
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                DisplayName = displayName,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            IdentityResult createResult;
            try
            {
                createResult = await _userManager.CreateAsync(user, password);
            }
            catch (Exception)
            {
                _logger.LogError("Development administrator creation failed during an Identity operation.");
                throw ConfigurationError("Identity could not create the configured development administrator.");
            }

            if (!createResult.Succeeded)
            {
                LogIdentityCodes("create the development administrator", createResult.Errors);
                throw ConfigurationError("Identity could not create the configured development administrator.");
            }
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            _logger.LogInformation("Development administrator bootstrap is already satisfied for user {UserId}.", user.Id);
            return;
        }

        IdentityResult roleResult;
        try
        {
            roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Admin);
        }
        catch (Exception)
        {
            _logger.LogError("Development administrator role assignment failed during an Identity operation for user {UserId}.", user.Id);
            throw ConfigurationError("Identity could not assign the development administrator role.");
        }

        if (!roleResult.Succeeded)
        {
            LogIdentityCodes("assign the development administrator role", roleResult.Errors);
            throw ConfigurationError("Identity could not assign the development administrator role.");
        }

        _logger.LogInformation("Development administrator bootstrap completed for user {UserId}.", user.Id);
    }

    private static string RequireValue(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw ConfigurationError($"DevelopmentAdmin {field} must be configured when bootstrapping is enabled.");

        return value.Trim();
    }

    private void LogIdentityCodes(string operation, IEnumerable<IdentityError> errors)
    {
        var codes = errors.Select(error => error.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => new string(code.Where(char.IsLetterOrDigit).ToArray()))
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _logger.LogError("Identity failed to {Operation}. Error codes: {ErrorCodes}.", operation, codes);
    }

    private static InvalidOperationException ConfigurationError(string detail) =>
        new($"Development administrator bootstrap configuration error: {detail}");
}
