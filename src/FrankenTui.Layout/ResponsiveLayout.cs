// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/responsive_layout.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// COMPATIBILITY: The earlier static Select(width, ResponsiveBreakpoint[]) helper
// remains available on this now-instantiable type.

using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>The active breakpoint and rectangles produced by a responsive split.</summary>
public sealed class ResponsiveSplit : IEquatable<ResponsiveSplit>
{
    public ResponsiveSplit(Breakpoint breakpoint, IEnumerable<Rect> rects)
    {
        ArgumentNullException.ThrowIfNull(rects);
        Breakpoint = breakpoint;
        Rects = Array.AsReadOnly(rects.ToArray());
    }

    public Breakpoint Breakpoint { get; }

    public IReadOnlyList<Rect> Rects { get; }

    public bool Equals(ResponsiveSplit? other) =>
        other is not null &&
        Breakpoint == other.Breakpoint &&
        Rects.SequenceEqual(other.Rects);

    public override bool Equals(object? obj) => obj is ResponsiveSplit other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Breakpoint);
        foreach (Rect rect in Rects) hash.Add(rect);
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"ResponsiveSplit {{ Breakpoint = {Breakpoint}, Rects = [{string.Join(", ", Rects)}] }}";
}

/// <summary>A breakpoint-aware collection of complete <see cref="Flex"/> layouts.</summary>
public sealed class ResponsiveLayout
{
    private readonly Responsive<Flex> _layouts;
    private Breakpoints _breakpoints;

    public ResponsiveLayout(Flex baseLayout)
        : this(new Responsive<Flex>(baseLayout ?? throw new ArgumentNullException(nameof(baseLayout))),
            Breakpoints.Default)
    {
    }

    private ResponsiveLayout(Responsive<Flex> layouts, Breakpoints breakpoints)
    {
        _layouts = layouts;
        _breakpoints = breakpoints;
    }

    public static ResponsiveLayout New(Flex baseLayout) => new(baseLayout);

    public ResponsiveLayout At(Breakpoint breakpoint, Flex layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return new ResponsiveLayout(_layouts.At(breakpoint, layout), _breakpoints);
    }

    public ResponsiveLayout WithBreakpoints(Breakpoints breakpoints) =>
        new(_layouts.Clone(), breakpoints);

    public void Set(Breakpoint breakpoint, Flex layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        _layouts.Set(breakpoint, layout);
    }

    public void Clear(Breakpoint breakpoint) => _layouts.Clear(breakpoint);

    public ResponsiveSplit Split(Rect area)
    {
        Breakpoint breakpoint = _breakpoints.ClassifyWidth(area.Width);
        return SplitFor(breakpoint, area);
    }

    public ResponsiveSplit SplitFor(Breakpoint breakpoint, Rect area) =>
        new(breakpoint, _layouts.Resolve(breakpoint).Split(area));

    public Breakpoint Classify(ushort width) => _breakpoints.ClassifyWidth(width);

    public Flex LayoutFor(Breakpoint breakpoint) => _layouts.Resolve(breakpoint);

    public bool HasExplicit(Breakpoint breakpoint) => _layouts.HasExplicit(breakpoint);

    public Breakpoints GetBreakpoints() => _breakpoints;

    public int ConstraintCount(Breakpoint breakpoint) =>
        _layouts.Resolve(breakpoint).ConstraintCount();

    public (Breakpoint Old, Breakpoint New)? DetectTransition(ushort oldWidth, ushort newWidth)
    {
        Breakpoint oldBreakpoint = _breakpoints.ClassifyWidth(oldWidth);
        Breakpoint newBreakpoint = _breakpoints.ClassifyWidth(newWidth);
        return oldBreakpoint == newBreakpoint ? null : (oldBreakpoint, newBreakpoint);
    }

    /// <summary>
    /// Legacy name-based breakpoint selection retained for existing simple ports.
    /// </summary>
    public static ResponsiveBreakpoint Select(
        ushort width,
        IReadOnlyList<ResponsiveBreakpoint> breakpoints)
    {
        ArgumentNullException.ThrowIfNull(breakpoints);

        if (breakpoints.Count == 0)
            return new ResponsiveBreakpoint("default", 0);

        return breakpoints
            .Where(point => width >= point.MinimumWidth)
            .DefaultIfEmpty(breakpoints[0])
            .MaxBy(static point => point.MinimumWidth);
    }
}
