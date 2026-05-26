using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

// === Item 14: Debug Overlay ===
public sealed class DebugOverlayWidget : IWidget
{
    public bool Visible { get; set; }
    public string? FrameStats { get; set; }
    public string? RenderStats { get; set; }
    public string? WidgetTree { get; set; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (!Visible) return;
        var area = context.Bounds;
        var fg = PackedRgba.Rgb(100, 255, 100);
        var bg = PackedRgba.Rgb(0, 30, 0);

        var lines = new List<string>();
        if (FrameStats is { } fs) lines.Add(fs);
        if (RenderStats is { } rs) lines.Add(rs);
        if (WidgetTree is { } wt) lines.AddRange(wt.Split('\n'));

        for (var i = 0; i < Math.Min(lines.Count, area.Height); i++)
        {
            var y = (ushort)(area.Y + i);
            for (var x = 0; x < Math.Min(lines[i].Length, area.Width); x++)
                context.Buffer.Set((ushort)(area.X + x), y,
                    new Cell(CellContent.FromChar(lines[i][x]), fg, bg, CellAttributes.None));
        }
    }
    public Size Measure(Size available) => available;
}

// === Item 18: VOI Debug Overlay ===
public sealed class VoiDebugOverlayWidget : IWidget
{
    public bool Visible { get; set; }
    public double? PosteriorGain { get; set; }
    public double? ExpectedCost { get; set; }
    public double? EValue { get; set; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (!Visible) return;
        var area = context.Bounds;
        var fg = PackedRgba.Rgb(255, 200, 100);
        var bg = PackedRgba.Rgb(40, 30, 10);

        var lines = new List<string>
        {
            "══ VOI Debug ══",
            PosteriorGain is { } g ? $"Posterior Gain: {g:F4}" : "",
            ExpectedCost is { } c ? $"Expected Cost: {c:F4}" : "",
            EValue is { } e ? $"E-Value: {e:F4}" : ""
        }.Where(l => l.Length > 0).ToList();

        for (var i = 0; i < Math.Min(lines.Count, area.Height); i++)
        {
            var y = (ushort)(area.Y + i);
            for (var x = 0; x < Math.Min(lines[i].Length, area.Width); x++)
                context.Buffer.Set((ushort)(area.X + x), y,
                    new Cell(CellContent.FromChar(lines[i][x]), fg, bg, CellAttributes.None));
        }
    }
    public Size Measure(Size available) => available;
}

// === Item 16: Keyboard Drag (port of keyboard_drag.rs) ===
public sealed class KeyboardDragWidget : IWidget
{
    public bool Active { get; set; }
    public int FocusedIndex { get; set; }
    public int FocusedList { get; set; }
    public string? Announcement { get; set; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (!Active) return;
        var area = context.Bounds;
        var fg = PackedRgba.Rgb(200, 200, 100);
        var text = $"KeyboardDrag active={Active} focused={FocusedIndex} list={FocusedList}";
        if (Announcement is { } a) text += $" | {a}";
        for (var i = 0; i < Math.Min(text.Length, area.Width); i++)
            context.Buffer.Set((ushort)(area.X + i), (ushort)(area.Y + area.Height / 2),
                Cell.FromChar(text[i]).WithForeground(fg));
    }
    public Size Measure(Size available) => new(available.Width, 1);
}
