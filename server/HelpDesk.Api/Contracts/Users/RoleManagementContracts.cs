namespace HelpDesk.Api.Contracts.Users;

public sealed class RoleManagedUserResponse
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public Guid? ManagerUserId { get; init; }
    public string? ManagerDisplayName { get; init; }
}

public sealed class UpdateUserRolesRequest
{
    public IReadOnlyList<string>? Roles { get; init; }
}
