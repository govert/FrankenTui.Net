// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/visibility.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust's Default trait maps to Visibility.Default/new Visibility().
// default(Visibility) remains the CLR all-zero value and therefore equals Never.

using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>Breakpoint-aware visibility bitmask.</summary>
public readonly record struct Visibility
{
    private const byte BreakpointMask = 0b1_1111;
    private readonly byte _mask;

    public Visibility() => _mask = BreakpointMask;

    private Visibility(byte mask) => _mask = (byte)(mask & BreakpointMask);

    public static Visibility Always { get; } = new(BreakpointMask);

    public static Visibility Never { get; } = new(0);

    public static Visibility Default => new();

    public static Visibility VisibleAbove(Breakpoint breakpoint)
    {
        int index = breakpoint.Index();
        return new Visibility((byte)(BreakpointMask << index));
    }

    public static Visibility VisibleBelow(Breakpoint breakpoint)
    {
        int index = breakpoint.Index();
        return new Visibility((byte)((1 << (index + 1)) - 1));
    }

    public static Visibility Only(Breakpoint breakpoint) =>
        new((byte)(1 << breakpoint.Index()));

    public static Visibility At(IEnumerable<Breakpoint> breakpoints)
    {
        ArgumentNullException.ThrowIfNull(breakpoints);
        byte mask = 0;
        foreach (Breakpoint breakpoint in breakpoints)
            mask |= (byte)(1 << breakpoint.Index());
        return new Visibility(mask);
    }

    public static Visibility HiddenBelow(Breakpoint breakpoint) => VisibleAbove(breakpoint);

    public static Visibility HiddenAbove(Breakpoint breakpoint)
    {
        int index = breakpoint.Index();
        return index == 0 ? Never : new Visibility((byte)((1 << index) - 1));
    }

    public static Visibility FromMask(byte mask) => new(mask);

    public bool IsVisible(Breakpoint breakpoint) =>
        (_mask & (1 << breakpoint.Index())) != 0;

    public bool IsHidden(Breakpoint breakpoint) => !IsVisible(breakpoint);

    public bool IsAlways() => _mask == BreakpointMask;

    public bool IsNever() => _mask == 0;

    public byte Mask() => _mask;

    public uint VisibleCount()
    {
        uint count = 0;
        byte value = _mask;
        while (value != 0)
        {
            count += (uint)(value & 1);
            value >>= 1;
        }
        return count;
    }

    public IEnumerable<Breakpoint> VisibleBreakpoints()
    {
        foreach (Breakpoint breakpoint in BreakpointExtensions.All)
        {
            if (IsVisible(breakpoint)) yield return breakpoint;
        }
    }

    public static List<(int Index, Rect Rect)> FilterRects(
        IReadOnlyList<Visibility> visibilities,
        IReadOnlyList<Rect> rects,
        Breakpoint breakpoint)
    {
        ArgumentNullException.ThrowIfNull(visibilities);
        ArgumentNullException.ThrowIfNull(rects);
        int count = Math.Min(visibilities.Count, rects.Count);
        var visible = new List<(int Index, Rect Rect)>();
        for (int index = 0; index < count; index++)
        {
            if (visibilities[index].IsVisible(breakpoint))
                visible.Add((index, rects[index]));
        }
        return visible;
    }

    public static int CountVisible(
        IEnumerable<Visibility> visibilities,
        Breakpoint breakpoint)
    {
        ArgumentNullException.ThrowIfNull(visibilities);
        return visibilities.Count(visibility => visibility.IsVisible(breakpoint));
    }

    public override string ToString()
    {
        if (IsAlways()) return "always";
        if (IsNever()) return "never";
        return string.Join("+", VisibleBreakpoints().Select(static breakpoint => breakpoint.Label()));
    }

    public string ToDebugString() => $"Visibility {{ Mask = {_mask} }}";
}
