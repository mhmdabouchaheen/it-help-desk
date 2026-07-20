using Microsoft.AspNetCore.Identity;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents an Identity user-role membership with assignment audit metadata.
/// </summary>
public class UserRole : IdentityUserRole<Guid>
{
    /// <summary>
    /// Gets or sets the UTC date and time when the role was assigned to the user.
    /// </summary>
    public DateTime AssignedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who assigned the role, or null for a bootstrap or system assignment.
    /// </summary>
    public Guid? AssignedByUserId { get; set; }
}
