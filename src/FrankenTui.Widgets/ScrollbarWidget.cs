using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class ScrollbarWidget : IWidget
{
    public int TotalItems { get; init; }

    public int ViewportItems { get; init; }

    public int Offset { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        if (!WidgetRenderHelpers.RenderDecorative(context))
        {
            WidgetRenderHelpers.ClearTextArea(context, context.Theme.Default);
            return;
        }

        if (context.Bounds.IsEmpty || TotalItems <= 0 || ViewportItems <= 0)
        {
            return;
        }

        var thumbGlyph = context.DegradationLevel >= RuntimeDegradationLevel.SimpleBorders ? '|' : '█';
        var height = Math.Max(1, (int)context.Bounds.Height);
        var thumbSize = Math.Max(1, (height * ViewportItems) / TotalItems);
        var thumbStart = Math.Min(height - thumbSize, (height * Offset) / Math.Max(TotalItems, 1));
        for (ushort row = 0; row < context.Bounds.Height; row++)
        {
            var style = WidgetRenderHelpers.ApplyStyling(context)
                ? row >= thumbStart && row < thumbStart + thumbSize
                    ? context.Theme.Accent
                    : context.Theme.Border
                : context.Theme.Default;
            context.Buffer.Set(context.Bounds.X, (ushort)(context.Bounds.Y + row), style.ToCell(thumbGlyph));
        }
    }
}
