namespace FrankenTui.Render;

public enum EraseLineMode : byte
{
    ToEnd = 0,
    Right = ToEnd,
    ToStart = 1,
    Left = ToStart,
    All = 2
}
