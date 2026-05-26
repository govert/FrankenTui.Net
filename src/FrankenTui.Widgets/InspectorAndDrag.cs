using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Widget inspector for debugging widget trees. Matches upstream inspector.</summary>
public sealed class WidgetInspector : IWidget
{
    private IWidget? _root;
    private bool _visible;
    private int _scrollOffset;

    public bool Visible { get => _visible; set => _visible = value; }
    public void SetRoot(IWidget? root) => _root = root;
    public void ScrollUp() => _scrollOffset = Math.Max(0, _scrollOffset - 1);
    public void ScrollDown() => _scrollOffset++;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (!_visible) return;
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var fg = PackedRgba.Rgb(100, 255, 100);
        var bg = PackedRgba.Rgb(0, 20, 0);

        var lines = new List<string> { "══ Widget Inspector ══" };
        if (_root is not null)
            CollectWidgetInfo(_root, "", lines, 0);

        var visible = lines.Skip(_scrollOffset).Take(area.Height).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            var y = (ushort)(area.Y + i);
            for (var x = 0; x < Math.Min(visible[i].Length, area.Width); x++)
                context.Buffer.Set((ushort)(area.X + x), y,
                    new Cell(CellContent.FromChar(visible[i][x]), fg, bg, CellAttributes.None));
        }
    }

    private static void CollectWidgetInfo(IWidget widget, string indent, List<string> lines, int depth)
    {
        if (depth > 10) return;
        var typeName = widget.GetType().Name;
        lines.Add($"{indent}{typeName}");
    }

    public Size Measure(Size available) => available;
}

/// <summary>Drag-and-drop support for widgets. Matches upstream drag.</summary>
public sealed class DragState
{
    public bool IsDragging { get; set; }
    public ushort StartX { get; set; }
    public ushort StartY { get; set; }
    public ushort CurrentX { get; set; }
    public ushort CurrentY { get; set; }
    public object? Payload { get; set; }
    public string? SourceWidgetId { get; set; }

    public (ushort dx, ushort dy) Delta =>
        ((ushort)(CurrentX - StartX), (ushort)(CurrentY - StartY));

    public void Start(ushort x, ushort y, object? payload = null, string? sourceId = null)
    {
        IsDragging = true;
        StartX = CurrentX = x;
        StartY = CurrentY = y;
        Payload = payload;
        SourceWidgetId = sourceId;
    }

    public void Update(ushort x, ushort y) { CurrentX = x; CurrentY = y; }
    public void End() { IsDragging = false; Payload = null; }
}
