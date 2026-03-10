using FrankenTui.Core;

namespace FrankenTui.Layout;

public sealed class LayoutCache
{
    private readonly Dictionary<LayoutCacheKey, LayoutTrace> _entries = [];

    public int Count => _entries.Count;

    public bool TryGet(Rect bounds, LayoutDirection direction, IReadOnlyList<LayoutConstraint> constraints, out LayoutTrace trace) =>
        _entries.TryGetValue(LayoutCacheKey.Create(bounds, direction, constraints), out trace!);

    public void Set(LayoutTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        _entries[LayoutCacheKey.Create(trace.Bounds, trace.Direction, trace.Constraints)] = trace;
    }

    public void Clear() => _entries.Clear();
}

public readonly record struct LayoutCacheKey(Rect Bounds, LayoutDirection Direction, string ConstraintFingerprint)
{
    public static LayoutCacheKey Create(Rect bounds, LayoutDirection direction, IReadOnlyList<LayoutConstraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        var fingerprint = string.Join(
            "|",
            constraints.Select(static constraint =>
                $"{constraint.Kind}:{constraint.Value}"));
        return new LayoutCacheKey(bounds, direction, fingerprint);
    }

    public override string ToString() =>
        $"{Direction}:{Bounds.X},{Bounds.Y},{Bounds.Width}x{Bounds.Height}:{ConstraintFingerprint}";
}
