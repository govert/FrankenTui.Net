namespace FrankenTui.Core;

[Flags]
public enum TerminalModifiers : byte
{
    None = 0,
    Shift = 1 << 0,
    Alt = 1 << 1,
    Control = 1 << 2
}
