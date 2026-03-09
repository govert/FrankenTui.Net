using FrankenTui.Core;

namespace FrankenTui.Layout;

public sealed record LayoutTrace(
    Rect Bounds,
    LayoutDirection Direction,
    IReadOnlyList<LayoutConstraint> Constraints,
    IReadOnlyList<Rect> Result);
