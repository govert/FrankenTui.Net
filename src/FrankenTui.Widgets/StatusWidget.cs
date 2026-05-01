using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class StatusWidget : IWidget
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public bool IsHealthy { get; init; } = true;

    public void Render(RuntimeRenderContext context)
    {
        WidgetRenderHelpers.ClearTextRow(context, 0, context.Theme.Default);
        if (!WidgetRenderHelpers.RenderContent(context) && string.IsNullOrEmpty(Label) && string.IsNullOrEmpty(Value))
        {
            return;
        }

        var valueStyle = WidgetRenderHelpers.ApplyStyling(context)
            ? IsHealthy ? context.Theme.Success : context.Theme.Danger
            : context.Theme.Default;
        BufferPainter.WriteText(context.Buffer, context.Bounds.X, context.Bounds.Y, $"{Label}: ", context.Theme.Default.ToCell());
        BufferPainter.WriteText(context.Buffer, (ushort)(context.Bounds.X + Label.Length + 2), context.Bounds.Y, Value, valueStyle.ToCell());
    }
}
