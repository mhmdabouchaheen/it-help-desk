using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id).UseIdentityAlwaysColumn();
        builder.Property(activity => activity.Action).IsRequired().HasMaxLength(150);
        builder.Property(activity => activity.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(activity => activity.EntityIdentifier).IsRequired().HasMaxLength(100);
        builder.Property(activity => activity.Metadata).HasColumnType("jsonb");

        builder.HasIndex(activity => new
        {
            activity.EntityType,
            activity.EntityIdentifier,
            activity.OccurredAtUtc
        });
        builder.HasIndex(activity => new { activity.ActorUserId, activity.OccurredAtUtc })
            .HasFilter("\"ActorUserId\" IS NOT NULL");
        builder.HasIndex(activity => activity.OccurredAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(activity => activity.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
