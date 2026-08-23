// SPDX-License-Identifier: Apache-2.0
// Consolidated tests adapted from crates/ftui-render/src/fit_metrics.rs at
// upstream basis 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class FitMetricsTests
{
    [Fact]
    public void CellMetricDefaultsPresetsAndDisplayMatchSource()
    {
        var metrics = CellMetrics.Default;
        Assert.Equal(8u, metrics.WidthPixels);
        Assert.Equal(16u, metrics.HeightPixels);
        Assert.Equal("8x16px (8.00x16.00 sub-px)", metrics.ToString());
        Assert.Equal(10u, CellMetrics.Large.WidthPixels);
        Assert.Equal(20u, CellMetrics.Large.HeightPixels);
        Assert.Equal(metrics, new CellMetrics());
    }

    [Fact]
    public void CellMetricsConvertFractionalPixelsDeterministically()
    {
        var metrics = Assert.IsType<CellMetrics>(CellMetrics.FromPixels(8.5, 16.75));

        Assert.Equal(2176u, metrics.WidthSubpixels);
        Assert.Equal(4288u, metrics.HeightSubpixels);
        Assert.Equal(8u, metrics.WidthPixels);
        Assert.Equal(16u, metrics.HeightPixels);
    }

    [Fact]
    public void CellMetricsRejectInvalidInputs()
    {
        Assert.Null(CellMetrics.New(0, 256));
        Assert.Null(CellMetrics.New(256, 0));
        Assert.Null(CellMetrics.FromPixels(-1, 16));
        Assert.Null(CellMetrics.FromPixels(8, double.NaN));
        Assert.Null(CellMetrics.FromPixels(8, double.PositiveInfinity));
    }

    [Theory]
    [InlineData(0.0, 0u)]
    [InlineData(0.5, 128u)]
    [InlineData(1.0, 256u)]
    [InlineData(2.0, 512u)]
    [InlineData(1.001953125, 257u)] // exactly 256.5, Rust round() is away from zero
    public void SubpixelConversionMatchesPinnedRounding(double pixels, uint expected) =>
        Assert.Equal(expected, FitMetrics.PixelsToSubpixels(pixels));

    [Fact]
    public void SubpixelConversionRejectsNonFiniteNegativeAndOverflow()
    {
        Assert.Null(FitMetrics.PixelsToSubpixels(-1));
        Assert.Null(FitMetrics.PixelsToSubpixels(double.NaN));
        Assert.Null(FitMetrics.PixelsToSubpixels(double.PositiveInfinity));
        Assert.Null(FitMetrics.PixelsToSubpixels((uint.MaxValue / 256d) + 1));
    }

    [Fact]
    public void ViewportSimpleAndDisplayMatchSource()
    {
        var viewport = Assert.IsType<ContainerViewport>(ContainerViewport.Simple(800, 600));

        Assert.Equal(800u, viewport.WidthPixels);
        Assert.Equal(600u, viewport.HeightPixels);
        Assert.Equal(256u, viewport.DprSubpixels);
        Assert.Equal(256u, viewport.ZoomSubpixels);
        Assert.Equal(800u * 256, viewport.EffectiveWidthSubpixels);
        Assert.Equal(600u * 256, viewport.EffectiveHeightSubpixels);
        Assert.Contains("800x600px @1.00x DPR, 100% zoom", viewport.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ViewportAppliesDprAndZoomWithIntegerArithmetic()
    {
        var retina = Assert.IsType<ContainerViewport>(ContainerViewport.New(1600, 1200, 2, 1));
        Assert.Equal(800u * 256, retina.EffectiveWidthSubpixels);
        Assert.Equal(600u * 256, retina.EffectiveHeightSubpixels);

        var zoomed = Assert.IsType<ContainerViewport>(ContainerViewport.New(800, 600, 1, 1.5));
        Assert.Equal(136_533u, zoomed.EffectiveWidthSubpixels);
    }

    [Fact]
    public void ViewportRejectsZeroDimensionsAndScale()
    {
        Assert.Null(ContainerViewport.Simple(0, 600));
        Assert.Null(ContainerViewport.Simple(800, 0));
        Assert.Null(ContainerViewport.New(800, 600, 0, 1));
        Assert.Null(ContainerViewport.New(800, 600, 1, 0));
    }

    [Fact]
    public void AutomaticFitProducesClassic80By24Grid()
    {
        var viewport = ContainerViewport.Simple(640, 384)!.Value;
        var outcome = FitMetrics.FitToContainer(viewport, CellMetrics.MonospaceDefault, FitPolicy.Default);
        var result = Assert.IsType<FitResult>(outcome.Result);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(new FitResult(80, 24, 0, 0), result);
        Assert.True(result.IsValid);
        Assert.Equal("80x24 cells", result.ToString());
    }

    [Fact]
    public void AutomaticFitReportsSubpixelRemaindersAndClampsSmallContainer()
    {
        var remainder = FitMetrics.FitToContainer(
            ContainerViewport.Simple(645, 390)!.Value,
            CellMetrics.MonospaceDefault,
            FitPolicy.Default).Result!.Value;
        Assert.Equal(80, remainder.Columns);
        Assert.Equal(24, remainder.Rows);
        Assert.Equal(5u * 256, remainder.PaddingRightSubpixels);
        Assert.Equal(6u * 256, remainder.PaddingBottomSubpixels);

        var small = FitMetrics.FitToContainer(
            ContainerViewport.Simple(4, 8)!.Value,
            CellMetrics.MonospaceDefault,
            FitPolicy.Default).Result!.Value;
        Assert.Equal((1, 1), (small.Columns, small.Rows));
        Assert.Equal((0u, 0u), (small.PaddingRightSubpixels, small.PaddingBottomSubpixels));
    }

    [Fact]
    public void FixedPolicyIgnoresContainerAndMayRepresentInvalidGrid()
    {
        var viewport = ContainerViewport.Simple(100, 100)!.Value;
        var fixedResult = FitMetrics.FitToContainer(
            viewport, CellMetrics.MonospaceDefault, new FitPolicy.Fixed(80, 24)).Result!.Value;
        Assert.Equal((80, 24), (fixedResult.Columns, fixedResult.Rows));

        var empty = FitMetrics.FitToContainer(
            viewport, CellMetrics.MonospaceDefault, new FitPolicy.Fixed(0, 0)).Result!.Value;
        Assert.False(empty.IsValid);
    }

    [Fact]
    public void MinimumPolicyUsesGreaterOfActualAndMinimum()
    {
        var minimum = new FitPolicy.FitWithMinimum(10, 5);
        var small = FitMetrics.FitToContainer(
            ContainerViewport.Simple(40, 48)!.Value,
            CellMetrics.MonospaceDefault,
            minimum).Result!.Value;
        Assert.Equal((10, 5), (small.Columns, small.Rows));

        var large = FitMetrics.FitToContainer(
            ContainerViewport.Simple(800, 600)!.Value,
            CellMetrics.MonospaceDefault,
            minimum).Result!.Value;
        Assert.Equal((100, 37), (large.Columns, large.Rows));
    }

    [Theory]
    [InlineData(2.0, 1600u, 768u, 100, 24)]
    [InlineData(3.0, 2400u, 1152u, 100, 24)]
    public void FitAccountsForDevicePixelRatio(
        double dpr, uint width, uint height, ushort columns, ushort rows)
    {
        var result = FitMetrics.FitToContainer(
            ContainerViewport.New(width, height, dpr, 1)!.Value,
            CellMetrics.MonospaceDefault,
            FitPolicy.Default).Result!.Value;
        Assert.Equal((columns, rows), (result.Columns, result.Rows));
    }

    [Fact]
    public void FitIsDeterministicAndDetectsDimensionOverflow()
    {
        var viewport = ContainerViewport.Simple(800, 600)!.Value;
        var first = FitMetrics.FitToContainer(viewport, CellMetrics.MonospaceDefault, FitPolicy.Default);
        var second = FitMetrics.FitToContainer(viewport, CellMetrics.MonospaceDefault, FitPolicy.Default);
        Assert.Equal(first, second);

        var overflow = FitMetrics.FitToContainer(
            ContainerViewport.Simple(ushort.MaxValue, 1)!.Value,
            new CellMetrics(1, 1),
            FitPolicy.Default);
        Assert.Equal(FitError.DimensionOverflow, overflow.Error);
        Assert.False(overflow.IsSuccess);
    }

    [Fact]
    public void FitErrorsHaveStableDisplayVocabulary()
    {
        Assert.Equal("container too small to fit any cells", FitError.ContainerTooSmall.ToDisplayString());
        Assert.Equal("computed grid dimensions overflow u16", FitError.DimensionOverflow.ToDisplayString());
    }

    [Fact]
    public void GenerationStartsAtZeroOrdersAndSaturates()
    {
        var zero = MetricGeneration.Zero;
        var two = zero.Next.Next;
        Assert.Equal(0UL, zero.Value);
        Assert.Equal(2UL, two.Value);
        Assert.True(two > zero);
        Assert.Equal("gen:2", two.ToString());
        Assert.Equal(ulong.MaxValue, new MetricGeneration(ulong.MaxValue).Next.Value);
    }

    [Fact]
    public void InvalidationVocabularyAndRasterizationRequirementsMatchSource()
    {
        Assert.True(MetricInvalidation.FontLoaded.RequiresRasterization());
        Assert.True(MetricInvalidation.DprChanged.RequiresRasterization());
        Assert.True(MetricInvalidation.FontSizeChanged.RequiresRasterization());
        Assert.True(MetricInvalidation.FullReset.RequiresRasterization());
        Assert.False(MetricInvalidation.ZoomChanged.RequiresRasterization());
        Assert.False(MetricInvalidation.ContainerResized.RequiresRasterization());
        Assert.All(Enum.GetValues<MetricInvalidation>(), reason => Assert.True(reason.RequiresRefit()));
        Assert.Equal("font_loaded", MetricInvalidation.FontLoaded.ToDisplayString());
        Assert.Equal("dpr_changed", MetricInvalidation.DprChanged.ToDisplayString());
    }

    [Fact]
    public void LifecycleStartsWithoutPendingWork()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        Assert.Equal(MetricGeneration.Zero, lifecycle.Generation);
        Assert.False(lifecycle.IsPending);
        Assert.Null(lifecycle.LastFit);
        Assert.Equal(0UL, lifecycle.TotalInvalidations);
        Assert.Equal(0UL, lifecycle.TotalRefits);
    }

    [Fact]
    public void InvalidateBumpsGenerationAndCanReplaceMetrics()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        lifecycle.Invalidate(MetricInvalidation.FontSizeChanged, CellMetrics.Large);

        Assert.Equal(1UL, lifecycle.Generation.Value);
        Assert.True(lifecycle.IsPending);
        Assert.Equal(1UL, lifecycle.TotalInvalidations);
        Assert.Equal(CellMetrics.Large, lifecycle.CellMetrics);
        Assert.Equal(MetricInvalidation.FontSizeChanged, lifecycle.LastInvalidation);
    }

    [Fact]
    public void SetViewportMarksOnlyActualChangesPending()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        var viewport = ContainerViewport.Simple(800, 600)!.Value;
        lifecycle.SetViewport(viewport);
        var generation = lifecycle.Generation;
        Assert.True(lifecycle.IsPending);
        Assert.Equal([MetricInvalidation.ContainerResized], lifecycle.PendingInvalidations);

        lifecycle.SetViewport(viewport);
        Assert.Equal(generation, lifecycle.Generation);
    }

    [Fact]
    public void RefitWithoutViewportConsumesPendingAttempt()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        lifecycle.Invalidate(MetricInvalidation.FontLoaded);

        Assert.Null(lifecycle.Refit());
        Assert.False(lifecycle.IsPending);
        Assert.Empty(lifecycle.PendingInvalidations);
        Assert.Equal(1UL, lifecycle.TotalRefits);
    }

    [Fact]
    public void LifecycleRefitComputesAndDetectsGridChangesOnly()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        lifecycle.SetViewport(ContainerViewport.Simple(640, 384)!.Value);
        Assert.Equal((80, 24), Dimensions(lifecycle.Refit()));
        Assert.Equal(1UL, lifecycle.TotalRefits);

        lifecycle.SetViewport(ContainerViewport.Simple(800, 600)!.Value);
        Assert.Equal((100, 37), Dimensions(lifecycle.Refit()));

        lifecycle.SetPolicy(new FitPolicy.Fixed(100, 37));
        Assert.Null(lifecycle.Refit());
        Assert.Equal((100, 37), Dimensions(lifecycle.LastFit));
    }

    [Fact]
    public void LifecycleSnapshotCarriesCurrentEvidenceShape()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        lifecycle.SetViewport(ContainerViewport.Simple(640, 384)!.Value);
        lifecycle.Refit();

        var snapshot = lifecycle.Snapshot;
        Assert.Equal((80, 24), (snapshot.FitColumns, snapshot.FitRows));
        Assert.Equal((640u, 384u), (snapshot.ViewportWidthPixels, snapshot.ViewportHeightPixels));
        Assert.Equal((256u, 256u), (snapshot.DprSubpixels, snapshot.ZoomSubpixels));
        Assert.False(snapshot.PendingRefit);
        Assert.Equal(0, snapshot.PendingInvalidationCount);
    }

    [Fact]
    public void PendingInvalidationsAreDeduplicatedAndCanonical()
    {
        var lifecycle = ReadyLifecycle();
        lifecycle.Invalidate(MetricInvalidation.FontLoaded);
        lifecycle.Invalidate(MetricInvalidation.ZoomChanged);
        lifecycle.Invalidate(MetricInvalidation.FontLoaded);
        lifecycle.Invalidate(MetricInvalidation.FullReset);
        lifecycle.Invalidate(MetricInvalidation.ContainerResized);

        Assert.Equal(
            [
                MetricInvalidation.FullReset,
                MetricInvalidation.ZoomChanged,
                MetricInvalidation.FontLoaded,
                MetricInvalidation.ContainerResized,
            ],
            lifecycle.PendingInvalidations);
    }

    [Fact]
    public void ViewportChangeTracksDprZoomAndResizeTogether()
    {
        var lifecycle = ReadyLifecycle();
        lifecycle.SetViewport(ContainerViewport.New(1600, 1200, 2, 1.25)!.Value);

        Assert.Equal(MetricInvalidation.DprChanged, lifecycle.LastInvalidation);
        Assert.Equal(
            [
                MetricInvalidation.DprChanged,
                MetricInvalidation.ZoomChanged,
                MetricInvalidation.ContainerResized,
            ],
            lifecycle.PendingInvalidations);
    }

    [Fact]
    public void DelayedFontLoadUsesLatestMetrics()
    {
        var lifecycle = ReadyLifecycle();
        Assert.Equal((100, 37), Dimensions(lifecycle.LastFit));

        lifecycle.Invalidate(MetricInvalidation.FontSizeChanged, CellMetrics.FromPixels(9, 18));
        Assert.Equal((88, 33), Dimensions(lifecycle.Refit()));
        lifecycle.Invalidate(MetricInvalidation.FontLoaded, CellMetrics.Large);
        Assert.Equal((80, 30), Dimensions(lifecycle.Refit()));
        Assert.Equal(CellMetrics.Large, lifecycle.CellMetrics);
    }

    [Fact]
    public void FontSwapRaceCoalescesInCanonicalOrderUsingNewestMetrics()
    {
        var lifecycle = ReadyLifecycle();
        var fallback = CellMetrics.FromPixels(9, 18)!.Value;
        var swapped = CellMetrics.FromPixels(11, 22)!.Value;
        lifecycle.Invalidate(MetricInvalidation.FontLoaded, fallback);
        lifecycle.Invalidate(MetricInvalidation.FontLoaded, swapped);
        lifecycle.SetViewport(ContainerViewport.New(1600, 1200, 2, 1.25)!.Value);

        Assert.Equal(
            [
                MetricInvalidation.DprChanged,
                MetricInvalidation.ZoomChanged,
                MetricInvalidation.FontLoaded,
                MetricInvalidation.ContainerResized,
            ],
            lifecycle.PendingInvalidations);
        Assert.Equal((58, 21), Dimensions(lifecycle.Refit()));
        Assert.Equal(swapped, lifecycle.CellMetrics);
    }

    [Fact]
    public void DynamicFontEventStreamKeepsSnapshotAndFitSynchronized()
    {
        var lifecycle = ReadyLifecycle();
        lifecycle.Invalidate(MetricInvalidation.FontLoaded, CellMetrics.FromPixels(9, 18));
        lifecycle.SetViewport(ContainerViewport.New(1600, 1200, 2, 1.25)!.Value);
        lifecycle.Invalidate(MetricInvalidation.FontLoaded, CellMetrics.Large);

        var fit = lifecycle.Refit();
        Assert.Equal((64, 24), Dimensions(fit));
        Assert.Equal(Dimensions(fit), (lifecycle.Snapshot.FitColumns, lifecycle.Snapshot.FitRows));
        Assert.Equal(0, lifecycle.Snapshot.PendingInvalidationMask);
        Assert.Equal(0, lifecycle.Snapshot.PendingInvalidationCount);
    }

    [Fact]
    public void FontSizeAndDprChangesAffectFit()
    {
        var fontLifecycle = ReadyLifecycle();
        fontLifecycle.Invalidate(
            MetricInvalidation.FontSizeChanged,
            CellMetrics.New(16 * 256, 32 * 256));
        Assert.Equal((50, 18), Dimensions(fontLifecycle.Refit()));

        var dprLifecycle = ReadyLifecycle();
        dprLifecycle.SetViewport(ContainerViewport.New(800, 600, 2, 1)!.Value);
        Assert.Equal((50, 18), Dimensions(dprLifecycle.Refit()));
    }

    private static MetricLifecycle ReadyLifecycle()
    {
        var lifecycle = new MetricLifecycle(CellMetrics.Default, FitPolicy.Default);
        lifecycle.SetViewport(ContainerViewport.Simple(800, 600)!.Value);
        lifecycle.Refit();
        return lifecycle;
    }

    private static (ushort Columns, ushort Rows) Dimensions(FitResult? result) =>
        result is { } fit ? (fit.Columns, fit.Rows) : default;
}
