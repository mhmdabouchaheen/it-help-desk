using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HelpDesk.Api.Infrastructure.Users;

public sealed class UserRoleManagementService(UserManager<User> users, ApplicationDbContext db,
    IRefreshTokenService refreshTokens, IActivityLogService activityLogs,
    ILogger<UserRoleManagementService> logger) : IUserRoleManagementService
{
    public async Task<IReadOnlyList<RoleManagedUserResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await users.Users.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(cancellationToken);
        var names = rows.ToDictionary(x => x.Id, x => x.DisplayName);
        var result = new List<RoleManagedUserResponse>(rows.Count);
        foreach (var user in rows)
            result.Add(Map(user, await users.GetRolesAsync(user), names));
        return result;
    }

    public async Task<RoleManagedUserResponse> UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request,
        Guid actingAdminUserId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || actingAdminUserId == Guid.Empty || request?.Roles is null || request.Roles.Count == 0)
            throw new RoleManagementValidationException("At least one valid application role is required.");
        var desired = request.Roles.ToArray();
        if (desired.Any(string.IsNullOrWhiteSpace) || desired.Distinct(StringComparer.Ordinal).Count() != desired.Length ||
            desired.Any(role => !AppRoles.All.Contains(role, StringComparer.Ordinal)))
            throw new RoleManagementValidationException("Roles must be unique existing application roles.");

        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new RoleManagementValidationException("The selected user does not exist.");
        var previous = (await users.GetRolesAsync(user)).ToArray();
        var removing = previous.Except(desired, StringComparer.Ordinal).ToArray();
        var adding = desired.Except(previous, StringComparer.Ordinal).ToArray();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (removing.Contains(AppRoles.Manager, StringComparer.Ordinal) &&
            await db.Users.AsNoTracking().AnyAsync(x => x.ManagerUserId == userId, cancellationToken))
            throw new RoleManagementValidationException("This user is currently assigned as a Manager. Reassign or remove their direct reports first.");
        if (user.IsActive && removing.Contains(AppRoles.Admin, StringComparer.Ordinal))
        {
            var anotherActiveAdmin = await (from candidate in db.Users.AsNoTracking()
                join userRole in db.UserRoles.AsNoTracking() on candidate.Id equals userRole.UserId
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where candidate.IsActive && candidate.Id != userId && role.Name == AppRoles.Admin
                select candidate.Id).AnyAsync(cancellationToken);
            if (!anotherActiveAdmin)
                throw new RoleManagementValidationException("The final active Admin cannot lose the Admin role.");
        }
        if (adding.Length == 0 && removing.Length == 0)
            return await MapAsync(user, cancellationToken);

        if (adding.Length > 0) EnsureSucceeded(await users.AddToRolesAsync(user, adding));
        if (removing.Length > 0) EnsureSucceeded(await users.RemoveFromRolesAsync(user, removing));
        await refreshTokens.RevokeAllForUserAsync(user.Id, ipAddress, "Roles changed", cancellationToken);
        try
        {
            await activityLogs.WriteAsync(actingAdminUserId, ActivityActions.UserRolesChanged,
                ActivityEntityTypes.User, user.Id.ToString(), new Dictionary<string, string?>
                { ["previousRoles"] = string.Join(", ", previous), ["newRoles"] = string.Join(", ", desired) }, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Role change audit failed for user {UserId}.", user.Id);
        }
        await transaction.CommitAsync(cancellationToken);
        return await MapAsync(user, cancellationToken);
    }

    private async Task<RoleManagedUserResponse> MapAsync(User user, CancellationToken cancellationToken)
    {
        var managerName = user.ManagerUserId.HasValue
            ? await db.Users.AsNoTracking().Where(x => x.Id == user.ManagerUserId).Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken)
            : null;
        return Map(user, await users.GetRolesAsync(user), managerName is null ? [] : new Dictionary<Guid, string>{{user.ManagerUserId!.Value, managerName}});
    }

    private static RoleManagedUserResponse Map(User user, IEnumerable<string> roles, IReadOnlyDictionary<Guid, string> names) => new()
    {
        UserId=user.Id, DisplayName=user.DisplayName, Email=user.Email ?? string.Empty, IsActive=user.IsActive,
        Roles=roles.OrderBy(role => Array.IndexOf(AppRoles.All.ToArray(), role)).ToArray(), ManagerUserId=user.ManagerUserId,
        ManagerDisplayName=user.ManagerUserId.HasValue ? names.GetValueOrDefault(user.ManagerUserId.Value) : null
    };

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new RoleManagementValidationException("The requested role change could not be completed.");
    }
}
