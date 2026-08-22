using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;

namespace HelpDesk.Api.Infrastructure.Authorization;

public static class TicketReadScope
{
    public static IQueryable<Ticket> Apply(
        IQueryable<Ticket> tickets,
        ApplicationDbContext db,
        TicketAccessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.UserId == Guid.Empty || context.Roles is null ||
            !context.Roles.Any(role => AppRoles.All.Contains(role, StringComparer.Ordinal)))
            throw new TicketAccessDeniedException();

        if (context.Roles.Any(role => AppRoles.SupportStaff.Contains(role, StringComparer.Ordinal)))
            return tickets;

        if (context.Roles.Contains(AppRoles.Manager, StringComparer.Ordinal))
            return tickets.Where(ticket =>
                ticket.CreatedByUserId == context.UserId ||
                db.Users.Any(user => user.Id == ticket.CreatedByUserId && user.ManagerUserId == context.UserId));

        return tickets.Where(ticket => ticket.CreatedByUserId == context.UserId);
    }

    public static bool IsSupportWide(TicketAccessContext context) =>
        context.Roles.Any(role => AppRoles.SupportStaff.Contains(role, StringComparer.Ordinal));
}
