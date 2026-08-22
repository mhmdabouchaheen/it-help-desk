using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using HelpDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Users;

public sealed class UserTeamManagementService(ApplicationDbContext db) : IUserTeamManagementService
{
    public async Task<IReadOnlyList<TeamMemberResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await db.Users.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(cancellationToken);
        var roleRows = await (from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            select new { userRole.UserId, Role = role.Name! }).ToListAsync(cancellationToken);
        var roles = roleRows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.Select(y => y.Role).ToList());
        var names = users.ToDictionary(x => x.Id, x => x.DisplayName);
        return users.Select(user => new TeamMemberResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Roles = roles.GetValueOrDefault(user.Id) ?? [],
            ManagerUserId = user.ManagerUserId,
            ManagerDisplayName = user.ManagerUserId.HasValue ? names.GetValueOrDefault(user.ManagerUserId.Value) : null
        }).ToArray();
    }

    public async Task<TeamMemberResponse> UpdateManagerAsync(Guid userId, UpdateUserManagerRequest request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new TeamManagementValidationException("A user is required.");
        ArgumentNullException.ThrowIfNull(request);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new TeamManagementValidationException("The selected user does not exist.");

        if (request.ManagerUserId == userId)
            throw new TeamManagementValidationException("A user cannot manage themselves.");

        if (request.ManagerUserId.HasValue)
        {
            var employeeRole = await (from userRole in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == userId && role.Name == AppRoles.Employee
                select userRole.UserId).AnyAsync(cancellationToken);
            if (!employeeRole)
                throw new TeamManagementValidationException("Only a user with the Employee role can be assigned to a manager.");

            var managerId = request.ManagerUserId.Value;
            var manager = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == managerId, cancellationToken);
            if (manager is null || !manager.IsActive)
                throw new TeamManagementValidationException("The selected manager must be an active user.");
            var managerRole = await (from userRole in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == managerId && role.Name == AppRoles.Manager
                select userRole.UserId).AnyAsync(cancellationToken);
            if (!managerRole)
                throw new TeamManagementValidationException("The selected user does not have the Manager role.");

            var ancestorId = manager.ManagerUserId;
            var visited = new HashSet<Guid> { managerId };
            while (ancestorId.HasValue)
            {
                if (ancestorId.Value == userId)
                    throw new TeamManagementValidationException("The manager assignment would create a cycle.");
                if (!visited.Add(ancestorId.Value))
                    throw new TeamManagementValidationException("The existing manager hierarchy contains a cycle.");
                ancestorId = await db.Users.AsNoTracking().Where(x => x.Id == ancestorId.Value)
                    .Select(x => x.ManagerUserId).SingleOrDefaultAsync(cancellationToken);
            }
        }

        user.ManagerUserId = request.ManagerUserId;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (await GetUsersAsync(cancellationToken)).Single(x => x.UserId == userId);
    }
}
