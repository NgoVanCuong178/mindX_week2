using TicketManager.Cli.Models;
using TicketManager.Cli.Storage;

namespace TicketManager.Cli.Services;

public class TicketService
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

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
        var ticket = new Ticket
        {
            Title = ValidateTitle(title),
            Description = ValidateDescription(description),
            Status = status,
            Priority = priority,
            Tags = NormalizeTags(tags),
        };

        var tickets = _repository.LoadAll().ToList();
        // max + 1 (not count + 1) so ids stay unique when the sequence has gaps.
        ticket.Id = tickets.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1;
        ticket.CreatedAt = _timeProvider.GetUtcNow();

        tickets.Add(ticket);
        _repository.SaveAll(tickets);
        return ticket;
    }

    public IReadOnlyList<Ticket> List(TicketStatus? status = null,
                                      TicketPriority? priority = null,
                                      IEnumerable<string>? tags = null)
    {
        var wantedTags = (tags ?? []).Select(NormalizeTag).ToList();

        // All filters are combined with AND; an empty tag list matches every ticket.
        return _repository.LoadAll()
            .Where(t => status is null || t.Status == status)
            .Where(t => priority is null || t.Priority == priority)
            .Where(t => wantedTags.All(t.Tags.Contains))
            .OrderBy(t => t.Id)
            .ToList();
    }

    public Ticket Get(int id) => FindOrThrow(_repository.LoadAll(), id);

    public Ticket UpdateStatus(int id, TicketStatus status)
    {
        var tickets = _repository.LoadAll().ToList();
        var ticket = FindOrThrow(tickets, id);

        ticket.Status = status;
        ticket.UpdatedAt = _timeProvider.GetUtcNow();
        _repository.SaveAll(tickets);
        return ticket;
    }

    private static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException("Title is required.");
        }

        // Length is checked after trimming, so surrounding spaces do not count.
        if (trimmed.Length > MaxTitleLength)
        {
            throw new ValidationException($"Title must not exceed {MaxTitleLength} characters.");
        }

        return trimmed;
    }

    private static string ValidateDescription(string? description)
    {
        description ??= "";
        if (description.Length > MaxDescriptionLength)
        {
            throw new ValidationException($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        return description;
    }

    private static List<string> NormalizeTags(IEnumerable<string>? tags)
        => (tags ?? [])
            .Select(NormalizeTag)
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

    private static string NormalizeTag(string tag) => tag.Trim().ToLowerInvariant();

    private static Ticket FindOrThrow(IEnumerable<Ticket> tickets, int id)
        => tickets.FirstOrDefault(t => t.Id == id) ?? throw new TicketNotFoundException(id);
}
