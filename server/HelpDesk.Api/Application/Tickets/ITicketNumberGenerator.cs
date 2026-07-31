namespace HelpDesk.Api.Application.Tickets;

/// <summary>Generates human-readable, non-user-derived ticket reference numbers.</summary>
public interface ITicketNumberGenerator
{
    /// <summary>Generates a new ticket number using UTC time and secure randomness.</summary>
    string Generate();
}
