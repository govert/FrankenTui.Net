// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/cache.rs,
// tests/layout_cache_ratio.rs, tests/coherence_bug.rs, and the cache-routed
// property_temporal_tests module in src/lib.rs.
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Closed source denominator: 49 tests = 43 cache.rs inline + 2 integration +
// 4 routed temporal tests. Those tests are represented one-for-one below.
// Additional managed contract tests verify exact observable Rust hashes, defensive
// ownership, legacy surface retention, Clone, and S3-FIFO scan resistance.

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class LayoutCacheTests
{
    // cache.rs inline 01/43
    [Fact]
    public void SameParamsProduceSameKey() =>
        Assert.Equal(MakeKey(80, 24), MakeKey(80, 24));

    // cache.rs inline 02/43
    [Fact]
    public void DifferentAreaDifferentKey() =>
        Assert.NotEqual(MakeKey(80, 24), MakeKey(120, 40));

    // cache.rs inline 03/43
    [Fact]
    public void DifferentConstraintsDifferentKey()
    {
        LayoutCacheKey first = LayoutCacheKey.New(
            Area(80, 24), [Constraint.Fixed(20)], Direction.Horizontal);
        LayoutCacheKey second = LayoutCacheKey.New(
            Area(80, 24), [Constraint.Fixed(30)], Direction.Horizontal);
        Assert.NotEqual(first, second);
    }

    // cache.rs inline 04/43
    [Fact]
    public void DifferentDirectionDifferentKey()
    {
        LayoutCacheKey first = LayoutCacheKey.New(
            Area(80, 24), [Constraint.Fill], Direction.Horizontal);
        LayoutCacheKey second = LayoutCacheKey.New(
            Area(80, 24), [Constraint.Fill], Direction.Vertical);
        Assert.NotEqual(first, second);
    }

    // cache.rs inline 05/43
    [Fact]
    public void DifferentIntrinsicsDifferentKey()
    {
        LayoutSizeHint[] firstHints = [new(10, 20, null)];
        LayoutSizeHint[] secondHints = [new(10, 30, null)];
        LayoutCacheKey first = LayoutCacheKey.New(
            Area(80, 24), [Constraint.FitContent], Direction.Horizontal, firstHints);
        LayoutCacheKey second = LayoutCacheKey.New(
            Area(80, 24), [Constraint.FitContent], Direction.Horizontal, secondHints);
        Assert.NotEqual(first, second);
    }

    // cache.rs inline 06/43
    [Fact]
    public void CacheReturnsSameResult()
    {
        var cache = new LayoutCache(100);
        LayoutCacheKey key = MakeKey(80, 24);
        int computes = 0;

        IReadOnlyList<Rect> first = cache.GetOrCompute(key, () =>
        {
            computes++;
            return [new Rect(0, 0, 40, 24), new Rect(40, 0, 40, 24)];
        });
        IReadOnlyList<Rect> second = cache.GetOrCompute(
            key,
            () => throw new InvalidOperationException("A cache hit must not recompute."));

        Assert.Equal(first, second);
        Assert.Equal(1, computes);
    }

    // cache.rs inline 07/43
    [Fact]
    public void DifferentAreaIsCacheMiss()
    {
        var cache = new LayoutCache(100);
        int computes = 0;
        IReadOnlyList<Rect> Compute()
        {
            computes++;
            return [default];
        }

        cache.GetOrCompute(MakeKey(80, 24), Compute);
        cache.GetOrCompute(MakeKey(120, 40), Compute);
        Assert.Equal(2, computes);
    }

    // cache.rs inline 08/43
    [Fact]
    public void InvalidationClearsCache()
    {
        var cache = new LayoutCache(100);
        LayoutCacheKey key = MakeKey(80, 24);
        int computes = 0;
        IReadOnlyList<Rect> Compute()
        {
            computes++;
            return [];
        }

        cache.GetOrCompute(key, Compute);
        cache.InvalidateAll();
        cache.GetOrCompute(key, Compute);
        Assert.Equal(2, computes);
    }

    // cache.rs inline 09/43
    [Fact]
    public void LruEvictionWorks()
    {
        var cache = new LayoutCache(2);
        LayoutCacheKey first = MakeKey(10, 10);
        LayoutCacheKey second = MakeKey(20, 20);
        LayoutCacheKey third = MakeKey(30, 30);

        cache.GetOrCompute(first, () => [new Rect(0, 0, 10, 10)]);
        cache.GetOrCompute(second, () => [new Rect(0, 0, 20, 20)]);
        cache.GetOrCompute(first, NeverCompute);
        cache.GetOrCompute(third, () => [new Rect(0, 0, 30, 30)]);

        Assert.Equal(2, cache.Len());
        bool secondWasComputed = false;
        cache.GetOrCompute(second, () =>
        {
            secondWasComputed = true;
            return [];
        });
        Assert.True(secondWasComputed);
        cache.GetOrCompute(first, NeverCompute);
    }

    // cache.rs inline 10/43
    [Fact]
    public void StatsTrackHitsAndMisses()
    {
        var cache = new LayoutCache(100);
        LayoutCacheKey first = MakeKey(80, 24);
        LayoutCacheKey second = MakeKey(120, 40);

        cache.GetOrCompute(first, EmptyRects);
        cache.GetOrCompute(first, NeverCompute);
        cache.GetOrCompute(second, EmptyRects);

        LayoutCacheStats stats = cache.Stats();
        Assert.Equal(1UL, stats.Hits);
        Assert.Equal(2UL, stats.Misses);
        Assert.InRange(Math.Abs(stats.HitRate - 1.0 / 3.0), 0.0, 0.01);
    }

    // cache.rs inline 11/43
    [Fact]
    public void ResetStatsClearsCounters()
    {
        var cache = new LayoutCache(100);
        LayoutCacheKey key = MakeKey(80, 24);
        cache.GetOrCompute(key, EmptyRects);
        cache.GetOrCompute(key, NeverCompute);
        Assert.Equal((1UL, 1UL), (cache.Stats().Hits, cache.Stats().Misses));

        cache.ResetStats();
        LayoutCacheStats reset = cache.Stats();
        Assert.Equal(0UL, reset.Hits);
        Assert.Equal(0UL, reset.Misses);
        Assert.Equal(0.0, reset.HitRate);
    }

    // cache.rs inline 12/43
    [Fact]
    public void ClearRemovesAllEntries()
    {
        var cache = new LayoutCache(100);
        cache.GetOrCompute(MakeKey(80, 24), EmptyRects);
        cache.GetOrCompute(MakeKey(120, 40), EmptyRects);
        Assert.Equal(2, cache.Len());

        cache.Clear();
        Assert.Equal(0, cache.Len());
        Assert.True(cache.IsEmpty());

        bool computed = false;
        cache.GetOrCompute(MakeKey(80, 24), () =>
        {
            computed = true;
            return [];
        });
        Assert.True(computed);
    }

    // cache.rs inline 13/43
    [Fact]
    public void DefaultCapacityIs64() => Assert.Equal(64, new LayoutCache().Capacity());

    // cache.rs inline 14/43
    [Fact]
    public void GenerationWrapsAround()
    {
        var cache = new LayoutCache(100) { Generation = ulong.MaxValue };
        cache.InvalidateAll();
        Assert.Equal(0UL, cache.Generation);
    }

    // cache.rs inline 15/43
    [Fact]
    public void ConstraintHashIsStable()
    {
        Constraint[] constraints =
        [
            Constraint.Fixed(20),
            Constraint.Percentage(50.0f),
            Constraint.Min(10),
        ];
        Assert.Equal(
            LayoutCacheKey.New(Area(1, 1), constraints, Direction.Horizontal).ConstraintsHash,
            LayoutCacheKey.New(Area(2, 2), constraints, Direction.Horizontal).ConstraintsHash);
    }

    // cache.rs inline 16/43
    [Fact]
    public void DifferentConstraintValuesDifferentHash()
    {
        ulong first = LayoutCacheKey.New(
            Area(1, 1), [Constraint.Fixed(20)], Direction.Horizontal).ConstraintsHash;
        ulong second = LayoutCacheKey.New(
            Area(1, 1), [Constraint.Fixed(30)], Direction.Horizontal).ConstraintsHash;
        Assert.NotEqual(first, second);
    }

    // cache.rs inline 17/43
    [Fact]
    public void DifferentConstraintTypesDifferentHash()
    {
        ulong first = LayoutCacheKey.New(
            Area(1, 1), [Constraint.Fixed(20)], Direction.Horizontal).ConstraintsHash;
        ulong second = LayoutCacheKey.New(
            Area(1, 1), [Constraint.Min(20)], Direction.Horizontal).ConstraintsHash;
        Assert.NotEqual(first, second);
    }

    // cache.rs inline 18/43
    [Fact]
    public void FitContentBoundedValuesInHash()
    {
        ulong first = LayoutCacheKey.New(
            Area(1, 1), [Constraint.FitContentBounded(10, 50)], Direction.Horizontal).ConstraintsHash;
        ulong second = LayoutCacheKey.New(
            Area(1, 1), [Constraint.FitContentBounded(10, 60)], Direction.Horizontal).ConstraintsHash;
        Assert.NotEqual(first, second);
    }

    // cache.rs inline 19/43
    [Fact]
    public void IntrinsicsHashIsStable()
    {
        LayoutSizeHint[] hints = [new(10, 20, 30), new(5, 15, null)];
        LayoutCacheKey first = LayoutCacheKey.New(
            Area(1, 1), [Constraint.FitContent], Direction.Horizontal, hints);
        LayoutCacheKey second = LayoutCacheKey.New(
            Area(2, 2), [Constraint.FitContent], Direction.Horizontal, hints);
        Assert.Equal(first.IntrinsicsHash, second.IntrinsicsHash);
        Assert.Equal(first.IntrinsicsHashFx, second.IntrinsicsHashFx);
    }

    // cache.rs inline 20/43
    [Fact]
    public void DifferentIntrinsicsDifferentHash()
    {
        LayoutCacheKey first = LayoutCacheKey.New(
            Area(1, 1),
            [Constraint.FitContent],
            Direction.Horizontal,
            [new LayoutSizeHint(10, 20, null)]);
        LayoutCacheKey second = LayoutCacheKey.New(
            Area(1, 1),
            [Constraint.FitContent],
            Direction.Horizontal,
            [new LayoutSizeHint(10, 25, null)]);
        Assert.NotEqual(first.IntrinsicsHash, second.IntrinsicsHash);
        Assert.NotEqual(first.IntrinsicsHashFx, second.IntrinsicsHashFx);
    }

    // cache.rs inline 21/43
    [Fact]
    public void CacheIsDeterministic()
    {
        var first = new LayoutCache(100);
        var second = new LayoutCache(100);
        for (ushort index = 0; index < 10; index++)
        {
            LayoutCacheKey key = MakeKey((ushort)(index * 10), (ushort)(index * 5));
            Rect[] result = [new Rect(0, 0, index, index)];
            first.GetOrCompute(key, () => result);
            second.GetOrCompute(key, () => result);
        }

        Assert.Equal(first.Stats().Entries, second.Stats().Entries);
        Assert.Equal(first.Stats().Misses, second.Stats().Misses);
    }

    // cache.rs inline 22/43
    [Fact]
    public void HitCountIncrementsOnEachAccess()
    {
        var cache = new LayoutCache(100);
        LayoutCacheKey key = MakeKey(80, 24);
        cache.GetOrCompute(key, EmptyRects);
        for (int index = 0; index < 5; index++)
            cache.GetOrCompute(key, NeverCompute);

        Assert.Equal(1UL, cache.Stats().Misses);
        Assert.Equal(5UL, cache.Stats().Hits);
    }

    // cache.rs inline 23/43
    [Fact]
    public void CoherenceStoreAndGet()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        Assert.Null(cache.Get(id));
        cache.Store(id, [30, 50]);
        Assert.Equal([30, 50], cache.Get(id));
    }

    // cache.rs inline 24/43
    [Fact]
    public void CoherenceUpdateReplacesAllocation()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        cache.Store(id, [30, 50]);
        cache.Store(id, [31, 49]);
        Assert.Equal([31, 49], cache.Get(id));
        Assert.Equal(1, cache.Len());
    }

    // cache.rs inline 25/43
    [Fact]
    public void CoherenceDifferentIdsAreSeparate()
    {
        var cache = new CoherenceCache(64);
        CoherenceId first = MakeCoherenceId(1);
        CoherenceId second = MakeCoherenceId(2);
        cache.Store(first, [40, 40]);
        cache.Store(second, [30, 50]);
        Assert.Equal([40, 40], cache.Get(first));
        Assert.Equal([30, 50], cache.Get(second));
    }

    // cache.rs inline 26/43
    [Fact]
    public void CoherenceEvictionAtCapacity()
    {
        var cache = new CoherenceCache(2);
        CoherenceId first = MakeCoherenceId(1);
        CoherenceId second = MakeCoherenceId(2);
        CoherenceId third = MakeCoherenceId(3);
        cache.Store(first, [10]);
        cache.Store(second, [20]);
        cache.Store(third, [30]);

        Assert.Equal(2, cache.Len());
        Assert.Null(cache.Get(first));
        Assert.Equal([20], cache.Get(second));
        Assert.Equal([30], cache.Get(third));
    }

    // cache.rs inline 27/43
    [Fact]
    public void CoherenceClear()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        cache.Store(id, [10, 20]);
        Assert.Equal(1, cache.Len());
        cache.Clear();
        Assert.True(cache.IsEmpty());
        Assert.Null(cache.Get(id));
    }

    // cache.rs inline 28/43
    [Fact]
    public void CoherenceDisplacementWithPrevious()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        cache.Store(id, [30, 50]);
        Assert.Equal((4UL, 2U), cache.Displacement(id, [32, 48]));
    }

    // cache.rs inline 29/43
    [Fact]
    public void CoherenceDisplacementWithoutPrevious()
    {
        var cache = new CoherenceCache(64);
        Assert.Equal((0UL, 0U), cache.Displacement(MakeCoherenceId(1), [30, 50]));
    }

    // cache.rs inline 30/43
    [Fact]
    public void CoherenceDisplacementDifferentLengths()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        cache.Store(id, [30, 50]);
        Assert.Equal((10UL, 10U), cache.Displacement(id, [30, 50, 10]));
    }

    // cache.rs inline 31/43
    [Fact]
    public void CoherenceFromCacheKey()
    {
        CoherenceId first = CoherenceId.FromCacheKey(MakeKey(80, 24));
        CoherenceId second = CoherenceId.FromCacheKey(MakeKey(120, 40));
        Assert.Equal(first, second);
    }

    // cache.rs inline 32/43
    [Fact]
    public void UnitCacheReuseUnchangedConstraintsYieldIdenticalLayout()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        double[] targets = [26.67, 26.67, 26.66];
        IReadOnlyList<ushort> first = LayoutRounding.RoundLayoutStable(targets, 80, cache.Get(id));
        cache.Store(id, first);
        IReadOnlyList<ushort> second = LayoutRounding.RoundLayoutStable(targets, 80, cache.Get(id));
        Assert.Equal(first, second);
    }

    // cache.rs inline 33/43
    [Fact]
    public void EndToEndResizeSweepBoundedDisplacement()
    {
        var cache = new CoherenceCache(64);
        CoherenceId id = MakeCoherenceId(1);
        uint maximumEver = 0;
        ulong displacementSum = 0;
        const int steps = 61;

        for (ushort width = 60; width <= 120; width++)
        {
            double third = (double)width / 3.0;
            IReadOnlyList<ushort> allocation = LayoutRounding.RoundLayoutStable(
                [third, third, third],
                width,
                cache.Get(id));
            (ulong sum, uint maximum) = cache.Displacement(id, allocation);
            displacementSum += sum;
            maximumEver = Math.Max(maximumEver, maximum);
            cache.Store(id, allocation);
        }

        Assert.True(maximumEver <= 2, $"Maximum displacement was {maximumEver}.");
        Assert.True((double)displacementSum / steps < 3.0);
    }

    // cache.rs inline 34/43
    [Fact]
    public void EndToEndResizeSweepDeterministic()
    {
        static List<(ushort Width, ushort[] Allocation, ulong Sum, uint Max)> Sweep(ushort seed)
        {
            var cache = new CoherenceCache(64);
            CoherenceId id = CoherenceId.New(
                [Constraint.Percentage(30.0f), Constraint.Fill],
                Direction.Horizontal);
            var log = new List<(ushort, ushort[], ulong, uint)>();
            for (ushort width = (ushort)(40 + seed); width < 100 + seed; width++)
            {
                IReadOnlyList<ushort> allocation = LayoutRounding.RoundLayoutStable(
                    [width * 0.3, width * 0.7],
                    width,
                    cache.Get(id));
                (ulong sum, uint maximum) = cache.Displacement(id, allocation);
                cache.Store(id, allocation);
                log.Add((width, allocation.ToArray(), sum, maximum));
            }

            return log;
        }

        List<(ushort Width, ushort[] Allocation, ulong Sum, uint Max)> first = Sweep(0);
        List<(ushort Width, ushort[] Allocation, ulong Sum, uint Max)> second = Sweep(0);
        Assert.Equal(first.Count, second.Count);
        for (int index = 0; index < first.Count; index++)
        {
            Assert.Equal(first[index].Width, second[index].Width);
            Assert.Equal(first[index].Allocation, second[index].Allocation);
            Assert.Equal(first[index].Sum, second[index].Sum);
            Assert.Equal(first[index].Max, second[index].Max);
        }
    }

    // cache.rs inline 35/43
    [Fact]
    public void DefaultCoherenceCacheCapacityIs64() =>
        Assert.Equal(64, new CoherenceCache().Capacity());

    // cache.rs inline 36/43
    [Fact]
    public void S3FifoLayoutNewIsEmpty()
    {
        var cache = new S3FifoLayoutCache(64);
        Assert.True(cache.IsEmpty());
        Assert.Equal(0, cache.Len());
        Assert.Equal(64, cache.Capacity());
    }

    // cache.rs inline 37/43
    [Fact]
    public void S3FifoLayoutDefaultCapacity() =>
        Assert.Equal(64, new S3FifoLayoutCache().Capacity());

    // cache.rs inline 38/43
    [Fact]
    public void S3FifoLayoutGetOrComputeCaches()
    {
        var cache = new S3FifoLayoutCache(64);
        LayoutCacheKey key = S3Key(0, 80);
        IReadOnlyList<Rect> first = cache.GetOrCompute(key, () => [new Rect(0, 0, 40, 24)]);
        IReadOnlyList<Rect> second = cache.GetOrCompute(key, NeverCompute);
        Assert.Equal(first, second);
        Assert.Equal((1UL, 1UL), (cache.Stats().Hits, cache.Stats().Misses));
    }

    // cache.rs inline 39/43
    [Fact]
    public void S3FifoLayoutGenerationInvalidation()
    {
        var cache = new S3FifoLayoutCache(64);
        LayoutCacheKey key = S3Key(0, 80);
        cache.GetOrCompute(key, () => [new Rect(0, 0, 40, 24)]);
        cache.InvalidateAll();
        IReadOnlyList<Rect> result = cache.GetOrCompute(key, () => [new Rect(0, 0, 80, 24)]);
        Assert.Equal([new Rect(0, 0, 80, 24)], result);
        Assert.Equal(2UL, cache.Stats().Misses);
    }

    // cache.rs inline 40/43
    [Fact]
    public void S3FifoLayoutClear()
    {
        var cache = new S3FifoLayoutCache(64);
        cache.GetOrCompute(S3Key(0, 80), () => [new Rect(0, 0, 40, 24)]);
        cache.Clear();
        Assert.True(cache.IsEmpty());
    }

    // cache.rs inline 41/43
    [Fact]
    public void S3FifoLayoutDifferentKeys()
    {
        var cache = new S3FifoLayoutCache(64);
        cache.GetOrCompute(S3Key(0, 80), () => [new Rect(0, 0, 40, 24)]);
        cache.GetOrCompute(S3Key(0, 120), () => [new Rect(0, 0, 60, 24)]);
        Assert.Equal(2, cache.Len());
    }

    // cache.rs inline 42/43
    [Fact]
    public void S3FifoLayoutResetStats()
    {
        var cache = new S3FifoLayoutCache(64);
        LayoutCacheKey key = S3Key(0, 80);
        cache.GetOrCompute(key, EmptyRects);
        cache.GetOrCompute(key, EmptyRects);
        cache.ResetStats();
        Assert.Equal((0UL, 0UL), (cache.Stats().Hits, cache.Stats().Misses));
    }

    // cache.rs inline 43/43
    [Fact]
    public void S3FifoLayoutProducesSameResultsAsLru()
    {
        var lru = new LayoutCache(64);
        var s3 = new S3FifoLayoutCache(64);
        foreach (ushort width in new ushort[] { 80, 100, 120, 160, 200 })
        {
            LayoutCacheKey key = S3Key(0, width);
            Rect[] expected = [new Rect(0, 0, (ushort)(width / 2), 24)];
            Assert.Equal(
                lru.GetOrCompute(key, () => expected),
                s3.GetOrCompute(key, () => expected));
        }
    }

    // tests/layout_cache_ratio.rs 01/01
    [Fact]
    public void RatioCanonicalization()
    {
        LayoutCacheKey first = LayoutCacheKey.New(
            new Rect(0, 0, 100, 100),
            [Constraint.Ratio(1, 2)],
            Direction.Horizontal);
        LayoutCacheKey second = LayoutCacheKey.New(
            new Rect(0, 0, 100, 100),
            [Constraint.Ratio(2, 4)],
            Direction.Horizontal);
        Assert.Equal(first, second);
    }

    // tests/coherence_bug.rs 01/01
    [Fact]
    public void CoherenceCacheIndexingBug()
    {
        var cache = new CoherenceCache();
        Flex flex = Flex.Horizontal().Constraints([Constraint.Fixed(10), Constraint.Min(10)]);
        IReadOnlyList<Rect> rectangles = flex.SplitWithMeasurerStably(
            new Rect(0, 0, 100, 10),
            static (_, _) => LayoutSizeHint.Zero,
            cache);
        Assert.Equal((ushort)90, rectangles[1].Width);
    }

    // src/lib.rs property_temporal_tests 01/04
    [Fact]
    public void PropertyTemporalStabilitySmallResize()
    {
        Constraint[] constraints =
        [
            Constraint.Percentage(33.3f),
            Constraint.Percentage(33.3f),
            Constraint.Fill,
        ];
        var coherence = new CoherenceCache(64);
        CoherenceId id = CoherenceId.New(constraints, Direction.Horizontal);

        foreach (ushort total in new ushort[] { 80, 100, 120 })
        {
            IReadOnlyList<Rect> rects = Flex.Horizontal().Constraints(constraints)
                .Split(new Rect(0, 0, total, 10));
            double[] targets = rects.Select(static rect => (double)rect.Width).ToArray();
            IReadOnlyList<ushort>? previous = coherence.Get(id);
            IReadOnlyList<ushort> rounded = LayoutRounding.RoundLayoutStable(targets, total, previous);

            if (previous is not null)
            {
                (_, uint maximum) = coherence.Displacement(id, rounded);
                ushort previousTotal = SumU16(previous);
                Assert.True(maximum <= Math.Abs((int)total - previousTotal) + 1U);
            }

            coherence.Store(id, rounded);
        }
    }

    // src/lib.rs property_temporal_tests 02/04
    [Fact]
    public void PropertyTemporalStabilityRandomWalk()
    {
        Constraint[] constraints =
        [
            Constraint.Ratio(1, 3),
            Constraint.Ratio(1, 3),
            Constraint.Ratio(1, 3),
        ];
        CoherenceId id = CoherenceId.New(constraints, Direction.Horizontal);
        var coherence = new CoherenceCache(64);
        var random = new Lcg(0x5555_AAAA);
        ushort total = 90;

        for (int step = 0; step < 200; step++)
        {
            ushort previousTotal = total;
            int delta = (int)(random.NextUInt32() % 7) - 3;
            total = (ushort)Math.Clamp(total + delta, 10, 250);
            IReadOnlyList<Rect> rects = Flex.Horizontal().Constraints(constraints)
                .Split(new Rect(0, 0, total, 10));
            double[] targets = rects.Select(static rect => (double)rect.Width).ToArray();
            IReadOnlyList<ushort> rounded = LayoutRounding.RoundLayoutStable(
                targets,
                total,
                coherence.Get(id));

            if (coherence.Get(id) is not null)
            {
                (_, uint maximum) = coherence.Displacement(id, rounded);
                ushort sizeChange = (ushort)Math.Abs((int)total - previousTotal);
                Assert.True(
                    maximum <= sizeChange + 2U,
                    $"step {step}: max displacement {maximum} exceeded {sizeChange} + 2");
            }

            coherence.Store(id, rounded);
        }
    }

    // src/lib.rs property_temporal_tests 03/04
    [Fact]
    public void PropertyTemporalStabilityIdenticalFrames()
    {
        Constraint[] constraints =
        [
            Constraint.Fixed(20),
            Constraint.Fill,
            Constraint.Fixed(15),
        ];
        CoherenceId id = CoherenceId.New(constraints, Direction.Horizontal);
        var coherence = new CoherenceCache(64);
        ushort[] widths = Flex.Horizontal().Constraints(constraints)
            .Split(new Rect(0, 0, 100, 10))
            .Select(static rect => rect.Width)
            .ToArray();
        coherence.Store(id, widths);

        for (int index = 0; index < 10; index++)
        {
            double[] targets = widths.Select(static width => (double)width).ToArray();
            IReadOnlyList<ushort> rounded = LayoutRounding.RoundLayoutStable(
                targets,
                100,
                coherence.Get(id));
            Assert.Equal(0UL, coherence.Displacement(id, rounded).SumDisplacement);
            coherence.Store(id, rounded);
        }
    }

    // src/lib.rs property_temporal_tests 04/04
    [Fact]
    public void PropertyTemporalCoherenceSweep()
    {
        Constraint[] constraints =
        [
            Constraint.Percentage(25.0f),
            Constraint.Percentage(50.0f),
            Constraint.Fill,
        ];
        CoherenceId id = CoherenceId.New(constraints, Direction.Horizontal);
        var coherence = new CoherenceCache(64);
        ulong totalDisplacement = 0;

        for (ushort total = 60; total <= 140; total++)
        {
            IReadOnlyList<Rect> rects = Flex.Horizontal().Constraints(constraints)
                .Split(new Rect(0, 0, total, 10));
            double[] targets = rects.Select(static rect => (double)rect.Width).ToArray();
            IReadOnlyList<ushort> rounded = LayoutRounding.RoundLayoutStable(
                targets,
                total,
                coherence.Get(id));
            if (coherence.Get(id) is not null)
                totalDisplacement += coherence.Displacement(id, rounded).SumDisplacement;
            coherence.Store(id, rounded);
        }

        Assert.True(totalDisplacement <= 80UL * 3UL);
    }

    // Managed contract: the observable primary hashes equal Rust DefaultHasher probes.
    [Fact]
    public void CanonicalHashesMatchRustHashers()
    {
        LayoutCacheKey constraints = LayoutCacheKey.New(
            Area(1, 1),
            [Constraint.Fixed(10), Constraint.Percentage(50.0f), Constraint.Fill],
            Direction.Horizontal);
        LayoutCacheKey ratio = LayoutCacheKey.New(
            Area(1, 1),
            [Constraint.Ratio(1, 2)],
            Direction.Horizontal);
        LayoutCacheKey intrinsics = LayoutCacheKey.New(
            Area(1, 1),
            [Constraint.FitContent],
            Direction.Horizontal,
            [new LayoutSizeHint(1, 2, 3), new LayoutSizeHint(4, 5, null)]);

        Assert.Equal(16102665421519340506UL, constraints.ConstraintsHash);
        Assert.Equal(4982389713869121572UL, constraints.ConstraintsHashFx);
        Assert.Equal(16987548686916225972UL, ratio.ConstraintsHash);
        Assert.Equal(11863350927598522429UL, ratio.ConstraintsHashFx);
        Assert.Equal(17824750828774063358UL, intrinsics.IntrinsicsHash);
        Assert.Equal(13628170008425111548UL, intrinsics.IntrinsicsHashFx);
    }

    [Fact]
    public void CanonicalCachesReturnDefensiveResults()
    {
        LayoutCacheKey key = MakeKey(80, 24);
        var lru = new LayoutCache(4);
        IReadOnlyList<Rect> first = lru.GetOrCompute(key, () => [new Rect(1, 2, 3, 4)]);
        ((Rect[])first)[0] = default;
        Assert.Equal(new Rect(1, 2, 3, 4), lru.GetOrCompute(key, NeverCompute)[0]);

        var s3 = new S3FifoLayoutCache(4);
        IReadOnlyList<Rect> second = s3.GetOrCompute(key, () => [new Rect(5, 6, 7, 8)]);
        ((Rect[])second)[0] = default;
        Assert.Equal(new Rect(5, 6, 7, 8), s3.GetOrCompute(key, NeverCompute)[0]);
    }

    [Fact]
    public void CoherenceCloneAndReadsOwnIndependentSnapshots()
    {
        var original = new CoherenceCache(4);
        CoherenceId id = MakeCoherenceId(7);
        ushort[] input = [10, 20];
        original.Store(id, input);
        input[0] = 99;
        IReadOnlyList<ushort> read = original.Get(id)!;
        ((ushort[])read)[1] = 88;

        CoherenceCache clone = original.Clone();
        original.Store(id, [30, 40]);
        Assert.Equal([10, 20], clone.Get(id));
        Assert.Equal([30, 40], original.Get(id));
    }

    [Fact]
    public void LegacyLayoutCacheSurfaceRemainsCompatible()
    {
        var cache = new LayoutCache();
        Rect bounds = Area(10, 4);
        LayoutConstraint[] constraints = [LayoutConstraint.Fixed(3), LayoutConstraint.Fill()];
        LayoutCacheKey key = LayoutCacheKey.Create(bounds, LayoutDirection.Horizontal, constraints);
        var trace = new LayoutTrace(
            bounds,
            LayoutDirection.Horizontal,
            constraints,
            [3, 7],
            10,
            3,
            7,
            [new Rect(0, 0, 3, 4), new Rect(3, 0, 7, 4)],
            key.ToString(),
            false);

        cache.Set(trace);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet(bounds, LayoutDirection.Horizontal, constraints, out LayoutTrace found));
        Assert.Equal(trace, found);
        (Rect deconstructedBounds, LayoutDirection direction, string fingerprint) = key;
        Assert.Equal(bounds, deconstructedBounds);
        Assert.Equal(LayoutDirection.Horizontal, direction);
        Assert.Equal("Fixed:3|Fill:1", fingerprint);
        Assert.Equal("Horizontal:0,0,10x4:Fixed:3|Fill:1", key.ToString());
    }

    [Fact]
    public void S3FifoPromotesHotEntriesAcrossAColdScan()
    {
        var cache = new S3FifoLayoutCache(10);
        LayoutCacheKey hot = S3Key(0, 1);
        cache.GetOrCompute(hot, () => [new Rect(0, 0, 1, 1)]);
        cache.GetOrCompute(hot, NeverCompute);

        for (ushort width = 2; width <= 12; width++)
            cache.GetOrCompute(S3Key(0, width), () => [new Rect(0, 0, width, 1)]);

        cache.GetOrCompute(hot, NeverCompute);
    }

    private static LayoutCacheKey MakeKey(ushort width, ushort height) =>
        LayoutCacheKey.New(
            Area(width, height),
            [Constraint.Percentage(50.0f), Constraint.Fill],
            Direction.Horizontal);

    private static LayoutCacheKey S3Key(ushort x, ushort width) =>
        new(
            x,
            0,
            width,
            24,
            42,
            42,
            2,
            Direction.Horizontal,
            null,
            null);

    private static CoherenceId MakeCoherenceId(ushort value) =>
        CoherenceId.New([Constraint.Fixed(value), Constraint.Fill], Direction.Horizontal);

    private static Rect Area(ushort width, ushort height) => new(0, 0, width, height);

    private static IReadOnlyList<Rect> EmptyRects() => [];

    private static IReadOnlyList<Rect> NeverCompute() =>
        throw new InvalidOperationException("A cache hit must not recompute.");

    private static ushort SumU16(IEnumerable<ushort> values)
    {
        uint sum = 0;
        foreach (ushort value in values)
            sum += value;
        return unchecked((ushort)sum);
    }

    private sealed class Lcg(ulong state)
    {
        private ulong _state = state;

        public uint NextUInt32()
        {
            _state = unchecked(_state * 6_364_136_223_846_793_005UL + 1);
            return (uint)(_state >> 33);
        }
    }
}
