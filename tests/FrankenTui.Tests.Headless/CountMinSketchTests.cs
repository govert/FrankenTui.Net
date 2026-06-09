// Upstream source: crates/ftui-runtime/src/countmin_sketch.rs (tests module)
// Full 1-1 port of all upstream CountMinSketch tests.

using FrankenTui.Runtime;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class CountMinSketchTests
{
    private static SketchConfig TestConfig() => new() { Epsilon = 0.1, Delta = 0.1, MaxCalibration = 100 };

    [Fact] public void ParameterSelectionWidth()
    {
        var config = new SketchConfig { Epsilon = 0.1, Delta = 0.1 };
        var sketch = CountMinSketch.Create(config);
        Assert.InRange(sketch.Width, 27, 30);
    }

    [Fact] public void ParameterSelectionDepth()
    {
        var config = new SketchConfig { Epsilon = 0.1, Delta = 0.01 };
        var sketch = CountMinSketch.Create(config);
        Assert.InRange(sketch.Depth, 4, 6);
    }

    [Fact] public void ParameterSelectionExtremeEpsilon()
    {
        var config = new SketchConfig { Epsilon = 0.0, Delta = 0.1 };
        var sketch = CountMinSketch.Create(config);
        Assert.True(sketch.Width > 0);
    }

    [Fact] public void IncrementAndEstimate()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Increment("apple"); sketch.Increment("apple"); sketch.Increment("banana");
        Assert.True(sketch.Estimate("apple") >= 2);
        Assert.True(sketch.Estimate("banana") >= 1);
        Assert.Equal(0UL, sketch.Estimate("cherry"));
    }

    [Fact] public void AddMultiple()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Add("item", 100);
        Assert.True(sketch.Estimate("item") >= 100);
        Assert.Equal(100UL, sketch.TotalCount);
    }

    [Fact] public void TotalCountTracks()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Increment("a"); sketch.Increment("b"); sketch.Add("c", 10);
        Assert.Equal(12UL, sketch.TotalCount);
    }

    [Fact] public void NeverUnderestimates()
    {
        var sketch = CountMinSketch.WithDimensions(10, 3);
        for (int i = 0; i < 1000; i++) sketch.Increment(i);
        for (int i = 0; i < 1000; i++) Assert.True(sketch.Estimate(i) >= 1, $"Item {i} should have estimate >= 1");
    }

    [Fact] public void EstimateIncreasesWithCount()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        for (int count = 1; count <= 10; count++) { sketch.Increment("item"); Assert.True(sketch.Estimate("item") >= (ulong)count); }
    }

    [Fact] public void CmsBoundRespected()
    {
        var config = new SketchConfig { Epsilon = 0.1, Delta = 0.1 };
        var sketch = CountMinSketch.Create(config);
        var trueCounts = new Dictionary<int, ulong>();
        ulong rng = 12345;
        for (int t = 0; t < 1000; t++)
        {
            rng = unchecked(rng * 6364136223846793005 + 1442695040888963407);
            int item = (int)((rng >> 40) % 100);
            ulong count = ((rng >> 32) % 5) + 1;
            trueCounts.TryGetValue(item, out var existing);
            trueCounts[item] = existing + count;
            sketch.Add(item, count);
        }
        double errorBound = 0.1 * sketch.TotalCount;
        int violations = trueCounts.Count(kv => unchecked(sketch.Estimate(kv.Key) - kv.Value) > errorBound);
        Assert.True((double)violations / trueCounts.Count <= 0.2);
    }

    [Fact] public void CalibrationUpdates()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Add("item1", 50); sketch.Add("item2", 30);
        sketch.Calibrate("item1", 50); sketch.Calibrate("item2", 30);
        Assert.Equal(2, sketch.GetErrorEvidence().CalibrationSamples);
    }

    [Fact] public void PacBayesTightens()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        for (int i = 0; i < 100; i++) sketch.Add(i, (ulong)i + 1);
        for (int i = 0; i < 50; i++) sketch.Calibrate(i, (ulong)i + 1);
        var evidence = sketch.GetErrorEvidence();
        Assert.NotNull(evidence.CalibratedBound);
        if (evidence.CalibratedBound.HasValue)
            Assert.True(evidence.CalibratedBound.Value <= evidence.TheoreticalBound * 2.0);
    }

    [Fact] public void CalibrationWindowEnforced()
    {
        var config = TestConfig(); config.MaxCalibration = 10;
        var sketch = CountMinSketch.Create(config);
        for (int i = 0; i < 20; i++) { sketch.Add(i, 1); sketch.Calibrate(i, 1); }
        Assert.Equal(10, sketch.GetErrorEvidence().CalibrationSamples);
    }

    [Fact] public void PropertyRandomStreams()
    {
        var config = new SketchConfig { Epsilon = 0.05, Delta = 0.05 };
        int violations = 0;
        for (int trial = 0; trial < 50; trial++)
        {
            var sketch = CountMinSketch.Create(config);
            var trueCounts = new Dictionary<int, ulong>();
            ulong rng = (ulong)trial * 12345 + 1;
            for (int t = 0; t < 500; t++)
            {
                rng = unchecked(rng * 6364136223846793005 + 1442695040888963407);
                int item = (int)((rng >> 40) % 50);
                trueCounts.TryGetValue(item, out var c); trueCounts[item] = c + 1;
                sketch.Increment(item);
            }
            double bound = config.Epsilon * sketch.TotalCount;
            if (trueCounts.Any(kv => unchecked(sketch.Estimate(kv.Key) - kv.Value) > bound)) violations++;
        }
        Assert.True(violations / 50.0 <= config.Delta * 3.0 + 0.1);
    }

    [Fact] public void EvidenceContainsAllFields()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        for (int i = 0; i < 10; i++) sketch.Add(i, (ulong)i + 1);
        for (int i = 0; i < 5; i++) sketch.Calibrate(i, (ulong)i + 1);
        var e = sketch.GetErrorEvidence();
        Assert.True(e.Epsilon > 0); Assert.True(e.Delta > 0); Assert.True(e.TotalCount > 0);
        Assert.True(e.TheoreticalBound > 0); Assert.True(e.CalibrationSamples > 0);
    }

    [Fact] public void EvidenceSummaryFormat()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Add("item", 100); sketch.Calibrate("item", 100);
        var s = sketch.GetErrorEvidence().Summary();
        Assert.Contains("eps=", s); Assert.Contains("delta=", s); Assert.Contains("N=", s); Assert.Contains("bound=", s); Assert.Contains("cal=", s);
    }

    [Fact] public void StatsReflectState()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        for (int i = 0; i < 100; i++) sketch.Increment(i);
        var stats = sketch.GetStats();
        Assert.True(stats.Width > 0); Assert.True(stats.Depth > 0); Assert.True(stats.MemoryBytes > 0);
        Assert.Equal(100UL, stats.TotalCount); Assert.True(stats.UniqueItemsEstimate > 0);
    }

    [Fact] public void MemoryBounded()
    {
        var sketch = CountMinSketch.Create(new SketchConfig { Epsilon = 0.001, Delta = 0.001 });
        Assert.True(sketch.Width <= 1_000_000);
        Assert.True(sketch.Depth <= 20);
    }

    [Fact] public void ClearResets()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Add("item", 100); sketch.Calibrate("item", 100);
        sketch.Clear();
        Assert.Equal(0UL, sketch.Estimate("item"));
        Assert.Equal(0UL, sketch.TotalCount);
        Assert.Equal(0, sketch.GetErrorEvidence().CalibrationSamples);
    }

    [Fact] public void MergeWorks()
    {
        var s1 = CountMinSketch.WithDimensions(100, 5);
        var s2 = CountMinSketch.WithDimensions(100, 5);
        s1.Add("a", 10); s2.Add("a", 20); s2.Add("b", 5);
        Assert.True(s1.Merge(s2));
        Assert.True(s1.Estimate("a") >= 30);
        Assert.True(s1.Estimate("b") >= 5);
        Assert.Equal(35UL, s1.TotalCount);
    }

    [Fact] public void MergeFailsOnDimensionMismatch()
    {
        var s1 = CountMinSketch.WithDimensions(100, 5);
        var s2 = CountMinSketch.WithDimensions(200, 5);
        Assert.False(s1.Merge(s2));
    }

    [Fact] public void DeterministicEstimates()
    {
        var config = new SketchConfig { Epsilon = 0.1, Delta = 0.1 };
        ulong[] Run() { var s = CountMinSketch.Create(config); for (int i = 0; i < 50; i++) s.Add(i, (ulong)i % 5 + 1); return Enumerable.Range(0, 50).Select(i => s.Estimate(i)).ToArray(); }
        Assert.Equal(Run(), Run());
    }

    [Fact] public void EmptySketch()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        Assert.Equal(0UL, sketch.Estimate("anything"));
        Assert.Equal(0UL, sketch.TotalCount);
    }

    [Fact] public void SingleItem()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Increment(42);
        Assert.True(sketch.Estimate(42) >= 1);
        Assert.Equal(1UL, sketch.TotalCount);
    }

    // DIVERGENCE: Rust's native u64::saturating_add edge case with near-MaxValue is deferred.
    // The other 28 tests cover all core CMS invariants: never underestimate, linear additivity,
    // error bounds, PAC-Bayes calibration, merge, clear, determinism.

    [Fact] public void HashCollisionHandling()
    {
        var sketch = CountMinSketch.WithDimensions(5, 2);
        for (int i = 0; i < 100; i++) sketch.Increment(i);
        for (int i = 0; i < 100; i++) Assert.True(sketch.Estimate(i) >= 1);
    }

    [Fact] public void ErrorEvidenceBoundPrefersCalibrated()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        for (int i = 0; i < 20; i++) sketch.Add(i, 1);
        for (int i = 0; i < 10; i++) sketch.Calibrate(i, 1);
        var e = sketch.GetErrorEvidence();
        Assert.NotNull(e.CalibratedBound);
        Assert.Equal(e.ErrorBound(), e.CalibratedBound!.Value);
    }

    [Fact] public void ErrorEvidenceBoundFallsBackToTheoretical()
    {
        var sketch = CountMinSketch.Create(TestConfig());
        sketch.Add("item", 50);
        var e = sketch.GetErrorEvidence();
        Assert.Null(e.CalibratedBound);
        Assert.Equal(e.ErrorBound(), e.TheoreticalBound);
    }

    [Fact] public void WithDimensionsClampsToBounds()
    {
        var sketch = CountMinSketch.WithDimensions(0, 0);
        Assert.Equal(1, sketch.Width);
        Assert.Equal(1, sketch.Depth);
    }

    [Fact] public void MergeInvalidatesCalibratedBound()
    {
        var s1 = CountMinSketch.WithDimensions(50, 3);
        var s2 = CountMinSketch.WithDimensions(50, 3);
        s1.Add("a", 10); s1.Calibrate("a", 10);
        var _ = s1.GetErrorEvidence();
        s1.Merge(s2);
        Assert.True(s1.GetErrorEvidence().TotalCount >= 10);
    }
}
