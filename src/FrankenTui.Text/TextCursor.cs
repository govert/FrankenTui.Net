namespace FrankenTui.Text;

public readonly record struct TextCursor(int Line, int Column)
{
    public TextCursor MoveHorizontal(int delta) => new(Line, Math.Max(0, Column + delta));

    public TextCursor MoveVertical(int delta) => new(Math.Max(0, Line + delta), Column);
}
