using TicketManager.Cli.Models;
using TicketManager.Cli.Storage;

namespace TicketManager.Cli.Services;

public class TicketService
{
    private readonly ITicketRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TicketService(ITicketRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Ticket Create(string? title, string? description = null,
                         TicketStatus status = TicketStatus.Open,
                         TicketPriority priority = TicketPriority.Medium,
                         IEnumerable<string>? tags = null)
        => throw new NotImplementedException();

    public IReadOnlyList<Ticket> List(TicketStatus? status = null,
                                      TicketPriority? priority = null,
                                      IEnumerable<string>? tags = null)
        => throw new NotImplementedException();

    public Ticket Get(int id) => throw new NotImplementedException();

    public Ticket UpdateStatus(int id, TicketStatus status) => throw new NotImplementedException();
}
