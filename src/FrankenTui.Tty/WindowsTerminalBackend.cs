using FrankenTui.Core;

namespace FrankenTui.Tty;

public sealed class WindowsTerminalBackend : ConsoleTerminalBackend
{
    public WindowsTerminalBackend(TextWriter? writer = null)
        : base("windows-conpty", TerminalCapabilities.WindowsConsole(), writer)
    {
    }
}
