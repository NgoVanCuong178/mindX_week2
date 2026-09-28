namespace TicketManager.Cli.Storage;

public static class DataFilePath
{
    public const string EnvironmentVariable = "TICKETS_FILE";

    public static string Resolve(string? optionValue, string? environmentValue, string homeDirectory)
        => throw new NotImplementedException();
}
