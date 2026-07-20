using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(role => role.Id).ValueGeneratedOnAdd();
        builder.Property(role => role.Name).IsRequired().HasMaxLength(100);
        builder.Property(role => role.NormalizedName).IsRequired().HasMaxLength(100);
        builder.Property(role => role.Description).HasMaxLength(500);
        builder.Property(role => role.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(role => role.CreatedAtUtc).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(role => role.UpdatedAtUtc).IsRequired();

        builder.HasIndex(role => role.NormalizedName).IsUnique();
        builder.HasIndex(role => role.IsActive);

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Full administrative access to the help desk system.",
                IsActive = true,
                CreatedAtUtc = seedTimestamp,
                UpdatedAtUtc = seedTimestamp,
                ConcurrencyStamp = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
            },
            new Role
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "IT Support Agent",
                NormalizedName = "IT SUPPORT AGENT",
                Description = "Handles and resolves help desk tickets.",
                IsActive = true,
                CreatedAtUtc = seedTimestamp,
                UpdatedAtUtc = seedTimestamp,
                ConcurrencyStamp = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
            },
            new Role
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Employee",
                NormalizedName = "EMPLOYEE",
                Description = "Creates and follows help desk requests.",
                IsActive = true,
                CreatedAtUtc = seedTimestamp,
                UpdatedAtUtc = seedTimestamp,
                ConcurrencyStamp = "cccccccc-cccc-cccc-cccc-cccccccccccc"
            },
            new Role
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Manager",
                NormalizedName = "MANAGER",
                Description = "Oversees team requests and help desk activity.",
                IsActive = true,
                CreatedAtUtc = seedTimestamp,
                UpdatedAtUtc = seedTimestamp,
                ConcurrencyStamp = "dddddddd-dddd-dddd-dddd-dddddddddddd"
            });
    }
}
