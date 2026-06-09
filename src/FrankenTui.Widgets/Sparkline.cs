// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/sparkline.rs (650L)
// Compact trend visualization using 8-level Unicode block characters.

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

static class SparkChars { public static readonly char[] Blocks = {' ','▁','▂','▃','▄','▅','▆','▇','█'}; }

public sealed class Sparkline : IWidget
{
    double[] _data; double? _min, _max; WidgetStyle _style; (PackedRgba lo,PackedRgba hi)? _gradient; double _baseline;

    public Sparkline(double[] data) => _data = data;

    public Sparkline Min(double m) { _min = m; return this; }
    public Sparkline Max(double m) { _max = m; return this; }
    public Sparkline Style(WidgetStyle s) { _style = s; return this; }
    public Sparkline Gradient(PackedRgba lo, PackedRgba hi) { _gradient = (lo, hi); return this; }
    public Sparkline Baseline(double b) { _baseline = b; return this; }

    (double min, double max) Bounds()
    {
        if (_data.Length == 0) return (0, 1);
        double min = _min ?? _data.Min(), max = _max ?? _data.Max();
        if (Math.Abs(max - min) < 1e-9) max = min + 1;
        return (min, max);
    }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0 || _data.Length == 0) return;
        var deg = frame.Degradation;
        var style = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextRow(frame, area, style);
        if (!deg.RenderContent() || _data.Length == 0) return;

        var (min, max) = Bounds();
        double range = max - min;
        int n = Math.Min(_data.Length, area.Width);
        for (int i = 0; i < n; i++)
        {
            double v = Math.Clamp(_data[i], min, max);
            int idx = (int)((v - min) / range * 8.0 + 0.5);
            idx = Math.Clamp(idx, 0, 8);
            var cell = Cell.FromChar(SparkChars.Blocks[idx]);
            var cs = style;
            if (_gradient is (var lo, var hi))
            {
                double t = (v - min) / range;
                cs = new WidgetStyle(Interpolate(lo, hi, t), null, null);
            }
            WidgetDrawing.ApplyStyle(ref cell, cs);
            frame.Buffer.SetFast((ushort)(area.X + i), area.Y, cell);
        }
    }

    static PackedRgba Interpolate(PackedRgba a, PackedRgba b, double t)
    {
        byte Interp(byte aa, byte bb) => (byte)(aa + (bb - aa) * t);
        return PackedRgba.Rgba(Interp(a.R, b.R), Interp(a.G, b.G), Interp(a.B, b.B), 255);
    }
}
