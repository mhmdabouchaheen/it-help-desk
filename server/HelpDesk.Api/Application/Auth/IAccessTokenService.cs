using HelpDesk.Api.Entities;

namespace HelpDesk.Api.Application.Auth;

/// <summary>
/// Defines access-token creation for an application user and their assigned roles.
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Creates a signed access token for the supplied user and roles.
    /// </summary>
    /// <param name="user">The user represented by the token.</param>
    /// <param name="roles">The role names to include as claims.</param>
    /// <param name="cancellationToken">A token that can cancel token creation.</param>
    /// <returns>The serialized token and its UTC expiration time.</returns>
    Task<AccessTokenResult> CreateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}
