namespace FrankenTui.Backend;

public sealed record TerminalLogWriteOptions
{
    public static readonly TerminalLogWriteOptions Default = new();

    public bool AllowRaw { get; init; }
}
