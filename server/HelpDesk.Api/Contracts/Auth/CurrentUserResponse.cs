namespace HelpDesk.Api.Contracts.Auth;

/// <summary>
/// Represents the application identity of the current user.
/// </summary>
public class CurrentUserResponse
{
    /// <summary>Gets or sets the user's identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets a new list containing the user's role names.</summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the user is active.</summary>
    public bool IsActive { get; set; }
}
