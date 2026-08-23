// Ported from crates/ftui-render/src/drawing.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: FC0900F0C4531C1002879AF8356F54B8F38EB06125DC34742E82F6D0C3546053.

using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Render;

/// <summary>Border corner and edge characters for drawing a rectangle.
/// <summary>
/// Matches upstream ftui-render/src/drawing.rs BorderChars.
/// This is the render-level type (6 basic chars).
/// Higher-level presets (with tees, cross) live in FrankenTui.Widgets.BorderSet.
/// </summary>
public readonly record struct BorderChars(
    char Vertical, char Horizontal,
    char TopLeft, char TopRight, char BottomLeft, char BottomRight)
{
    public static readonly BorderChars Rounded = new('│', '─', '╭', '╮', '╰', '╯');
    public static readonly BorderChars Square = new('│', '─', '┌', '┐', '└', '┘');
    public static readonly BorderChars Double = new('║', '═', '╔', '╗', '╚', '╝');
    public static readonly BorderChars Heavy = new('┃', '━', '┏', '┓', '┗', '┛');
    public static readonly BorderChars Ascii = new('|', '-', '+', '+', '+', '+');
}

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
        foreach (var textElement in TerminalTextWidth.EnumerateTextElements(text))
        {
            var width = Math.Max(TerminalTextWidth.TextElementWidth(textElement), 1);
            if (column >= buffer.Width)
            {
                break;
            }

            buffer.SetText(column, y, textElement, template);
            if (column > ushort.MaxValue - width)
            {
                break;
            }

            column += (ushort)width;
        }
    }

    /// <summary>
    /// Source-shaped text drawing. Multi-scalar graphemes render their first
    /// scalar and preserve the grapheme display width with styled spaces.
    /// </summary>
    public static ushort PrintText(
        this Buffer buffer,
        ushort x,
        ushort y,
        string text,
        Cell baseCell)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return PrintTextClipped(buffer, x, y, text, baseCell, buffer.Width);
    }

    public static ushort PrintTextClipped(
        this Buffer buffer,
        ushort x,
        ushort y,
        string text,
        Cell baseCell,
        ushort maxX)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(text);

        var column = x;
        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(text))
        {
            if (column >= maxX)
            {
                break;
            }

            var first = Rune.GetRuneAt(grapheme, 0);
            var renderedContent = CellContent.FromRune(first);
            var renderedWidth = renderedContent.Width();
            var width = TerminalTextWidth.TextElementWidth(grapheme);
            if (width == 0)
            {
                width = renderedWidth;
            }

            width = Math.Max(width, renderedWidth);
            if (width == 0)
            {
                continue;
            }

            if ((uint)column + (uint)width > maxX)
            {
                break;
            }

            buffer.SetFast(column, y, baseCell.WithContent(renderedContent));
            if (renderedWidth < width)
            {
                var filler = baseCell.WithChar(' ');
                for (var offset = renderedWidth; offset < width; offset++)
                {
                    buffer.SetFast(SaturatingAdd(column, offset), y, filler);
                }
            }

            column = SaturatingAdd(column, width);
        }

        return column;
    }

    public static void DrawHorizontalLine(
        this Buffer buffer,
        ushort x,
        ushort y,
        ushort width,
        Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        for (ushort offset = 0; offset < width; offset++)
        {
            buffer.SetFast(SaturatingAdd(x, offset), y, template);
        }
    }

    public static void DrawVerticalLine(
        this Buffer buffer,
        ushort x,
        ushort y,
        ushort height,
        Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        for (ushort offset = 0; offset < height; offset++)
        {
            buffer.SetFast(x, SaturatingAdd(y, offset), template);
        }
    }

    public static void DrawRectFilled(this Buffer buffer, Rect rect, Cell cell)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        buffer.Fill(rect, cell);
    }

    public static void DrawRectOutline(this Buffer buffer, Rect rect, Cell cell)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (rect.IsEmpty)
        {
            return;
        }

        buffer.DrawHorizontalLine(rect.X, rect.Y, rect.Width, cell);
        if (rect.Height > 1)
        {
            buffer.DrawHorizontalLine(rect.X, (ushort)(rect.Bottom - 1), rect.Width, cell);
        }

        if (rect.Height > 2)
        {
            buffer.DrawVerticalLine(rect.X, SaturatingAdd(rect.Y, 1), (ushort)(rect.Height - 2), cell);
        }

        if (rect.Width > 1 && rect.Height > 2)
        {
            buffer.DrawVerticalLine(
                (ushort)(rect.Right - 1),
                SaturatingAdd(rect.Y, 1),
                (ushort)(rect.Height - 2),
                cell);
        }
    }

    public static void DrawBorder(
        this Buffer buffer,
        Rect rect,
        BorderChars border,
        Cell template)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (rect.IsEmpty)
        {
            return;
        }

        var horizontal = template.WithChar(border.Horizontal);
        var vertical = template.WithChar(border.Vertical);
        for (var x = rect.Left; x < rect.Right; x++)
        {
            buffer.SetFast(x, rect.Top, horizontal);
        }

        if (rect.Height > 1)
        {
            for (var x = rect.Left; x < rect.Right; x++)
            {
                buffer.SetFast(x, (ushort)(rect.Bottom - 1), horizontal);
            }
        }

        if (rect.Height > 2)
        {
            for (var y = SaturatingAdd(rect.Top, 1); y < rect.Bottom - 1; y++)
            {
                buffer.SetFast(rect.Left, y, vertical);
            }
        }

        if (rect.Width > 1 && rect.Height > 2)
        {
            for (var y = SaturatingAdd(rect.Top, 1); y < rect.Bottom - 1; y++)
            {
                buffer.SetFast((ushort)(rect.Right - 1), y, vertical);
            }
        }

        buffer.SetFast(rect.Left, rect.Top, template.WithChar(border.TopLeft));
        if (rect.Width > 1)
        {
            buffer.SetFast((ushort)(rect.Right - 1), rect.Top, template.WithChar(border.TopRight));
        }

        if (rect.Height > 1)
        {
            buffer.SetFast(rect.Left, (ushort)(rect.Bottom - 1), template.WithChar(border.BottomLeft));
        }

        if (rect.Width > 1 && rect.Height > 1)
        {
            buffer.SetFast(
                (ushort)(rect.Right - 1),
                (ushort)(rect.Bottom - 1),
                template.WithChar(border.BottomRight));
        }
    }

    /// <summary>Compatibility overload retaining the original target argument order.</summary>
    public static void DrawBorder(Buffer buffer, Rect rect, Cell template, BorderChars border) =>
        DrawBorder(buffer, rect, border, template);

    public static void DrawBorder(Buffer buffer, Rect rect, Cell template) =>
        DrawBorder(buffer, rect, BorderChars.Square, template);

    public static void DrawBox(
        this Buffer buffer,
        Rect rect,
        BorderChars border,
        Cell borderCell,
        Cell fillCell)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (rect.IsEmpty)
        {
            return;
        }

        if (rect.Width > 2 && rect.Height > 2)
        {
            buffer.Fill(new Rect(
                SaturatingAdd(rect.X, 1),
                SaturatingAdd(rect.Y, 1),
                (ushort)(rect.Width - 2),
                (ushort)(rect.Height - 2)), fillCell);
        }

        buffer.DrawBorder(rect, border, borderCell);
    }

    public static void PaintArea(
        this Buffer buffer,
        Rect rect,
        PackedRgba? foreground,
        PackedRgba? background)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        buffer.PaintAreaColors(rect, foreground, background);
    }

    /// <summary>Target compatibility overload that also merges style flags.</summary>
    public static void PaintArea(
        this Buffer buffer,
        Rect rect,
        PackedRgba? foreground,
        PackedRgba? background,
        CellStyleFlags? attributes)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        buffer.PaintAreaColors(
            rect,
            foreground,
            background,
            attributes,
            compositeBackground: true);
    }

    private static ushort SaturatingAdd(ushort value, int increment) =>
        (ushort)Math.Min(ushort.MaxValue, (uint)value + (uint)Math.Max(increment, 0));
}
