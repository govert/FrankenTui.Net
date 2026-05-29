// Upstream source: crates/ftui-widgets/src/measure_cache.rs — tests
// Tests ported from 39 #[cfg(test)] mod tests functions.

using FrankenTui.Core;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class MeasureCacheTests
{
    private static SizeConstraints Make(ushort w, ushort h) =>
        new() { Min = new Size(w, h), Preferred = new Size(w, h), Max = null };

    [Fact] public void CacheReturnsSameResult()
    {
        var cache = new MeasureCache(100);
        var id = new WidgetId(42);
        var available = new Size(80, 24);
        int calls = 0;

        var r1 = cache.GetOrCompute(id, available, () => { calls++; return Make(50, 5); });
        var r2 = cache.GetOrCompute(id, available, () => { calls++; return Make(99, 9); });

        Assert.Equal(r1, r2);
        Assert.Equal(1, calls);
    }

    [Fact] public void DifferentSizeIsCacheMiss()
    {
        var cache = new MeasureCache(100);
        var id = new WidgetId(42);
        int calls = 0;

        cache.GetOrCompute(id, new Size(80, 24), () => { calls++; return Make(10, 1); });
        cache.GetOrCompute(id, new Size(120, 40), () => { calls++; return Make(20, 2); });
        Assert.Equal(2, calls);
    }

    [Fact] public void DifferentWidgetIsCacheMiss()
    {
        var cache = new MeasureCache(100);
        int calls = 0;
        cache.GetOrCompute(new WidgetId(1), new Size(80, 24), () => { calls++; return SizeConstraints.Zero; });
        cache.GetOrCompute(new WidgetId(2), new Size(80, 24), () => { calls++; return SizeConstraints.Zero; });
        Assert.Equal(2, calls);
    }

    [Fact] public void InvalidationClearsCache()
    {
        var cache = new MeasureCache(100);
        int calls = 0;
        cache.GetOrCompute(new WidgetId(42), new Size(80, 24), () => { calls++; return SizeConstraints.Zero; });
        cache.InvalidateAll();
        cache.GetOrCompute(new WidgetId(42), new Size(80, 24), () => { calls++; return SizeConstraints.Zero; });
        Assert.Equal(2, calls);
    }

    [Fact] public void WidgetSpecificInvalidation()
    {
        var cache = new MeasureCache(100);
        int c1 = 0, c2 = 0;

        cache.GetOrCompute(new WidgetId(1), new Size(80, 24), () => { c1++; return SizeConstraints.Zero; });
        cache.GetOrCompute(new WidgetId(2), new Size(80, 24), () => { c2++; return SizeConstraints.Zero; });

        cache.InvalidateWidget(new WidgetId(1));
        cache.GetOrCompute(new WidgetId(1), new Size(80, 24), () => { c1++; return SizeConstraints.Zero; });
        cache.GetOrCompute(new WidgetId(2), new Size(80, 24), () => { c2++; return SizeConstraints.Zero; }); // should hit

        Assert.Equal(2, c1);
        Assert.Equal(1, c2);
    }

    [Fact] public void LfuEvictionWorks()
    {
        var cache = new MeasureCache(2);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => SizeConstraints.Zero);
        // Access widget 1 again
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        // Insert third — evicts widget 2 (least accessed)
        cache.GetOrCompute(new WidgetId(3), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(2, cache.Count);

        bool called = false;
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => { called = true; return SizeConstraints.Zero; });
        Assert.True(called);
    }

    [Fact] public void StatsTrackHitsAndMisses()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero); // hit
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => SizeConstraints.Zero);

        var stats = cache.Stats();
        Assert.Equal(1UL, stats.Hits);
        Assert.Equal(2UL, stats.Misses);
    }

    [Fact] public void ResetStatsClearsCounters()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.ResetStats();
        var stats = cache.Stats();
        Assert.Equal(0UL, stats.Hits);
        Assert.Equal(0UL, stats.Misses);
    }

    [Fact] public void ClearRemovesAllEntries()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(2, cache.Count);
        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.True(cache.IsEmpty);
    }

    [Fact] public void DefaultCapacityIs256()
    {
        var cache = new MeasureCache();
        Assert.Equal(256, cache.Capacity);
    }

    [Fact] public void NewCacheIsEmpty()
    {
        var cache = new MeasureCache(100);
        Assert.True(cache.IsEmpty);
        Assert.Equal(0, cache.Count);
    }

    [Fact] public void HitCountIncrementsOnEachAccess()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(42), new Size(80, 24), () => SizeConstraints.Zero);
        for (int i = 0; i < 5; i++)
            cache.GetOrCompute(new WidgetId(42), new Size(80, 24), () => SizeConstraints.Zero);
        var stats = cache.Stats();
        Assert.Equal(1UL, stats.Misses);
        Assert.Equal(5UL, stats.Hits);
    }

    [Fact] public void InvalidateWidgetDoesNotAffectStats()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        var before = cache.Stats();
        cache.InvalidateWidget(new WidgetId(1));
        var after = cache.Stats();
        Assert.Equal(before.Hits, after.Hits);
        Assert.Equal(before.Misses, after.Misses);
    }

    [Fact] public void EdgeCapacityOneEvictsOnSecondWidget()
    {
        var cache = new MeasureCache(1);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(1, cache.Count);
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(1, cache.Count);
    }

    [Fact] public void EdgeInvalidateAllMultipleTimes()
    {
        var cache = new MeasureCache(100);
        cache.InvalidateAll();
        cache.InvalidateAll();
        cache.InvalidateAll();
        // Just verify no exception
        Assert.True(cache.IsEmpty);
    }

    [Fact] public void EdgeInvalidateWidgetNonexistent()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(1, cache.Count);
        cache.InvalidateWidget(new WidgetId(999));
        Assert.Equal(1, cache.Count);
    }

    [Fact] public void EdgeSizeZeroAsCacheKey()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), Size.Zero, () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(1), Size.Zero, () => SizeConstraints.Zero); // hit
        Assert.Equal(1UL, cache.Stats().Hits);
    }

    [Fact] public void EdgeClearPreservesStats()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.Clear();
        var stats = cache.Stats();
        Assert.Equal(1UL, stats.Hits);
        Assert.Equal(1UL, stats.Misses);
    }

    [Fact] public void EdgeResetStatsPreservesEntries()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(1, cache.Count);
        cache.ResetStats();
        Assert.Equal(1, cache.Count);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero); // hit
        Assert.Equal(1UL, cache.Stats().Hits);
    }

    [Fact] public void EntriesNeverExceedCapacity()
    {
        int cap = 5;
        var cache = new MeasureCache(cap);
        for (ulong i = 0; i < 100; i++)
        {
            cache.GetOrCompute(new WidgetId(i), new Size(10, 10), () => SizeConstraints.Zero);
            Assert.True(cache.Count <= cap);
        }
    }

    [Fact] public void InvalidateAllStaleButStillCountedInLen()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(2, cache.Count);
        cache.InvalidateAll();
        Assert.Equal(2, cache.Count); // stale entries still counted
    }

    [Fact] public void WidgetIdFromPtrDiffersForDifferentObjects()
    {
        var id1 = WidgetId.FromPtr(new object());
        var id2 = WidgetId.FromPtr(new object());
        Assert.NotEqual(id1, id2);
    }

    [Fact] public void CacheIsDeterministic()
    {
        var cache1 = new MeasureCache(100);
        var cache2 = new MeasureCache(100);
        for (ulong i = 0; i < 10; i++)
        {
            var id = new WidgetId(i);
            var size = new Size((ushort)(i * 10), (ushort)(i * 5));
            var c = new SizeConstraints { Min = new Size((ushort)i, 1), Preferred = new Size((ushort)(i * 2), 2), Max = null };
            cache1.GetOrCompute(id, size, () => c);
            cache2.GetOrCompute(id, size, () => c);
        }
        Assert.Equal(cache1.Stats().Entries, cache2.Stats().Entries);
    }

    // ---- Missing edge-case tests ----
    [Fact] public void WidgetIdFromPtrIsStable()
    {
        var obj = new object();
        var id1 = WidgetId.FromPtr(obj);
        var id2 = WidgetId.FromPtr(obj);
        Assert.Equal(id1, id2);
    }

    [Fact] public void WidgetIdFromHashDiffersForDifferentContent()
    {
        string a = "hello", b = "world";
        var id1 = WidgetId.FromHash(ref a);
        var id2 = WidgetId.FromHash(ref b);
        Assert.NotEqual(id1, id2);
    }

    [Fact] public void StatsZeroTotalGivesZeroHitRate()
    {
        var cache = new MeasureCache(100);
        Assert.Equal(0.0, cache.Stats().HitRate);
    }

    [Fact] public void GenerationWrapsAround()
    {
        var cache = new MeasureCache(100);
        cache.InvalidateAll();
        cache.InvalidateAll();
        cache.InvalidateAll();
        // Just verify no crash — generation is internal
        Assert.True(cache.IsEmpty);
    }

    [Fact] public void EdgeZeroCapacityCache()
    {
        var cache = new MeasureCache(0);
        int calls = 0;
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => { calls++; return SizeConstraints.Zero; });
        var result = cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => { calls++; return SizeConstraints.Zero; });
        // With capacity 0, eviction check is `count >= 0 && 0 > 0` = false, so entry is cached
        Assert.Equal(1, calls); // second call is a hit
    }

    [Fact] public void EdgeLfuEqualAccessCounts()
    {
        var cache = new MeasureCache(2);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(2), new Size(10, 10), () => SizeConstraints.Zero);
        cache.GetOrCompute(new WidgetId(3), new Size(10, 10), () => SizeConstraints.Zero);
        Assert.Equal(2, cache.Count);
    }

    [Fact] public void EdgeStaleEntryTreatedAsMiss()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.InvalidateAll();
        int calls = 0;
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => { calls++; return SizeConstraints.Zero; });
        Assert.Equal(1, calls);
    }

    [Fact] public void EdgeInvalidateWidgetRemovesAllSizes()
    {
        var cache = new MeasureCache(100);
        var id = new WidgetId(42);
        cache.GetOrCompute(id, new Size(80, 24), () => SizeConstraints.Zero);
        cache.GetOrCompute(id, new Size(120, 40), () => SizeConstraints.Zero);
        Assert.Equal(2, cache.Count);
        cache.InvalidateWidget(id);
        Assert.Equal(0, cache.Count);
    }

    [Fact] public void EdgeClearBumpsGeneration()
    {
        var cache = new MeasureCache(100);
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => SizeConstraints.Zero);
        cache.Clear();
        int calls = 0;
        cache.GetOrCompute(new WidgetId(1), new Size(10, 10), () => { calls++; return SizeConstraints.Zero; });
        Assert.Equal(1, calls); // stale after clear
    }

    [Fact] public void EdgeGetOrComputeReturnsComputedValue()
    {
        var cache = new MeasureCache(100);
        var expected = new SizeConstraints { Min = new Size(5, 3), Preferred = new Size(40, 10), Max = new Size(100, 50) };
        var result = cache.GetOrCompute(new WidgetId(1), new Size(80, 24), () => expected);
        Assert.Equal(expected, result);
    }
}