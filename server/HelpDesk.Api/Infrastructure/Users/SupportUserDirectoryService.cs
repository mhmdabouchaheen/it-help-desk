using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using HelpDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Users;

/// <summary>Reads the assignment-only support-user directory from Identity tables.</summary>
public sealed class SupportUserDirectoryService(ApplicationDbContext dbContext)
    : ISupportUserDirectoryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SupportUserResponse>> GetEligibleSupportUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var eligible =
            from user in dbContext.Users.AsNoTracking()
            where user.IsActive
            let roles = (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == user.Id && AppRoles.SupportStaff.Contains(role.Name!)
                orderby role.Name
                select role.Name!).ToList()
            where roles.Count != 0
            orderby user.DisplayName, user.Id
            select new SupportUserResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Roles = roles
            };

        return await eligible.ToListAsync(cancellationToken);
    }
}
