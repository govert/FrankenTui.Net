using FrankenTui.Layout;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class StackWidget : IWidget
{
    public StackWidget(LayoutDirection direction, IReadOnlyList<(LayoutConstraint Constraint, IWidget Widget)> children)
    {
        Direction = direction;
        Children = children ?? throw new ArgumentNullException(nameof(children));
    }

    public LayoutDirection Direction { get; }

    public IReadOnlyList<(LayoutConstraint Constraint, IWidget Widget)> Children { get; }

    public void Render(RuntimeRenderContext context)
    {
        var constraints = Children.Select(static child => child.Constraint).ToArray();
        var rects = LayoutSolver.Split(context.Bounds, Direction, constraints);
        for (var index = 0; index < Children.Count && index < rects.Count; index++)
        {
            if (!rects[index].IsEmpty)
            {
                Children[index].Widget.Render(context.WithBounds(rects[index]));
            }
        }
    }
}
