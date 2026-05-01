using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

public sealed class BufferInspectorWidget : IWidget
{
    public BufferInspectorWidget(RenderBuffer buffer)
    {
        Buffer = buffer;
    }

    public RenderBuffer Buffer { get; }

    public void Render(RuntimeRenderContext context)
    {
        var style = WidgetRenderHelpers.ApplyStyling(context) ? context.Theme.Muted : context.Theme.Default;
        WidgetRenderHelpers.ClearTextArea(context, style);
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            return;
        }

        var screen = HeadlessBufferView.ScreenText(Buffer);
        for (var row = 0; row < Math.Min(screen.Count, context.Bounds.Height); row++)
        {
            WidgetRenderHelpers.ClearTextRow(context, (ushort)row, style);
            BufferPainter.WriteText(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + row), screen[row], style.ToCell());
        }
    }
}

public sealed class LayoutInspectorWidget : IWidget
{
    public LayoutInspectorWidget(LayoutTrace trace)
    {
        Trace = trace;
    }

    public LayoutTrace Trace { get; }

    public void Render(RuntimeRenderContext context)
    {
        if (context.Bounds.Height == 0)
        {
            return;
        }

        var bodyStyle = WidgetRenderHelpers.ApplyStyling(context) ? context.Theme.Muted : context.Theme.Default;
        var headerStyle = WidgetRenderHelpers.ApplyStyling(context) ? context.Theme.Accent : context.Theme.Default;
        WidgetRenderHelpers.ClearTextArea(context, bodyStyle);
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            return;
        }

        WidgetRenderHelpers.ClearTextRow(context, 0, headerStyle);
        BufferPainter.WriteText(
            context.Buffer,
            context.Bounds.X,
            context.Bounds.Y,
            $"{Trace.Direction} total={Trace.TotalLength} reserved={Trace.ReservedLength} remaining={Trace.RemainingLength} cache={(Trace.CacheHit ? "hit" : "miss")}",
            headerStyle.ToCell());

        for (var index = 0; index < Math.Min(Trace.Result.Count, Math.Max(context.Bounds.Height - 1, 0)); index++)
        {
            var rect = Trace.Result[index];
            WidgetRenderHelpers.ClearTextRow(context, (ushort)(index + 1), bodyStyle);
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + 1 + index),
                $"[{index}] {Trace.Constraints[index].Kind}:{Trace.Constraints[index].Value} len={Trace.RequestedLengths[index]} rect={rect.X},{rect.Y} {rect.Width}x{rect.Height}",
                bodyStyle.ToCell());
        }
    }
}
