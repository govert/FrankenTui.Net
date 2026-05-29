using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

/// <summary>
/// Drawing primitives on a Buffer. Matches upstream Draw trait.
/// </summary>
public static class BufferDrawExtensions
{
    /// <summary>
    /// Draw a rectangle border on the buffer using the given BorderSet character set.
    /// </summary>
    public static void DrawBorder(this FrankenTui.Render.Buffer buffer, Rect area, BorderSet chars, Cell template)
    {
        if (area.IsEmpty) return;
        var r = (ushort)(area.Bottom - 1);
        for (ushort x = (ushort)area.X; x < (ushort)area.Right; x++)
        {
            buffer.Set(x, (ushort)area.Y, template.WithChar(chars.Horizontal));
            if (r > area.Y) buffer.Set(x, r, template.WithChar(chars.Horizontal));
        }
        for (ushort y = (ushort)(area.Y + 1); y < r; y++)
        {
            buffer.Set((ushort)area.X, y, template.WithChar(chars.Vertical));
            buffer.Set((ushort)(area.Right - 1), y, template.WithChar(chars.Vertical));
        }
        buffer.Set((ushort)area.X, (ushort)area.Y, template.WithChar(chars.TopLeft));
        buffer.Set((ushort)(area.Right - 1), (ushort)area.Y, template.WithChar(chars.TopRight));
        buffer.Set((ushort)area.X, r, template.WithChar(chars.BottomLeft));
        buffer.Set((ushort)(area.Right - 1), r, template.WithChar(chars.BottomRight));
    }

    public static void DrawText(this FrankenTui.Render.Buffer buffer, ushort x, ushort y, string text, Cell template)
    {
        for (var i = 0; i < text.Length && x + i < buffer.Width; i++)
            buffer.Set((ushort)(x + i), y, template.WithChar(text[i]));
    }

    public static void DrawFilled(this FrankenTui.Render.Buffer buffer, Rect area, Cell template)
    {
        for (ushort cy = (ushort)area.Y; cy < (ushort)area.Bottom && cy < buffer.Height; cy++)
            for (ushort cx = (ushort)area.X; cx < (ushort)area.Right && cx < buffer.Width; cx++)
                buffer.Set(cx, cy, template);
    }
}
