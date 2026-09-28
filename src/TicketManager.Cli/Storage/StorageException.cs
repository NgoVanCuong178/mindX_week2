namespace TicketManager.Cli.Storage;

public class StorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
