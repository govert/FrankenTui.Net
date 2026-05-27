using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>1D size hint for intrinsic content measurement.
/// Matches Rust LayoutSizeHint.</summary>
public readonly record struct SizeHint(ushort Min, ushort Preferred, ushort? Max)
{
    public static readonly SizeHint Zero = new(0, 0, null);
    public static readonly SizeHint Fill = new(0, 0, null);
    public static SizeHint Fixed(ushort size) => new(size, size, size);
    public static SizeHint AtLeast(ushort min) => new(min, min, null);
}

public static class LayoutSolver
{
    public static IReadOnlyList<Rect> Split(Rect bounds, LayoutDirection direction, IReadOnlyList<LayoutConstraint> constraints) =>
        SplitWithTrace(bounds, direction, constraints).Result;

    /// <summary>Split with a measurer for FitContent constraints.
    /// Matches Rust Flex::split_with_measurer.</summary>
    public static IReadOnlyList<Rect> SplitWithMeasurer(
        Rect bounds,
        LayoutDirection direction,
        IReadOnlyList<LayoutConstraint> constraints,
        Func<int, ushort, SizeHint> measurer)
    {
        return SplitWithTrace(bounds, direction, constraints, measurer).Result;
    }

    public static LayoutTrace SplitWithTrace(
        Rect bounds,
        LayoutDirection direction,
        IReadOnlyList<LayoutConstraint> constraints,
        Func<int, ushort, SizeHint>? measurer = null,
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
                case LayoutConstraintKind.FitContent:
                    var hint = measurer?.Invoke(i, (ushort)remaining) ?? SizeHint.Zero;
                    var fc = Math.Min(hint.Min, remaining);
                    lengths[i] = fc;
                    remaining -= fc;
                    break;
            }
        }

        // Pass 2: Soft constraints (Percentage, FitContent preferred)
        // Rust: "These fill remaining space after hard minimums."
        for (var i = 0; i < constraints.Count; i++)
        {
            var c = constraints[i];
            switch (c.Kind)
            {
                case LayoutConstraintKind.Percentage:
                    var target = (total * c.Value) / 100;
                    var needed = Math.Max(0, target - lengths[i]);
                    var alloc = Math.Min(needed, remaining);
                    lengths[i] += alloc;
                    remaining -= alloc;
                    break;
                case LayoutConstraintKind.FitContent:
                    var hint = measurer?.Invoke(i, (ushort)remaining) ?? SizeHint.Zero;
                    var preferred = Math.Max(hint.Min, hint.Preferred);
                    if (hint.Max.HasValue)
                        preferred = Math.Min(preferred, hint.Max.Value);
                    var needed2 = Math.Max(0, preferred - lengths[i]);
                    var alloc2 = Math.Min(needed2, remaining);
                    lengths[i] += alloc2;
                    remaining -= alloc2;
                    break;
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
        SplitWithTrace(bounds, direction, constraints, measurer: null, cache);
}
