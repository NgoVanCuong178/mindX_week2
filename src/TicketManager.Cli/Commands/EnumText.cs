using System.Text.RegularExpressions;
using TicketManager.Cli.Services;

namespace TicketManager.Cli.Commands;

// Converts between enum values and the snake_case text used on the command line ("in_progress").
public static class EnumText
{
    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(Format(candidate), value, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        var allowed = string.Join(", ", Enum.GetValues<TEnum>().Select(Format));
        throw new ValidationException($"Invalid value '{value}'. Allowed values: {allowed}.");
    }

    public static string Format<TEnum>(TEnum value) where TEnum : struct, Enum
        => Regex.Replace(value.ToString(), "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}
