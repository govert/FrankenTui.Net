using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Extras;

public enum CanvasMode
{
    Braille,
    Block,
    HalfBlock
}

public readonly record struct CanvasPixelRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public int Left => X;

    public int Top => Y;

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool Contains(int x, int y) =>
        x >= X && x < Right && y >= Y && y < Bottom;

    public static CanvasPixelRect? FromCellIntersection(Rect cellRect, Rect canvasArea, CanvasMode mode)
    {
        if (!cellRect.TryIntersection(canvasArea, out var clipped))
        {
            return null;
        }

        var cols = ColsPerCell(mode);
        var rows = RowsPerCell(mode);
        var rect = new CanvasPixelRect(
            (clipped.X - canvasArea.X) * cols,
            (clipped.Y - canvasArea.Y) * rows,
            clipped.Width * cols,
            clipped.Height * rows);
        return rect.IsEmpty ? null : rect;
    }

    public static int ColsPerCell(CanvasMode mode) =>
        mode switch
        {
            CanvasMode.HalfBlock => 1,
            _ => 2
        };

    public static int RowsPerCell(CanvasMode mode) =>
        mode switch
        {
            CanvasMode.Braille => 4,
            _ => 2
        };
}

public sealed class CanvasPainter
{
    private PackedRgba[]? _colors;
    private bool[] _pixels;
    private ushort _width, _height;

    public CanvasPainter(ushort width, ushort height, CanvasMode mode = CanvasMode.Braille, bool colored = false)
    {
        _width = Math.Max(width, (ushort)1);
        _height = Math.Max(height, (ushort)1);
        Mode = mode;
        _pixels = new bool[_width * _height];
        if (colored) _colors = new PackedRgba[_width * _height];
    }

    public ushort Width => _width;

    public ushort Height => _height;

    public CanvasMode Mode { get; set; }

    public void Point(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        _pixels[y * Width + x] = true;
    }

    public void PointColored(int x, int y, PackedRgba color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        var idx = y * Width + x;
        _pixels[idx] = true;
        if (_colors != null) _colors[idx] = color;
    }

    public void LineColored(int x0, int y0, int x1, int y1, PackedRgba color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            PointColored(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void Clear()
    {
        Array.Clear(_pixels, 0, _pixels.Length);
        if (_colors != null) Array.Clear(_colors, 0, _colors.Length);
    }

    public void EnsureSize(ushort w, ushort h)
    {
        if (w != _width || h != _height)
        {
            _width = Math.Max(w, (ushort)1);
            _height = Math.Max(h, (ushort)1);
            var len = _width * _height;
            if (_pixels.Length < len) _pixels = new bool[len];
            if (_colors != null && _colors.Length < len) _colors = new PackedRgba[len];
        }
    }

    public void EnsureForArea(Rect area, CanvasMode mode)
    {
        Mode = mode;
        EnsureSize((ushort)(area.Width * 2), (ushort)(area.Height * 4));
    }

    public PackedRgba GetColor(int x, int y) =>
        _colors != null && x >= 0 && y >= 0 && x < Width && y < Height
            ? _colors[y * Width + x] : PackedRgba.White;

    public void Line(int x1, int y1, int x2, int y2)
    {
        var dx = Math.Abs(x2 - x1);
        var dy = -Math.Abs(y2 - y1);
        var sx = x1 < x2 ? 1 : -1;
        var sy = y1 < y2 ? 1 : -1;
        var err = dx + dy;
        while (true)
        {
            Point(x1, y1);
            if (x1 == x2 && y1 == y2) break;
            var e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }

    public void Rect(int x, int y, int w, int h)
    {
        Line(x, y, x + w, y);
        Line(x + w, y, x + w, y + h);
        Line(x + w, y + h, x, y + h);
        Line(x, y + h, x, y);
    }

    public void Circle(int cx, int cy, int radius)
    {
        var x = radius;
        var y = 0;
        var err = 0;
        while (x >= y)
        {
            Point(cx + x, cy + y);
            Point(cx + y, cy + x);
            Point(cx - y, cy + x);
            Point(cx - x, cy + y);
            Point(cx - x, cy - y);
            Point(cx - y, cy - x);
            Point(cx + y, cy - x);
            Point(cx + x, cy - y);
            if (err <= 0) { y += 1; err += 2 * y + 1; }
            if (err > 0) { x -= 1; err -= 2 * x + 1; }
        }
    }

    public void Render(Rect area, RenderBuffer buffer, Cell template) =>
        RenderCore(area, buffer, template, exclude: null);

    public void RenderExcluding(Rect area, RenderBuffer buffer, Cell template, Rect exclude) =>
        RenderCore(area, buffer, template, exclude);

    private void RenderCore(Rect area, RenderBuffer buffer, Cell template, Rect? exclude)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (Mode != CanvasMode.Braille || area.IsEmpty)
        {
            return;
        }

        var cellsWide = Math.Min(area.Width, (ushort)Math.Ceiling(Width / 2.0));
        var cellsHigh = Math.Min(area.Height, (ushort)Math.Ceiling(Height / 4.0));
        for (ushort cellY = 0; cellY < cellsHigh; cellY++)
        {
            for (ushort cellX = 0; cellX < cellsWide; cellX++)
            {
                var absoluteX = (ushort)(area.X + cellX);
                var absoluteY = (ushort)(area.Y + cellY);
                if (exclude is { } excluded && excluded.Contains(absoluteX, absoluteY))
                {
                    continue;
                }

                var pattern = BraillePattern(cellX * 2, cellY * 4);
                if (pattern == 0)
                {
                    continue;
                }

                buffer.Set(absoluteX, absoluteY, template.WithChar((char)(0x2800 + pattern)));
            }
        }
    }

    private int BraillePattern(int originX, int originY)
    {
        var pattern = 0;
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                if (IsSet(originX + x, originY + y))
                {
                    pattern |= BrailleBit(x, y);
                }
            }
        }

        return pattern;
    }

    private bool IsSet(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height && _pixels[y * Width + x];

    private static int BrailleBit(int x, int y) =>
        (x, y) switch
        {
            (0, 0) => 0x01,
            (0, 1) => 0x02,
            (0, 2) => 0x04,
            (0, 3) => 0x40,
            (1, 0) => 0x08,
            (1, 1) => 0x10,
            (1, 2) => 0x20,
            (1, 3) => 0x80,
            _ => 0
        };
}

public sealed class CanvasWidget : IWidget
{
    public required CanvasPainter Painter { get; init; }

    public Cell Template { get; init; } = Cell.FromChar(' ');

    public Rect? Exclude { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        if (context.Bounds.IsEmpty)
        {
            return;
        }

        if (Exclude is { } excluded)
        {
            Painter.RenderExcluding(context.Bounds, context.Buffer, Template, excluded);
        }
        else
        {
            Painter.Render(context.Bounds, context.Buffer, Template);
        }
    }
}
