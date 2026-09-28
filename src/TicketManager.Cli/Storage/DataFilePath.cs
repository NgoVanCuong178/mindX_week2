namespace TicketManager.Cli.Storage;

public static class DataFilePath
{
    public const string EnvironmentVariable = "TICKETS_FILE";

    public static string Resolve(string? optionValue, string? environmentValue, string homeDirectory)
    {
        if (!string.IsNullOrWhiteSpace(optionValue))
        {
            return optionValue;
        }

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        return Path.Combine(homeDirectory, ".tickets", "tickets.json");
    }
}
