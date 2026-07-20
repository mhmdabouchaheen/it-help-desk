using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class TicketAssignmentConfiguration : IEntityTypeConfiguration<TicketAssignment>
{
    public void Configure(EntityTypeBuilder<TicketAssignment> builder)
    {
        builder.ToTable("TicketAssignments", table =>
            table.HasCheckConstraint(
                "CK_TicketAssignments_EndedAfterAssigned",
                "\"EndedAtUtc\" IS NULL OR \"EndedAtUtc\" >= \"AssignedAtUtc\""));
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id).ValueGeneratedOnAdd();
        builder.Property(assignment => assignment.Reason).HasMaxLength(500);

        builder.HasIndex(assignment => assignment.TicketId)
            .IsUnique()
            .HasFilter("\"EndedAtUtc\" IS NULL");
        builder.HasIndex(assignment => new { assignment.TicketId, assignment.AssignedAtUtc });
        builder.HasIndex(assignment => new { assignment.AssignedToUserId, assignment.AssignedAtUtc });

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(assignment => assignment.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.EndedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
