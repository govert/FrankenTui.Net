// SPDX-License-Identifier: MIT
// Port of ftui-text::justification.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 3F2F4BB1333A547497933F010B6497F17CA60BA1AC521737052E49E4D5A7DD7A.

using System.Globalization;

namespace FrankenTui.Text;

/// <summary>Text alignment and space-modulation mode.</summary>
public readonly record struct JustifyMode
{
    private readonly byte _value;

    private JustifyMode(byte value) => _value = value;

    public static JustifyMode Left => default;

    public static JustifyMode Right => new(1);

    public static JustifyMode Center => new(2);

    public static JustifyMode Full => new(3);

    public static JustifyMode Distributed => new(4);

    public bool RequiresJustification => _value is 3 or 4;

    public bool JustifyLastLine => _value == 4;

    public override string ToString() => _value switch
    {
        0 => "left",
        1 => "right",
        2 => "center",
        3 => "full",
        4 => "distributed",
        _ => "unknown"
    };
}

/// <summary>Classification of horizontal space.</summary>
public readonly record struct SpaceCategory
{
    private readonly byte _value;

    private SpaceCategory(byte value) => _value = value;

    public static SpaceCategory InterWord => default;

    public static SpaceCategory InterSentence => new(1);

    public static SpaceCategory InterCharacter => new(2);

    public override string ToString() => _value switch
    {
        0 => "inter-word",
        1 => "inter-sentence",
        2 => "inter-character",
        _ => "unknown"
    };
}

/// <summary>TeX-style natural width, stretch, and shrink in 1/256-cell units.</summary>
public record struct GlueSpec
{
    public const uint SubcellScale = 256;

    public GlueSpec()
    {
        this = WordSpace;
    }

    public GlueSpec(uint naturalSubcell, uint stretchSubcell, uint shrinkSubcell)
    {
        NaturalSubcell = naturalSubcell;
        StretchSubcell = stretchSubcell;
        ShrinkSubcell = shrinkSubcell;
    }

    public uint NaturalSubcell { get; set; }

    public uint StretchSubcell { get; set; }

    public uint ShrinkSubcell { get; set; }

    public static GlueSpec WordSpace => new(SubcellScale, SubcellScale / 2, SubcellScale / 3);

    public static GlueSpec SentenceSpace => new(SubcellScale * 3 / 2, SubcellScale, SubcellScale / 3);

    public static GlueSpec FrenchSpace => WordSpace;

    public static GlueSpec InterCharacter => new(0, SubcellScale / 16, SubcellScale / 32);

    public static GlueSpec Default => WordSpace;

    public static GlueSpec Rigid(uint widthSubcell) => new(widthSubcell, 0, 0);

    public readonly uint AdjustedWidth(int ratioFixed)
    {
        if (ratioFixed == 0)
        {
            return NaturalSubcell;
        }

        if (ratioFixed > 0)
        {
            var delta = ((ulong)StretchSubcell * (uint)ratioFixed) / SubcellScale;
            return SaturatingAdd(NaturalSubcell, (uint)Math.Min(delta, StretchSubcell));
        }

        var absoluteRatio = UnsignedAbs(ratioFixed);
        var shrink = ((ulong)ShrinkSubcell * absoluteRatio) / SubcellScale;
        var boundedShrink = (uint)Math.Min(shrink, ShrinkSubcell);
        return NaturalSubcell >= boundedShrink ? NaturalSubcell - boundedShrink : 0;
    }

    public readonly uint Elasticity => SaturatingAdd(StretchSubcell, ShrinkSubcell);

    public readonly bool IsRigid => StretchSubcell == 0 && ShrinkSubcell == 0;

    public readonly override string ToString() =>
        $"{(NaturalSubcell / 256d).ToString("F2", CultureInfo.InvariantCulture)} " +
        $"+{(StretchSubcell / 256d).ToString("F2", CultureInfo.InvariantCulture)} " +
        $"-{(ShrinkSubcell / 256d).ToString("F2", CultureInfo.InvariantCulture)}";

    private static uint UnsignedAbs(int value) => value == int.MinValue
        ? 2_147_483_648u
        : (uint)Math.Abs(value);

    private static uint SaturatingAdd(uint left, uint right)
    {
        var sum = (ulong)left + right;
        return sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
    }
}

/// <summary>Penalty modifiers for visible spacing adjustments.</summary>
public record struct SpacePenalty
{
    public SpacePenalty()
    {
        this = Default;
    }

    public SpacePenalty(ulong excessiveStretch, ulong excessiveShrink, ulong trackingPenalty)
    {
        ExcessiveStretch = excessiveStretch;
        ExcessiveShrink = excessiveShrink;
        TrackingPenalty = trackingPenalty;
    }

    public ulong ExcessiveStretch { get; set; }

    public ulong ExcessiveShrink { get; set; }

    public ulong TrackingPenalty { get; set; }

    public static SpacePenalty Default => new(50, 80, 200);

    public static SpacePenalty Permissive => new(10, 20, 50);

    public static SpacePenalty Strict => new(200, 300, 1000);

    public readonly ulong Evaluate(int ratioFixed, SpaceCategory category)
    {
        const int threshold = 192;
        var penalty = ratioFixed > threshold
            ? ExcessiveStretch
            : ratioFixed < -threshold
                ? ExcessiveShrink
                : 0;
        if (category == SpaceCategory.InterCharacter && ratioFixed != 0)
        {
            penalty = SaturatingAdd(penalty, TrackingPenalty);
        }

        return penalty;
    }

    private static ulong SaturatingAdd(ulong left, ulong right) =>
        ulong.MaxValue - left < right ? ulong.MaxValue : left + right;
}

/// <summary>Unified horizontal spacing and justification configuration.</summary>
public record struct JustificationControl
{
    public JustificationControl()
    {
        this = Terminal;
    }

    public JustificationControl(
        JustifyMode mode,
        GlueSpec wordSpace,
        GlueSpec sentenceSpace,
        GlueSpec characterSpace,
        SpacePenalty penalties,
        bool frenchSpacing,
        byte maxConsecutiveHyphens,
        uint emergencyStretchFactor)
    {
        Mode = mode;
        WordSpace = wordSpace;
        SentenceSpace = sentenceSpace;
        CharacterSpace = characterSpace;
        Penalties = penalties;
        FrenchSpacing = frenchSpacing;
        MaxConsecutiveHyphens = maxConsecutiveHyphens;
        EmergencyStretchFactor = emergencyStretchFactor;
    }

    public JustifyMode Mode { get; set; }

    public GlueSpec WordSpace { get; set; }

    public GlueSpec SentenceSpace { get; set; }

    public GlueSpec CharacterSpace { get; set; }

    public GlueSpec CharSpace
    {
        readonly get => CharacterSpace;
        set => CharacterSpace = value;
    }

    public SpacePenalty Penalties { get; set; }

    public bool FrenchSpacing { get; set; }

    public byte MaxConsecutiveHyphens { get; set; }

    public uint EmergencyStretchFactor { get; set; }

    public static JustificationControl Terminal => new(
        JustifyMode.Left,
        GlueSpec.Rigid(GlueSpec.SubcellScale),
        GlueSpec.Rigid(GlueSpec.SubcellScale),
        GlueSpec.Rigid(0),
        SpacePenalty.Default,
        true,
        0,
        GlueSpec.SubcellScale);

    public static JustificationControl Readable => new(
        JustifyMode.Full,
        GlueSpec.WordSpace,
        GlueSpec.FrenchSpace,
        GlueSpec.Rigid(0),
        SpacePenalty.Default,
        true,
        3,
        GlueSpec.SubcellScale * 3 / 2);

    public static JustificationControl Typographic => new(
        JustifyMode.Full,
        GlueSpec.WordSpace,
        GlueSpec.SentenceSpace,
        GlueSpec.InterCharacter,
        SpacePenalty.Strict,
        false,
        2,
        GlueSpec.SubcellScale * 2);

    public static JustificationControl Default => Terminal;

    public readonly GlueSpec GlueFor(SpaceCategory category)
    {
        if (category == SpaceCategory.InterSentence)
        {
            return FrenchSpacing ? WordSpace : SentenceSpace;
        }

        return category == SpaceCategory.InterCharacter ? CharacterSpace : WordSpace;
    }

    public readonly uint TotalNatural(ReadOnlySpan<SpaceCategory> spaces) =>
        Sum(spaces, static glue => glue.NaturalSubcell);

    public readonly uint TotalStretch(ReadOnlySpan<SpaceCategory> spaces) =>
        Sum(spaces, static glue => glue.StretchSubcell);

    public readonly uint TotalShrink(ReadOnlySpan<SpaceCategory> spaces) =>
        Sum(spaces, static glue => glue.ShrinkSubcell);

    public readonly int? AdjustmentRatio(int slackSubcell, uint totalStretch, uint totalShrink)
    {
        if (slackSubcell == 0)
        {
            return 0;
        }

        if (slackSubcell > 0)
        {
            if (totalStretch == 0)
            {
                return null;
            }

            var ratio = ((long)slackSubcell * GlueSpec.SubcellScale) / totalStretch;
            return ratio > int.MaxValue ? int.MaxValue : (int)ratio;
        }

        if (totalShrink == 0)
        {
            return null;
        }

        var shrinkRatio = ((long)slackSubcell * GlueSpec.SubcellScale) / totalShrink;
        return shrinkRatio < -(long)GlueSpec.SubcellScale ? null : (int)shrinkRatio;
    }

    public static ulong Badness(int ratioFixed)
    {
        if (ratioFixed == 0)
        {
            return 0;
        }

        var absolute = ratioFixed == int.MinValue
            ? 2_147_483_648ul
            : (ulong)Math.Abs(ratioFixed);
        var cube = SaturatingMultiply(SaturatingMultiply(absolute, absolute), absolute);
        return SaturatingMultiply(cube, 10_000) / 16_777_216;
    }

    public readonly ulong LineDemerits(
        int ratioFixed,
        ReadOnlySpan<SpaceCategory> spaces,
        long breakPenalty)
    {
        var badness = Badness(ratioFixed);
        if (badness == ulong.MaxValue)
        {
            return ulong.MaxValue;
        }

        var basis = SaturatingAdd(badness, 10);
        var demerits = SaturatingMultiply(basis, basis);
        var penaltyMagnitude = breakPenalty == long.MinValue
            ? 9_223_372_036_854_775_808ul
            : (ulong)Math.Abs(breakPenalty);
        demerits = SaturatingAdd(demerits, SaturatingMultiply(penaltyMagnitude, penaltyMagnitude));
        foreach (var category in spaces)
        {
            demerits = SaturatingAdd(demerits, Penalties.Evaluate(ratioFixed, category));
        }

        return demerits;
    }

    public readonly IReadOnlyList<string> Validate()
    {
        var warnings = new List<string>(4);
        if (Mode.RequiresJustification && WordSpace.IsRigid)
        {
            warnings.Add("justified mode with rigid word space cannot modulate spacing");
        }

        if (WordSpace.ShrinkSubcell > WordSpace.NaturalSubcell)
        {
            warnings.Add("word space shrink exceeds natural width (would go negative)");
        }

        if (SentenceSpace.ShrinkSubcell > SentenceSpace.NaturalSubcell)
        {
            warnings.Add("sentence space shrink exceeds natural width (would go negative)");
        }

        if (EmergencyStretchFactor == 0)
        {
            warnings.Add("emergency stretch factor is zero (no emergency fallback)");
        }

        return warnings;
    }

    public readonly override string ToString() =>
        $"mode={Mode} word=[{WordSpace}] french={(FrenchSpacing ? "true" : "false")}";

    private readonly uint Sum(ReadOnlySpan<SpaceCategory> spaces, Func<GlueSpec, uint> selector)
    {
        var total = 0u;
        foreach (var category in spaces)
        {
            var value = selector(GlueFor(category));
            var sum = (ulong)total + value;
            total = sum > uint.MaxValue ? uint.MaxValue : (uint)sum;
        }

        return total;
    }

    private static ulong SaturatingAdd(ulong left, ulong right) =>
        ulong.MaxValue - left < right ? ulong.MaxValue : left + right;

    private static ulong SaturatingMultiply(ulong left, ulong right) =>
        left != 0 && right > ulong.MaxValue / left ? ulong.MaxValue : left * right;
}
