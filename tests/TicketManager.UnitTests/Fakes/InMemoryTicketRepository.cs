using TicketManager.Cli.Models;
using TicketManager.Cli.Storage;

namespace TicketManager.UnitTests.Fakes;

// Repo giả lưu ticket trong bộ nhớ, dùng cho unit test thay cho file JSON thật.
// Mỗi lần đọc/ghi đều sao chép ticket: nếu service sửa ticket mà quên gọi SaveAll
// thì thay đổi sẽ không có trong repo → test bắt được lỗi, giống như khi dùng file thật.
public class InMemoryTicketRepository : ITicketRepository
{
    private List<Ticket> _tickets = new();

    public IReadOnlyList<Ticket> Tickets => _tickets.Select(Clone).ToList();

    public void Seed(params Ticket[] tickets) => _tickets = tickets.Select(Clone).ToList();

    public IReadOnlyList<Ticket> LoadAll() => Tickets;

    public void SaveAll(IEnumerable<Ticket> tickets) => _tickets = tickets.Select(Clone).ToList();

    private static Ticket Clone(Ticket ticket) => new()
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        Status = ticket.Status,
        Priority = ticket.Priority,
        Tags = new List<string>(ticket.Tags),
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
    };
}
