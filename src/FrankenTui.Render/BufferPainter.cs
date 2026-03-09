using FrankenTui.Core;

namespace FrankenTui.Render;

public static class BufferPainter
{
    public static void WriteText(
        Buffer buffer,
        ushort x,
        ushort y,
        string text,
        Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(text);

        var column = x;
        foreach (var rune in text.EnumerateRunes())
        {
            var width = Math.Max(TerminalTextWidth.RuneWidth(rune), 1);
            if (column >= buffer.Width)
            {
                break;
            }

            buffer.Set(column, y, template.WithRune(rune));
            if (column > ushort.MaxValue - width)
            {
                break;
            }

            column += (ushort)width;
        }
    }

    public static void DrawHorizontalLine(Buffer buffer, ushort x, ushort y, ushort width, Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        for (ushort offset = 0; offset < width; offset++)
        {
            if (x + offset >= buffer.Width)
            {
                break;
            }

            buffer.Set((ushort)(x + offset), y, template);
        }
    }

    public static void DrawVerticalLine(Buffer buffer, ushort x, ushort y, ushort height, Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        for (ushort offset = 0; offset < height; offset++)
        {
            if (y + offset >= buffer.Height)
            {
                break;
            }

            buffer.Set(x, (ushort)(y + offset), template);
        }
    }

    public static void DrawBorder(Buffer buffer, Rect rect, Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (rect.IsEmpty)
        {
            return;
        }

        var left = rect.X;
        var right = (ushort)(rect.Right - 1);
        var top = rect.Y;
        var bottom = (ushort)(rect.Bottom - 1);

        DrawHorizontalLine(buffer, left, top, rect.Width, template.WithChar('─'));
        DrawHorizontalLine(buffer, left, bottom, rect.Width, template.WithChar('─'));
        DrawVerticalLine(buffer, left, top, rect.Height, template.WithChar('│'));
        DrawVerticalLine(buffer, right, top, rect.Height, template.WithChar('│'));
        buffer.Set(left, top, template.WithChar('┌'));
        buffer.Set(right, top, template.WithChar('┐'));
        buffer.Set(left, bottom, template.WithChar('└'));
        buffer.Set(right, bottom, template.WithChar('┘'));
    }
}
