// SPDX-License-Identifier: MIT
// Port of ftui-text::layout_policy.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 0F9DBA2F20032805C4FA37F6B032319FB6AA44A93EE848142490E7273467C539.
// ADAPTATION: Rust Result is represented by PolicyResolution; usize maps to nuint.

namespace FrankenTui.Text;

/// <summary>Named text-layout quality tier with source ordering.</summary>
public readonly record struct LayoutTier : IComparable<LayoutTier>
{
    // Zero deliberately means Balanced so CLR default matches Rust's derived Default.
    private readonly byte _value;

    private LayoutTier(byte value) => _value = value;

    public static LayoutTier Emergency => new(1);

    public static LayoutTier Fast => new(2);

    public static LayoutTier Balanced => default;

    public static LayoutTier Quality => new(3);

    public LayoutTier? Degrade() => Rank switch
    {
        3 => Balanced,
        2 => Fast,
        1 => Emergency,
        _ => null
    };

    public IReadOnlyList<LayoutTier> DegradationChain()
    {
        var result = new List<LayoutTier>(4) { this };
        var current = this;
        while (current.Degrade() is { } next)
        {
            result.Add(next);
            current = next;
        }

        return result;
    }

    public int CompareTo(LayoutTier other) => Rank.CompareTo(other.Rank);

    public static bool operator <(LayoutTier left, LayoutTier right) => left.Rank < right.Rank;

    public static bool operator >(LayoutTier left, LayoutTier right) => left.Rank > right.Rank;

    public static bool operator <=(LayoutTier left, LayoutTier right) => left.Rank <= right.Rank;

    public static bool operator >=(LayoutTier left, LayoutTier right) => left.Rank >= right.Rank;

    public override string ToString() => Rank switch
    {
        0 => "emergency",
        1 => "fast",
        2 => "balanced",
        3 => "quality",
        _ => "unknown"
    };

    private int Rank => _value switch
    {
        1 => 0,
        2 => 1,
        0 => 2,
        3 => 3,
        _ => -1
    };
}

/// <summary>Runtime features that constrain text-layout realization.</summary>
public record struct RuntimeCapability(
    bool ProportionalFonts,
    bool SubpixelPositioning,
    bool HyphenationAvailable,
    bool TrackingSupport,
    bool LigatureSupport,
    nuint MaxParagraphWords)
{
    public static RuntimeCapability Full => new(true, true, true, true, true, 0);

    public static RuntimeCapability Terminal => new(false, false, false, false, false, 0);

    public static RuntimeCapability Web => new(true, true, true, false, true, 0);

    public readonly bool SupportsTier(LayoutTier tier) =>
        tier != LayoutTier.Quality || ProportionalFonts;

    public readonly LayoutTier BestTier() => SupportsTier(LayoutTier.Quality)
        ? LayoutTier.Quality
        : SupportsTier(LayoutTier.Balanced)
            ? LayoutTier.Balanced
            : LayoutTier.Fast;

    public readonly override string ToString() =>
        $"proportional={Bool(ProportionalFonts)} subpixel={Bool(SubpixelPositioning)} " +
        $"hyphen={Bool(HyphenationAvailable)} tracking={Bool(TrackingSupport)} " +
        $"ligature={Bool(LigatureSupport)}";

    private static string Bool(bool value) => value ? "true" : "false";
}

/// <summary>User-facing layout policy plus optional subsystem overrides.</summary>
public record struct LayoutPolicy
{
    private const uint DefaultLineHeightSubpixels = 16 * 256;
    private bool _degradationDisabled;

    public LayoutPolicy()
    {
        this = Balanced;
    }

    public LayoutPolicy(
        LayoutTier tier,
        bool allowDegradation,
        JustifyMode? justifyOverride = null,
        VerticalPolicy? verticalOverride = null,
        uint lineHeightSubpixels = 0)
    {
        Tier = tier;
        _degradationDisabled = !allowDegradation;
        JustifyOverride = justifyOverride;
        VerticalOverride = verticalOverride;
        LineHeightSubpixels = lineHeightSubpixels;
    }

    public LayoutTier Tier { get; set; }

    public bool AllowDegradation
    {
        readonly get => !_degradationDisabled;
        set => _degradationDisabled = !value;
    }

    public JustifyMode? JustifyOverride { get; set; }

    public VerticalPolicy? VerticalOverride { get; set; }

    public uint LineHeightSubpixels { get; set; }

    public uint LineHeightSubpx
    {
        readonly get => LineHeightSubpixels;
        set => LineHeightSubpixels = value;
    }

    public static LayoutPolicy Emergency => new(LayoutTier.Emergency, false);

    public static LayoutPolicy Fast => new(LayoutTier.Fast, true);

    public static LayoutPolicy Balanced => new(LayoutTier.Balanced, true);

    public static LayoutPolicy Quality => new(LayoutTier.Quality, true);

    public readonly uint EffectiveLineHeight =>
        LineHeightSubpixels == 0 ? DefaultLineHeightSubpixels : LineHeightSubpixels;

    public readonly PolicyResolution Resolve(RuntimeCapability capabilities)
    {
        var effectiveTier = Tier;
        if (!capabilities.SupportsTier(effectiveTier))
        {
            if (AllowDegradation)
            {
                effectiveTier = capabilities.BestTier();
            }
            else
            {
                return PolicyResolution.Failure(new PolicyError(Tier, capabilities.BestTier()));
            }
        }

        var objective = effectiveTier <= LayoutTier.Fast
            ? ParagraphObjective.Terminal
            : effectiveTier == LayoutTier.Balanced
                ? ParagraphObjective.Default
                : ParagraphObjective.Typographic;

        var verticalPolicy = VerticalOverride ?? (effectiveTier <= LayoutTier.Fast
            ? VerticalPolicy.Compact
            : effectiveTier == LayoutTier.Balanced
                ? VerticalPolicy.Readable
                : VerticalPolicy.Typographic);

        var justification = effectiveTier <= LayoutTier.Fast
            ? JustificationControl.Terminal
            : effectiveTier == LayoutTier.Balanced
                ? JustificationControl.Readable
                : JustificationControl.Typographic;
        if (JustifyOverride is { } justifyOverride)
        {
            justification.Mode = justifyOverride;
        }

        if (!capabilities.TrackingSupport)
        {
            justification.CharacterSpace = GlueSpec.Rigid(0);
        }

        if (!capabilities.ProportionalFonts)
        {
            justification.WordSpace = GlueSpec.Rigid(GlueSpec.SubcellScale);
            justification.SentenceSpace = GlueSpec.Rigid(GlueSpec.SubcellScale);
            justification.CharacterSpace = GlueSpec.Rigid(0);
        }

        return PolicyResolution.Success(new ResolvedPolicy
        {
            RequestedTier = Tier,
            EffectiveTier = effectiveTier,
            Degraded = effectiveTier != Tier,
            Objective = objective,
            Vertical = verticalPolicy.Resolve(EffectiveLineHeight),
            Justification = justification,
            UseHyphenation = capabilities.HyphenationAvailable && effectiveTier >= LayoutTier.Balanced,
            UseOptimalBreaking = effectiveTier >= LayoutTier.Balanced,
            LineHeightSubpixels = EffectiveLineHeight
        });
    }

    public readonly override string ToString() =>
        $"tier={Tier} degrade={(AllowDegradation ? "true" : "false")}";
}

/// <summary>Fully resolved configuration ready for the layout engine.</summary>
public sealed record ResolvedPolicy
{
    public required LayoutTier RequestedTier { get; init; }

    public required LayoutTier EffectiveTier { get; init; }

    public required bool Degraded { get; init; }

    public required ParagraphObjective Objective { get; init; }

    public required VerticalMetrics Vertical { get; init; }

    public required JustificationControl Justification { get; init; }

    public required bool UseHyphenation { get; init; }

    public required bool UseOptimalBreaking { get; init; }

    public required uint LineHeightSubpixels { get; init; }

    public uint LineHeightSubpx => LineHeightSubpixels;

    public bool IsJustified => Justification.Mode.RequiresJustification;

    public IReadOnlyList<string> FeatureSummary()
    {
        var features = new List<string>(6)
        {
            UseOptimalBreaking ? "optimal-breaking" : "greedy-wrapping"
        };
        if (IsJustified)
        {
            features.Add("justified");
        }

        if (UseHyphenation)
        {
            features.Add("hyphenation");
        }

        if (Vertical.BaselineGrid.IsActive)
        {
            features.Add("baseline-grid");
        }

        if (Vertical.FirstLineIndentSubpixels > 0)
        {
            features.Add("first-line-indent");
        }

        if (!Justification.CharacterSpace.IsRigid)
        {
            features.Add("tracking");
        }

        return features;
    }

    public override string ToString() =>
        $"{EffectiveTier} (requested {RequestedTier}{(Degraded ? ", degraded" : string.Empty)})";
}

/// <summary>Typed policy-resolution failure.</summary>
public sealed record PolicyError(LayoutTier Requested, LayoutTier BestAvailable)
{
    public override string ToString() =>
        $"requested tier '{Requested}' not supported; best available is '{BestAvailable}'";
}

/// <summary>Managed projection of Rust's Result&lt;ResolvedPolicy, PolicyError&gt;.</summary>
public readonly record struct PolicyResolution
{
    private PolicyResolution(ResolvedPolicy? value, PolicyError? error)
    {
        Value = value;
        Error = error;
    }

    public ResolvedPolicy? Value { get; }

    public PolicyError? Error { get; }

    public bool IsSuccess => Value is not null;

    public bool IsError => Error is not null;

    public static PolicyResolution Success(ResolvedPolicy value) =>
        new(value ?? throw new ArgumentNullException(nameof(value)), null);

    public static PolicyResolution Failure(PolicyError error) =>
        new(null, error ?? throw new ArgumentNullException(nameof(error)));

    public ResolvedPolicy Unwrap() => Value ?? throw new InvalidOperationException(Error?.ToString());
}
