using FrankenTui.Core;

namespace FrankenTui.Layout;

public sealed record LayoutPlan(Rect Bounds, LayoutDirection Direction, IReadOnlyList<LayoutConstraint> Constraints);
