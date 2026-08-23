// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/direction.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust enum methods and the free mirror function map to C# extension
// methods and DirectionLayout respectively; observable behavior is unchanged.

using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>Horizontal text and child flow direction.</summary>
public enum FlowDirection
{
    Ltr,
    Rtl,
}

public static class FlowDirectionExtensions
{
    public static bool IsRtl(this FlowDirection direction) => direction == FlowDirection.Rtl;

    public static bool IsLtr(this FlowDirection direction) => direction == FlowDirection.Ltr;

    public static bool LocaleIsRtl(string locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        int separator = locale.IndexOfAny(['-', '_']);
        string language = (separator >= 0 ? locale[..separator] : locale).ToLowerInvariant();
        return language is
            "ar" or "he" or "fa" or "ur" or "ps" or "sd" or "yi" or "ku" or
            "dv" or "ks" or "ckb" or "syr" or "arc" or "nqo" or "man" or "sam";
    }

    public static FlowDirection FromLocale(string locale) =>
        LocaleIsRtl(locale) ? FlowDirection.Rtl : FlowDirection.Ltr;
}

/// <summary>Direction-aware start, end, and center alignment.</summary>
public enum LogicalAlignment
{
    Start,
    End,
    Center,
}

public static class LogicalAlignmentExtensions
{
    public static Alignment Resolve(this LogicalAlignment alignment, FlowDirection flow) =>
        (alignment, flow) switch
        {
            (LogicalAlignment.Start, FlowDirection.Ltr) => Alignment.Start,
            (LogicalAlignment.Start, FlowDirection.Rtl) => Alignment.End,
            (LogicalAlignment.End, FlowDirection.Ltr) => Alignment.End,
            (LogicalAlignment.End, FlowDirection.Rtl) => Alignment.Start,
            (LogicalAlignment.Center, _) => Alignment.Center,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null),
        };
}

/// <summary>Padding or margin expressed in logical direction-aware terms.</summary>
public readonly record struct LogicalSides(
    ushort Top,
    ushort Bottom,
    ushort Start,
    ushort End)
{
    public static LogicalSides All(ushort value) => new(value, value, value, value);

    public static LogicalSides Symmetric(ushort block, ushort inline) =>
        new(block, block, inline, inline);

    public static LogicalSides Inline(ushort start, ushort end) => new(0, 0, start, end);

    public static LogicalSides Block(ushort top, ushort bottom) => new(top, bottom, 0, 0);

    public Sides Resolve(FlowDirection flow) => flow switch
    {
        FlowDirection.Ltr => new Sides(Top, End, Bottom, Start),
        FlowDirection.Rtl => new Sides(Top, Start, Bottom, End),
        _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null),
    };

    public ushort InlineSum() => unchecked((ushort)(Start + End));

    public ushort BlockSum() => unchecked((ushort)(Top + Bottom));

    public static LogicalSides FromPhysical(Sides sides, FlowDirection flow) => flow switch
    {
        FlowDirection.Ltr => new LogicalSides(sides.Top, sides.Bottom, sides.Left, sides.Right),
        FlowDirection.Rtl => new LogicalSides(sides.Top, sides.Bottom, sides.Right, sides.Left),
        _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null),
    };
}

public static class DirectionLayout
{
    /// <summary>Mirror rectangles horizontally inside a containing area.</summary>
    public static void MirrorRectsHorizontal(IList<Rect> rects, Rect area)
    {
        ArgumentNullException.ThrowIfNull(rects);
        for (int index = 0; index < rects.Count; index++)
        {
            Rect rect = rects[index];
            ushort offsetFromLeft = SaturatingSubtract(rect.X, area.X);
            ushort newOffset = SaturatingSubtract(
                SaturatingSubtract(area.Width, offsetFromLeft),
                rect.Width);
            rects[index] = rect with { X = SaturatingAdd(area.X, newOffset) };
        }
    }

    private static ushort SaturatingAdd(ushort left, ushort right) =>
        (ushort)Math.Min((uint)left + right, ushort.MaxValue);

    private static ushort SaturatingSubtract(ushort left, ushort right) =>
        left > right ? (ushort)(left - right) : (ushort)0;
}
