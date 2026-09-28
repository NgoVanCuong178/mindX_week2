namespace TicketManager.Cli.Commands;

// Converts between enum values and the snake_case text used on the command line ("in_progress").
public static class EnumText
{
    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
        => throw new NotImplementedException();

    public static string Format<TEnum>(TEnum value) where TEnum : struct, Enum
        => throw new NotImplementedException();
}
