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
        if (context.Bounds.Height == 0)
        {
            return;
        }

        BufferPainter.WriteText(
            context.Buffer,
            context.Bounds.X,
            context.Bounds.Y,
            $"{Trace.Direction} total={Trace.TotalLength} reserved={Trace.ReservedLength} remaining={Trace.RemainingLength} cache={(Trace.CacheHit ? "hit" : "miss")}",
            context.Theme.Accent.ToCell());

        for (var index = 0; index < Math.Min(Trace.Result.Count, Math.Max(context.Bounds.Height - 1, 0)); index++)
        {
            var rect = Trace.Result[index];
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + 1 + index),
                $"[{index}] {Trace.Constraints[index].Kind}:{Trace.Constraints[index].Value} len={Trace.RequestedLengths[index]} rect={rect.X},{rect.Y} {rect.Width}x{rect.Height}",
                context.Theme.Muted.ToCell());
        }
    }
}
