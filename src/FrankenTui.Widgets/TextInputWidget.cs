using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Single-line text input widget. Matches upstream ftui-widgets TextInput.</summary>
public sealed class TextInputWidget : IWidget
{
    private string _value = "";
    private int _cursor;
    private int _scrollCells;
    private int? _selectionAnchor;

    public string Value => _value;
    public string Placeholder { get; init; } = "";
    public char? MaskChar { get; init; }
    public int? MaxLength { get; init; }
    public bool Focused { get; set; }
    public PackedRgba? CursorColor { get; init; }
    public PackedRgba? SelectionColor { get; init; }
    public PackedRgba? PlaceholderColor { get; init; }

    public void SetValue(string value) { _value = value; _cursor = value.Length; _selectionAnchor = null; }
    public void Insert(char ch)
    {
        if (MaxLength is { } max && _value.Length >= max) return;
        _value = _value[.._cursor] + ch + _value[_cursor..];
        _cursor++;
        _selectionAnchor = null;
    }
    public void Backspace()
    {
        if (_cursor <= 0) return;
        _value = _value[..(_cursor - 1)] + _value[_cursor..];
        _cursor--;
        _selectionAnchor = null;
    }
    public void MoveLeft() { if (_cursor > 0) _cursor--; }
    public void MoveRight() { if (_cursor < _value.Length) _cursor++; }
    public void MoveHome() { _cursor = 0; }
    public void MoveEnd() { _cursor = _value.Length; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;

        var fg = PackedRgba.White;
        var bg = PackedRgba.Rgb(30, 34, 42);
        var display = MaskChar is { } m ? new string(m, _value.Length) : _value;
        var displayWithCursor = display;
        if (Focused && _cursor <= display.Length)
            displayWithCursor = display[.._cursor] + "│" + display[_cursor..];

        // Scroll to keep cursor visible
        var cursorVisual = _cursor;
        if (cursorVisual >= _scrollCells + area.Width - 1) _scrollCells = Math.Max(0, cursorVisual - area.Width + 2);
        if (cursorVisual < _scrollCells) _scrollCells = cursorVisual;

        var visible = displayWithCursor[_scrollCells..];
        if (visible.Length == 0 && !string.IsNullOrEmpty(Placeholder))
        {
            visible = Placeholder;
            fg = PlaceholderColor ?? PackedRgba.Rgb(100, 110, 120);
        }

        // Fill background
        for (ushort x = (ushort)area.X; x < (ushort)area.Right; x++)
            context.Buffer.Set(x, (ushort)area.Y, new Cell(CellContent.FromChar(' '), PackedRgba.White, bg, CellAttributes.None));

        for (var i = 0; i < Math.Min(visible.Length, area.Width); i++)
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y,
                new Cell(CellContent.FromChar(visible[i]),
                    visible[i] == '│' ? (CursorColor ?? PackedRgba.Rgb(255, 200, 50)) : fg,
                    bg, CellAttributes.None));
    }

    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)60), 1);
}
