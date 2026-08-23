// SPDX-License-Identifier: MIT
// Port of ftui-text::vertical_metrics.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 8FB2F6AC012F127B0D95836B6CC04EFE33E6C0AF28BB2C0EA52C497A561D770B.
// ADAPTATION: Rust usize line counts map to nuint; source u32/u16 units remain exact.

using System.Globalization;

namespace FrankenTui.Text;

/// <summary>Discriminant for the source <see cref="LeadingSpec"/> value.</summary>
public enum LeadingSpecKind : byte
{
    None,
    Fixed,
    Proportional
}

/// <summary>Extra vertical space distributed between lines.</summary>
public readonly record struct LeadingSpec
{
    private const uint SubpixelScale = 256;

    private LeadingSpec(LeadingSpecKind kind, uint value)
    {
        Kind = kind;
        Value = value;
    }

    public LeadingSpecKind Kind { get; }

    /// <summary>Fixed subpixels or a 1/256 proportional fraction, according to <see cref="Kind"/>.</summary>
    public uint Value { get; }

    public static LeadingSpec None => default;

    public static LeadingSpec CssDefault => Proportional(51);

    public static LeadingSpec OneHalf => Proportional(128);

    public static LeadingSpec Double => Proportional(256);

    public static LeadingSpec Fixed(uint subpixels) => new(LeadingSpecKind.Fixed, subpixels);

    public static LeadingSpec Proportional(uint fraction256) =>
        new(LeadingSpecKind.Proportional, fraction256);

    public uint Resolve(uint lineHeightSubpixels) => Kind switch
    {
        LeadingSpecKind.None => 0,
        LeadingSpecKind.Fixed => Value,
        LeadingSpecKind.Proportional =>
            unchecked((uint)(((ulong)lineHeightSubpixels * Value) / SubpixelScale)),
        _ => 0
    };

    public override string ToString() => Kind switch
    {
        LeadingSpecKind.None => "none",
        LeadingSpecKind.Fixed =>
            $"fixed({(Value / (double)SubpixelScale).ToString("F1", CultureInfo.InvariantCulture)}px)",
        LeadingSpecKind.Proportional =>
            (Value / (double)SubpixelScale * 100d).ToString("F0", CultureInfo.InvariantCulture) + "%",
        _ => "none"
    };
}

/// <summary>Space before and after a paragraph, in 1/256-pixel units.</summary>
public record struct ParagraphSpacing(uint BeforeSubpixels, uint AfterSubpixels)
{
    public static ParagraphSpacing None => default;

    public uint BeforeSubpx
    {
        readonly get => BeforeSubpixels;
        set => BeforeSubpixels = value;
    }

    public uint AfterSubpx
    {
        readonly get => AfterSubpixels;
        set => AfterSubpixels = value;
    }

    public static ParagraphSpacing OneLine(uint lineHeightSubpixels) => new(0, lineHeightSubpixels);

    public static ParagraphSpacing HalfLine(uint lineHeightSubpixels) => new(0, lineHeightSubpixels / 2);

    public static ParagraphSpacing Custom(uint beforeSubpixels, uint afterSubpixels) =>
        new(beforeSubpixels, afterSubpixels);

    public readonly uint Total() => SaturatingAdd(BeforeSubpixels, AfterSubpixels);

    public readonly override string ToString() =>
        $"before={(BeforeSubpixels / 256d).ToString("F1", CultureInfo.InvariantCulture)}px " +
        $"after={(AfterSubpixels / 256d).ToString("F1", CultureInfo.InvariantCulture)}px";

    private static uint SaturatingAdd(uint left, uint right)
    {
        var sum = (ulong)left + right;
        return sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
    }
}

/// <summary>Regular vertical grid used to align baselines across columns.</summary>
public record struct BaselineGrid(uint IntervalSubpixels, uint OffsetSubpixels)
{
    public static BaselineGrid None => default;

    public uint IntervalSubpx
    {
        readonly get => IntervalSubpixels;
        set => IntervalSubpixels = value;
    }

    public uint OffsetSubpx
    {
        readonly get => OffsetSubpixels;
        set => OffsetSubpixels = value;
    }

    public static BaselineGrid FromLineHeight(uint lineHeightSubpixels, uint leadingSubpixels) =>
        new(SaturatingAdd(lineHeightSubpixels, leadingSubpixels), 0);

    public readonly bool IsActive => IntervalSubpixels > 0;

    public readonly uint Snap(uint positionSubpixels)
    {
        if (IntervalSubpixels == 0)
        {
            return positionSubpixels;
        }

        var adjusted = positionSubpixels >= OffsetSubpixels
            ? positionSubpixels - OffsetSubpixels
            : 0;
        var remainder = adjusted % IntervalSubpixels;
        return remainder == 0
            ? positionSubpixels
            : SaturatingAdd(positionSubpixels, IntervalSubpixels - remainder);
    }

    private static uint SaturatingAdd(uint left, uint right)
    {
        var sum = (ulong)left + right;
        return sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
    }
}

/// <summary>Progressive vertical-layout quality tier.</summary>
public readonly record struct VerticalPolicy
{
    private const uint SubpixelScale = 256;
    private readonly byte _value;

    private VerticalPolicy(byte value) => _value = value;

    public static VerticalPolicy Compact => default;

    public static VerticalPolicy Readable => new(1);

    public static VerticalPolicy Typographic => new(2);

    public VerticalMetrics Resolve(uint lineHeightSubpixels) => _value switch
    {
        0 => new VerticalMetrics(
            LeadingSpec.None,
            ParagraphSpacing.None,
            BaselineGrid.None,
            0),
        1 => ResolveReadable(lineHeightSubpixels),
        2 => ResolveTypographic(lineHeightSubpixels),
        _ => throw new InvalidOperationException("Unknown vertical policy.")
    };

    public override string ToString() => _value switch
    {
        0 => "compact",
        1 => "readable",
        2 => "typographic",
        _ => "unknown"
    };

    private static VerticalMetrics ResolveReadable(uint lineHeightSubpixels)
    {
        var leading = LeadingSpec.CssDefault;
        var totalLine = SaturatingAdd(lineHeightSubpixels, leading.Resolve(lineHeightSubpixels));
        return new VerticalMetrics(
            leading,
            ParagraphSpacing.HalfLine(totalLine),
            BaselineGrid.None,
            0);
    }

    private static VerticalMetrics ResolveTypographic(uint lineHeightSubpixels)
    {
        var leading = LeadingSpec.CssDefault;
        var leadingValue = leading.Resolve(lineHeightSubpixels);
        var totalLine = SaturatingAdd(lineHeightSubpixels, leadingValue);
        return new VerticalMetrics(
            leading,
            ParagraphSpacing.OneLine(totalLine),
            BaselineGrid.FromLineHeight(lineHeightSubpixels, leadingValue),
            2 * SubpixelScale);
    }

    private static uint SaturatingAdd(uint left, uint right)
    {
        var sum = (ulong)left + right;
        return sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
    }
}

/// <summary>Resolved vertical-layout configuration in 1/256-pixel units.</summary>
public record struct VerticalMetrics(
    LeadingSpec Leading,
    ParagraphSpacing ParagraphSpacing,
    BaselineGrid BaselineGrid,
    uint FirstLineIndentSubpixels)
{
    public uint FirstLineIndentSubpx
    {
        readonly get => FirstLineIndentSubpixels;
        set => FirstLineIndentSubpixels = value;
    }

    public readonly uint ParagraphHeight(nuint lineCount, uint lineHeightSubpixels)
    {
        if (lineCount == 0)
        {
            return 0;
        }

        var leading = Leading.Resolve(lineHeightSubpixels);
        var linesHeight = unchecked((uint)lineCount * lineHeightSubpixels);
        var interLeading = lineCount > 1
            ? unchecked((uint)(lineCount - 1) * leading)
            : 0;
        var contentHeight = SaturatingAdd(linesHeight, interLeading);
        var total = SaturatingAdd(
            SaturatingAdd(ParagraphSpacing.BeforeSubpixels, contentHeight),
            ParagraphSpacing.AfterSubpixels);
        return BaselineGrid.IsActive ? BaselineGrid.Snap(total) : total;
    }

    public readonly uint LineY(nuint lineIndex, uint lineHeightSubpixels)
    {
        var lineStep = SaturatingAdd(lineHeightSubpixels, Leading.Resolve(lineHeightSubpixels));
        var offset = unchecked((uint)lineIndex * lineStep);
        var raw = SaturatingAdd(ParagraphSpacing.BeforeSubpixels, offset);
        return BaselineGrid.IsActive ? BaselineGrid.Snap(raw) : raw;
    }

    public readonly uint DocumentHeight(ReadOnlySpan<nuint> paragraphs, uint lineHeightSubpixels)
    {
        var total = 0u;
        for (var index = 0; index < paragraphs.Length; index++)
        {
            total = index > 0
                ? SaturatingAdd(total, Math.Max(ParagraphSpacing.AfterSubpixels, ParagraphSpacing.BeforeSubpixels))
                : SaturatingAdd(total, ParagraphSpacing.BeforeSubpixels);

            var lineCount = paragraphs[index];
            var linesHeight = unchecked((uint)lineCount * lineHeightSubpixels);
            var interLeading = lineCount > 1
                ? unchecked((uint)(lineCount - 1) * Leading.Resolve(lineHeightSubpixels))
                : 0;
            total = SaturatingAdd(SaturatingAdd(total, linesHeight), interLeading);

            if (index == paragraphs.Length - 1)
            {
                total = SaturatingAdd(total, ParagraphSpacing.AfterSubpixels);
            }
        }

        return BaselineGrid.IsActive ? BaselineGrid.Snap(total) : total;
    }

    public static ushort ToCellRows(uint heightSubpixels, uint cellHeightSubpixels)
    {
        if (cellHeightSubpixels == 0)
        {
            return 0;
        }

        var rows = (heightSubpixels / cellHeightSubpixels) +
            (heightSubpixels % cellHeightSubpixels == 0 ? 0u : 1u);
        return (ushort)Math.Min(rows, ushort.MaxValue);
    }

    private static uint SaturatingAdd(uint left, uint right)
    {
        var sum = (ulong)left + right;
        return sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
    }
}
