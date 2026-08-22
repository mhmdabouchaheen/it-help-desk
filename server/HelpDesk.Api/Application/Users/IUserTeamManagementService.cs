using HelpDesk.Api.Contracts.Users;

namespace HelpDesk.Api.Application.Users;

public interface IUserTeamManagementService
{
    Task<IReadOnlyList<TeamMemberResponse>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<TeamMemberResponse> UpdateManagerAsync(Guid userId, UpdateUserManagerRequest request, CancellationToken cancellationToken = default);
}
