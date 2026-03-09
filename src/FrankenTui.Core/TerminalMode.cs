namespace FrankenTui.Core;

[Flags]
public enum TerminalMode : ushort
{
    None = 0,
    RawInput = 1 << 0,
    Inline = 1 << 1,
    AlternateScreen = 1 << 2,
    MouseTracking = 1 << 3,
    FocusEvents = 1 << 4,
    BracketedPaste = 1 << 5,
    SynchronizedOutput = 1 << 6,
    HiddenCursor = 1 << 7
}
