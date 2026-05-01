using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;

namespace FrankenTui.Widgets;

public sealed class BlockWidget : IWidget
{
    public string? Title { get; init; }

    public UiStyle? BorderStyle { get; init; }

    public IWidget? Child { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            WidgetRenderHelpers.ClearTextArea(context, context.Theme.Default);
            return;
        }

        if (!WidgetRenderHelpers.RenderDecorative(context))
        {
            WidgetRenderHelpers.ClearTextArea(context, context.Theme.Default);
            return;
        }

        var style = WidgetRenderHelpers.ApplyStyling(context)
            ? BorderStyle ?? context.Theme.Border
            : context.Theme.Default;
        if (context.DegradationLevel >= RuntimeDegradationLevel.SimpleBorders)
        {
            DrawAsciiBorder(context, style);
        }
        else
        {
            BufferPainter.DrawBorder(context.Buffer, context.Bounds, style.ToCell());
        }

        if (!string.IsNullOrWhiteSpace(Title) && context.Bounds.Width > 4)
        {
            var titleStyle = WidgetRenderHelpers.ApplyStyling(context) ? context.Theme.Title : context.Theme.Default;
            BufferPainter.WriteText(context.Buffer, (ushort)(context.Bounds.X + 2), context.Bounds.Y, $" {Title} ", titleStyle.ToCell());
        }

        if (Child is not null)
        {
            var inner = context.Bounds.Inner(1);
            if (!inner.IsEmpty)
            {
                Child.Render(context.WithBounds(inner));
            }
        }
    }

    private static void DrawAsciiBorder(RuntimeRenderContext context, UiStyle style)
    {
        if (context.Bounds.IsEmpty)
        {
            return;
        }

        var left = context.Bounds.X;
        var right = (ushort)(context.Bounds.Right - 1);
        var top = context.Bounds.Y;
        var bottom = (ushort)(context.Bounds.Bottom - 1);
        var cell = style.ToCell();
        BufferPainter.DrawHorizontalLine(context.Buffer, left, top, context.Bounds.Width, cell.WithChar('-'));
        BufferPainter.DrawHorizontalLine(context.Buffer, left, bottom, context.Bounds.Width, cell.WithChar('-'));
        BufferPainter.DrawVerticalLine(context.Buffer, left, top, context.Bounds.Height, cell.WithChar('|'));
        BufferPainter.DrawVerticalLine(context.Buffer, right, top, context.Bounds.Height, cell.WithChar('|'));
        context.Buffer.Set(left, top, cell.WithChar('+'));
        context.Buffer.Set(right, top, cell.WithChar('+'));
        context.Buffer.Set(left, bottom, cell.WithChar('+'));
        context.Buffer.Set(right, bottom, cell.WithChar('+'));
    }
}
