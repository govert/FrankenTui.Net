// SPDX-License-Identifier: Apache-2.0
// Consolidated tests adapted from crates/ftui-render/src/spatial_hit_index.rs
// at upstream basis 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;
using FrankenTui.Render;
using RenderCacheStats = FrankenTui.Render.CacheStats;

namespace FrankenTui.Tests.Headless;

public sealed class SpatialHitIndexTests
{
    private static SpatialHitIndex Index() => SpatialHitIndex.WithDefaults(80, 24);

    [Fact]
    public void ConstructionCoversDefaultAndGridExtremes()
    {
        var defaults = Index();
        Assert.True(defaults.IsEmpty);
        Assert.Equal(8, defaults.Config.CellSize);
        Assert.Equal(64, defaults.Config.BucketWarnThreshold);
        Assert.False(defaults.Config.TrackCacheStats);
        Assert.Equal(10, defaults.GridWidth);
        Assert.Equal(3, defaults.GridHeight);

        var zeroCell = new SpatialHitIndex(80, 24, new SpatialHitConfig { CellSize = 0 });
        Assert.Equal(1, zeroCell.Config.CellSize);
        Assert.Equal(80, zeroCell.GridWidth);
        Assert.Equal(24, zeroCell.GridHeight);
        zeroCell.RegisterSimple(HitId.New(1), new Rect(0, 0, 1, 1), HitRegionKind.Content, 0);
        Assert.NotNull(zeroCell.HitTest(0, 0));

        var largeCell = new SpatialHitIndex(80, 24, new SpatialHitConfig { CellSize = 100 });
        Assert.Equal(1, largeCell.GridWidth);
        Assert.Equal(1, largeCell.GridHeight);

        var emptyScreen = SpatialHitIndex.WithDefaults(0, 0);
        Assert.Null(emptyScreen.HitTestReadonly(0, 0));
    }

    [Fact]
    public void MaximumDimensionsDoNotLoseRightEdgeHits()
    {
        var oneRow = SpatialHitIndex.WithDefaults(ushort.MaxValue, 8);
        oneRow.RegisterSimple(HitId.New(1), new Rect(65_520, 0, 15, 8), HitRegionKind.Button, 0);
        Assert.Equal(HitId.New(1), oneRow.HitTest(65_530, 5)?.Id);

        var threeRows = SpatialHitIndex.WithDefaults(ushort.MaxValue, 24);
        threeRows.RegisterSimple(HitId.New(2), new Rect(65_520, 0, 15, 8), HitRegionKind.Button, 0);
        Assert.Equal(HitId.New(2), threeRows.HitTest(65_530, 0)?.Id);
    }

    [Fact]
    public void BasicHitTestingUsesHalfOpenRectanglesAndScreenBounds()
    {
        var index = Index();
        index.RegisterSimple(HitId.New(1), new Rect(10, 10, 5, 5), HitRegionKind.Content, 42);

        Assert.Equal((HitId.New(1), HitRegionKind.Content, new HitData(42)), index.HitTest(10, 10));
        Assert.NotNull(index.HitTest(14, 14));
        Assert.Null(index.HitTest(15, 10));
        Assert.Null(index.HitTest(10, 15));
        Assert.Null(index.HitTest(80, 0));
        Assert.Null(index.HitTest(0, 24));
        Assert.Null(index.HitTestReadonly(ushort.MaxValue, ushort.MaxValue));
    }

    [Fact]
    public void ZeroIdIsRejectedAndZeroAreaRemainsRegisteredButUnhittable()
    {
        var index = Index();
        index.RegisterSimple(default, new Rect(0, 0, 10, 10), HitRegionKind.Button, 7);
        Assert.Equal(0, index.Count);

        index.RegisterSimple(HitId.New(1), new Rect(5, 5, 0, 10), HitRegionKind.Content, 0);
        index.RegisterSimple(HitId.New(2), new Rect(5, 5, 10, 0), HitRegionKind.Content, 0);
        Assert.Equal(2, index.Count);
        Assert.Null(index.HitTest(5, 5));
    }

    [Fact]
    public void RectanglesMayExtendPastScreenWithoutEscapingBounds()
    {
        var index = Index();
        index.RegisterSimple(HitId.New(1), new Rect(70, 20, 20, 10), HitRegionKind.Content, 0);

        Assert.NotNull(index.HitTest(75, 22));
        Assert.NotNull(index.HitTest(79, 23));
        Assert.Null(index.HitTest(80, 23));
    }

    [Fact]
    public void HigherZWinsThenLaterRegistrationBreaksTies()
    {
        var index = Index();
        index.Register(HitId.New(1), new Rect(0, 0, 20, 20), HitRegionKind.Content, 10, 10);
        index.Register(HitId.New(2), new Rect(0, 0, 20, 20), HitRegionKind.Border, 20, 5);
        Assert.Equal(HitId.New(1), index.HitTest(5, 5)?.Id);

        index.Register(HitId.New(3), new Rect(0, 0, 20, 20), HitRegionKind.Button, 30, 10);
        Assert.Equal(HitId.New(3), index.HitTest(5, 5)?.Id);
    }

    [Fact]
    public void ReregisteringAnIdReplacesInsteadOfLeavingGhosts()
    {
        var index = Index();
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 5, 5), HitRegionKind.Button, 7);
        index.RegisterSimple(HitId.New(1), new Rect(20, 10, 5, 5), HitRegionKind.Button, 9);

        Assert.Equal(1, index.Count);
        Assert.Null(index.HitTest(2, 2));
        Assert.Equal(new HitData(9), index.HitTest(22, 12)?.Data);
        Assert.True(index.Remove(HitId.New(1)));
        Assert.Null(index.HitTest(22, 12));
        Assert.Equal(0, index.Count);
    }

    [Fact]
    public void UpdateMovesShrinksAndCanDisableAnEntry()
    {
        var index = Index();
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 20, 20), HitRegionKind.Content, 0);
        Assert.True(index.Update(HitId.New(1), new Rect(50, 10, 10, 10)));
        Assert.Null(index.HitTest(5, 5));
        Assert.NotNull(index.HitTest(55, 15));

        Assert.True(index.Update(HitId.New(1), new Rect(50, 10, 2, 2)));
        Assert.Null(index.HitTest(55, 15));
        Assert.NotNull(index.HitTest(51, 11));

        Assert.True(index.Update(HitId.New(1), new Rect(0, 0, 0, 0)));
        Assert.Null(index.HitTest(0, 0));
        Assert.False(index.Update(HitId.New(999), new Rect(0, 0, 1, 1)));
    }

    [Fact]
    public void RemovalCompactsMiddleEntryAndIsIdempotent()
    {
        var index = Index();
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 5, 5), HitRegionKind.Content, 10);
        index.RegisterSimple(HitId.New(2), new Rect(10, 0, 5, 5), HitRegionKind.Content, 20);
        index.RegisterSimple(HitId.New(3), new Rect(20, 0, 5, 5), HitRegionKind.Content, 30);

        Assert.True(index.Remove(HitId.New(2)));
        Assert.False(index.Remove(HitId.New(2)));
        Assert.Equal(HitId.New(1), index.HitTest(2, 2)?.Id);
        Assert.Equal(HitId.New(3), index.HitTestReadonly(22, 2)?.Id);
        Assert.False(index.Remove(HitId.New(999)));
        Assert.Equal(1UL, index.Stats.Rebuilds);
    }

    [Fact]
    public void ClearIsIdempotentAndPreservesLegacyPointApiCompatibility()
    {
        var index = new SpatialHitIndex();
        index.Register(2, 3, "legacy");
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 5, 5), HitRegionKind.Button, 99);
        Assert.Equal("legacy", index.Hit(2, 3));

        index.Clear();
        index.Clear();

        Assert.Null(index.Hit(2, 3));
        Assert.True(index.IsEmpty);
        Assert.Null(index.HitTest(2, 2));
    }

    [Fact]
    public void CacheTracksOnePositionOnly()
    {
        var index = new SpatialHitIndex(80, 24, new SpatialHitConfig { TrackCacheStats = true });
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 40, 12), HitRegionKind.Content, 1);
        index.RegisterSimple(HitId.New(2), new Rect(40, 12, 40, 12), HitRegionKind.Border, 2);

        Assert.Equal(HitId.New(1), index.HitTest(5, 5)?.Id);
        Assert.Equal((0UL, 1UL), (index.Stats.Hits, index.Stats.Misses));
        index.HitTest(5, 5);
        Assert.Equal((1UL, 1UL), (index.Stats.Hits, index.Stats.Misses));
        index.HitTest(50, 15);
        index.HitTest(5, 5);
        Assert.Equal(3UL, index.Stats.Misses);
    }

    [Fact]
    public void ReadonlyQueriesDoNotAffectCacheOrStatistics()
    {
        var index = new SpatialHitIndex(80, 24, new SpatialHitConfig { TrackCacheStats = true });
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 10, 10), HitRegionKind.Content, 1);

        var readOnly = index.HitTestReadonly(5, 5);

        Assert.Equal(0UL, index.Stats.Hits);
        Assert.Equal(0UL, index.Stats.Misses);
        Assert.False(index.CacheValid);
        Assert.Equal(readOnly, index.HitTest(5, 5));
        Assert.Equal(1UL, index.Stats.Misses);
    }

    [Fact]
    public void RegisterAndInvalidationRespectCachedPosition()
    {
        var index = new SpatialHitIndex(80, 24, new SpatialHitConfig { TrackCacheStats = true });
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 10, 10), HitRegionKind.Content, 1);
        index.HitTest(5, 5);
        Assert.True(index.CacheValid);

        index.InvalidateRegion(new Rect(50, 20, 5, 4));
        Assert.True(index.CacheValid);
        index.InvalidateRegion(new Rect(5, 5, 0, 0));
        Assert.True(index.CacheValid);
        index.InvalidateRegion(new Rect(0, 0, 10, 10));
        Assert.False(index.CacheValid);

        index.HitTest(5, 5);
        index.RegisterSimple(HitId.New(2), new Rect(0, 0, 10, 10), HitRegionKind.Button, 2);
        Assert.False(index.CacheValid);
        Assert.Equal(HitId.New(2), index.HitTest(5, 5)?.Id);

        index.InvalidateAll();
        Assert.False(index.CacheValid);
    }

    [Fact]
    public void ResetStatsClearsHitsMissesAndRebuilds()
    {
        var index = new SpatialHitIndex(80, 24, new SpatialHitConfig { TrackCacheStats = true });
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 10, 10), HitRegionKind.Content, 0);
        index.HitTest(5, 5);
        index.HitTest(5, 5);
        index.Update(HitId.New(1), new Rect(10, 10, 5, 5));
        Assert.NotEqual(default, index.Stats);

        index.ResetStats();

        Assert.Equal(default, index.Stats);
    }

    [Fact]
    public void RandomLayoutMatchesNaiveTopmostScan()
    {
        var index = Index();
        (HitId Id, Rect Rect, ushort Z)[] widgets =
        [
            (HitId.New(1), new Rect(0, 0, 20, 10), 0),
            (HitId.New(2), new Rect(10, 5, 20, 10), 1),
            (HitId.New(3), new Rect(25, 0, 15, 15), 2),
        ];
        foreach (var widget in widgets)
        {
            index.Register(widget.Id, widget.Rect, HitRegionKind.Content, new HitData(widget.Id.Id), widget.Z);
        }

        for (ushort x = 0; x < 60; x++)
        {
            for (ushort y = 0; y < 20; y++)
            {
                var expected = widgets
                    .Where(widget => widget.Rect.Contains(x, y))
                    .OrderBy(widget => widget.Z)
                    .LastOrDefault().Id;
                Assert.Equal(expected == default ? null : expected, index.HitTestReadonly(x, y)?.Id);
            }
        }
    }

    [Fact]
    public void LargeRectAndCellBoundaryBucketingAreComplete()
    {
        var whole = Index();
        whole.RegisterSimple(HitId.New(1), new Rect(0, 0, 80, 24), HitRegionKind.Content, 0);
        Assert.NotNull(whole.HitTest(0, 0));
        Assert.NotNull(whole.HitTest(40, 12));
        Assert.NotNull(whole.HitTest(79, 23));

        var boundary = Index();
        boundary.RegisterSimple(HitId.New(1), new Rect(6, 6, 4, 4), HitRegionKind.Content, 0);
        Assert.NotNull(boundary.HitTest(7, 7));
        Assert.NotNull(boundary.HitTest(8, 8));
        Assert.NotNull(boundary.HitTest(9, 9));
        Assert.Null(boundary.HitTest(10, 10));
    }

    [Fact]
    public void HitEntryContainsAndEqualityMatchSourceBoundaries()
    {
        var entry = new HitEntry(
            HitId.New(1), new Rect(10, 10, 20, 20), HitRegionKind.Content, 0, 0, 0);
        Assert.True(entry.Contains(10, 10));
        Assert.True(entry.Contains(29, 29));
        Assert.False(entry.Contains(30, 30));
        Assert.Equal(entry, entry);
        Assert.NotEqual(entry, new HitEntry(
            HitId.New(2), entry.Rect, entry.Region, entry.Data, entry.ZOrder, 0));

        Assert.False(new HitEntry(
            HitId.New(1), new Rect(10, 10, 0, 5), HitRegionKind.Content, 0, 0, 0).Contains(10, 10));
        var saturated = new HitEntry(
            HitId.New(1), new Rect(65_530, 65_530, 10, 10), HitRegionKind.Content, 0, 0, 0);
        Assert.True(saturated.Contains(65_534, 65_534));
        Assert.False(saturated.Contains(ushort.MaxValue, ushort.MaxValue));
    }

    [Theory]
    [InlineData(0UL, 0UL, 0f)]
    [InlineData(75UL, 25UL, 75f)]
    [InlineData(100UL, 0UL, 100f)]
    [InlineData(0UL, 100UL, 0f)]
    public void CacheHitRateMatchesSource(ulong hits, ulong misses, float expected)
    {
        var stats = new RenderCacheStats(hits, misses, 0);
        Assert.Equal(expected, stats.HitRate, 3);
    }

    [Fact]
    public void ManyWidgetsAndAllManagedRegionVariantsRoundTrip()
    {
        var index = Index();
        var regions = Enum.GetValues<HitRegionKind>();
        for (var i = 0; i < regions.Length; i++)
        {
            var x = (ushort)(i * 5);
            index.RegisterSimple(HitId.New((uint)i + 1), new Rect(x, 0, 4, 4), regions[i], i);
        }

        for (var i = 0; i < regions.Length; i++)
        {
            var hit = index.HitTest((ushort)((i * 5) + 1), 1);
            Assert.Equal(regions[i], hit?.Region);
            Assert.Equal((ulong)i, hit?.Data.Value);
        }

        for (uint i = (uint)regions.Length; i < 100; i++)
        {
            index.RegisterSimple(HitId.New(i + 1), new Rect((ushort)(i % 80), 10, 1, 1), HitRegionKind.Content, i);
        }
        Assert.Equal(100, index.Count);
    }

    [Fact]
    public void MutableAndReadonlyQueriesAgreeAcrossGrid()
    {
        var index = Index();
        index.Register(HitId.New(1), new Rect(0, 0, 40, 12), HitRegionKind.Content, 1, 0);
        index.Register(HitId.New(2), new Rect(30, 8, 20, 10), HitRegionKind.Border, 2, 1);
        index.Register(HitId.New(3), new Rect(60, 0, 20, 24), HitRegionKind.Button, 3, 2);

        for (ushort x = 0; x < 80; x += 5)
        {
            for (ushort y = 0; y < 24; y += 3)
            {
                Assert.Equal(index.HitTestReadonly(x, y), index.HitTest(x, y));
            }
        }
    }

    [Fact]
    public void SingleCellScreenRoutesOnlyItsOneCell()
    {
        var index = SpatialHitIndex.WithDefaults(1, 1);
        index.RegisterSimple(HitId.New(1), new Rect(0, 0, 1, 1), HitRegionKind.Content, 0);

        Assert.NotNull(index.HitTest(0, 0));
        Assert.Null(index.HitTest(1, 0));
    }
}
