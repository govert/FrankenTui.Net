using System.Globalization;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Parity coverage for the closed 107-test denominator in upstream
/// ftui-render/diff_strategy.rs at 15cc6543. The Rust cases are grouped by
/// behavioral partition here (configuration, estimator, cost/selection,
/// evidence, lifecycle, determinism) rather than copied one-for-one.
/// </summary>
public sealed class DiffStrategyParityTests
{
    [Fact]
    public void DefaultConfigMatchesSource()
    {
        var config = DiffStrategyConfig.Default;

        AssertClose(1.0, config.CScan);
        AssertClose(6.0, config.CEmit);
        AssertClose(0.1, config.CRow);
        AssertClose(1.0, config.PriorAlpha);
        AssertClose(19.0, config.PriorBeta);
        AssertClose(0.95, config.Decay);
        Assert.False(config.Conservative);
        AssertClose(0.95, config.ConservativeQuantile);
        Assert.Equal(1, config.MinObservationCells);
        AssertClose(0.05, config.HysteresisRatio);
        AssertClose(0.002, config.UncertaintyGuardVariance);
    }

    [Fact]
    public void SanitizationPreservesValidConfig()
    {
        var config = new DiffStrategyConfig
        {
            CScan = 2.0,
            CEmit = 8.0,
            CRow = 0.5,
            PriorAlpha = 3.0,
            PriorBeta = 17.0,
            Decay = 0.9,
            Conservative = true,
            ConservativeQuantile = 0.9,
            MinObservationCells = 5,
            HysteresisRatio = 0.1,
            UncertaintyGuardVariance = 0.005
        }.Sanitized();

        AssertClose(2.0, config.CScan);
        AssertClose(8.0, config.CEmit);
        AssertClose(0.5, config.CRow);
        AssertClose(3.0, config.PriorAlpha);
        AssertClose(17.0, config.PriorBeta);
        AssertClose(0.9, config.Decay);
        Assert.True(config.Conservative);
        AssertClose(0.9, config.ConservativeQuantile);
        Assert.Equal(5, config.MinObservationCells);
        AssertClose(0.1, config.HysteresisRatio);
        AssertClose(0.005, config.UncertaintyGuardVariance);
    }

    [Fact]
    public void SanitizationNormalizesEveryInvalidNumericFamily()
    {
        var config = new DiffStrategyConfig
        {
            CScan = -1.0,
            CEmit = double.NaN,
            CRow = double.PositiveInfinity,
            PriorAlpha = 0.0,
            PriorBeta = double.NegativeInfinity,
            Decay = -1.0,
            Conservative = true,
            ConservativeQuantile = 2.0,
            MinObservationCells = -5,
            HysteresisRatio = double.PositiveInfinity,
            UncertaintyGuardVariance = -1.0
        }.Sanitized();

        AssertClose(1.0, config.CScan);
        AssertClose(6.0, config.CEmit);
        AssertClose(0.1, config.CRow);
        AssertClose(1.0, config.PriorAlpha);
        AssertClose(19.0, config.PriorBeta);
        AssertClose(1.0, config.Decay);
        AssertClose(1.0 - 1e-6, config.ConservativeQuantile);
        Assert.Equal(0, config.MinObservationCells);
        AssertClose(0.05, config.HysteresisRatio);
        AssertClose(0.002, config.UncertaintyGuardVariance);

        var allNaN = new DiffStrategyConfig
        {
            CScan = double.NaN,
            CEmit = double.NaN,
            CRow = double.NaN,
            PriorAlpha = double.NaN,
            PriorBeta = double.NaN,
            Decay = double.NaN,
            ConservativeQuantile = double.NaN,
            HysteresisRatio = double.NaN,
            UncertaintyGuardVariance = double.NaN
        }.Sanitized();

        AssertClose(1.0, allNaN.CScan);
        AssertClose(6.0, allNaN.CEmit);
        AssertClose(0.1, allNaN.CRow);
        AssertClose(1.0, allNaN.PriorAlpha);
        AssertClose(19.0, allNaN.PriorBeta);
        AssertClose(1.0, allNaN.Decay);
        AssertClose(1e-6, allNaN.ConservativeQuantile);
        AssertClose(0.05, allNaN.HysteresisRatio);
        AssertClose(0.002, allNaN.UncertaintyGuardVariance);
    }

    [Fact]
    public void ConfigCloneHasIndependentIdentityAndEqualValues()
    {
        var original = DiffStrategyConfig.Default;
        var clone = original.Clone();

        clone.CScan = 99.0;

        AssertClose(1.0, original.CScan);
        AssertClose(99.0, clone.CScan);
        Assert.Contains(nameof(DiffStrategyConfig), original.GetType().Name, StringComparison.Ordinal);
        Assert.Contains("c_scan", original.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void EstimatorInitialStateFormulasAndResetMatchSource()
    {
        var estimator = new ChangeRateEstimator(3.0, 7.0, 1.0, 0);
        var (alpha, beta) = estimator.PosteriorParams;

        AssertClose(3.0, alpha);
        AssertClose(7.0, beta);
        AssertClose(0.3, estimator.Mean);
        AssertClose((3.0 * 7.0) / (10.0 * 10.0 * 11.0), estimator.Variance);

        estimator.Observe(100, 25);
        estimator.Reset();

        Assert.Equal((3.0, 7.0), estimator.PosteriorParams);
    }

    [Fact]
    public void EstimatorDecayMinimumAndChangedClampMatchSource()
    {
        var estimator = new ChangeRateEstimator(1.0, 19.0, 0.95, 10);

        estimator.Observe(9, 9);
        Assert.Equal((1.0, 19.0), estimator.PosteriorParams);

        estimator.Observe(100, 10);
        var (alpha, beta) = estimator.PosteriorParams;
        AssertClose(10.95, alpha);
        AssertClose(108.05, beta);

        estimator.Observe(10, 100);
        (alpha, beta) = estimator.PosteriorParams;
        AssertClose(20.4025, alpha);
        AssertClose(102.6475, beta);
    }

    [Fact]
    public void EstimatorEmptyObservationPausesDecayWhenMinimumIsOne()
    {
        var estimator = new ChangeRateEstimator(1.0, 19.0, 0.5, 1);

        estimator.Observe(0, 0);

        Assert.Equal((1.0, 19.0), estimator.PosteriorParams);
    }

    [Fact]
    public void EstimatorBoundaryObservationsStayBoundedAndClampPosterior()
    {
        var unchanged = new ChangeRateEstimator(1.0, 19.0, 1.0, 0);
        var changed = new ChangeRateEstimator(1.0, 19.0, 1.0, 0);
        unchanged.Observe(100, 0);
        changed.Observe(100, 100);

        Assert.InRange(unchanged.Mean, 0.0, 1.0);
        Assert.InRange(changed.Mean, 0.0, 1.0);
        Assert.True(changed.Mean > unchanged.Mean);

        for (var index = 0; index < 100; index++)
        {
            changed.Observe(1_000_000, 1_000_000);
        }

        var (alpha, beta) = changed.PosteriorParams;
        Assert.InRange(alpha, 1e-6, 1e6);
        Assert.InRange(beta, 1e-6, 1e6);
        Assert.True(changed.Variance >= 0.0);
    }

    [Fact]
    public void EstimatorQuantilesAreMonotonicBoundedAndTightenWithEvidence()
    {
        var estimator = new ChangeRateEstimator(5.0, 15.0, 1.0, 0);
        var q25 = estimator.UpperQuantile(0.25);
        var q50 = estimator.UpperQuantile(0.5);
        var q75 = estimator.UpperQuantile(0.75);
        var q95 = estimator.UpperQuantile(0.95);

        Assert.InRange(q25, 0.0, 1.0);
        Assert.True(q25 <= q50);
        Assert.True(q50 <= q75);
        Assert.True(q75 <= q95);

        var initialVariance = estimator.Variance;
        for (var index = 0; index < 1_000; index++)
        {
            estimator.Observe(100, 5);
        }

        Assert.True(estimator.Variance < initialVariance);
        Assert.InRange(estimator.UpperQuantile(double.NegativeInfinity), 0.0, 1.0);
        Assert.InRange(estimator.UpperQuantile(double.PositiveInfinity), 0.0, 1.0);
        Assert.True(Math.Abs(estimator.UpperQuantile(0.95) - estimator.Mean) < 0.01);
    }

    [Fact]
    public void EstimatorCloneRetainsStateWithoutSharingFutureUpdates()
    {
        var original = new ChangeRateEstimator(1.0, 19.0, 0.95, 1);
        original.Observe(100, 10);
        var clone = original.Clone();

        clone.Observe(100, 100);

        Assert.NotEqual(original.PosteriorParams, clone.PosteriorParams);
        Assert.Contains(nameof(ChangeRateEstimator), original.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SignedManagedInputsAreNormalizedAtTheSourceDomainBoundary()
    {
        var estimator = new ChangeRateEstimator(1.0, 19.0, 0.5, 1);
        estimator.Observe(-10, -5);
        Assert.Equal((1.0, 19.0), estimator.PosteriorParams);

        var selector = DiffStrategySelector.WithDefaults();
        var strategy = selector.Select(-80, -24, -2);

        Assert.Equal(DiffStrategy.DirtyRows, strategy);
        Assert.NotNull(selector.LastEvidence);
        Assert.Equal(0, selector.LastEvidence.TotalCells);
        Assert.Equal("zero_dirty_rows", selector.LastEvidence.GuardReason);
    }

    [Fact]
    public void SelectorDefaultsAndAccessorsStartAtTheSourcePrior()
    {
        var selector = DiffStrategySelector.WithDefaults();

        AssertClose(0.05, selector.PosteriorMean);
        AssertClose(19.0 / 8400.0, selector.PosteriorVariance);
        Assert.Equal((1.0, 19.0), selector.PosteriorParams);
        Assert.Equal(0UL, selector.FrameCount);
        Assert.Null(selector.LastEvidence);
        AssertClose(1.0, selector.Config.CScan);
    }

    [Theory]
    [InlineData(80, 24, 2)]
    [InlineData(80, 24, 0)]
    [InlineData(200, 60, 1)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 24, 5)]
    [InlineData(80, 0, 5)]
    public void DefaultSelectorCoversSmallAndZeroDimensionSourceCases(
        int width,
        int height,
        int dirtyRows)
    {
        var selector = DiffStrategySelector.WithDefaults();

        var strategy = selector.Select(width, height, dirtyRows);

        Assert.True(Enum.IsDefined(strategy));
        Assert.NotNull(selector.LastEvidence);
        Assert.True(double.IsFinite(selector.LastEvidence.CostFull));
        Assert.True(double.IsFinite(selector.LastEvidence.CostDirty));
        Assert.True(double.IsFinite(selector.LastEvidence.CostRedraw));
    }

    [Fact]
    public void ZeroDirtyRowsForcesZeroEmissionEstimateAndTypedGuard()
    {
        var selector = DiffStrategySelector.WithDefaults();

        var strategy = selector.Select(80, 24, 0);
        var evidence = Assert.IsType<StrategyEvidence>(selector.LastEvidence);

        Assert.Equal(DiffStrategy.DirtyRows, strategy);
        Assert.Equal("zero_dirty_rows", evidence.GuardReason);
        AssertClose(2.4, evidence.CostFull);
        AssertClose(0.0, evidence.CostDirty);
        AssertClose(11_520.0, evidence.CostRedraw);
    }

    [Fact]
    public void CurrentCostModelPricesEmissionAgainstEachPathsScannedCells()
    {
        var selector = new DiffStrategySelector(new DiffStrategyConfig
        {
            PriorAlpha = 1.0,
            PriorBeta = 3.0,
            Decay = 1.0,
            HysteresisRatio = 0.0,
            UncertaintyGuardVariance = 0.0
        });

        var strategy = selector.SelectWithScanEstimate(10, 4, 2, 5);
        var evidence = Assert.IsType<StrategyEvidence>(selector.LastEvidence);

        Assert.Equal(DiffStrategy.DirtyRows, strategy);
        AssertClose(50.4, evidence.CostFull);
        AssertClose(12.5, evidence.CostDirty);
        AssertClose(240.0, evidence.CostRedraw);
        AssertClose(0.25, evidence.PosteriorMean);
    }

    [Fact]
    public void ScanEstimateIsCappedToTotalCellsAndZeroEstimateIsAllowed()
    {
        var config = new DiffStrategyConfig
        {
            HysteresisRatio = 0.0,
            UncertaintyGuardVariance = 0.0
        };
        var capped = new DiffStrategySelector(config);
        var zero = new DiffStrategySelector(config);

        capped.SelectWithScan(80, 24, 5, 1_000_000);
        zero.SelectWithScan(80, 24, 5, 0);

        AssertClose(1_920.0 + 6.0 * 0.05 * 1_920.0, capped.LastEvidence!.CostDirty);
        AssertClose(0.0, zero.LastEvidence!.CostDirty);
        Assert.Equal(DiffStrategy.DirtyRows, zero.LastEvidence.Strategy);
    }

    [Fact]
    public void SelectorCanChooseEverySourceStrategyVariant()
    {
        var dirty = DiffStrategySelector.WithDefaults();
        var full = new DiffStrategySelector(new DiffStrategyConfig
        {
            CEmit = 100.0,
            HysteresisRatio = 0.0,
            UncertaintyGuardVariance = 0.0
        });
        var redraw = new DiffStrategySelector(new DiffStrategyConfig
        {
            CScan = 10.0,
            CEmit = 1.0,
            PriorAlpha = 9.0,
            PriorBeta = 1.0,
            HysteresisRatio = 0.0,
            UncertaintyGuardVariance = 0.0
        });

        Assert.Equal(DiffStrategy.DirtyRows, dirty.Select(80, 24, 2));
        Assert.Equal(DiffStrategy.Full, full.SelectWithScan(80, 24, 2, 1_000_000));
        Assert.Equal(DiffStrategy.FullRedraw, redraw.Select(80, 24, 24));
    }

    [Fact]
    public void UncertaintyGuardAvoidsRedrawAndConservativeModeRaisesExpectedCost()
    {
        var guarded = new DiffStrategySelector(new DiffStrategyConfig
        {
            CScan = 10.0,
            CEmit = 1.0,
            UncertaintyGuardVariance = 1e-6,
            HysteresisRatio = 0.0
        });
        var conservative = new DiffStrategySelector(new DiffStrategyConfig
        {
            Conservative = true,
            UncertaintyGuardVariance = 0.0,
            HysteresisRatio = 0.0
        });
        var normal = new DiffStrategySelector(new DiffStrategyConfig
        {
            UncertaintyGuardVariance = 0.0,
            HysteresisRatio = 0.0
        });

        for (var index = 0; index < 20; index++)
        {
            conservative.Observe(100, 5);
            normal.Observe(100, 5);
        }

        var guardedStrategy = guarded.Select(80, 24, 24);
        conservative.Select(80, 24, 12);
        normal.Select(80, 24, 12);

        Assert.NotEqual(DiffStrategy.FullRedraw, guardedStrategy);
        Assert.Equal("uncertainty_variance", guarded.LastEvidence!.GuardReason);
        Assert.True(conservative.LastEvidence!.CostDirty >= normal.LastEvidence!.CostDirty - 1e-6);
    }

    [Fact]
    public void HysteresisMatchesSourceFirstFrameAndStickyTransitionRules()
    {
        var selector = new DiffStrategySelector(new DiffStrategyConfig
        {
            HysteresisRatio = 1.0,
            UncertaintyGuardVariance = 0.0
        });

        var first = selector.Select(80, 24, 1);
        Assert.False(selector.LastEvidence!.HysteresisApplied);
        selector.OverrideLastStrategy(DiffStrategy.Full, "forced_previous");

        var second = selector.Select(80, 24, 1);

        Assert.Equal(DiffStrategy.DirtyRows, first);
        Assert.Equal(DiffStrategy.Full, second);
        Assert.True(selector.LastEvidence!.HysteresisApplied);
    }

    [Fact]
    public void ConcentratedHalfScreenRegressionNeverChoosesFullRedraw()
    {
        var selector = DiffStrategySelector.WithDefaults();

        for (var frame = 0; frame < 10; frame++)
        {
            var strategy = selector.SelectWithScan(200, 60, 30, 6_000);
            Assert.NotEqual(DiffStrategy.FullRedraw, strategy);
            selector.Observe(6_000, 6_000);
        }
    }

    [Fact]
    public void EvidenceFieldsTrackSelectorStateAndCostMonotonicity()
    {
        var few = new DiffStrategySelector(new DiffStrategyConfig
        {
            UncertaintyGuardVariance = 0.0,
            HysteresisRatio = 0.0
        });
        var many = new DiffStrategySelector(new DiffStrategyConfig
        {
            UncertaintyGuardVariance = 0.0,
            HysteresisRatio = 0.0
        });

        few.Select(80, 24, 2);
        many.Select(80, 24, 20);

        Assert.Equal(11_520.0, few.LastEvidence!.CostRedraw);
        Assert.Equal(few.LastEvidence.CostRedraw, many.LastEvidence!.CostRedraw);
        Assert.True(many.LastEvidence.CostFull > few.LastEvidence.CostFull);
        Assert.True(many.LastEvidence.CostDirty > few.LastEvidence.CostDirty);
        Assert.Equal(many.PosteriorParams.Item1, many.LastEvidence.Alpha);
        Assert.Equal(many.PosteriorParams.Item2, many.LastEvidence.Beta);
        Assert.Equal(many.PosteriorMean, many.LastEvidence.PosteriorMean);
        Assert.Equal(many.PosteriorVariance, many.LastEvidence.PosteriorVariance);
    }

    [Fact]
    public void EvidenceJsonAndDisplayMatchSourceFormattingInAnyCulture()
    {
        var evidence = new StrategyEvidence
        {
            Strategy = DiffStrategy.Full,
            CostFull = 1.234,
            CostDirty = 2.346,
            CostRedraw = 3.456,
            PosteriorMean = 0.1234564,
            PosteriorVariance = 0.001234567,
            Alpha = 4.25,
            Beta = 5.75,
            DirtyRows = 2,
            TotalRows = 4,
            TotalCells = 40,
            GuardReason = "none",
            HysteresisApplied = true,
            HysteresisRatio = 0.05
        };
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal(
                "{\"schema\":\"diff-strategy-v1\",\"strategy\":\"Full\",\"cost_full\":1.23,\"cost_dirty\":2.35,\"cost_redraw\":3.46,\"posterior_mean\":0.123456,\"posterior_var\":0.00123457,\"alpha\":4.2500,\"beta\":5.7500,\"dirty_rows\":2,\"total_rows\":4,\"total_cells\":40,\"guard\":\"none\",\"hysteresis\":true,\"hysteresis_ratio\":0.0500}",
                evidence.ToJsonl());
            Assert.Equal(
                "Strategy: Full\nCosts: Full=1.23, Dirty=2.35, Redraw=3.46\nPosterior: p~Beta(4.25,5.75), E[p]=0.1235, Var[p]=0.001235\nDirty: 2/4 rows, 40 total cells\nGuard: none, Hysteresis: true (ratio 0.050)\n",
                evidence.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ManagedOverrideReasonIsEscapedWithoutChangingSourceGuardValues()
    {
        var selector = DiffStrategySelector.WithDefaults();
        selector.Select(80, 24, 5);

        selector.OverrideLastStrategy(DiffStrategy.FullRedraw, "forced\"line\nnext");

        Assert.Equal(DiffStrategy.FullRedraw, selector.LastEvidence!.Strategy);
        Assert.False(selector.LastEvidence.HysteresisApplied);
        Assert.Contains("\"guard\":\"forced\\\"line\\nnext\"", selector.LastEvidence.ToJsonl(), StringComparison.Ordinal);
    }

    [Fact]
    public void EvidenceAndSelectorClonesAreIndependentAndSourceShaped()
    {
        var selector = DiffStrategySelector.WithDefaults();
        selector.Select(80, 24, 5);
        selector.Ledger.Record(selector.LastEvidence!.Clone());
        var originalGuard = selector.Ledger.Entries[0].GuardReason;
        var clone = selector.Clone();

        clone.OverrideLastStrategy(DiffStrategy.FullRedraw, "clone_only");
        clone.Observe(100, 100);
        clone.Ledger.Entries[0].GuardReason = "changed";

        Assert.NotEqual(selector.LastEvidence.Strategy, clone.LastEvidence!.Strategy);
        Assert.NotEqual(selector.PosteriorParams, clone.PosteriorParams);
        Assert.Equal(originalGuard, selector.Ledger.Entries[0].GuardReason);
        Assert.Contains(nameof(DiffStrategySelector), selector.ToString(), StringComparison.Ordinal);
        Assert.Contains("frame_count", selector.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void FrameCountObserveOverrideAndResetFollowSourceLifecycle()
    {
        var selector = DiffStrategySelector.WithDefaults();

        selector.Observe(100, 10);
        Assert.Equal(0UL, selector.FrameCount);
        selector.OverrideLastStrategy(DiffStrategy.Full, "noop");
        Assert.Null(selector.LastEvidence);

        selector.Select(80, 24, 5);
        selector.Select(80, 24, 6);
        Assert.Equal(2UL, selector.FrameCount);
        Assert.NotNull(selector.LastEvidence);

        selector.Reset();

        Assert.Equal(0UL, selector.FrameCount);
        Assert.Null(selector.LastEvidence);
        Assert.Equal((1.0, 19.0), selector.PosteriorParams);
    }

    [Fact]
    public void LongTraceIsDeterministicAcrossIndependentSelectors()
    {
        var first = DiffStrategySelector.WithDefaults();
        var second = DiffStrategySelector.WithDefaults();

        for (var frame = 0; frame < 200; frame++)
        {
            var dirty = frame * 3 % 24 + 1;
            var scanned = 80 * dirty;
            var changed = Math.Max(1, frame * 7 % Math.Max(1, scanned));

            Assert.Equal(first.Select(80, 24, dirty), second.Select(80, 24, dirty));
            Assert.Equal(first.LastEvidence!.ToJsonl(), second.LastEvidence!.ToJsonl());

            first.Observe(scanned, changed);
            second.Observe(scanned, changed);
            AssertClose(first.PosteriorMean, second.PosteriorMean, 1e-12);
        }
    }

    [Fact]
    public void SourceDimensionsUseFullCostRangeWhileManagedEvidenceSaturatesItsLegacyIntField()
    {
        var selector = DiffStrategySelector.WithDefaults();

        var strategy = selector.Select(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);
        var evidence = Assert.IsType<StrategyEvidence>(selector.LastEvidence);

        Assert.True(Enum.IsDefined(strategy));
        Assert.True(double.IsFinite(evidence.CostFull));
        Assert.True(double.IsFinite(evidence.CostDirty));
        Assert.True(double.IsFinite(evidence.CostRedraw));
        Assert.Equal(int.MaxValue, evidence.TotalCells);
        AssertClose(6.0 * ushort.MaxValue * (double)ushort.MaxValue, evidence.CostRedraw);
    }

    [Fact]
    public void ExistingManagedRuntimeWrapperAndLedgerRemainCompatible()
    {
        var selector = DiffStrategySelector.WithDefaults();

        var resize = selector.Select(20, 4, 4, resized: true, TimeSpan.Zero);
        var degraded = selector.Select(20, 4, 1, resized: false, TimeSpan.FromMilliseconds(10));
        var burst = selector.Select(20, 4, 2, resized: false, TimeSpan.Zero);
        var stable = selector.Select(20, 4, 1, resized: false, TimeSpan.Zero);

        Assert.Equal((DiffRegime.ResizeRegime, DiffStrategy.FullRedraw), (resize.Regime, resize.Strategy));
        Assert.Equal((DiffRegime.DegradedTerminal, DiffStrategy.SignificantDirtyRows), (degraded.Regime, degraded.Strategy));
        Assert.Equal((DiffRegime.BurstyChange, DiffStrategy.Full), (burst.Regime, burst.Strategy));
        Assert.Equal((DiffRegime.StableFrame, DiffStrategy.DirtyRows), (stable.Regime, stable.Strategy));
        Assert.Equal(4, selector.Ledger.Decisions.Count);
        Assert.Equal(4, selector.Ledger.Transitions.Count);
    }

    [Fact]
    public void SourceEnumDisplayAndManagedExtensionRemainDistinct()
    {
        Assert.Equal("Full", DiffStrategy.Full.ToString());
        Assert.Equal("DirtyRows", DiffStrategy.DirtyRows.ToString());
        Assert.Equal("FullRedraw", DiffStrategy.FullRedraw.ToString());
        Assert.Equal("SignificantDirtyRows", DiffStrategy.SignificantDirtyRows.ToString());
        Assert.Equal(4, Enum.GetValues<DiffStrategy>().Distinct().Count());
    }

    private static void AssertClose(double expected, double actual, double tolerance = 1e-9) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
