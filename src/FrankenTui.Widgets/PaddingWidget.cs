using FrankenTui.Core;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class PaddingWidget : IWidget
{
    public PaddingWidget(IWidget child, Sides padding)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Padding = padding;
    }

    public IWidget Child { get; }

    public Sides Padding { get; }

    public void Render(RuntimeRenderContext context)
    {
        var inner = context.Bounds.Inner(Padding);
        if (!inner.IsEmpty)
        {
            Child.Render(context.WithBounds(inner));
        }
    }
}
