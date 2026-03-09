using FrankenTui.Core;

namespace FrankenTui.Tty;

public sealed record TerminalHostProfile(
    string Platform,
    string Host,
    TerminalCapabilities Capabilities,
    string Notes);
