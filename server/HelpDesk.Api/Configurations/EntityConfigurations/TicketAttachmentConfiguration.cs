using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments", table =>
            table.HasCheckConstraint(
                "CK_TicketAttachments_SizeBytes_NonNegative",
                "\"SizeBytes\" >= 0"));
        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id).ValueGeneratedOnAdd();
        builder.Property(attachment => attachment.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(attachment => attachment.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(attachment => attachment.StorageProvider).IsRequired().HasMaxLength(50);
        builder.Property(attachment => attachment.StorageKey).IsRequired().HasMaxLength(1024);
        builder.Property(attachment => attachment.ContentHash).HasMaxLength(128);
        builder.Property(attachment => attachment.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(attachment => new { attachment.StorageProvider, attachment.StorageKey }).IsUnique();
        builder.HasIndex(attachment => new { attachment.TicketId, attachment.CreatedAtUtc });
        builder.HasIndex(attachment => attachment.CommentId)
            .HasFilter("\"CommentId\" IS NOT NULL");

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(attachment => attachment.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TicketComment>()
            .WithMany()
            .HasForeignKey(attachment => attachment.CommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
