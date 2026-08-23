// SPDX-License-Identifier: MIT
// Port of ftui-text::tier_budget.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 7D44620FAE07889E0E3FC7246C26C2BA1A87BB10724925CDAC3019264CC61704.
// ADAPTATION: Rust usize maps to nuint; Duration maps to TimeSpan within its signed range.

using System.Text;

namespace FrankenTui.Text;

/// <summary>Wall-clock time budget for one render frame, in microseconds.</summary>
public readonly record struct FrameBudget(
    ulong TotalUs,
    ulong LayoutUs,
    ulong ShapingUs,
    ulong DiffUs,
    ulong PresentUs,
    ulong HeadroomUs)
{
    public static ulong FromFps(uint framesPerSecond) => 1_000_000UL / framesPerSecond;

    /// <summary>
    /// Converts the source duration to the managed duration representation.
    /// Values outside <see cref="TimeSpan"/>'s signed range fail explicitly.
    /// </summary>
    public TimeSpan AsDuration() =>
        new(checked((long)TotalUs * TimeSpan.TicksPerMicrosecond));

    public ulong Allocated() => unchecked(LayoutUs + ShapingUs + DiffUs + PresentUs + HeadroomUs);

    public bool IsConsistent() => Allocated() == TotalUs;

    public override string ToString() =>
        $"{TotalUs}µs (layout={LayoutUs}µs shaping={ShapingUs}µs diff={DiffUs}µs " +
        $"present={PresentUs}µs headroom={HeadroomUs}µs)";
}

/// <summary>Transient per-frame memory and persistent-cache capacity budget.</summary>
public readonly record struct MemoryBudget(
    nuint ShapingBytes,
    nuint LayoutBytes,
    nuint DiffBytes,
    nuint WidthCacheEntries,
    nuint ShapingCacheEntries)
{
    public nuint TransientTotal() => unchecked(ShapingBytes + LayoutBytes + DiffBytes);

    public override string ToString() =>
        $"transient={TransientTotal()}B (shaping={ShapingBytes}B layout={LayoutBytes}B " +
        $"diff={DiffBytes}B) caches: width={WidthCacheEntries} shaping={ShapingCacheEntries}";
}

/// <summary>Depth limits for deferred layout and shaping queues.</summary>
public readonly record struct QueueBudget(
    nuint MaxReshapePending,
    nuint MaxRewrapPending,
    nuint MaxReflowPending)
{
    public nuint TotalMax() => unchecked(MaxReshapePending + MaxRewrapPending + MaxReflowPending);

    public override string ToString() =>
        $"reshape={MaxReshapePending} rewrap={MaxRewrapPending} reflow={MaxReflowPending}";
}

/// <summary>Frame, memory, and queue budgets for one quality tier.</summary>
public readonly record struct TierBudget(
    LayoutTier Tier,
    FrameBudget Frame,
    MemoryBudget Memory,
    QueueBudget Queue)
{
    public override string ToString() =>
        $"[{Tier}] frame: {Frame} | mem: {Memory} | queue: {Queue}";
}

/// <summary>Subsystem feature toggles for one quality tier.</summary>
public record struct TierFeatures(
    LayoutTier Tier,
    bool ShapedText,
    bool TerminalFallback,
    bool OptimalBreaking,
    bool Hyphenation,
    bool Justification,
    bool Tracking,
    bool BaselineGrid,
    bool ParagraphSpacing,
    bool FirstLineIndent,
    bool WidthCache,
    bool ShapingCache,
    bool IncrementalDiff,
    bool SubcellSpacing)
{
    public readonly IReadOnlyList<string> ActiveList()
    {
        var active = new List<string>(13);
        AddIf(active, ShapedText, "shaped-text");
        AddIf(active, TerminalFallback, "terminal-fallback");
        AddIf(active, OptimalBreaking, "optimal-breaking");
        AddIf(active, Hyphenation, "hyphenation");
        AddIf(active, Justification, "justification");
        AddIf(active, Tracking, "tracking");
        AddIf(active, BaselineGrid, "baseline-grid");
        AddIf(active, ParagraphSpacing, "paragraph-spacing");
        AddIf(active, FirstLineIndent, "first-line-indent");
        AddIf(active, WidthCache, "width-cache");
        AddIf(active, ShapingCache, "shaping-cache");
        AddIf(active, IncrementalDiff, "incremental-diff");
        AddIf(active, SubcellSpacing, "subcell-spacing");
        return active;
    }

    public readonly override string ToString() => $"[{Tier}] {string.Join(", ", ActiveList())}";

    private static void AddIf(List<string> output, bool enabled, string name)
    {
        if (enabled)
        {
            output.Add(name);
        }
    }
}

/// <summary>A semantic safety property that no quality tier may disable.</summary>
public readonly record struct SafetyInvariant
{
    private readonly byte _value;

    private SafetyInvariant(byte value) => _value = value;

    public static SafetyInvariant NoContentLoss => default;

    public static SafetyInvariant WideCharWidth => new(1);

    public static SafetyInvariant BufferSizeMatch => new(2);

    public static SafetyInvariant CursorInBounds => new(3);

    public static SafetyInvariant StyleBoundary => new(4);

    public static SafetyInvariant DiffIdempotence => new(5);

    public static SafetyInvariant GreedyWrapFallback => new(6);

    public static SafetyInvariant WidthDeterminism => new(7);

    public static IReadOnlyList<SafetyInvariant> All { get; } = Array.AsReadOnly(
    [
        NoContentLoss,
        WideCharWidth,
        BufferSizeMatch,
        CursorInBounds,
        StyleBoundary,
        DiffIdempotence,
        GreedyWrapFallback,
        WidthDeterminism
    ]);

    public override string ToString() => _value switch
    {
        0 => "no-content-loss",
        1 => "wide-char-width",
        2 => "buffer-size-match",
        3 => "cursor-in-bounds",
        4 => "style-boundary",
        5 => "diff-idempotence",
        6 => "greedy-wrap-fallback",
        7 => "width-determinism",
        _ => "unknown"
    };
}

/// <summary>The canonical quality ladder consumed by adaptive layout control.</summary>
public sealed class TierLadder : ICloneable
{
    public TierLadder()
        : this(CreateDefaultBudgets(), CreateDefaultFeatures(), takeOwnership: true)
    {
    }

    public TierLadder(TierBudget[] budgets, TierFeatures[] features)
        : this(budgets, features, takeOwnership: false)
    {
    }

    private TierLadder(TierBudget[] budgets, TierFeatures[] features, bool takeOwnership)
    {
        ArgumentNullException.ThrowIfNull(budgets);
        ArgumentNullException.ThrowIfNull(features);
        if (budgets.Length != 4)
        {
            throw new ArgumentException("A tier ladder requires exactly four budgets.", nameof(budgets));
        }

        if (features.Length != 4)
        {
            throw new ArgumentException("A tier ladder requires exactly four feature sets.", nameof(features));
        }

        Budgets = takeOwnership ? budgets : (TierBudget[])budgets.Clone();
        Features = takeOwnership ? features : (TierFeatures[])features.Clone();
    }

    /// <summary>Budgets ordered Emergency, Fast, Balanced, Quality.</summary>
    public TierBudget[] Budgets { get; }

    /// <summary>Feature sets ordered Emergency, Fast, Balanced, Quality.</summary>
    public TierFeatures[] Features { get; }

    public static TierLadder Default60Fps() => new();

    public TierBudget Budget(LayoutTier tier) => Budgets[TierIndex(tier)];

    public TierFeatures FeaturesFor(LayoutTier tier) => Features[TierIndex(tier)];

    public IReadOnlyList<string> CheckMonotonicity()
    {
        var violations = new List<string>();
        for (var index = 0; index < Features.Length - 1; index++)
        {
            var lower = Features[index];
            var higher = Features[index + 1];
            Check(violations, lower, higher, "terminal_fallback", lower.TerminalFallback, higher.TerminalFallback);
            Check(violations, lower, higher, "width_cache", lower.WidthCache, higher.WidthCache);
            Check(violations, lower, higher, "incremental_diff", lower.IncrementalDiff, higher.IncrementalDiff);
            Check(violations, lower, higher, "shaped_text", lower.ShapedText, higher.ShapedText);
            Check(violations, lower, higher, "optimal_breaking", lower.OptimalBreaking, higher.OptimalBreaking);
            Check(violations, lower, higher, "hyphenation", lower.Hyphenation, higher.Hyphenation);
            Check(violations, lower, higher, "justification", lower.Justification, higher.Justification);
            Check(violations, lower, higher, "tracking", lower.Tracking, higher.Tracking);
            Check(violations, lower, higher, "baseline_grid", lower.BaselineGrid, higher.BaselineGrid);
            Check(violations, lower, higher, "paragraph_spacing", lower.ParagraphSpacing, higher.ParagraphSpacing);
            Check(violations, lower, higher, "first_line_indent", lower.FirstLineIndent, higher.FirstLineIndent);
            Check(violations, lower, higher, "shaping_cache", lower.ShapingCache, higher.ShapingCache);
            Check(violations, lower, higher, "subcell_spacing", lower.SubcellSpacing, higher.SubcellSpacing);
        }

        return violations;
    }

    public IReadOnlyList<string> CheckBudgetConsistency()
    {
        var issues = new List<string>();
        foreach (var budget in Budgets)
        {
            if (!budget.Frame.IsConsistent())
            {
                issues.Add(
                    $"[{budget.Tier}] frame sub-budgets sum to {budget.Frame.Allocated()}µs " +
                    $"but total is {budget.Frame.TotalUs}µs");
            }
        }

        return issues;
    }

    public IReadOnlyList<string> CheckBudgetOrdering()
    {
        var issues = new List<string>();
        for (var index = 0; index < Budgets.Length - 1; index++)
        {
            var lower = Budgets[index];
            var higher = Budgets[index + 1];
            if (lower.Frame.TotalUs >= higher.Frame.TotalUs)
            {
                issues.Add(
                    $"frame budget {lower.Tier} ({lower.Frame.TotalUs}µs) >= " +
                    $"{higher.Tier} ({higher.Frame.TotalUs}µs)");
            }
        }

        return issues;
    }

    public TierLadder Clone() => new(Budgets, Features);

    object ICloneable.Clone() => Clone();

    public override string ToString()
    {
        var output = new StringBuilder();
        foreach (var budget in Budgets)
        {
            output.Append(budget).Append('\n');
        }

        output.Append('\n');
        foreach (var features in Features)
        {
            output.Append(features).Append('\n');
        }

        return output.ToString();
    }

    private static int TierIndex(LayoutTier tier)
    {
        if (tier == LayoutTier.Emergency)
        {
            return 0;
        }

        if (tier == LayoutTier.Fast)
        {
            return 1;
        }

        if (tier == LayoutTier.Balanced)
        {
            return 2;
        }

        if (tier == LayoutTier.Quality)
        {
            return 3;
        }

        throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown layout tier.");
    }

    private static void Check(
        List<string> output,
        TierFeatures lower,
        TierFeatures higher,
        string name,
        bool lowerEnabled,
        bool higherEnabled)
    {
        if (lowerEnabled && !higherEnabled)
        {
            output.Add($"{name} is enabled at {lower.Tier} but disabled at {higher.Tier}");
        }
    }

    private static TierBudget[] CreateDefaultBudgets() =>
    [
        new(
            LayoutTier.Emergency,
            new FrameBudget(2_000, 100, 200, 200, 500, 1_000),
            new MemoryBudget(64 * 1024, 16 * 1024, 32 * 1024, 256, 0),
            new QueueBudget(0, 4, 1)),
        new(
            LayoutTier.Fast,
            new FrameBudget(4_000, 200, 800, 500, 1_000, 1_500),
            new MemoryBudget(256 * 1024, 64 * 1024, 128 * 1024, 1_000, 0),
            new QueueBudget(0, 16, 4)),
        new(
            LayoutTier.Balanced,
            new FrameBudget(8_000, 500, 2_500, 1_000, 1_500, 2_500),
            new MemoryBudget(1024 * 1024, 256 * 1024, 512 * 1024, 4_000, 512),
            new QueueBudget(32, 64, 16)),
        new(
            LayoutTier.Quality,
            new FrameBudget(16_000, 1_000, 5_000, 2_000, 3_000, 5_000),
            new MemoryBudget(4 * 1024 * 1024, 1024 * 1024, 2 * 1024 * 1024, 16_000, 2_048),
            new QueueBudget(128, 256, 64))
    ];

    private static TierFeatures[] CreateDefaultFeatures() =>
    [
        new(LayoutTier.Emergency, false, true, false, false, false, false, false, false, false, true, false, true, false),
        new(LayoutTier.Fast, false, true, false, false, false, false, false, false, false, true, false, true, false),
        new(LayoutTier.Balanced, true, true, true, false, false, false, false, true, false, true, true, true, true),
        new(LayoutTier.Quality, true, true, true, true, true, true, true, true, true, true, true, true, true)
    ];
}
