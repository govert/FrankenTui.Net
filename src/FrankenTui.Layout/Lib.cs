// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/lib.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: This file currently contains the lib.rs support surface consumed by
// direction.rs, visibility.rs, responsive.rs, responsive_layout.rs, and grid.rs.
// Unrelated advanced lib.rs surfaces remain separate porting units.
// DIVERGENCE: Associated-data Rust enums use closed record hierarchies, following
// the repository's C# mapping rule. Breakpoint methods use extension methods because
// C# enum declarations cannot contain behavior.
// The cache.rs routed temporal-coherence seam is included here: Rust SmallVec/Option
// allocations map to defensive IReadOnlyList arrays/null, and closure usize indices
// map to Int32.

using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>Responsive terminal-width tiers in ascending order.</summary>
public enum Breakpoint : byte
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
}

public static class BreakpointExtensions
{
    private static readonly IReadOnlyList<Breakpoint> Ordered =
        Array.AsReadOnly(new[]
        {
            Breakpoint.Xs,
            Breakpoint.Sm,
            Breakpoint.Md,
            Breakpoint.Lg,
            Breakpoint.Xl,
        });

    public static IReadOnlyList<Breakpoint> All => Ordered;

    public static byte Index(this Breakpoint breakpoint) => (byte)breakpoint;

    public static string Label(this Breakpoint breakpoint) => breakpoint switch
    {
        Breakpoint.Xs => "xs",
        Breakpoint.Sm => "sm",
        Breakpoint.Md => "md",
        Breakpoint.Lg => "lg",
        Breakpoint.Xl => "xl",
        _ => throw new ArgumentOutOfRangeException(nameof(breakpoint), breakpoint, null),
    };
}

/// <summary>Width thresholds used to classify responsive breakpoints.</summary>
public readonly record struct Breakpoints(ushort Sm, ushort Md, ushort Lg, ushort Xl)
{
    public static Breakpoints Default { get; } = new(60, 90, 120, 160);

    public static Breakpoints New(ushort sm, ushort md, ushort lg)
    {
        md = Math.Max(md, sm);
        lg = Math.Max(lg, md);
        ushort xl = SaturatingAdd(lg, 40);
        return new Breakpoints(sm, md, lg, xl);
    }

    public static Breakpoints NewWithXl(ushort sm, ushort md, ushort lg, ushort xl)
    {
        md = Math.Max(md, sm);
        lg = Math.Max(lg, md);
        xl = Math.Max(xl, lg);
        return new Breakpoints(sm, md, lg, xl);
    }

    public Breakpoint ClassifyWidth(ushort width)
    {
        if (width >= Xl) return Breakpoint.Xl;
        if (width >= Lg) return Breakpoint.Lg;
        if (width >= Md) return Breakpoint.Md;
        if (width >= Sm) return Breakpoint.Sm;
        return Breakpoint.Xs;
    }

    public Breakpoint ClassifySize(Size size) => ClassifyWidth(size.Width);

    public bool AtLeast(ushort width, Breakpoint minimum) =>
        ClassifyWidth(width).Index() >= minimum.Index();

    public bool Between(ushort width, Breakpoint minimum, Breakpoint maximum)
    {
        byte index = ClassifyWidth(width).Index();
        return index >= minimum.Index() && index <= maximum.Index();
    }

    public ushort Threshold(Breakpoint breakpoint) => breakpoint switch
    {
        Breakpoint.Xs => 0,
        Breakpoint.Sm => Sm,
        Breakpoint.Md => Md,
        Breakpoint.Lg => Lg,
        Breakpoint.Xl => Xl,
        _ => throw new ArgumentOutOfRangeException(nameof(breakpoint), breakpoint, null),
    };

    public IReadOnlyList<(Breakpoint Breakpoint, ushort MinimumWidth)> Thresholds() =>
    [
        (Breakpoint.Xs, 0),
        (Breakpoint.Sm, Sm),
        (Breakpoint.Md, Md),
        (Breakpoint.Lg, Lg),
        (Breakpoint.Xl, Xl),
    ];

    private static ushort SaturatingAdd(ushort left, ushort right) =>
        (ushort)Math.Min((uint)left + right, ushort.MaxValue);
}

/// <summary>A constraint on one layout axis.</summary>
public abstract record Constraint
{
    private Constraint() { }

    public sealed record FixedValue(ushort Value) : Constraint;
    public sealed record PercentageValue(float Value) : Constraint;
    public sealed record MinValue(ushort Value) : Constraint;
    public sealed record MaxValue(ushort Value) : Constraint;
    public sealed record RatioValue(uint Numerator, uint Denominator) : Constraint;
    public sealed record FillValue : Constraint;
    public sealed record FitContentValue : Constraint;
    public sealed record FitContentBoundedValue(ushort Minimum, ushort Maximum) : Constraint;
    public sealed record FitMinValue : Constraint;

    public static Constraint Fixed(ushort value) => new FixedValue(value);
    public static Constraint Percentage(float value) => new PercentageValue(value);
    public static Constraint Min(ushort value) => new MinValue(value);
    public static Constraint Max(ushort value) => new MaxValue(value);
    public static Constraint Ratio(uint numerator, uint denominator) =>
        new RatioValue(numerator, denominator);
    public static Constraint Fill { get; } = new FillValue();
    public static Constraint FitContent { get; } = new FitContentValue();
    public static Constraint FitContentBounded(ushort minimum, ushort maximum) =>
        new FitContentBoundedValue(minimum, maximum);
    public static Constraint FitMin { get; } = new FitMinValue();
}

/// <summary>One-dimensional intrinsic size hint.</summary>
public readonly record struct LayoutSizeHint(ushort Min, ushort Preferred, ushort? Max)
{
    public static LayoutSizeHint Zero { get; } = new(0, 0, null);

    public static LayoutSizeHint Exact(ushort size) => new(size, size, size);

    public static LayoutSizeHint AtLeast(ushort minimum, ushort preferred) =>
        new(minimum, preferred, null);

    public ushort Clamp(ushort value) => Math.Max(Min, Math.Min(value, Max ?? ushort.MaxValue));
}

/// <summary>Primary axis for a flex layout.</summary>
public enum Direction
{
    Vertical,
    Horizontal,
}

/// <summary>Distribution of items along the primary axis.</summary>
public enum Alignment
{
    Start,
    Center,
    End,
    SpaceAround,
    SpaceBetween,
}

/// <summary>Declarative overflow behavior attached to a layout container.</summary>
public abstract record OverflowBehavior
{
    private OverflowBehavior() { }

    public sealed record ClipValue : OverflowBehavior;
    public sealed record VisibleValue : OverflowBehavior;
    public sealed record ScrollValue(ushort? MaxContent) : OverflowBehavior;
    public sealed record WrapValue : OverflowBehavior;

    public static OverflowBehavior Clip { get; } = new ClipValue();
    public static OverflowBehavior Visible { get; } = new VisibleValue();
    public static OverflowBehavior Scroll(ushort? maxContent = null) => new ScrollValue(maxContent);
    public static OverflowBehavior Wrap { get; } = new WrapValue();
}

/// <summary>Immutable-builder, one-dimensional constraint layout.</summary>
public sealed class Flex
{
    private readonly Direction _direction;
    private readonly Constraint[] _constraints;
    private readonly Sides _margin;
    private readonly ushort _gap;
    private readonly Alignment _alignment;
    private readonly FlowDirection _flowDirection;
    private readonly OverflowBehavior _overflow;

    private Flex(
        Direction direction,
        Constraint[] constraints,
        Sides margin,
        ushort gap,
        Alignment alignment,
        FlowDirection flowDirection,
        OverflowBehavior overflow)
    {
        _direction = direction;
        _constraints = constraints;
        _margin = margin;
        _gap = gap;
        _alignment = alignment;
        _flowDirection = flowDirection;
        _overflow = overflow;
    }

    public static Flex Default { get; } = new(
        FrankenTui.Layout.Direction.Vertical,
        [],
        default,
        0,
        FrankenTui.Layout.Alignment.Start,
        FrankenTui.Layout.FlowDirection.Ltr,
        FrankenTui.Layout.OverflowBehavior.Clip);

    public static Flex Vertical() => Default;

    public static Flex Horizontal() => Default.WithDirection(FrankenTui.Layout.Direction.Horizontal);

    public Flex WithDirection(Direction direction) =>
        Copy(direction: direction);

    public Flex Constraints(IEnumerable<Constraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        return Copy(constraints: constraints.ToArray());
    }

    public Flex Margin(Sides margin) => Copy(margin: margin);

    public Flex Gap(ushort gap) => Copy(gap: gap);

    public Flex Align(Alignment alignment) => Copy(alignment: alignment);

    public Flex FlowDirection(FlowDirection flowDirection) => Copy(flowDirection: flowDirection);

    public Flex Overflow(OverflowBehavior overflow) =>
        Copy(overflow: overflow ?? throw new ArgumentNullException(nameof(overflow)));

    public OverflowBehavior OverflowBehavior() => _overflow;

    public int ConstraintCount() => _constraints.Length;

    public IReadOnlyList<Rect> Split(Rect area) =>
        SplitCore(area, static (_, _) => LayoutSizeHint.Zero);

    public IReadOnlyList<Rect> SplitWithMeasurer(
        Rect area,
        Func<int, ushort, LayoutSizeHint> measurer)
    {
        ArgumentNullException.ThrowIfNull(measurer);
        return SplitCore(area, measurer);
    }

    /// <summary>Split with intrinsic measurement and prior-frame rounding coherence.</summary>
    public IReadOnlyList<Rect> SplitWithMeasurerStably(
        Rect area,
        Func<int, ushort, LayoutSizeHint> measurer,
        CoherenceCache cache)
    {
        ArgumentNullException.ThrowIfNull(measurer);
        ArgumentNullException.ThrowIfNull(cache);
        CoherenceId id = CoherenceId.New(_constraints, _direction);
        return SplitCore(area, measurer, cache, id);
    }

    private IReadOnlyList<Rect> SplitCore(
        Rect area,
        Func<int, ushort, LayoutSizeHint> measurer,
        CoherenceCache? coherence = null,
        CoherenceId? coherenceId = null)
    {
        Rect inner = area.Inner(_margin);
        if (inner.IsEmpty)
            return Enumerable.Repeat(default(Rect), _constraints.Length).ToArray();
        if (_constraints.Length == 0)
            return Array.Empty<Rect>();

        ushort totalSize = _direction == Direction.Horizontal ? inner.Width : inner.Height;
        ushort totalGap = SaturatingProduct(_constraints.Length - 1, _gap);
        ushort availableSize = SaturatingSubtract(totalSize, totalGap);
        ushort[] sizes = ConstraintSolver.Solve(
            _constraints,
            availableSize,
            measurer,
            coherence,
            coherenceId);
        Rect[] rects = SizesToRects(inner, sizes);
        if (_flowDirection.IsRtl() && _direction == Direction.Horizontal)
            DirectionLayout.MirrorRectsHorizontal(rects, inner);
        return rects;
    }

    private Rect[] SizesToRects(Rect area, IReadOnlyList<ushort> sizes)
    {
        if (sizes.Count == 0) return [];

        ushort totalItemsSize = 0;
        foreach (ushort size in sizes)
            totalItemsSize = SaturatingAdd(totalItemsSize, size);

        ushort totalAvailable = _direction == Direction.Horizontal ? area.Width : area.Height;
        ushort explicitGapSpace = SaturatingProduct(sizes.Count - 1, _gap);
        ushort used = SaturatingAdd(totalItemsSize, explicitGapSpace);
        ushort leftover = SaturatingSubtract(totalAvailable, used);
        ushort startShift = _alignment switch
        {
            Alignment.End => leftover,
            Alignment.Center => (ushort)(leftover / 2),
            _ => 0,
        };

        var rects = new Rect[sizes.Count];
        ushort accumulated = 0;
        for (int index = 0; index < sizes.Count; index++)
        {
            ushort explicitGap = SaturatingProduct(index, _gap);
            ushort gapOffset = explicitGap;
            if (_alignment == Alignment.SpaceBetween && sizes.Count > 1 && index > 0)
            {
                gapOffset = SaturatingAdd(
                    explicitGap,
                    (ushort)((ulong)leftover * (ulong)index / (ulong)(sizes.Count - 1)));
            }
            else if (_alignment == Alignment.SpaceAround)
            {
                ulong slots = (ulong)sizes.Count * 2UL;
                ulong numerator = (ulong)leftover * (2UL * (ulong)index + 1UL);
                ushort around = (ushort)Math.Min(
                    (numerator + slots / 2UL) / slots,
                    ushort.MaxValue);
                gapOffset = SaturatingAdd(explicitGap, around);
            }

            ushort position = SaturatingAdd(
                _direction == Direction.Horizontal ? area.X : area.Y,
                SaturatingAdd(startShift, SaturatingAdd(accumulated, gapOffset)));
            ushort size = sizes[index];
            rects[index] = _direction == Direction.Horizontal
                ? new Rect(
                    position,
                    area.Y,
                    Math.Min(size, SaturatingSubtract(area.Right, position)),
                    area.Height)
                : new Rect(
                    area.X,
                    position,
                    area.Width,
                    Math.Min(size, SaturatingSubtract(area.Bottom, position)));
            accumulated = SaturatingAdd(accumulated, size);
        }

        return rects;
    }

    private Flex Copy(
        Direction? direction = null,
        Constraint[]? constraints = null,
        Sides? margin = null,
        ushort? gap = null,
        Alignment? alignment = null,
        FlowDirection? flowDirection = null,
        OverflowBehavior? overflow = null) =>
        new(
            direction ?? _direction,
            constraints ?? _constraints,
            margin ?? _margin,
            gap ?? _gap,
            alignment ?? _alignment,
            flowDirection ?? _flowDirection,
            overflow ?? _overflow);

    private static ushort SaturatingAdd(ushort left, ushort right) =>
        (ushort)Math.Min((uint)left + right, ushort.MaxValue);

    private static ushort SaturatingSubtract(ushort left, ushort right) =>
        left > right ? (ushort)(left - right) : (ushort)0;

    private static ushort SaturatingProduct(int count, ushort value) =>
        (ushort)Math.Min((ulong)Math.Max(0, count) * value, ushort.MaxValue);
}

internal static class ConstraintSolver
{
    public static ushort[] Solve(
        IReadOnlyList<Constraint> constraints,
        ushort availableSize,
        Func<int, ushort, LayoutSizeHint>? measurer = null,
        CoherenceCache? coherence = null,
        CoherenceId? coherenceId = null)
    {
        measurer ??= static (_, _) => LayoutSizeHint.Zero;
        var sizes = new ushort[constraints.Count];
        ushort remaining = availableSize;
        var growIndices = new List<int>();

        for (int index = 0; index < constraints.Count; index++)
        {
            Constraint constraint = constraints[index];
            ushort hardMinimum = constraint switch
            {
                Constraint.FixedValue fixedValue => fixedValue.Value,
                Constraint.MinValue minValue => minValue.Value,
                Constraint.FitMinValue => measurer(index, remaining).Min,
                Constraint.FitContentValue => measurer(index, remaining).Min,
                Constraint.FitContentBoundedValue bounded => bounded.Minimum,
                _ => 0,
            };
            if (hardMinimum == 0 && constraint is not Constraint.FixedValue)
                continue;
            ushort allocation = Math.Min(hardMinimum, remaining);
            sizes[index] = allocation;
            remaining = SaturatingSubtract(remaining, allocation);
        }

        for (int index = 0; index < constraints.Count; index++)
        {
            switch (constraints[index])
            {
                case Constraint.PercentageValue percentage:
                {
                    float rounded = MathF.Round(
                        availableSize * percentage.Value / 100f,
                        MidpointRounding.AwayFromZero);
                    ushort target = FloatToU16Saturating(rounded);
                    AllocateToward(index, target, sizes, ref remaining);
                    break;
                }
                case Constraint.RatioValue ratio:
                {
                    ushort target = ratio.Denominator == 0
                        ? (ushort)0
                        : (ushort)Math.Min(
                            (ulong)availableSize * ratio.Numerator / ratio.Denominator,
                            ushort.MaxValue);
                    AllocateToward(index, target, sizes, ref remaining);
                    break;
                }
                case Constraint.FitContentValue:
                {
                    LayoutSizeHint hint = measurer(index, remaining);
                    ushort target = Math.Max(hint.Preferred, sizes[index]);
                    target = Math.Min(target, hint.Max ?? ushort.MaxValue);
                    AllocateToward(index, target, sizes, ref remaining);
                    break;
                }
                case Constraint.FitContentBoundedValue bounded:
                {
                    LayoutSizeHint hint = measurer(index, remaining);
                    ushort target = Math.Min(Math.Max(hint.Preferred, sizes[index]), bounded.Maximum);
                    AllocateToward(index, target, sizes, ref remaining);
                    break;
                }
                case Constraint.MinValue:
                case Constraint.MaxValue:
                case Constraint.FillValue:
                    growIndices.Add(index);
                    break;
            }
        }

        while (remaining > 0 && growIndices.Count > 0)
        {
            double target = (double)remaining / growIndices.Count;
            IReadOnlyList<ushort>? previous = null;
            if (coherence is not null && coherenceId is { } id)
            {
                IReadOnlyList<ushort>? fullPrevious = coherence.Get(id);
                if (fullPrevious is not null)
                {
                    previous = growIndices
                        .Select(index => index < fullPrevious.Count ? fullPrevious[index] : (ushort)0)
                        .ToArray();
                }
            }
            ushort[] distributed = RoundLayoutStable(
                Enumerable.Repeat(target, growIndices.Count).ToArray(),
                remaining,
                previous);
            var violations = new List<int>();
            for (int position = 0; position < growIndices.Count; position++)
            {
                int index = growIndices[position];
                if (constraints[index] is Constraint.MaxValue maximum &&
                    SaturatingAdd(sizes[index], distributed[position]) > maximum.Value)
                {
                    violations.Add(index);
                }
            }

            if (violations.Count == 0)
            {
                for (int position = 0; position < growIndices.Count; position++)
                {
                    int index = growIndices[position];
                    sizes[index] = SaturatingAdd(sizes[index], distributed[position]);
                }
                if (coherence is not null && coherenceId is { } storedId &&
                    distributed.Length == growIndices.Count)
                {
                    var fullShares = new ushort[constraints.Count];
                    for (int position = 0; position < growIndices.Count; position++)
                        fullShares[growIndices[position]] = distributed[position];
                    coherence.Store(storedId, fullShares);
                }
                break;
            }

            foreach (int index in violations)
            {
                ushort maximum = ((Constraint.MaxValue)constraints[index]).Value;
                ushort consumed = SaturatingSubtract(maximum, sizes[index]);
                sizes[index] = maximum;
                remaining = SaturatingSubtract(remaining, consumed);
                growIndices.Remove(index);
            }
        }

        return sizes;
    }

    /// <summary>
    /// Round continuous layout targets while conserving the requested total and using a
    /// prior allocation as a deterministic temporal tie-break.
    /// </summary>
    public static ushort[] RoundLayoutStable(
        IReadOnlyList<double> targets,
        ushort total,
        IReadOnlyList<ushort>? previous = null)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0) return [];

        var result = new ushort[targets.Count];
        ulong floorSum = 0;
        var remainders = new (int Index, double Remainder, bool PreviousUsedCeiling)[targets.Count];
        for (int index = 0; index < targets.Count; index++)
        {
            double sanitized = double.IsNaN(targets[index])
                ? 0.0
                : Math.Max(0.0, targets[index]);
            ushort floor = sanitized >= ushort.MaxValue
                ? ushort.MaxValue
                : (ushort)Math.Floor(sanitized);
            result[index] = floor;
            floorSum += floor;
            double rawRemainder = targets[index] - floor;
            double remainder = double.IsFinite(rawRemainder) ? rawRemainder : 0.0;
            ushort ceiling = SaturatingAdd(floor, 1);
            bool previousUsedCeiling =
                previous is not null &&
                index < previous.Count &&
                previous[index] == ceiling;
            remainders[index] = (index, remainder, previousUsedCeiling);
        }

        if (floorSum > total)
            return RedistributeOverflow(result, total);

        ushort deficit = (ushort)(total - floorSum);
        if (deficit == 0) return result;

        Array.Sort(remainders, static (left, right) =>
        {
            int remainder = right.Remainder.CompareTo(left.Remainder);
            if (remainder != 0) return remainder;
            int temporal = right.PreviousUsedCeiling.CompareTo(left.PreviousUsedCeiling);
            return temporal != 0 ? temporal : left.Index.CompareTo(right.Index);
        });

        if (deficit >= result.Length)
        {
            ushort perItem = (ushort)(deficit / result.Length);
            for (int index = 0; index < result.Length; index++)
                result[index] = SaturatingAdd(result[index], perItem);
            deficit %= (ushort)result.Length;
        }

        for (int index = 0; index < deficit; index++)
        {
            int targetIndex = remainders[index].Index;
            result[targetIndex] = SaturatingAdd(result[targetIndex], 1);
        }

        return result;
    }

    private static ushort[] RedistributeOverflow(ushort[] floors, ushort total)
    {
        var result = (ushort[])floors.Clone();
        ulong current = result.Aggregate<ushort, ulong>(0, static (sum, value) => sum + value);
        ulong overflow = current - total;
        while (overflow > 0)
        {
            ushort maximum = result.Max();
            if (maximum == 0) break;
            ushort nextMaximum = result.Where(value => value < maximum).DefaultIfEmpty().Max();
            ulong maximumCount = (ulong)result.Count(value => value == maximum);
            ulong delta = (ulong)(maximum - nextMaximum);
            ushort reduction = (ushort)Math.Max(
                1UL,
                Math.Min(delta, (overflow + maximumCount - 1) / maximumCount));
            for (int index = 0; index < result.Length && overflow > 0; index++)
            {
                if (result[index] != maximum) continue;
                ushort amount = (ushort)Math.Min(
                    Math.Min((ulong)result[index], reduction),
                    overflow);
                result[index] -= amount;
                overflow -= amount;
            }
        }
        return result;
    }

    private static void AllocateToward(
        int index,
        ushort target,
        ushort[] sizes,
        ref ushort remaining)
    {
        ushort needed = SaturatingSubtract(target, sizes[index]);
        ushort allocation = Math.Min(needed, remaining);
        sizes[index] = SaturatingAdd(sizes[index], allocation);
        remaining = SaturatingSubtract(remaining, allocation);
    }

    private static ushort FloatToU16Saturating(float value)
    {
        if (float.IsNaN(value) || value <= 0f) return 0;
        if (value >= ushort.MaxValue) return ushort.MaxValue;
        return (ushort)value;
    }

    private static ushort SaturatingAdd(ushort left, ushort right) =>
        (ushort)Math.Min((uint)left + right, ushort.MaxValue);

    private static ushort SaturatingSubtract(ushort left, ushort right) =>
        left > right ? (ushort)(left - right) : (ushort)0;
}

/// <summary>Public layout-rounding algorithms exported by the upstream crate root.</summary>
public static class LayoutRounding
{
    /// <summary>
    /// Round continuous targets to cells while conserving the total and favoring the
    /// previous frame's rounding direction when remainders tie.
    /// </summary>
    public static IReadOnlyList<ushort> RoundLayoutStable(
        IReadOnlyList<double> targets,
        ushort total,
        IReadOnlyList<ushort>? previous = null) =>
        ConstraintSolver.RoundLayoutStable(targets, total, previous);
}
