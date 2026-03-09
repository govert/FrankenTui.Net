using System.Text.Json;

namespace FrankenTui.Testing.Harness;

internal static class HarnessJson
{
    public static JsonSerializerOptions IndentedSnakeCase { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static JsonSerializerOptions SnakeCase { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}
