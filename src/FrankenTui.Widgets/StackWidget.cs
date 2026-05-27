using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class StackWidget : IWidget, IMeasurableWidget
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
        var hasFitContent = Children.Any(c => c.Constraint.Kind == LayoutConstraintKind.FitContent);
        IReadOnlyList<Rect> rects;
        if (hasFitContent)
        {
            rects = LayoutSolver.SplitWithMeasurer(context.Bounds, Direction, constraints, (i, remaining) =>
            {
                if (i < Children.Count && Children[i].Widget is IMeasurableWidget m)
                {
                    var size = Direction == LayoutDirection.Vertical
                        ? new Size(context.Bounds.Width, (ushort)Math.Min(remaining, ushort.MaxValue))
                        : new Size((ushort)Math.Min(remaining, ushort.MaxValue), context.Bounds.Height);
                    return m.MeasureAxis(size, Direction);
                }
                return SizeHint.Zero;
            });
        }
        else
        {
            rects = LayoutSolver.Split(context.Bounds, Direction, constraints);
        }

        for (var index = 0; index < Children.Count && index < rects.Count; index++)
        {
            if (!rects[index].IsEmpty)
            {
                Children[index].Widget.Render(context.WithBounds(rects[index]));
            }
        }
    }

    public Size Measure(Size available)
    {
        // Sum of children along direction, max across
        ushort totalMain = 0;
        ushort maxCross = 0;
        foreach (var (constraint, widget) in Children)
        {
            var childSize = (widget as IMeasurableWidget)?.Measure(available) ?? available;
            if (Direction == LayoutDirection.Vertical)
            {
                totalMain = (ushort)Math.Min(totalMain + childSize.Height, ushort.MaxValue);
                maxCross = Math.Max(maxCross, childSize.Width);
            }
            else
            {
                totalMain = (ushort)Math.Min(totalMain + childSize.Width, ushort.MaxValue);
                maxCross = Math.Max(maxCross, childSize.Height);
            }
        }
        return Direction == LayoutDirection.Vertical
            ? new Size(maxCross, totalMain)
            : new Size(totalMain, maxCross);
    }

    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        // Sum child hints along the main axis, take max across
        ushort totalMin = 0, totalPref = 0;
        ushort? totalMax = null;
        foreach (var (constraint, widget) in Children)
        {
            var hint = (widget as IMeasurableWidget)?.MeasureAxis(available, direction) ?? SizeHint.Fill;
            totalMin = (ushort)Math.Min((ushort)(totalMin + hint.Min), ushort.MaxValue);
            totalPref = (ushort)Math.Min((ushort)(totalPref + hint.Preferred), ushort.MaxValue);
            if (hint.Max.HasValue && totalMax.HasValue)
                totalMax = (ushort)Math.Min((ushort)(totalMax.Value + hint.Max.Value), ushort.MaxValue);
            else if (hint.Max.HasValue)
                totalMax = hint.Max;
            else
                totalMax = null;
        }
        return new SizeHint(totalMin, totalPref, totalMax);
    }
}
