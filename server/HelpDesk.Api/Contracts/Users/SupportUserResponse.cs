namespace HelpDesk.Api.Contracts.Users;

/// <summary>Represents a safe, assignment-eligible support user.</summary>
public sealed class SupportUserResponse
{
    /// <summary>Gets the user's identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the user's display name.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the assignment-relevant roles held by the user.</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
