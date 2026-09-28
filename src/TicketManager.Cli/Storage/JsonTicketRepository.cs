using TicketManager.Cli.Models;

namespace TicketManager.Cli.Storage;

public class JsonTicketRepository : ITicketRepository
{
    private readonly string _filePath;

    public JsonTicketRepository(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<Ticket> LoadAll() => throw new NotImplementedException();

    public void SaveAll(IEnumerable<Ticket> tickets) => throw new NotImplementedException();
}
