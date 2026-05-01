using FrankenTui.Core;
using FrankenTui.Runtime;
using FrankenTui.Style;

namespace FrankenTui.Widgets;

public static class WidgetRenderHelpers
{
    public static bool ApplyStyling(RuntimeRenderContext context) =>
        context.DegradationLevel < RuntimeDegradationLevel.NoStyling;

    public static bool RenderDecorative(RuntimeRenderContext context) =>
        context.DegradationLevel < RuntimeDegradationLevel.EssentialOnly;

    public static bool RenderContent(RuntimeRenderContext context) =>
        context.DegradationLevel < RuntimeDegradationLevel.Skeleton;

    public static void ClearTextArea(RuntimeRenderContext context, UiStyle style)
    {
        if (context.Bounds.IsEmpty)
        {
            return;
        }

        context.Buffer.Fill(context.Bounds, style.ToCell());
    }

    public static void ClearTextRow(RuntimeRenderContext context, ushort rowOffset, UiStyle style)
    {
        if (rowOffset >= context.Bounds.Height || context.Bounds.Width == 0)
        {
            return;
        }

        context.Buffer.Fill(
            new Rect(
                context.Bounds.X,
                (ushort)(context.Bounds.Y + rowOffset),
                context.Bounds.Width,
                1),
            style.ToCell());
    }
}
