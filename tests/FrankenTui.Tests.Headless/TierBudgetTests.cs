// SPDX-License-Identifier: MIT
// Closed source-behavior denominator for ftui-text::tier_budget at 15cc6543.

using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class TierBudgetTests
{
    [Theory]
    [InlineData(60u, 16_666ul)]
    [InlineData(30u, 33_333ul)]
    public void FrameBudgetFromFpsUsesIntegerMicroseconds(uint fps, ulong expected) =>
        Assert.Equal(expected, FrameBudget.FromFps(fps));

    [Fact]
    public void ZeroFpsFailsRatherThanInventingAnUnboundedBudget() =>
        Assert.Throws<DivideByZeroException>(() => FrameBudget.FromFps(0));

    [Fact]
    public void FrameBudgetConvertsToExactManagedDuration()
    {
        var budget = new FrameBudget(16_000, 1_000, 5_000, 2_000, 3_000, 5_000);

        Assert.Equal(TimeSpan.FromMicroseconds(16_000), budget.AsDuration());
    }

    [Fact]
    public void ManagedDurationBoundaryIsExplicit()
    {
        var budget = new FrameBudget(ulong.MaxValue, 0, 0, 0, 0, 0);

        Assert.Throws<OverflowException>(() => budget.AsDuration());
    }

    [Fact]
    public void FrameBudgetChecksAllocationConsistency()
    {
        var consistent = new FrameBudget(10_000, 1_000, 2_000, 2_000, 2_000, 3_000);
        var inconsistent = consistent with { HeadroomUs = 999 };

        Assert.Equal(10_000ul, consistent.Allocated());
        Assert.True(consistent.IsConsistent());
        Assert.False(inconsistent.IsConsistent());
    }

    [Fact]
    public void FrameBudgetDisplayMatchesSourceVocabulary()
    {
        var text = new FrameBudget(4_000, 200, 800, 500, 1_000, 1_500).ToString();

        Assert.Equal(
            "4000µs (layout=200µs shaping=800µs diff=500µs present=1000µs headroom=1500µs)",
            text);
    }

    [Fact]
    public void MemoryBudgetComputesAndDisplaysTransientTotal()
    {
        var budget = new MemoryBudget(1024, 512, 256, 100, 50);

        Assert.Equal((nuint)1792, budget.TransientTotal());
        Assert.Equal(
            "transient=1792B (shaping=1024B layout=512B diff=256B) caches: width=100 shaping=50",
            budget.ToString());
    }

    [Fact]
    public void QueueBudgetComputesAndDisplaysTotal()
    {
        var budget = new QueueBudget(10, 20, 5);

        Assert.Equal((nuint)35, budget.TotalMax());
        Assert.Equal("reshape=10 rewrap=20 reflow=5", budget.ToString());
    }

    [Fact]
    public void TierBudgetDisplayIncludesTierAndFrameBudget()
    {
        var text = TierLadder.Default60Fps().Budget(LayoutTier.Fast).ToString();

        Assert.Contains("[fast]", text);
        Assert.Contains("4000µs", text);
    }

    [Fact]
    public void EmergencyFeaturesAreMinimal()
    {
        var features = TierLadder.Default60Fps().FeaturesFor(LayoutTier.Emergency);

        Assert.False(features.ShapedText);
        Assert.False(features.OptimalBreaking);
        Assert.False(features.Justification);
        Assert.False(features.Hyphenation);
        Assert.True(features.TerminalFallback);
        Assert.True(features.WidthCache);
        Assert.True(features.IncrementalDiff);
    }

    [Fact]
    public void FastFeaturesUseTerminalPath()
    {
        var features = TierLadder.Default60Fps().FeaturesFor(LayoutTier.Fast);

        Assert.False(features.ShapedText);
        Assert.False(features.OptimalBreaking);
        Assert.True(features.TerminalFallback);
        Assert.True(features.WidthCache);
    }

    [Fact]
    public void BalancedFeaturesEnableMeasuredTextWithoutTypographicExtras()
    {
        var features = TierLadder.Default60Fps().FeaturesFor(LayoutTier.Balanced);

        Assert.True(features.ShapedText);
        Assert.True(features.OptimalBreaking);
        Assert.True(features.ShapingCache);
        Assert.True(features.SubcellSpacing);
        Assert.False(features.Hyphenation);
        Assert.False(features.Justification);
    }

    [Fact]
    public void QualityFeaturesEnableAllSourceFeatures()
    {
        var features = TierLadder.Default60Fps().FeaturesFor(LayoutTier.Quality);

        Assert.All(
        [
            features.ShapedText,
            features.TerminalFallback,
            features.OptimalBreaking,
            features.Hyphenation,
            features.Justification,
            features.Tracking,
            features.BaselineGrid,
            features.ParagraphSpacing,
            features.FirstLineIndent,
            features.WidthCache,
            features.ShapingCache,
            features.IncrementalDiff,
            features.SubcellSpacing
        ],
        Assert.True);
    }

    [Fact]
    public void ActiveFeatureListPreservesSourceOrderAndDisplay()
    {
        var emergency = TierLadder.Default60Fps().FeaturesFor(LayoutTier.Emergency);

        Assert.Equal(["terminal-fallback", "width-cache", "incremental-diff"], emergency.ActiveList());
        Assert.Equal("[emergency] terminal-fallback, width-cache, incremental-diff", emergency.ToString());
        Assert.Contains("justification", TierLadder.Default60Fps().FeaturesFor(LayoutTier.Quality).ToString());
    }

    [Fact]
    public void DefaultLadderBudgetsAreConsistentAndOrdered()
    {
        var ladder = TierLadder.Default60Fps();

        Assert.Empty(ladder.CheckBudgetConsistency());
        Assert.Empty(ladder.CheckBudgetOrdering());
    }

    [Fact]
    public void DefaultLadderFeaturesAreMonotonic() =>
        Assert.Empty(TierLadder.Default60Fps().CheckMonotonicity());

    [Fact]
    public void MonotonicityReportNamesFeatureAndAffectedTiers()
    {
        var ladder = TierLadder.Default60Fps();
        ladder.Features[3].WidthCache = false;

        Assert.Equal(
            ["width_cache is enabled at balanced but disabled at quality"],
            ladder.CheckMonotonicity());
    }

    [Fact]
    public void BudgetConsistencyReportExplainsMismatch()
    {
        var ladder = TierLadder.Default60Fps();
        ladder.Budgets[1] = ladder.Budgets[1] with
        {
            Frame = ladder.Budgets[1].Frame with { HeadroomUs = 1 }
        };

        Assert.Equal(
            ["[fast] frame sub-budgets sum to 2501µs but total is 4000µs"],
            ladder.CheckBudgetConsistency());
    }

    [Fact]
    public void BudgetOrderingReportExplainsInversion()
    {
        var ladder = TierLadder.Default60Fps();
        ladder.Budgets[1] = ladder.Budgets[1] with
        {
            Frame = ladder.Budgets[1].Frame with { TotalUs = 2_000 }
        };

        Assert.Equal(
            ["frame budget emergency (2000µs) >= fast (2000µs)"],
            ladder.CheckBudgetOrdering());
    }

    [Fact]
    public void LadderLookupReturnsCanonicalBudgets()
    {
        var ladder = TierLadder.Default60Fps();

        Assert.Equal(2_000ul, ladder.Budget(LayoutTier.Emergency).Frame.TotalUs);
        Assert.Equal(4_000ul, ladder.Budget(LayoutTier.Fast).Frame.TotalUs);
        Assert.Equal(8_000ul, ladder.Budget(LayoutTier.Balanced).Frame.TotalUs);
        Assert.Equal(16_000ul, ladder.Budget(LayoutTier.Quality).Frame.TotalUs);
    }

    [Fact]
    public void ConstructorAndFactoryProduceEquivalentDefaults()
    {
        var constructed = new TierLadder();
        var factory = TierLadder.Default60Fps();

        Assert.Equal(factory.Budgets, constructed.Budgets);
        Assert.Equal(factory.Features, constructed.Features);
    }

    [Fact]
    public void CloneOwnsIndependentArrays()
    {
        var original = TierLadder.Default60Fps();
        var clone = original.Clone();
        clone.Features[3].WidthCache = false;

        Assert.True(original.Features[3].WidthCache);
        Assert.False(clone.Features[3].WidthCache);
    }

    [Fact]
    public void PublicConstructorTakesOwnershipByValueAndRequiresClosedFourTierSurface()
    {
        var source = TierLadder.Default60Fps();
        var ladder = new TierLadder(source.Budgets, source.Features);
        source.Features[0].WidthCache = false;

        Assert.True(ladder.Features[0].WidthCache);
        Assert.Throws<ArgumentException>(() => new TierLadder([], new TierFeatures[4]));
        Assert.Throws<ArgumentException>(() => new TierLadder(new TierBudget[4], []));
    }

    [Fact]
    public void LadderDisplayContainsAllTiersAndSourceSectionBreak()
    {
        var text = TierLadder.Default60Fps().ToString();

        Assert.Contains("[emergency]", text);
        Assert.Contains("[fast]", text);
        Assert.Contains("[balanced]", text);
        Assert.Contains("[quality]", text);
        Assert.Contains("\n\n[emergency]", text);
    }

    [Fact]
    public void AllSafetyInvariantsAreClosedAndNamed()
    {
        Assert.Equal(8, SafetyInvariant.All.Count);
        Assert.Equal(SafetyInvariant.NoContentLoss, default(SafetyInvariant));
        Assert.Equal("no-content-loss", SafetyInvariant.NoContentLoss.ToString());
        Assert.Equal("wide-char-width", SafetyInvariant.WideCharWidth.ToString());
        Assert.Equal("greedy-wrap-fallback", SafetyInvariant.GreedyWrapFallback.ToString());
        Assert.Contains(SafetyInvariant.BufferSizeMatch, SafetyInvariant.All);
        Assert.Contains(SafetyInvariant.DiffIdempotence, SafetyInvariant.All);
        Assert.Contains(SafetyInvariant.WidthDeterminism, SafetyInvariant.All);
    }

    [Fact]
    public void AllTierBudgetsFitWithinSixtyFps()
    {
        var maximum = FrameBudget.FromFps(60);

        Assert.All(TierLadder.Default60Fps().Budgets, budget => Assert.True(budget.Frame.TotalUs <= maximum));
    }

    [Fact]
    public void EmergencyQueueDisablesReshapeAndQualityHasLargestCaches()
    {
        var ladder = TierLadder.Default60Fps();
        var emergency = ladder.Budget(LayoutTier.Emergency);
        var quality = ladder.Budget(LayoutTier.Quality);

        Assert.Equal((nuint)0, emergency.Queue.MaxReshapePending);
        Assert.True(quality.Memory.WidthCacheEntries > emergency.Memory.WidthCacheEntries);
        Assert.True(quality.Memory.ShapingCacheEntries > emergency.Memory.ShapingCacheEntries);
    }
}
