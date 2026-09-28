using System.Text.Json;
using TicketManager.Cli.Models;

namespace TicketManager.Cli.Storage;

public class JsonTicketRepository : ITicketRepository
{
    private readonly string _filePath;

    public JsonTicketRepository(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<Ticket> LoadAll()
    {
        // First run: no file yet means no tickets.
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Ticket>>(File.ReadAllText(_filePath))!;
        }
        catch (JsonException ex)
        {
            // Covers invalid syntax, wrong shape and an empty file. The file is never touched here.
            throw new StorageException($"Data file '{_filePath}' is corrupted or not valid JSON.", ex);
        }
    }

    public void SaveAll(IEnumerable<Ticket> tickets)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(tickets));
    }
}
