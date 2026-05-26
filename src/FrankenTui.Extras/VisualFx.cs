using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

/// <summary>Quality level for visual effects. Matches upstream FxQuality.</summary>
public enum FxQuality { Off, Minimal, Reduced, Full }

/// <summary>Theme color inputs for visual effects. Matches upstream ThemeInputs.</summary>
public readonly record struct FxThemeInputs(
    PackedRgba Background, PackedRgba Accent, PackedRgba Secondary, PackedRgba Muted)
{
    public static FxThemeInputs Default => new(
        PackedRgba.Rgb(20, 24, 31),
        PackedRgba.Rgb(100, 180, 255),
        PackedRgba.Rgb(200, 120, 50),
        PackedRgba.Rgb(80, 90, 100));
}

/// <summary>Plasma visual effect. Matches upstream PlasmaFx.</summary>
public sealed class PlasmaFx : IWidget
{
    private double _time;
    private readonly double[] _buffer;
    private readonly ushort _width;
    private readonly ushort _height;
    private FxQuality _quality = FxQuality.Full;
    private FxThemeInputs _theme = FxThemeInputs.Default;

    public PlasmaFx(ushort width, ushort height)
    {
        _width = Math.Max(width, (ushort)1);
        _height = Math.Max(height, (ushort)1);
        _buffer = new double[_width * _height];
    }

    public void SetQuality(FxQuality q) => _quality = q;
    public void SetTheme(FxThemeInputs t) => _theme = t;
    public void Tick(double dt) => _time += dt;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (_quality == FxQuality.Off) return;
        var area = context.Bounds;
        if (area.IsEmpty) return;

        var step = _quality == FxQuality.Minimal ? 2.0 : 1.0;
        for (ushort y = 0; y < Math.Min(area.Height, _height); y++)
        {
            for (ushort x = 0; x < Math.Min(area.Width, _width); x++)
            {
                var nx = x / (double)_width;
                var ny = y / (double)_height;
                var v = PlasmaSample(nx * 6.0 + _time * 0.5, ny * 6.0);
                v += PlasmaSample(-nx * 4.0 + _time * 0.3, -ny * 4.0 + 1.0) * 0.5;
                v += PlasmaSample(nx * 2.0 - _time * 0.7, ny * 2.0 + 2.0) * 0.3;
                v = (v + 1.5) / 3.0;

                var color = PlasmaGradient(Math.Clamp(v, 0.0, 1.0));
                context.Buffer.Set((ushort)(area.X + x), (ushort)(area.Y + y),
                    Cell.FromChar(' ').WithBackground(color));
            }
        }
    }

    private static double PlasmaSample(double x, double y) =>
        Math.Sin(x * 1.2 + y * 0.8) + Math.Cos(y * 0.7 - x * 0.3) + Math.Sin((x + y) * 0.5);

    private PackedRgba PlasmaGradient(double t)
    {
        // Simple gradient: dark blue → purple → orange → white
        if (t < 0.25) return LerpColor(_theme.Background, _theme.Accent, (float)(t * 4));
        if (t < 0.5) return LerpColor(_theme.Accent, _theme.Secondary, (float)((t - 0.25) * 4));
        if (t < 0.75) return LerpColor(_theme.Secondary, PackedRgba.White, (float)((t - 0.5) * 4));
        return PackedRgba.White;
    }

    private static PackedRgba LerpColor(PackedRgba a, PackedRgba b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return PackedRgba.Rgba(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            255);
    }

    public Size Measure(Size available) => new(_width, _height);
}

/// <summary>Metaball visual effect. Matches upstream MetaballsFx.</summary>
public sealed class MetaballsFx : IWidget
{
    private double _time;
    private readonly List<(double X, double Y, double R, double Vx, double Vy)> _balls = [];
    private FxQuality _quality = FxQuality.Full;
    private FxThemeInputs _theme = FxThemeInputs.Default;

    public MetaballsFx(int count = 5)
    {
        var rng = new Random(42);
        for (var i = 0; i < count; i++)
            _balls.Add((rng.NextDouble(), rng.NextDouble(), 0.05 + rng.NextDouble() * 0.1,
                (rng.NextDouble() - 0.5) * 0.3, (rng.NextDouble() - 0.5) * 0.3));
    }

    public void Tick(double dt)
    {
        _time += dt;
        for (var i = 0; i < _balls.Count; i++)
        {
            var (x, y, r, vx, vy) = _balls[i];
            x += vx * dt; y += vy * dt;
            if (x < 0 || x > 1) vx = -vx;
            if (y < 0 || y > 1) vy = -vy;
            _balls[i] = (Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1), r, vx, vy);
        }
    }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (_quality == FxQuality.Off) return;
        var area = context.Bounds;
        if (area.IsEmpty) return;

        for (ushort py = 0; py < area.Height; py++)
        {
            for (ushort px = 0; px < area.Width; px++)
            {
                var nx = px / (double)area.Width;
                var ny = py / (double)area.Height;
                double field = 0;
                foreach (var (bx, by, br, _, _) in _balls)
                {
                    var dx = nx - bx;
                    var dy = ny - by;
                    field += br / Math.Max(0.001, dx * dx + dy * dy);
                }
                var t = Math.Clamp(field * 0.02, 0.0, 1.0);
                var color = LerpColor(_theme.Background, _theme.Accent, (float)t);
                context.Buffer.Set((ushort)(area.X + px), (ushort)(area.Y + py),
                    Cell.FromChar(' ').WithBackground(color));
            }
        }
    }

    private static PackedRgba LerpColor(PackedRgba a, PackedRgba b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return PackedRgba.Rgba((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t), 255);
    }

    public Size Measure(Size available) => available;
}
