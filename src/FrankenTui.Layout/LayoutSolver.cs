using FrankenTui.Core;

namespace FrankenTui.Layout;

public static class LayoutSolver
{
    public static IReadOnlyList<Rect> Split(Rect bounds, LayoutDirection direction, IReadOnlyList<LayoutConstraint> constraints) =>
        SplitWithTrace(bounds, direction, constraints).Result;

    public static LayoutTrace SplitWithTrace(
        Rect bounds,
        LayoutDirection direction,
        IReadOnlyList<LayoutConstraint> constraints,
        LayoutCache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        if (constraints.Count == 0)
        {
            var emptyKey = LayoutCacheKey.Create(bounds, direction, constraints).ToString();
            return new LayoutTrace(bounds, direction, constraints, [], 0, 0, 0, [], emptyKey, false);
        }

        if (cache is not null && cache.TryGet(bounds, direction, constraints, out var cached))
        {
            return cached with { CacheHit = true };
        }

        var total = direction == LayoutDirection.Horizontal ? bounds.Width : bounds.Height;
        var lengths = new int[constraints.Count];
        var fillIndexes = new List<int>();
        var reserved = 0;

        for (var index = 0; index < constraints.Count; index++)
        {
            var constraint = constraints[index];
            lengths[index] = constraint.Kind switch
            {
                LayoutConstraintKind.Fixed => constraint.Value,
                LayoutConstraintKind.Minimum => constraint.Value,
                LayoutConstraintKind.Percentage => (total * constraint.Value) / 100,
                LayoutConstraintKind.Fill => 0,
                _ => 0
            };

            if (constraint.Kind == LayoutConstraintKind.Fill)
            {
                fillIndexes.Add(index);
            }

            reserved += lengths[index];
        }

        var remaining = Math.Max(0, total - reserved);
        if (fillIndexes.Count > 0)
        {
            var totalWeight = fillIndexes.Sum(index => constraints[index].Value);
            var consumed = 0;
            for (var offset = 0; offset < fillIndexes.Count; offset++)
            {
                var index = fillIndexes[offset];
                var weight = constraints[index].Value;
                var share = offset == fillIndexes.Count - 1
                    ? remaining - consumed
                    : (remaining * weight) / totalWeight;
                lengths[index] = share;
                consumed += share;
            }
        }

        var result = new Rect[constraints.Count];
        var cursorX = bounds.X;
        var cursorY = bounds.Y;
        for (var index = 0; index < constraints.Count; index++)
        {
            var length = Math.Max(0, lengths[index]);
            if (direction == LayoutDirection.Horizontal)
            {
                result[index] = new Rect(cursorX, bounds.Y, (ushort)length, bounds.Height);
                cursorX += (ushort)length;
            }
            else
            {
                result[index] = new Rect(bounds.X, cursorY, bounds.Width, (ushort)length);
                cursorY += (ushort)length;
            }
        }

        var trace = new LayoutTrace(
            bounds,
            direction,
            constraints,
            lengths,
            total,
            reserved,
            remaining,
            result,
            LayoutCacheKey.Create(bounds, direction, constraints).ToString(),
            false);
        cache?.Set(trace);
        return trace;
    }

    public static LayoutTrace SplitCached(
        Rect bounds,
        LayoutDirection direction,
        IReadOnlyList<LayoutConstraint> constraints,
        LayoutCache cache) =>
        SplitWithTrace(bounds, direction, constraints, cache);
}
