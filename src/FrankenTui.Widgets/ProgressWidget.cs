using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class ProgressWidget : IWidget
{
    public double Value { get; init; }

    public string? Label { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        var clamped = Math.Clamp(Value, 0d, 1d);
        var filled = (int)Math.Round((context.Bounds.Width - 2) * clamped, MidpointRounding.AwayFromZero);
        var bar = $"[{new string('=', Math.Max(0, filled))}{new string(' ', Math.Max(0, context.Bounds.Width - 2 - filled))}]";
        BufferPainter.WriteText(context.Buffer, context.Bounds.X, context.Bounds.Y, bar, context.Theme.Accent.ToCell());
        if (!string.IsNullOrWhiteSpace(Label) && context.Bounds.Height > 1)
        {
            BufferPainter.WriteText(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + 1), Label, context.Theme.Default.ToCell());
        }
    }
}
