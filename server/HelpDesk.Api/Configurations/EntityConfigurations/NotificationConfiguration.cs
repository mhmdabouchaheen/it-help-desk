using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id).ValueGeneratedOnAdd();
        builder.Property(notification => notification.Type).IsRequired().HasMaxLength(100);
        builder.Property(notification => notification.Title).IsRequired().HasMaxLength(250);
        builder.Property(notification => notification.Message).IsRequired();
        builder.Property(notification => notification.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(notification => new { notification.RecipientUserId, notification.CreatedAtUtc })
            .HasFilter("\"ReadAtUtc\" IS NULL");
        builder.HasIndex(notification => notification.TicketId)
            .HasFilter("\"TicketId\" IS NOT NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(notification => notification.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
