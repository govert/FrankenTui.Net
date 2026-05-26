using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

/// <summary>Glowing/animated text effect. Matches upstream glowing_text.</summary>
public sealed class GlowingTextWidget : IWidget
{
    public string Text { get; init; } = "";
    public PackedRgba? Color { get; init; }
    public PackedRgba? GlowColor { get; init; }
    public double Phase { get; set; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || string.IsNullOrEmpty(Text)) return;
        var baseColor = Color ?? PackedRgba.Rgb(100, 200, 255);
        var glow = GlowColor ?? PackedRgba.Rgb(200, 240, 255);
        var glowPhase = Math.Sin(Phase * 3.0) * 0.5 + 0.5;

        for (var i = 0; i < Math.Min(Text.Length, area.Width); i++)
        {
            var charPhase = Math.Sin(Phase * 3.0 + i * 0.3) * 0.5 + 0.5;
            var color = charPhase > 0.7 ? glow : baseColor;
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y,
                Cell.FromChar(Text[i]).WithForeground(color));
        }
    }

    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)Text.Length), 1);
}

/// <summary>Image widget using Braille/block characters to display pixel data. Matches upstream image.</summary>
public sealed class ImageWidget : IWidget
{
    private readonly byte[] _pixels;
    private readonly ushort _imgWidth;
    private readonly ushort _imgHeight;
    private bool _useBraille = true;

    public ImageWidget(byte[] pixels, ushort width, ushort height)
    {
        _pixels = pixels;
        _imgWidth = Math.Max(width, (ushort)1);
        _imgHeight = Math.Max(height, (ushort)1);
    }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;

        if (_useBraille)
        {
            // Braille: 2x4 subpixels per cell
            var cellsW = Math.Min(area.Width, (ushort)((_imgWidth + 1) / 2));
            var cellsH = Math.Min(area.Height, (ushort)((_imgHeight + 3) / 4));
            for (ushort cy = 0; cy < cellsH; cy++)
            {
                for (ushort cx = 0; cx < cellsW; cx++)
                {
                    var pattern = BraillePattern(cx * 2, cy * 4);
                    if (pattern > 0)
                        context.Buffer.Set((ushort)(area.X + cx), (ushort)(area.Y + cy),
                            Cell.FromChar((char)(0x2800 + pattern)));
                }
            }
        }
        else
        {
            // Simple: pixel → block character
            for (ushort y = 0; y < Math.Min(area.Height, _imgHeight); y++)
            {
                for (ushort x = 0; x < Math.Min(area.Width, _imgWidth); x++)
                {
                    var idx = y * _imgWidth + x;
                    var ch = idx < _pixels.Length && _pixels[idx] > 128 ? '█' : ' ';
                    context.Buffer.Set((ushort)(area.X + x), (ushort)(area.Y + y), Cell.FromChar(ch));
                }
            }
        }
    }

    private int BraillePattern(int ox, int oy)
    {
        var p = 0;
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 2; x++)
                if (PixelAt(ox + x, oy + y)) p |= (x, y) switch
                {
                    (0, 0) => 1, (0, 1) => 2, (0, 2) => 4, (0, 3) => 0x40,
                    (1, 0) => 8, (1, 1) => 0x10, (1, 2) => 0x20, (1, 3) => 0x80, _ => 0
                };
        return p;
    }

    private bool PixelAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _imgWidth || y >= _imgHeight) return false;
        var idx = y * _imgWidth + x;
        return idx < _pixels.Length && _pixels[idx] > 128;
    }

    public Size Measure(Size available) => new(_imgWidth, _imgHeight);
}
