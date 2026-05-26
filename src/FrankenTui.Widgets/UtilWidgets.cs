using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Horizontal or vertical rule/divider widget. Matches upstream ftui-widgets rule.
/// </summary>
public sealed class RuleWidget : IWidget
{
    public PackedRgba? Color { get; init; }
    public char Character { get; init; } = '─';

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var fg = Color ?? context.Theme.Default.Foreground;

        if (area.Width >= area.Height) // horizontal
        {
            for (ushort x = (ushort)area.X; x < (ushort)area.Right; x++)
                context.Buffer.Set(x, (ushort)(area.Y + area.Height / 2), Cell.FromChar(Character).WithForeground(fg));
        }
        else // vertical
        {
            for (ushort y = (ushort)area.Y; y < (ushort)area.Bottom; y++)
                context.Buffer.Set((ushort)(area.X + area.Width / 2), y, Cell.FromChar('│').WithForeground(fg));
        }
    }

    public Size Measure(Size available) => new(available.Width, 1);
}

/// <summary>
/// Popover overlay widget. Renders a bordered box at a position.
/// Matches upstream ftui-widgets popover.
/// </summary>
public sealed class PopoverWidget : IWidget
{
    public required IWidget Child { get; init; }
    public string? Title { get; init; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;

        var fg = PackedRgba.White;
        var bg = PackedRgba.Rgb(40, 44, 52);

        // Fill background
        for (ushort y = (ushort)area.Y; y < (ushort)area.Bottom && y < context.Buffer.Height; y++)
            for (ushort x = (ushort)area.X; x < (ushort)area.Right && x < context.Buffer.Width; x++)
                context.Buffer.Set(x, y, new Cell(CellContent.FromChar(' '), fg, bg, CellAttributes.None));

        // Title
        if (!string.IsNullOrEmpty(Title) && area.Height >= 2 && area.Width >= (ushort)(Title.Length + 4))
        {
            var titleX = (ushort)(area.X + 2);
            for (var i = 0; i < Title.Length; i++)
                context.Buffer.Set((ushort)(titleX + i), (ushort)area.Y, Cell.FromChar(Title[i]).WithForeground(PackedRgba.Rgb(100, 180, 255)));
        }

        // Child
        if (area.Height >= 2 && area.Width >= 2)
        {
            var childArea = new Rect((ushort)(area.X + 1), (ushort)(area.Y + 1),
                (ushort)(area.Width - 2), (ushort)(area.Height - 2));
            Child.Render(new RuntimeRenderContext(context.Buffer, childArea, context.Theme));
        }
    }

    public Size Measure(Size available) => available;
}
