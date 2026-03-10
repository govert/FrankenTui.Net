using FrankenTui.Core;

namespace FrankenTui.Layout;

public sealed record LayoutTrace(
    Rect Bounds,
    LayoutDirection Direction,
    IReadOnlyList<LayoutConstraint> Constraints,
    IReadOnlyList<int> RequestedLengths,
    int TotalLength,
    int ReservedLength,
    int RemainingLength,
    IReadOnlyList<Rect> Result,
    string CacheKey,
    bool CacheHit);
