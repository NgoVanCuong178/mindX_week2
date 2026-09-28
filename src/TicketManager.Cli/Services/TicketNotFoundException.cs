namespace TicketManager.Cli.Services;

public class TicketNotFoundException(int ticketId) : Exception($"Ticket #{ticketId} not found")
{
    public int TicketId { get; } = ticketId;
}
