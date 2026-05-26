using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Toast notification widget. Single notification with title, message, priority, and style.
/// Matches upstream ftui-widgets toast.
/// </summary>
public sealed class ToastWidget : IWidget
{
    public string? Title { get; init; }
    public required string Message { get; init; }
    public ToastPriority Priority { get; init; } = ToastPriority.Info;
    public PackedRgba? Color { get; init; }
    public bool Dismissible { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;

        var fg = Color ?? PriorityColor();
        var bg = PackedRgba.Rgb(30, 30, 40);
        var icon = PriorityIcon();

        // Fill background
        for (ushort y = (ushort)area.Y; y < (ushort)area.Bottom && y < context.Buffer.Height; y++)
            for (ushort x = (ushort)area.X; x < (ushort)area.Right && x < context.Buffer.Width; x++)
                context.Buffer.Set(x, y, new Cell(CellContent.FromChar(' '), PackedRgba.White, bg, CellAttributes.None));

        // Icon + title on first line
        if (area.Height >= 1)
        {
            var header = string.IsNullOrEmpty(Title) ? $"{icon} {Priority}" : $"{icon} {Title}";
            WriteLine(context, (ushort)area.X, (ushort)area.Y, header, fg, bg);
        }

        // Message on remaining lines
        if (area.Height >= 2 && !string.IsNullOrEmpty(Message))
        {
            var msg = Message;
            var lineY = (ushort)(area.Y + 1);
            while (msg.Length > 0 && lineY < (ushort)area.Bottom)
            {
                var len = Math.Min(msg.Length, area.Width - 2);
                WriteLine(context, (ushort)(area.X + 1), lineY, msg[..len], PackedRgba.White, bg);
                msg = msg[len..];
                lineY++;
            }
        }

        // Dismiss indicator
        if (Dismissible && area.Height >= 1 && area.Width >= 2)
        {
            context.Buffer.Set((ushort)(area.Right - 2), (ushort)area.Y,
                Cell.FromChar('×').WithForeground(PackedRgba.Rgb(150, 150, 150)));
        }
    }

    private PackedRgba PriorityColor() => Priority switch
    {
        ToastPriority.Error => PackedRgba.Rgb(255, 80, 80),
        ToastPriority.Warning => PackedRgba.Rgb(255, 200, 50),
        ToastPriority.Success => PackedRgba.Rgb(80, 220, 80),
        _ => PackedRgba.Rgb(100, 180, 255)
    };

    private static string PriorityIcon() => ""; // No icon, use text label

    private static void WriteLine(RuntimeRenderContext ctx, ushort x, ushort y, string text, PackedRgba fg, PackedRgba bg)
    {
        for (var i = 0; i < text.Length && x + i < ctx.Buffer.Width; i++)
            ctx.Buffer.Set((ushort)(x + i), y, new Cell(CellContent.FromChar(text[i]), fg, bg, CellAttributes.None));
    }

    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)40), Math.Min(available.Height, (ushort)3));
}

public enum ToastPriority { Info, Success, Warning, Error }
