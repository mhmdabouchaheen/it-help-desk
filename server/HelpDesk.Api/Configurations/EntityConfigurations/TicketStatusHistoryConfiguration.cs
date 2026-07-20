using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        builder.ToTable("TicketStatusHistory", table =>
            table.HasCheckConstraint(
                "CK_TicketStatusHistory_StatusChanged",
                "\"FromStatusId\" IS NULL OR \"FromStatusId\" <> \"ToStatusId\""));
        builder.HasKey(history => history.Id);

        builder.Property(history => history.Id).ValueGeneratedOnAdd();
        builder.Property(history => history.Reason).HasMaxLength(1000);

        builder.HasIndex(history => new { history.TicketId, history.ChangedAtUtc });
        builder.HasIndex(history => new { history.ToStatusId, history.ChangedAtUtc });

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(history => history.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(history => history.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(history => history.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
