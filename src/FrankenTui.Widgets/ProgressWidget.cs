using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class ProgressWidget : IWidget
{
    public double Value { get; init; }

    public string? Label { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        WidgetRenderHelpers.ClearTextArea(context, context.Theme.Default);
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            return;
        }

        var clamped = Math.Clamp(Value, 0d, 1d);
        if (!WidgetRenderHelpers.RenderDecorative(context))
        {
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                context.Bounds.Y,
                $"{(int)Math.Round(clamped * 100, MidpointRounding.AwayFromZero)}%",
                context.Theme.Default.ToCell());
            return;
        }

        var filled = (int)Math.Round((context.Bounds.Width - 2) * clamped, MidpointRounding.AwayFromZero);
        var fill = WidgetRenderHelpers.ApplyStyling(context) ? '=' : '#';
        var bar = $"[{new string(fill, Math.Max(0, filled))}{new string(' ', Math.Max(0, context.Bounds.Width - 2 - filled))}]";
        var barStyle = WidgetRenderHelpers.ApplyStyling(context) ? context.Theme.Accent : context.Theme.Default;
        WidgetRenderHelpers.ClearTextRow(context, 0, barStyle);
        BufferPainter.WriteText(context.Buffer, context.Bounds.X, context.Bounds.Y, bar, barStyle.ToCell());
        if (!string.IsNullOrWhiteSpace(Label) && context.Bounds.Height > 1)
        {
            WidgetRenderHelpers.ClearTextRow(context, 1, context.Theme.Default);
            BufferPainter.WriteText(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + 1), Label, context.Theme.Default.ToCell());
        }
    }
}
