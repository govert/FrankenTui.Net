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
        var screen = HeadlessBufferView.ScreenText(Buffer);
        for (var row = 0; row < Math.Min(screen.Count, context.Bounds.Height); row++)
        {
            BufferPainter.WriteText(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + row), screen[row], context.Theme.Muted.ToCell());
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
        for (var index = 0; index < Math.Min(Trace.Result.Count, context.Bounds.Height); index++)
        {
            var rect = Trace.Result[index];
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + index),
                $"[{index}] {rect.X},{rect.Y} {rect.Width}x{rect.Height}",
                context.Theme.Muted.ToCell());
        }
    }
}
