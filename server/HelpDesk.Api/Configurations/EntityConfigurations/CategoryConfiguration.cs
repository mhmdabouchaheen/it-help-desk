using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id).UseIdentityByDefaultColumn();
        builder.Property(category => category.Name).IsRequired().HasMaxLength(100).HasColumnType("citext");
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.SortOrder).HasDefaultValue((short)0);
        builder.Property(category => category.IsActive).HasDefaultValue(true);
        builder.Property(category => category.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasIndex(category => new { category.IsActive, category.SortOrder });

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Category { Id = 1, Name = "Hardware", SortOrder = 1, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Category { Id = 2, Name = "Software", SortOrder = 2, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Category { Id = 3, Name = "Network", SortOrder = 3, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Category { Id = 4, Name = "Email", SortOrder = 4, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Category { Id = 5, Name = "Access Request", SortOrder = 5, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp },
            new Category { Id = 6, Name = "Other", SortOrder = 6, IsActive = true, CreatedAtUtc = seedTimestamp, UpdatedAtUtc = seedTimestamp });
    }
}
