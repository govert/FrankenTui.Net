using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

/// <summary>Text effect types. Matches upstream TextEffect enum.</summary>
public enum TextEffectKind
{
    None, FadeIn, FadeOut, Pulse, RainbowGradient, Typing, Scramble, GradientFill, GradientAnimated
}

/// <summary>Styled text with effects. Matches upstream StyledText.</summary>
public sealed class TextEffectWidget : IWidget
{
    private string _text = "";
    private TextEffectKind _effect;
    private double _time;
    private double _speed = 1.0;
    private double _progress = 1.0;
    private PackedRgba _baseColor = PackedRgba.White;
    private PackedRgba _secondaryColor = PackedRgba.Rgb(200, 100, 255);
    private double _minAlpha = 0.3;

    public string Text { get => _text; init => _text = value; }
    public TextEffectKind Effect { get => _effect; init => _effect = value; }
    public double Speed { get => _speed; init => _speed = value; }
    public double Progress { get => _progress; init => _progress = value; }
    public PackedRgba BaseColor { get => _baseColor; init => _baseColor = value; }
    public PackedRgba SecondaryColor { get => _secondaryColor; init => _secondaryColor = value; }
    public double MinAlpha { get => _minAlpha; init => _minAlpha = value; }

    public void SetTime(double t) => _time = t;
    public TextEffectWidget WithProgress(double p) { _progress = Math.Clamp(p, 0, 1); return this; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _text.Length == 0) return;

        for (var i = 0; i < Math.Min(_text.Length, area.Width); i++)
        {
            var color = EffectColor(i, _text.Length);
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y,
                Cell.FromChar(_text[i]).WithForeground(color));
        }
    }

    private PackedRgba EffectColor(int idx, int total)
    {
        return _effect switch
        {
            TextEffectKind.FadeIn => FadeByProgress(_baseColor, _progress),
            TextEffectKind.FadeOut => FadeByProgress(_baseColor, 1.0 - _progress),
            TextEffectKind.Pulse => PulseColor(),
            TextEffectKind.RainbowGradient => RainbowColor(idx),
            TextEffectKind.Typing => idx < (int)(_progress * total) ? _baseColor : PackedRgba.Transparent,
            TextEffectKind.GradientFill => LerpColor(_baseColor, _secondaryColor, (double)idx / Math.Max(total - 1, 1)),
            TextEffectKind.GradientAnimated => GradientAnimatedColor(idx, total),
            _ => _baseColor
        };
    }

    private PackedRgba PulseColor()
    {
        var v = Math.Sin(_time * _speed * 4.0) * 0.5 + 0.5;
        var alpha = _minAlpha + (1.0 - _minAlpha) * v;
        return _baseColor.WithOpacity((float)alpha);
    }

    private PackedRgba RainbowColor(int idx)
    {
        var hue = (_time * _speed + idx * 0.15) % 1.0;
        return HueToRgb(hue);
    }

    private PackedRgba GradientAnimatedColor(int idx, int total)
    {
        var t = (idx / (double)Math.Max(total - 1, 1) + _time * _speed) % 1.0;
        return LerpColor(_baseColor, _secondaryColor, t);
    }

    private static PackedRgba FadeByProgress(PackedRgba color, double p) =>
        color.WithOpacity((float)Math.Clamp(p, 0, 1));

    private static PackedRgba HueToRgb(double hue)
    {
        hue = (hue % 1.0 + 1.0) % 1.0;
        var h6 = hue * 6;
        var c = 1.0;
        var x = c * (1 - Math.Abs(h6 % 2 - 1));
        var (r, g, b) = h6 switch
        {
            < 1 => (c, x, 0.0), < 2 => (x, c, 0.0), < 3 => (0.0, c, x),
            < 4 => (0.0, x, c), < 5 => (x, 0.0, c), _ => (c, 0.0, x)
        };
        return PackedRgba.Rgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static PackedRgba LerpColor(PackedRgba a, PackedRgba b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return PackedRgba.Rgba(
            (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t), 255);
    }

    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)_text.Length), 1);
}
