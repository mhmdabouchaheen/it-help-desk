using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Api.Configurations.EntityConfigurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments", table =>
        {
            table.HasCheckConstraint(
                "CK_TicketComments_Visibility",
                "\"Visibility\" IN ('Public', 'Internal')");
            table.HasCheckConstraint(
                "CK_TicketComments_Body_NotBlank",
                "btrim(\"Body\") <> ''");
        });
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id).ValueGeneratedOnAdd();
        builder.Property(comment => comment.Body).IsRequired();
        builder.Property(comment => comment.Visibility).IsRequired().HasMaxLength(20);
        builder.Property(comment => comment.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(comment => new { comment.TicketId, comment.CreatedAtUtc });

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(comment => comment.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
