using System.Collections.ObjectModel;

namespace HelpDesk.Api.Application.Authorization;

/// <summary>
/// Defines the exact seeded Identity role names and immutable reusable role groups.
/// </summary>
public static class AppRoles
{
    /// <summary>The administrator role.</summary>
    public const string Admin = "Admin";

    /// <summary>The IT support-agent role.</summary>
    public const string ItSupportAgent = "IT Support Agent";

    /// <summary>The employee role.</summary>
    public const string Employee = "Employee";

    /// <summary>The manager role.</summary>
    public const string Manager = "Manager";

    /// <summary>Gets every application role.</summary>
    public static IReadOnlyCollection<string> All { get; } =
        new ReadOnlyCollection<string>([Admin, ItSupportAgent, Employee, Manager]);

    /// <summary>Gets roles that may perform support-staff operations.</summary>
    public static IReadOnlyCollection<string> SupportStaff { get; } =
        new ReadOnlyCollection<string>([Admin, ItSupportAgent]);

    /// <summary>Gets roles that may perform management operations.</summary>
    public static IReadOnlyCollection<string> Management { get; } =
        new ReadOnlyCollection<string>([Admin, Manager]);
}
