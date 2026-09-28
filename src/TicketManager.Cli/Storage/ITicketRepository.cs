using TicketManager.Cli.Models;

namespace TicketManager.Cli.Storage;

public interface ITicketRepository
{
    IReadOnlyList<Ticket> LoadAll();
    void SaveAll(IEnumerable<Ticket> tickets);
}
