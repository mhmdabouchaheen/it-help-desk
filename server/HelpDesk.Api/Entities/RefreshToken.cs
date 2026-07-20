using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Entities;

/// <summary>
/// Represents persisted security and lifecycle data for a hashed refresh token.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the refresh token record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns the refresh token.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the lowercase hexadecimal SHA-256 hash of the refresh token.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the refresh token was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the refresh token expires.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the refresh token was used, or null if unused.
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the refresh token was revoked, or null if active.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the replacement refresh token, or null if not rotated.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Gets or sets the optional IP address from which the refresh token was created.
    /// </summary>
    [MaxLength(45)]
    public string? CreatedByIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the optional IP address from which the refresh token was revoked.
    /// </summary>
    [MaxLength(45)]
    public string? RevokedByIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the optional reason why the refresh token was revoked.
    /// </summary>
    [MaxLength(500)]
    public string? RevocationReason { get; set; }
}
