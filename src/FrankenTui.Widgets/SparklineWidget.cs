using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Compact sparkline widget for trend visualization using Unicode block characters (▁▂▃▄▅▆▇█).
/// Matches upstream ftui-widgets sparkline.
/// </summary>
public sealed class SparklineWidget : IWidget
{
    private static readonly char[] SparkChars = [' ', '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];

    private readonly IReadOnlyList<double> _data;
    private double _min = double.NaN;
    private double _max = double.NaN;
    private PackedRgba? _color;
    private double _baseline;

    public SparklineWidget(IReadOnlyList<double> data)
    {
        _data = data;
        _baseline = 0.0;
    }

    public SparklineWidget WithMin(double min) { _min = min; return this; }
    public SparklineWidget WithMax(double max) { _max = max; return this; }
    public SparklineWidget WithColor(PackedRgba color) { _color = color; return this; }
    public SparklineWidget WithBaseline(double baseline) { _baseline = baseline; return this; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var bounds = context.Bounds;
        var buffer = context.Buffer;
        var count = _data.Count;
        if (count == 0 || bounds.Width == 0 || bounds.Height == 0) return;

        var dataMin = double.IsNaN(_min) ? _data.Min() : _min;
        var dataMax = double.IsNaN(_max) ? _data.Max() : _max;
        var range = dataMax - dataMin;
        if (range <= 0) range = 1.0;

        var fg = _color ?? context.Theme.Default.Foreground;

        for (var i = 0; i < Math.Min(count, bounds.Width); i++)
        {
            var value = _data[i];
            var normalized = Math.Clamp((value - dataMin) / range, 0.0, 1.0);
            var charIndex = (int)Math.Round(normalized * (SparkChars.Length - 1));
            var ch = SparkChars[Math.Clamp(charIndex, 0, SparkChars.Length - 1)];
            var cell = Cell.FromChar(ch).WithForeground(fg);
            buffer.Set((ushort)(bounds.X + i), (ushort)bounds.Y, cell);
        }

        // Clear trailing cells
        for (var i = count; i < bounds.Width; i++)
            buffer.Set((ushort)(bounds.X + i), (ushort)bounds.Y, Cell.Empty);
    }

    public Size Measure(Size available) => new(Math.Min((ushort)_data.Count, available.Width), 1);
}
