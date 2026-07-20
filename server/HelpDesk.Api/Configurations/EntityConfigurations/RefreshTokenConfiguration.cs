using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", table =>
        {
            table.HasCheckConstraint(
                "CK_RefreshTokens_ExpiresAfterCreated",
                "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
            table.HasCheckConstraint(
                "CK_RefreshTokens_UsedAfterCreated",
                "\"UsedAtUtc\" IS NULL OR \"UsedAtUtc\" >= \"CreatedAtUtc\"");
            table.HasCheckConstraint(
                "CK_RefreshTokens_RevokedAfterCreated",
                "\"RevokedAtUtc\" IS NULL OR \"RevokedAtUtc\" >= \"CreatedAtUtc\"");
            table.HasCheckConstraint(
                "CK_RefreshTokens_TokenHash_Format",
                "\"TokenHash\" ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint(
                "CK_RefreshTokens_ReplacementDiffers",
                "\"ReplacedByTokenId\" IS NULL OR \"ReplacedByTokenId\" <> \"Id\"");
        });
        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id).ValueGeneratedOnAdd();
        builder.Property(refreshToken => refreshToken.UserId).IsRequired();
        builder.Property(refreshToken => refreshToken.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(refreshToken => refreshToken.CreatedAtUtc).IsRequired();
        builder.Property(refreshToken => refreshToken.ExpiresAtUtc).IsRequired();
        builder.Property(refreshToken => refreshToken.UsedAtUtc).IsRequired(false);
        builder.Property(refreshToken => refreshToken.RevokedAtUtc).IsRequired(false);
        builder.Property(refreshToken => refreshToken.ReplacedByTokenId).IsRequired(false);
        builder.Property(refreshToken => refreshToken.CreatedByIpAddress).HasMaxLength(45);
        builder.Property(refreshToken => refreshToken.RevokedByIpAddress).HasMaxLength(45);
        builder.Property(refreshToken => refreshToken.RevocationReason).HasMaxLength(500);

        builder.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();
        builder.HasIndex(refreshToken => new { refreshToken.UserId, refreshToken.ExpiresAtUtc });
        builder.HasIndex(refreshToken => new { refreshToken.UserId, refreshToken.CreatedAtUtc });
        builder.HasIndex(
                refreshToken => new { refreshToken.UserId, refreshToken.ExpiresAtUtc },
                "IX_RefreshTokens_UserId_ExpiresAtUtc_Active")
            .HasFilter("\"UsedAtUtc\" IS NULL AND \"RevokedAtUtc\" IS NULL");
        builder.HasIndex(refreshToken => refreshToken.ReplacedByTokenId)
            .HasFilter("\"ReplacedByTokenId\" IS NOT NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
