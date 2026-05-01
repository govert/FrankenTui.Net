using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Runtime;

public readonly record struct RuntimeRenderContext(
    RenderBuffer Buffer,
    Rect Bounds,
    Theme Theme,
    RuntimeDegradationLevel DegradationLevel = RuntimeDegradationLevel.Full)
{
    public RuntimeRenderContext WithBounds(Rect bounds) => this with { Bounds = bounds };

    public RuntimeRenderContext WithDegradation(RuntimeDegradationLevel level) =>
        this with { DegradationLevel = level };
}
