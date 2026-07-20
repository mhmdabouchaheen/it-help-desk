namespace HelpDesk.Api.Application.Authorization;

/// <summary>
/// Defines stable names for the application's authorization policies.
/// </summary>
public static class AppPolicies
{
    /// <summary>The policy requiring an active authenticated principal.</summary>
    public const string AuthenticatedUser = "AuthenticatedUser";

    /// <summary>The policy restricted to administrators.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>The policy for administrators and IT support agents.</summary>
    public const string SupportStaff = "SupportStaff";

    /// <summary>The policy for administrators and managers.</summary>
    public const string Management = "Management";
}
