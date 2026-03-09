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
        var style = BorderStyle ?? context.Theme.Border;
        BufferPainter.DrawBorder(context.Buffer, context.Bounds, style.ToCell());

        if (!string.IsNullOrWhiteSpace(Title) && context.Bounds.Width > 4)
        {
            BufferPainter.WriteText(context.Buffer, (ushort)(context.Bounds.X + 2), context.Bounds.Y, $" {Title} ", context.Theme.Title.ToCell());
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
}
