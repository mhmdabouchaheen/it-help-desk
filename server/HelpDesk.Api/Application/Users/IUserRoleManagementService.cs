using HelpDesk.Api.Contracts.Users;

namespace HelpDesk.Api.Application.Users;

public interface IUserRoleManagementService
{
    Task<IReadOnlyList<RoleManagedUserResponse>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<RoleManagedUserResponse> UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request,
        Guid actingAdminUserId, string? ipAddress, CancellationToken cancellationToken = default);
}
