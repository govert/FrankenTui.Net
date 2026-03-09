using FrankenTui.Core;

namespace FrankenTui.Tty;

public sealed class UnixTerminalBackend : ConsoleTerminalBackend
{
    public UnixTerminalBackend(TextWriter? writer = null)
        : base("unix-tty", TerminalCapabilityDetector.Detect(), writer)
    {
    }
}
