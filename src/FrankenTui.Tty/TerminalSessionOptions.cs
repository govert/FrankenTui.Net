using FrankenTui.Core;

namespace FrankenTui.Tty;

public sealed record TerminalSessionOptions
{
    public TerminalMode Modes { get; init; } =
        TerminalMode.RawInput |
        TerminalMode.AlternateScreen |
        TerminalMode.FocusEvents |
        TerminalMode.BracketedPaste |
        TerminalMode.HiddenCursor;

    public bool InlineMode { get; init; }

    public bool UseMouseTracking { get; init; }

    public bool UseAlternateScreen => !InlineMode && Modes.HasFlag(TerminalMode.AlternateScreen);

    public bool HideCursor => Modes.HasFlag(TerminalMode.HiddenCursor);
}
