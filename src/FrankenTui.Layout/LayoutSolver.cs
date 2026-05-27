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
        var growIndices = new List<int>();
        var remaining = (int)total;

        // Pass 1: Hard minimums (Fixed, Minimum)
        // Rust: "Allocate hard minimums. These constraints are non-negotiable."
        for (var i = 0; i < constraints.Count; i++)
        {
            var c = constraints[i];
            switch (c.Kind)
            {
                case LayoutConstraintKind.Fixed:
                    var sz = Math.Min(c.Value, remaining);
                    lengths[i] = sz;
                    remaining -= sz;
                    break;
                case LayoutConstraintKind.Minimum:
                    var mn = Math.Min(c.Value, remaining);
                    lengths[i] = mn;
                    remaining -= mn;
                    growIndices.Add(i);  // Min can grow beyond minimum
                    break;
                case LayoutConstraintKind.Fill:
                    growIndices.Add(i);  // Fill starts at 0, can grow
                    break;
                case LayoutConstraintKind.MinFill:
                    var mf = Math.Min(c.Value, remaining);
                    lengths[i] = mf;
                    remaining -= mf;
                    growIndices.Add(i);  // MinFill can grow beyond minimum
                    break;
            }
        }

        // Pass 2: Soft constraints (Percentage)
        // Rust: "These fill remaining space after hard minimums."
        for (var i = 0; i < constraints.Count; i++)
        {
            if (constraints[i].Kind == LayoutConstraintKind.Percentage)
            {
                var target = (total * constraints[i].Value) / 100;
                var needed = Math.Max(0, target - lengths[i]);
                var alloc = Math.Min(needed, remaining);
                lengths[i] += alloc;
                remaining -= alloc;
            }
        }

        // Pass 3: Grow loop — distribute remaining among growable constraints
        // Rust: "Iterative distribution to flexible constraints"
        if (remaining > 0 && growIndices.Count > 0)
        {
            var totalWeight = growIndices.Sum(i => constraints[i].Value > 0 ? (int)constraints[i].Value : 1);
            var consumed = 0;
            for (var offset = 0; offset < growIndices.Count; offset++)
            {
                var i = growIndices[offset];
                var weight = constraints[i].Value > 0 ? (int)constraints[i].Value : 1;
                var share = offset == growIndices.Count - 1
                    ? remaining - consumed
                    : (remaining * weight) / totalWeight;
                lengths[i] += share;
                consumed += share;
            }
            remaining = 0;
        }
        // If no grow indices, remaining space stays unused (Rust Flex Min behavior)

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
            0,
            0,
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
