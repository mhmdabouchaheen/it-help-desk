using System.Data;
using System.Security.Cryptography;
using System.Text;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace HelpDesk.Api.Infrastructure.Auth;

/// <summary>
/// Creates, rotates, and revokes refresh tokens while persisting only SHA-256 hashes.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const string ReuseRevocationReason = "Suspected refresh token reuse.";

    private readonly ApplicationDbContext _dbContext;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefreshTokenService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenService"/> class.
    /// </summary>
    public RefreshTokenService(
        ApplicationDbContext dbContext,
        IOptions<JwtOptions> options,
        TimeProvider timeProvider,
        ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RefreshTokenResult> CreateAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var normalizedIpAddress = NormalizeIpAddress(ipAddress);
        var createdToken = CreateToken(userId, normalizedIpAddress);

        _dbContext.RefreshTokens.Add(createdToken.Entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created refresh token record {TokenId} for user {UserId}.",
            createdToken.Entity.Id,
            userId);

        return ToResult(createdToken);
    }

    /// <inheritdoc />
    public async Task<RefreshTokenResult> RotateAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateTokenInput(refreshToken);
        var normalizedIpAddress = NormalizeIpAddress(ipAddress);
        var tokenHash = HashToken(refreshToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var existingToken = await _dbContext.RefreshTokens.SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);

            if (existingToken is null ||
                existingToken.ExpiresAtUtc <= now ||
                existingToken.RevokedAtUtc is not null)
            {
                throw new InvalidRefreshTokenException();
            }

            if (existingToken.UsedAtUtc is not null)
            {
                if (existingToken.ReplacedByTokenId is not null)
                {
                    await RevokeActiveTokensAsync(
                        existingToken.UserId,
                        normalizedIpAddress,
                        ReuseRevocationReason,
                        now,
                        cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogWarning(
                        "Detected reuse of refresh token record {TokenId} for user {UserId}; active refresh tokens were revoked.",
                        existingToken.Id,
                        existingToken.UserId);

                    throw new RefreshTokenReuseDetectedException();
                }

                throw new InvalidRefreshTokenException();
            }

            var replacement = CreateToken(existingToken.UserId, normalizedIpAddress, now);
            existingToken.UsedAtUtc = now;
            existingToken.ReplacedByTokenId = replacement.Entity.Id;
            _dbContext.RefreshTokens.Add(replacement.Entity);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Rotated refresh token record {TokenId} to {ReplacementTokenId} for user {UserId}.",
                existingToken.Id,
                replacement.Entity.Id,
                existingToken.UserId);

            return ToResult(replacement);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidRefreshTokenException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidRefreshTokenException();
        }
    }

    /// <inheritdoc />
    public async Task RevokeAsync(
        string refreshToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateTokenInput(refreshToken);
        var normalizedIpAddress = NormalizeIpAddress(ipAddress);
        var normalizedReason = NormalizeReason(reason);
        var tokenHash = HashToken(refreshToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var existingToken = await _dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

        if (existingToken is null)
        {
            throw new InvalidRefreshTokenException();
        }

        if (existingToken.UsedAtUtc is not null ||
            existingToken.RevokedAtUtc is not null ||
            existingToken.ExpiresAtUtc <= now)
        {
            return;
        }

        RevokeToken(existingToken, normalizedIpAddress, normalizedReason, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked refresh token record {TokenId} for user {UserId}.",
            existingToken.Id,
            existingToken.UserId);
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        var normalizedIpAddress = NormalizeIpAddress(ipAddress);
        var normalizedReason = NormalizeReason(reason);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var revokedCount = await RevokeActiveTokensAsync(
            userId,
            normalizedIpAddress,
            normalizedReason,
            now,
            cancellationToken);

        if (revokedCount == 0)
        {
            return;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked {TokenCount} active refresh token records for user {UserId}.",
            revokedCount,
            userId);
    }

    private CreatedToken CreateToken(
        Guid userId,
        string? ipAddress,
        DateTime? createdAtUtc = null)
    {
        var plaintextToken = GenerateToken();
        var createdAt = createdAtUtc ?? _timeProvider.GetUtcNow().UtcDateTime;
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plaintextToken),
            CreatedAtUtc = createdAt,
            ExpiresAtUtc = createdAt.AddDays(_options.RefreshTokenLifetimeDays),
            CreatedByIpAddress = ipAddress
        };

        return new CreatedToken(entity, plaintextToken);
    }

    private async Task<int> RevokeActiveTokensAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.UsedAtUtc == null &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            RevokeToken(activeToken, ipAddress, reason, now);
        }

        return activeTokens.Count;
    }

    private static void RevokeToken(
        RefreshToken refreshToken,
        string? ipAddress,
        string reason,
        DateTime revokedAtUtc)
    {
        refreshToken.RevokedAtUtc = revokedAtUtc;
        refreshToken.RevokedByIpAddress = ipAddress;
        refreshToken.RevocationReason = reason;
    }

    private static string GenerateToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        try
        {
            return Base64UrlEncoder.Encode(randomBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(randomBytes);
        }
    }

    private static string HashToken(string plaintextToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(plaintextToken);
        byte[]? hashBytes = null;

        try
        {
            hashBytes = SHA256.HashData(tokenBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenBytes);

            if (hashBytes is not null)
            {
                CryptographicOperations.ZeroMemory(hashBytes);
            }
        }
    }

    private static void ValidateTokenInput(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidRefreshTokenException();
        }
    }

    private static string? NormalizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var normalizedIpAddress = ipAddress.Trim();

        if (normalizedIpAddress.Length > 45)
        {
            throw new ArgumentException("The IP address must not exceed 45 characters.", nameof(ipAddress));
        }

        return normalizedIpAddress;
    }

    private static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A revocation reason is required.", nameof(reason));
        }

        var normalizedReason = reason.Trim();

        if (normalizedReason.Length > 500)
        {
            throw new ArgumentException("The revocation reason must not exceed 500 characters.", nameof(reason));
        }

        return normalizedReason;
    }

    private static RefreshTokenResult ToResult(CreatedToken createdToken) => new()
    {
        Token = createdToken.PlaintextToken,
        TokenId = createdToken.Entity.Id,
        UserId = createdToken.Entity.UserId,
        ExpiresAtUtc = createdToken.Entity.ExpiresAtUtc
    };

    private sealed record CreatedToken(RefreshToken Entity, string PlaintextToken);
}
