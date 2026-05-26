using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Bar chart widget. Supports grouped/stacked vertical/horizontal bars.
/// Matches upstream ftui-extras BarChart.
/// </summary>
public sealed class BarChartWidget : IWidget
{
    private static readonly char[] BarChars = [' ', '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];

    private readonly IReadOnlyList<BarGroup> _groups;
    private bool _vertical = true;
    private bool _stacked;
    private ushort _barWidth = 1;
    private ushort _barGap;
    private ushort _groupGap = 1;
    private PackedRgba[] _colors = [PackedRgba.Rgb(0, 150, 255), PackedRgba.Rgb(255, 100, 0), PackedRgba.Rgb(0, 200, 100), PackedRgba.Rgb(200, 50, 200)];
    private double _max = double.NaN;

    public BarChartWidget(IReadOnlyList<BarGroup> groups) => _groups = groups;

    public BarChartWidget Stacked() { _stacked = true; return this; }
    public BarChartWidget WithBarWidth(ushort w) { _barWidth = Math.Max(w, (ushort)1); return this; }
    public BarChartWidget WithBarGap(ushort g) { _barGap = g; return this; }
    public BarChartWidget WithGroupGap(ushort g) { _groupGap = g; return this; }
    public BarChartWidget WithColors(params PackedRgba[] colors) { _colors = colors; return this; }
    public BarChartWidget WithMax(double max) { _max = max; return this; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _groups.Count == 0) return;

        var maxVal = double.IsNaN(_max)
            ? (_stacked
                ? _groups.Max(g => g.Values.Sum())
                : _groups.SelectMany(g => g.Values).DefaultIfEmpty(0).Max())
            : _max;
        if (maxVal <= 0) return;

        var chartHeight = (double)(area.Height - 1);
        if (chartHeight <= 0) return;

        var labelY = (ushort)(area.Bottom - 1);
        var baseY = (ushort)(area.Bottom - 2);
        ushort xCursor = (ushort)area.X;

        foreach (var group in _groups)
        {
            var groupStartX = xCursor;
            var values = group.Values;

            if (_stacked)
            {
                double cumulative = 0;
                for (var si = 0; si < values.Count; si++)
                {
                    var prevRows = (ushort)Math.Round(cumulative / maxVal * chartHeight);
                    cumulative += values[si];
                    var currRows = (ushort)Math.Round(cumulative / maxVal * chartHeight);
                    var segment = (ushort)Math.Max(0, currRows - prevRows);
                    var color = GetColor(si);

                    for (ushort row = 0; row < segment; row++)
                    {
                        var y = (ushort)Math.Max(area.Y, baseY - prevRows - row);
                        FillBarRow(context, xCursor, y, color);
                    }
                }
                xCursor += _barWidth;
            }
            else
            {
                for (var si = 0; si < values.Count; si++)
                {
                    if (si > 0) xCursor += _barGap;
                    var h = values[si] / maxVal * chartHeight;
                    var full = (ushort)Math.Floor(h);
                    var fracIdx = (int)Math.Min(8, Math.Round((h - full) * 8));
                    var color = GetColor(si);

                    for (ushort row = 0; row < full; row++)
                    {
                        var y = (ushort)Math.Max(area.Y, baseY - row);
                        FillBarRow(context, xCursor, y, color);
                    }
                    if (fracIdx > 0)
                    {
                        var y = (ushort)Math.Max(area.Y, baseY - full);
                        var ch = BarChars[fracIdx];
                        for (ushort dx = 0; dx < _barWidth && xCursor + dx < (ushort)area.Right; dx++)
                            context.Buffer.Set((ushort)(xCursor + dx), y, Cell.FromChar(ch).WithForeground(color));
                    }
                    xCursor += _barWidth;
                }
            }

            // Label
            var groupW = (ushort)(xCursor - groupStartX);
            var label = group.Label;
            var labelLen = (ushort)Math.Min(label.Length, groupW);
            var labelX = (ushort)(groupStartX + (groupW - labelLen) / 2);
            for (ushort i = 0; i < labelLen && labelX + i < (ushort)area.Right; i++)
                context.Buffer.Set((ushort)(labelX + i), labelY, Cell.FromChar(label[i]));

            xCursor += _groupGap;
        }
    }

    private void FillBarRow(RuntimeRenderContext ctx, ushort x, ushort y, PackedRgba color)
    {
        for (ushort dx = 0; dx < _barWidth && x + dx < (ushort)ctx.Bounds.Right; dx++)
            ctx.Buffer.Set((ushort)(x + dx), y, Cell.FromChar('█').WithForeground(color));
    }

    private PackedRgba GetColor(int idx) => _colors.Length > 0 ? _colors[idx % _colors.Length] : PackedRgba.White;

    public Size Measure(Size available) => available;
}

public sealed record BarGroup(string Label, IReadOnlyList<double> Values);

/// <summary>
/// Line chart widget. Renders series as line-connected data points.
/// Matches upstream ftui-extras LineChart.
/// </summary>
public sealed class LineChartWidget : IWidget
{
    private static readonly char[] DotChars = ['·', '․', '‥', '…', '─', '╌', '╍', '╎', '╏', '═'];

    private readonly IReadOnlyList<ChartSeries> _series;
    private double _max = double.NaN;
    private double _min;
    private PackedRgba[] _colors = [PackedRgba.Rgb(0, 255, 255), PackedRgba.Rgb(255, 100, 0), PackedRgba.Rgb(0, 200, 100)];

    public LineChartWidget(IReadOnlyList<ChartSeries> series) => _series = series;

    public LineChartWidget WithMax(double max) { _max = max; return this; }
    public LineChartWidget WithMin(double min) { _min = min; return this; }
    public LineChartWidget WithColors(params PackedRgba[] colors) { _colors = colors; return this; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _series.Count == 0) return;

        var allValues = _series.SelectMany(s => s.Values).DefaultIfEmpty(0);
        var dataMin = double.IsNaN(_min) ? allValues.Min() : _min;
        var dataMax = double.IsNaN(_max) ? allValues.Max() : _max;
        var range = dataMax - dataMin;
        if (range <= 0) range = 1;

        foreach (var (series, si) in _series.Select((s, i) => (s, i)))
        {
            var color = _colors.Length > 0 ? _colors[si % _colors.Length] : PackedRgba.White;
            var values = series.Values;
            if (values.Count < 2) continue;

            for (var i = 0; i < values.Count && i < area.Width; i++)
            {
                var normalized = (values[i] - dataMin) / range;
                var row = (ushort)(area.Bottom - 1 - (ushort)Math.Round(normalized * (area.Height - 1)));
                row = Math.Clamp(row, (ushort)area.Y, (ushort)(area.Bottom - 1));
                var x = (ushort)(area.X + i);
                context.Buffer.Set(x, row, Cell.FromChar('●').WithForeground(color));

                // Draw connecting line to next point
                if (i + 1 < values.Count && i + 1 < area.Width)
                {
                    var nextNorm = (values[i + 1] - dataMin) / range;
                    var nextRow = (ushort)(area.Bottom - 1 - (ushort)Math.Round(nextNorm * (area.Height - 1)));
                    nextRow = Math.Clamp(nextRow, (ushort)area.Y, (ushort)(area.Bottom - 1));
                    DrawLine(context, x, row, (ushort)(x + 1), nextRow, color);
                }
            }

            // Label
            if (!string.IsNullOrEmpty(series.Name) && area.Width > (ushort)series.Name.Length)
            {
                var labelX = (ushort)(area.Right - series.Name.Length - 1);
                var labelY = (ushort)Math.Min(area.Bottom - 1, area.Y + (ushort)si);
                for (var i = 0; i < series.Name.Length; i++)
                    context.Buffer.Set((ushort)(labelX + i), labelY, Cell.FromChar(series.Name[i]).WithForeground(color));
            }
        }
    }

    private static void DrawLine(RuntimeRenderContext ctx, ushort x1, ushort y1, ushort x2, ushort y2, PackedRgba color)
    {
        var dx = Math.Abs((int)x2 - x1);
        var dy = Math.Abs((int)y2 - y1);
        var steps = Math.Max(dx, dy);
        if (steps == 0) return;

        for (var i = 1; i < steps; i++)
        {
            var x = (ushort)(x1 + (x2 - x1) * i / steps);
            var y = (ushort)(y1 + (y2 - y1) * i / steps);
            if (x < (ushort)ctx.Bounds.Right && y < (ushort)ctx.Bounds.Bottom)
                ctx.Buffer.Set(x, y, Cell.FromChar('·').WithForeground(color));
        }
    }

    public Size Measure(Size available) => available;
}

public sealed record ChartSeries(string Name, IReadOnlyList<double> Values);
