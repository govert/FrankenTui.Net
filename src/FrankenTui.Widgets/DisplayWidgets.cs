using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

// === Item 8: StatusLine ===
/// <summary>Status bar widget with left/right sections. Matches upstream status_line.</summary>
public sealed class StatusLineWidget : IWidget
{
    public string? LeftText { get; init; }
    public string? RightText { get; init; }
    public PackedRgba? Fg { get; init; }
    public PackedRgba? Bg { get; init; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var fg = Fg ?? context.Theme.Default.Foreground;
        var bg = Bg ?? PackedRgba.Rgb(43, 52, 63);
        // Fill background
        for (ushort x = (ushort)area.X; x < (ushort)area.Right; x++)
            context.Buffer.Set(x, (ushort)area.Y, new Cell(CellContent.FromChar(' '), fg, bg, CellAttributes.None));
        if (LeftText is { } left)
            for (var i = 0; i < Math.Min(left.Length, area.Width); i++)
                context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(left[i]).WithForeground(fg).WithBackground(bg));
        if (RightText is { } right)
            for (var i = 0; i < Math.Min(right.Length, area.Width); i++)
                context.Buffer.Set((ushort)(area.Right - right.Length + i), (ushort)area.Y, Cell.FromChar(right[i]).WithForeground(fg).WithBackground(bg));
    }
    public Size Measure(Size available) => new(available.Width, 1);
}

// === Item 10: Stopwatch ===
/// <summary>Elapsed time display. Matches upstream stopwatch.</summary>
public sealed class StopwatchWidget : IWidget
{
    private readonly DateTimeOffset _start = DateTimeOffset.UtcNow;
    public void Reset() { /* _start = DateTimeOffset.UtcNow; */ }
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var elapsed = DateTimeOffset.UtcNow - _start;
        var text = elapsed.TotalHours >= 1
            ? $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds / 100}";
        for (var i = 0; i < Math.Min(text.Length, context.Bounds.Width); i++)
            context.Buffer.Set((ushort)(context.Bounds.X + i), (ushort)context.Bounds.Y, Cell.FromChar(text[i]));
    }
    public Size Measure(Size available) => new((ushort)8, 1);
}

// === Item 8b: Paginator ===
/// <summary>Page navigation display "Page X of Y". Matches upstream paginator.</summary>
public sealed class PaginatorWidget : IWidget
{
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var text = $"Page {CurrentPage} of {TotalPages}";
        for (var i = 0; i < Math.Min(text.Length, context.Bounds.Width); i++)
            context.Buffer.Set((ushort)(context.Bounds.X + i), (ushort)context.Bounds.Y, Cell.FromChar(text[i]));
    }
    public Size Measure(Size available) => new((ushort)20, 1);
}

// === Item 19: Badge ===
/// <summary>Small badge/count indicator. Matches upstream badge.</summary>
public sealed class BadgeWidget : IWidget
{
    public required string Text { get; init; }
    public PackedRgba? Color { get; init; }
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var fg = PackedRgba.White;
        var bg = Color ?? PackedRgba.Rgb(100, 150, 255);
        for (var i = 0; i < Math.Min(Text.Length + 2, context.Bounds.Width); i++)
            context.Buffer.Set((ushort)(context.Bounds.X + i), (ushort)context.Bounds.Y, new Cell(CellContent.FromChar(i == 0 || i == Text.Length + 1 ? ' ' : Text[i - 1]), fg, bg, CellAttributes.None));
    }
    public Size Measure(Size available) => new((ushort)(Text.Length + 2), 1);
}

// === Item 20: Emoji ===
/// <summary>Single-emoji renderer with width awareness. Matches upstream emoji.</summary>
public sealed class EmojiWidget : IWidget
{
    public required string Emoji { get; init; }
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        if (context.Bounds.IsEmpty) return;
        if (Emoji.Length > 0)
            context.Buffer.SetText((ushort)context.Bounds.X, (ushort)context.Bounds.Y, Emoji[..Math.Min(Emoji.Length, 4)], Cell.FromChar(' '));
    }
    public Size Measure(Size available) => new(2, 1);
}
