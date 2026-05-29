using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Constraint bounds for widget layout. Matches upstream LayoutConstraints.</summary>
public readonly record struct LayoutConstraints(ushort MinWidth, ushort MaxWidth, ushort MinHeight, ushort MaxHeight)
{
    public static LayoutConstraints Unconstrained => new(0, 0, 0, 0);
    public bool WidthOverflow(ushort w) => MaxWidth != 0 && w > MaxWidth;
    public bool HeightOverflow(ushort h) => MaxHeight != 0 && h > MaxHeight;
    public bool WidthUnderflow(ushort w) => w < MinWidth;
    public bool HeightUnderflow(ushort h) => h < MinHeight;
}

/// <summary>Single widget layout record. Matches upstream LayoutRecord.</summary>
public sealed class LayoutRecord
{
    public string WidgetName { get; init; } = "";
    public Rect AreaRequested { get; init; }
    public Rect AreaReceived { get; init; }
    public LayoutConstraints Constraints { get; init; }
    public List<LayoutRecord> Children { get; init; } = [];
}

/// <summary>Records layout constraint data. Matches upstream LayoutDebugger.</summary>
public sealed class LayoutDebugger
{
    private readonly List<LayoutRecord> _records = [];
    public bool Enabled { get; set; }
    public IReadOnlyList<LayoutRecord> Records => _records;

    public void Record(LayoutRecord record) { if (Enabled) _records.Add(record); }
    public void Clear() => _records.Clear();
}

/// <summary>Constraint overlay style. Matches upstream ConstraintOverlayStyle.</summary>
public sealed class ConstraintOverlayStyle
{
    public PackedRgba NormalColor { get; init; } = PackedRgba.Rgb(100, 200, 100);
    public PackedRgba OverflowColor { get; init; } = PackedRgba.Rgb(240, 80, 80);
    public PackedRgba UnderflowColor { get; init; } = PackedRgba.Rgb(240, 200, 80);
    public PackedRgba RequestedColor { get; init; } = PackedRgba.Rgb(80, 150, 240);
    public bool ShowBorders { get; init; } = true;
    public bool ShowLabels { get; init; } = true;
    public BorderType BorderType { get; init; } = BorderType.Ascii;
}

/// <summary>Constraint overlay widget. Matches upstream ConstraintOverlay.</summary>
public sealed class ConstraintOverlayWidget : IWidget
{
    private readonly LayoutDebugger _debugger;
    private readonly ConstraintOverlayStyle _style = new();

    public ConstraintOverlayWidget(LayoutDebugger debugger) => _debugger = debugger;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (!_debugger.Enabled || _debugger.Records.Count == 0) return;
        foreach (var record in _debugger.Records)
            RenderRecord(context, record);
    }

    private void RenderRecord(RuntimeRenderContext ctx, LayoutRecord record)
    {
        var area = record.AreaReceived;
        if (area.IsEmpty) return;
        var constraints = record.Constraints;

        // Determine border color
        var hasOverflow = constraints.WidthOverflow(area.Width) || constraints.HeightOverflow(area.Height);
        var hasUnderflow = constraints.WidthUnderflow(area.Width) || constraints.HeightUnderflow(area.Height);
        var borderColor = hasOverflow ? _style.OverflowColor : hasUnderflow ? _style.UnderflowColor : _style.NormalColor;

        if (_style.ShowBorders)
        {
            DrawDashedBorder(ctx.Buffer, area, borderColor);
            // Draw requested area as dashed if different
            if (record.AreaRequested != area && !record.AreaRequested.IsEmpty)
                DrawDashedBorder(ctx.Buffer, record.AreaRequested, _style.RequestedColor);
        }

        if (_style.ShowLabels && !string.IsNullOrEmpty(record.WidgetName))
        {
            var label = $"{record.WidgetName} {area.Width}x{area.Height}";
            for (var i = 0; i < Math.Min(label.Length, area.Width - 2); i++)
                ctx.Buffer.Set((ushort)(area.X + 1 + i), (ushort)area.Y, Cell.FromChar(label[i]).WithForeground(PackedRgba.White).WithBackground(PackedRgba.Rgb(0, 0, 0)));
        }

        foreach (var child in record.Children)
            RenderRecord(ctx, child);
    }

    private static void DrawDashedBorder(FrankenTui.Render.Buffer buffer, Rect area, PackedRgba color)
    {
        if (area.Width < 2 || area.Height < 2) return;
        var cell = Cell.FromChar('·').WithForeground(color);
        // Dashed: every other cell
        for (ushort x = (ushort)area.X; x < (ushort)area.Right; x += 2)
        {
            buffer.Set(x, (ushort)area.Y, cell);
            buffer.Set(x, (ushort)(area.Bottom - 1), cell);
        }
        for (ushort y = (ushort)(area.Y + 2); y < (ushort)(area.Bottom - 1); y += 2)
        {
            buffer.Set((ushort)area.X, y, cell);
            buffer.Set((ushort)(area.Right - 1), y, cell);
        }
    }

    public Size Measure(Size available) => available;
}
