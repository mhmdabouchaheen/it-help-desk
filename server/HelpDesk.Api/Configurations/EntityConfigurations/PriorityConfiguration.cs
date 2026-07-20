using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class PriorityConfiguration : IEntityTypeConfiguration<Priority>
{
    public void Configure(EntityTypeBuilder<Priority> builder)
    {
        builder.ToTable("Priorities", table =>
            table.HasCheckConstraint("CK_Priorities_Rank_Positive", "\"Rank\" > 0"));
        builder.HasKey(priority => priority.Id);

        builder.Property(priority => priority.Id).UseIdentityByDefaultColumn();
        builder.Property(priority => priority.Name).IsRequired().HasMaxLength(50).HasColumnType("citext");
        builder.Property(priority => priority.Description).HasMaxLength(500);
        builder.Property(priority => priority.IsActive).HasDefaultValue(true);
        builder.Property(priority => priority.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(priority => priority.Name).IsUnique();
        builder.HasIndex(priority => priority.Rank).IsUnique();
        builder.HasIndex(priority => priority.IsActive);

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Priority { Id = 1, Name = "Low", Rank = 1, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Priority { Id = 2, Name = "Medium", Rank = 2, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Priority { Id = 3, Name = "High", Rank = 3, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Priority { Id = 4, Name = "Critical", Rank = 4, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp });
    }
}
