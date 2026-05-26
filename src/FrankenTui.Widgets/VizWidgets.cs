using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Drift visualization for showing data changes over time. Matches upstream drift_visualization.</summary>
public sealed class DriftVisualizationWidget : IWidget
{
    private readonly List<double[]> _snapshots = [];
    public int MaxSnapshots { get; init; } = 20;
    public PackedRgba? IncreaseColor { get; init; }
    public PackedRgba? DecreaseColor { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }

    public void AddSnapshot(double[] values) { _snapshots.Add(values); while (_snapshots.Count > MaxSnapshots) _snapshots.RemoveAt(0); }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _snapshots.Count < 2) return;
        var up = IncreaseColor ?? PackedRgba.Rgb(80, 220, 80);
        var down = DecreaseColor ?? PackedRgba.Rgb(255, 80, 80);
        var latest = _snapshots[^1];
        var previous = _snapshots[^2];
        var count = Math.Min(latest.Length, previous.Length);

        for (var i = 0; i < Math.Min(count, area.Width); i++)
        {
            var delta = latest[i] - previous[i];
            var ch = Math.Abs(delta) < 0.001 ? '·' : delta > 0 ? '▲' : '▼';
            var color = Math.Abs(delta) < 0.001 ? PackedRgba.Rgb(150, 150, 150) : delta > 0 ? up : down;
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(ch).WithForeground(color));
        }

        if (Labels is { } labels && area.Height >= 2)
        {
            for (var i = 0; i < Math.Min(Math.Min(labels.Count, count), area.Width); i++)
            {
                var label = labels[i];
                if (label.Length > 0)
                    context.Buffer.Set((ushort)(area.X + i), (ushort)(area.Y + 1), Cell.FromChar(label[0]).WithForeground(PackedRgba.Rgb(150, 160, 170)));
            }
        }
    }

    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)40), 2);
}

/// <summary>Error boundary widget. Renders error state with message. Matches upstream error_boundary.</summary>
public sealed class ErrorBoundaryWidget : IWidget
{
    public required IWidget Child { get; init; }
    public string? ErrorMessage { get; set; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (ErrorMessage is { } error)
        {
            var area = context.Bounds;
            var fg = PackedRgba.Rgb(255, 80, 80);
            var bg = PackedRgba.Rgb(60, 20, 20);
            for (ushort y = (ushort)area.Y; y < (ushort)area.Bottom; y++)
                for (ushort x = (ushort)area.X; x < (ushort)area.Right; x++)
                    context.Buffer.Set(x, y, new Cell(CellContent.FromChar(' '), fg, bg, CellAttributes.None));
            var msg = error[..Math.Min(error.Length, area.Width - 4)];
            for (var i = 0; i < msg.Length; i++)
                context.Buffer.Set((ushort)(area.X + 2 + i), (ushort)area.Y, Cell.FromChar(msg[i]).WithForeground(fg));
        }
        else
        {
            Child.Render(context);
        }
    }

    public Size Measure(Size available) => available;
}
