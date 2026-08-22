using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Users;

public sealed class TeamMemberResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = [];
    public Guid? ManagerUserId { get; set; }
    public string? ManagerDisplayName { get; set; }
}

public sealed class UpdateUserManagerRequest
{
    public Guid? ManagerUserId { get; set; }
}
