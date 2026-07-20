using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("Statuses");
        builder.HasKey(status => status.Id);

        builder.Property(status => status.Id).UseIdentityByDefaultColumn();
        builder.Property(status => status.Name).IsRequired().HasMaxLength(50).HasColumnType("citext");
        builder.Property(status => status.Description).HasMaxLength(500);
        builder.Property(status => status.SortOrder).HasDefaultValue((short)0);
        builder.Property(status => status.IsTerminal).HasDefaultValue(false);
        builder.Property(status => status.IsActive).HasDefaultValue(true);
        builder.Property(status => status.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(status => status.Name).IsUnique();
        builder.HasIndex(status => new { status.IsActive, status.SortOrder });

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Status { Id = 1, Name = "Open", SortOrder = 1, IsTerminal = false, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Status { Id = 2, Name = "In Progress", SortOrder = 2, IsTerminal = false, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Status { Id = 3, Name = "Pending", SortOrder = 3, IsTerminal = false, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Status { Id = 4, Name = "Resolved", SortOrder = 4, IsTerminal = false, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Status { Id = 5, Name = "Closed", SortOrder = 5, IsTerminal = true, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp });
    }
}
