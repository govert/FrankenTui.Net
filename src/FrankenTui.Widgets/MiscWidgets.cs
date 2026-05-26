using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Group widget — wraps a child with an optional border and title.
/// Matches upstream ftui-widgets group.
/// </summary>
public sealed class GroupWidget : IWidget
{
    public required IWidget Child { get; init; }
    public string? Title { get; init; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;

        if (!string.IsNullOrEmpty(Title) && area.Width >= (ushort)(Title.Length + 4))
        {
            var titleX = (ushort)(area.X + 2);
            for (var i = 0; i < Title.Length; i++)
                context.Buffer.Set((ushort)(titleX + i), (ushort)area.Y, Cell.FromChar(Title[i]).WithForeground(PackedRgba.Rgb(150, 170, 190)));
        }

        var childArea = area with { Y = (ushort)(area.Y + 1), Height = (ushort)(area.Height - 1) };
        if (!childArea.IsEmpty)
            Child.Render(new RuntimeRenderContext(context.Buffer, childArea, context.Theme));
    }

    public Size Measure(Size available) => available;
}

/// <summary>
/// Spinner widget — renders a rotating animation character.
/// Matches upstream ftui-widgets spinner.
/// </summary>
public sealed class SpinnerWidget : IWidget
{
    private static readonly char[] Frames = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];
    private int _frame;

    public void Tick() => _frame = (_frame + 1) % Frames.Length;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        context.Buffer.Set((ushort)area.X, (ushort)area.Y,
            Cell.FromChar(Frames[_frame]).WithForeground(PackedRgba.Rgb(100, 200, 255)));
    }

    public Size Measure(Size available) => new(1, 1);
}

/// <summary>
/// Decision card widget — renders a card with title, description, and color-coded decision.
/// Matches upstream ftui-widgets decision_card.
/// </summary>
public sealed class DecisionCardWidget : IWidget
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Decision { get; init; }
    public PackedRgba? Color { get; init; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var fg = PackedRgba.White;
        var bg = PackedRgba.Rgb(30, 35, 45);
        var accent = Color ?? PackedRgba.Rgb(100, 180, 255);

        // Left accent bar
        for (ushort y = (ushort)area.Y; y < (ushort)area.Bottom; y++)
            context.Buffer.Set((ushort)area.X, y, Cell.FromChar(' ').WithForeground(accent).WithBackground(accent));

        // Title
        if (!string.IsNullOrEmpty(Title) && area.Width > 2)
        {
            var t = Title[..Math.Min(Title.Length, area.Width - 2)];
            for (var i = 0; i < t.Length; i++)
                context.Buffer.Set((ushort)(area.X + 2 + i), (ushort)area.Y, Cell.FromChar(t[i]).WithForeground(fg));
        }

        // Description
        if (!string.IsNullOrEmpty(Description) && area.Height >= 2 && area.Width > 2)
        {
            var d = Description[..Math.Min(Description.Length, area.Width - 2)];
            for (var i = 0; i < d.Length; i++)
                context.Buffer.Set((ushort)(area.X + 2 + i), (ushort)(area.Y + 1), Cell.FromChar(d[i]).WithForeground(PackedRgba.Rgb(150, 160, 170)));
        }

        // Decision badge
        if (!string.IsNullOrEmpty(Decision) && area.Height >= 3 && area.Width > 2)
        {
            var dec = Decision[..Math.Min(Decision.Length, area.Width - 2)];
            for (var i = 0; i < dec.Length; i++)
                context.Buffer.Set((ushort)(area.X + 2 + i), (ushort)(area.Y + 2), Cell.FromChar(dec[i]).WithForeground(accent));
        }
    }

    public Size Measure(Size available) => new(available.Width, 3);
}
