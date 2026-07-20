using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets", table =>
        {
            table.HasCheckConstraint(
                "CK_Tickets_ReferenceNumber_NotBlank",
                "btrim(\"ReferenceNumber\") <> ''");
            table.HasCheckConstraint(
                "CK_Tickets_Title_NotBlank",
                "btrim(\"Title\") <> ''");
            table.HasCheckConstraint(
                "CK_Tickets_Description_NotBlank",
                "btrim(\"Description\") <> ''");
        });
        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Id).ValueGeneratedOnAdd();
        builder.Property(ticket => ticket.ReferenceNumber).IsRequired().HasMaxLength(30);
        builder.Property(ticket => ticket.Title).IsRequired().HasMaxLength(250);
        builder.Property(ticket => ticket.Description).IsRequired();
        builder.Property(ticket => ticket.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(ticket => ticket.ReferenceNumber).IsUnique();
        builder.HasIndex(ticket => new { ticket.StatusId, ticket.PriorityId, ticket.CreatedAtUtc });
        builder.HasIndex(ticket => ticket.PriorityId);
        builder.HasIndex(ticket => new { ticket.AssignedToUserId, ticket.StatusId, ticket.UpdatedAtUtc })
            .HasFilter("\"AssignedToUserId\" IS NOT NULL");
        builder.HasIndex(ticket => new { ticket.CreatedByUserId, ticket.CreatedAtUtc });
        builder.HasIndex(ticket => new { ticket.CategoryId, ticket.CreatedAtUtc });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(ticket => ticket.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Priority>()
            .WithMany()
            .HasForeignKey(ticket => ticket.PriorityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(ticket => ticket.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
