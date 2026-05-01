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
    private readonly bool[] _pixels;

    public CanvasPainter(ushort width, ushort height, CanvasMode mode = CanvasMode.Braille)
    {
        Width = Math.Max(width, (ushort)1);
        Height = Math.Max(height, (ushort)1);
        Mode = mode;
        _pixels = new bool[Width * Height];
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public CanvasMode Mode { get; }

    public void Point(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        _pixels[y * Width + x] = true;
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
