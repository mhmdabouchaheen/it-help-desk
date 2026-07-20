using HelpDesk.Api.Configurations;
using HelpDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<
        User,
        Role,
        Guid,
        IdentityUserClaim<Guid>,
        UserRole,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>(options)
{
    public new DbSet<User> Users => Set<User>();

    public new DbSet<Role> Roles => Set<Role>();

    public new DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Priority> Priorities => Set<Priority>();

    public DbSet<Status> Statuses => Set<Status>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    public DbSet<TicketAssignment> TicketAssignments => Set<TicketAssignment>();

    public DbSet<TicketStatusHistory> TicketStatusHistory => Set<TicketStatusHistory>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ConfigureIdentityTableNames();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
