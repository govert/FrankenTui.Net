namespace FrankenTui.Render;

[Flags]
public enum CellStyleFlags : byte
{
    None = 0,
    Bold = 1 << 0,
    Dim = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    Blink = 1 << 4,
    Reverse = 1 << 5,
    Strikethrough = 1 << 6,
    Hidden = 1 << 7
}
