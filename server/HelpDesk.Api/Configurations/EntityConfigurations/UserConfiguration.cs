using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint(
                "CK_Users_DeactivatedWhenInactive",
                "\"IsActive\" = FALSE OR \"DeactivatedAtUtc\" IS NULL");
            table.HasCheckConstraint(
                "CK_Users_DisplayName_NotBlank",
                "btrim(\"DisplayName\") <> ''");
        });
        builder.Property(user => user.Id).ValueGeneratedOnAdd();
        builder.Property(user => user.Email).IsRequired().HasMaxLength(320);
        builder.Property(user => user.NormalizedEmail).IsRequired().HasMaxLength(320);
        builder.Property(user => user.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(user => user.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(user => user.CreatedAtUtc).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(user => user.UpdatedAtUtc).IsRequired();
        builder.Property(user => user.DeactivatedAtUtc).IsRequired(false);

        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
        builder.HasIndex(user => user.IsActive).HasFilter("\"IsActive\" = TRUE");
    }
}
