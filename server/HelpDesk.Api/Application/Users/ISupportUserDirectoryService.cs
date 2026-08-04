using HelpDesk.Api.Contracts.Users;

namespace HelpDesk.Api.Application.Users;

/// <summary>Provides the restricted directory used to select ticket assignees.</summary>
public interface ISupportUserDirectoryService
{
    /// <summary>Gets active users who are valid support assignment targets.</summary>
    Task<IReadOnlyList<SupportUserResponse>> GetEligibleSupportUsersAsync(
        CancellationToken cancellationToken = default);
}
