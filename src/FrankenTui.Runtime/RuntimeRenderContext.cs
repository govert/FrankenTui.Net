using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Runtime;

public readonly record struct RuntimeRenderContext(RenderBuffer Buffer, Rect Bounds, Theme Theme)
{
    public RuntimeRenderContext WithBounds(Rect bounds) => this with { Bounds = bounds };
}
