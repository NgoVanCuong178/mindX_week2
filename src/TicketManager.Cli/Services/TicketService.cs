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
    {
        var trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle))
        {
            throw new ValidationException("Title is required.");
        }

        // Length is checked after trimming, so surrounding spaces do not count.
        if (trimmedTitle.Length > 200)
        {
            throw new ValidationException("Title must not exceed 200 characters.");
        }

        description ??= "";
        if (description.Length > 2000)
        {
            throw new ValidationException("Description must not exceed 2000 characters.");
        }

        var tickets = _repository.LoadAll().ToList();
        var ticket = new Ticket
        {
            // max + 1 (not count + 1) so ids stay unique when the sequence has gaps.
            Id = tickets.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1,
            Title = trimmedTitle,
            Description = description,
            Status = status,
            Priority = priority,
            Tags = (tags ?? []).Select(t => t.Trim().ToLowerInvariant())
                               .Where(t => t.Length > 0)
                               .Distinct()
                               .ToList(),
            CreatedAt = _timeProvider.GetUtcNow(),
        };

        tickets.Add(ticket);
        _repository.SaveAll(tickets);
        return ticket;
    }

    public IReadOnlyList<Ticket> List(TicketStatus? status = null,
                                      TicketPriority? priority = null,
                                      IEnumerable<string>? tags = null)
    {
        var wantedTags = (tags ?? []).Select(t => t.Trim().ToLowerInvariant()).ToList();

        // All filters are combined with AND; an empty tag list matches every ticket.
        return _repository.LoadAll()
            .Where(t => status is null || t.Status == status)
            .Where(t => priority is null || t.Priority == priority)
            .Where(t => wantedTags.All(t.Tags.Contains))
            .OrderBy(t => t.Id)
            .ToList();
    }

    public Ticket Get(int id)
        => _repository.LoadAll().FirstOrDefault(t => t.Id == id)
           ?? throw new TicketNotFoundException(id);

    public Ticket UpdateStatus(int id, TicketStatus status)
    {
        var tickets = _repository.LoadAll().ToList();
        var ticket = tickets.FirstOrDefault(t => t.Id == id)
                     ?? throw new TicketNotFoundException(id);

        ticket.Status = status;
        ticket.UpdatedAt = _timeProvider.GetUtcNow();
        _repository.SaveAll(tickets);
        return ticket;
    }
}
