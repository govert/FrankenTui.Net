namespace FrankenTui.Render;

public enum EraseDisplayMode : byte
{
    ToEnd = 0,
    Below = ToEnd,
    ToStart = 1,
    Above = ToStart,
    All = 2,
    Scrollback = 3
}
