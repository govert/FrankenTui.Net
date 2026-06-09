// Upstream source: crates/ftui-widgets/src/fenwick.rs (tests module)
// Full 1-1 port of all upstream FenwickTree tests.

using FrankenTui.Runtime;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class FenwickTreeTests
{
    // ── Basic construction ──
    [Fact] public void NewCreatesZeroedTree() { var ft = new FenwickTree(10); Assert.Equal(10, ft.Count); Assert.False(ft.IsEmpty); Assert.Equal(0U, ft.Total()); }
    [Fact] public void EmptyTree() { var ft = new FenwickTree(0); Assert.True(ft.IsEmpty); Assert.Equal(0U, ft.Total()); }

    [Fact] public void FromValuesMatchesSequential()
    {
        var ft = FenwickTree.FromValues([3, 1, 4, 1, 5, 9, 2, 6]);
        Assert.Equal(3U, ft.Prefix(0)); Assert.Equal(4U, ft.Prefix(1));
        Assert.Equal(8U, ft.Prefix(2)); Assert.Equal(31U, ft.Prefix(7));
        Assert.Equal(31U, ft.Total());
    }

    // ── Point operations ──
    [Fact] public void UpdateAndQuery() { var ft = new FenwickTree(5); ft.Update(0, 10); ft.Update(2, 20); ft.Update(4, 30); Assert.Equal(10U, ft.Prefix(0)); Assert.Equal(30U, ft.Prefix(2)); Assert.Equal(60U, ft.Prefix(4)); Assert.Equal(60U, ft.Total()); }
    [Fact] public void SetOverwritesValue() { var ft = FenwickTree.FromValues([5, 10, 15]); ft.Set(1, 20); Assert.Equal(5U, ft.Get(0)); Assert.Equal(20U, ft.Get(1)); Assert.Equal(15U, ft.Get(2)); Assert.Equal(40U, ft.Total()); }
    [Fact] public void GetRetrievesIndividualValues() { var v = new uint[] { 7, 3, 8, 2, 6 }; var ft = FenwickTree.FromValues(v); for (int i = 0; i < v.Length; i++) Assert.Equal(v[i], ft.Get(i)); }

    // ── Range queries ──
    [Fact] public void RangeSum() { var ft = FenwickTree.FromValues([1, 2, 3, 4, 5]); Assert.Equal(15U, ft.Range(0, 4)); Assert.Equal(9U, ft.Range(1, 3)); Assert.Equal(3U, ft.Range(2, 2)); Assert.Equal(1U, ft.Range(0, 0)); }

    // ── Batch update ──
    [Fact] public void BatchUpdateEquivalence()
    {
        var ftSeq = FenwickTree.FromValues([10, 20, 30, 40, 50]);
        var ftBatch = FenwickTree.FromValues([10, 20, 30, 40, 50]);
        var deltas = new (int, int)[] { (0, 5), (2, -3), (4, 10), (1, 7) };
        foreach (var (i, d) in deltas) ftSeq.Update(i, d);
        ftBatch.BatchUpdate(deltas);
        for (int i = 0; i < 5; i++) Assert.Equal(ftSeq.Get(i), ftBatch.Get(i));
    }

    // ── Rebuild ──
    [Fact] public void RebuildMatchesFromValues() { var v2 = new uint[] { 10, 20, 30, 40, 50 }; var ft1 = FenwickTree.FromValues(v2); var ft2 = FenwickTree.FromValues([1, 2, 3, 4, 5]); ft2.Rebuild(v2); for (int i = 0; i < 5; i++) Assert.Equal(ft1.Get(i), ft2.Get(i)); }

    // ── FindPrefix ──
    [Fact] public void FindPrefixScrollOffset()
    {
        var ft = FenwickTree.FromValues([20, 30, 10, 40, 25]);
        Assert.Null(ft.FindPrefix(0));
        Assert.Equal(0, ft.FindPrefix(20));
        Assert.Equal(1, ft.FindPrefix(50));
        Assert.Equal(2, ft.FindPrefix(99));
        Assert.Equal(4, ft.FindPrefix(125));
    }

    // ── Resize ──
    [Fact] public void ResizeGrow() { var ft = FenwickTree.FromValues([1, 2, 3]); ft.Resize(5); Assert.Equal(5, ft.Count); Assert.Equal(1U, ft.Get(0)); Assert.Equal(2U, ft.Get(1)); Assert.Equal(3U, ft.Get(2)); Assert.Equal(0U, ft.Get(3)); Assert.Equal(0U, ft.Get(4)); }
    [Fact] public void ResizeShrink() { var ft = FenwickTree.FromValues([1, 2, 3, 4, 5]); ft.Resize(3); Assert.Equal(3, ft.Count); Assert.Equal(6U, ft.Total()); }

    // ── Property test ──
    [Fact] public void PropertyPrefixSumCorrect()
    {
        ulong seed = 0xCAFE_BABE_0000_0001;
        int n = 100;
        var naive = new uint[n];
        var ft = new FenwickTree(n);
        for (int t = 0; t < 500; t++)
        {
            seed = unchecked(seed * 6364136223846793005 + 1442695040888963407);
            int idx = (int)((seed >> 33) % (uint)n);
            seed = unchecked(seed * 6364136223846793005 + 1442695040888963407);
            int delta = (int)(((seed >> 33) % 100));
            if (delta < 0) delta = -delta;
            naive[idx] = unchecked(naive[idx] + (uint)delta);
            ft.Update(idx, delta);
        }
        uint naivePrefix = 0;
        for (int i = 0; i < n; i++)
        {
            naivePrefix = unchecked(naivePrefix + naive[i]);
            Assert.Equal(naivePrefix, ft.Prefix(i));
        }
    }

    // ── Edge cases ──
    [Fact] public void UpdateI32Min() { var ft = FenwickTree.FromValues([0, 0, 0]); ft.Update(0, int.MinValue); Assert.Equal(unchecked((uint)int.MinValue), ft.Get(0)); }
    [Fact] public void SetLargeU32() { var ft = FenwickTree.FromValues([0, 100, 200]); ft.Set(0, uint.MaxValue); Assert.Equal(uint.MaxValue, ft.Get(0)); Assert.Equal(100U, ft.Get(1)); Assert.Equal(200U, ft.Get(2)); }

    // ── Edge-case tests ──
    [Fact] public void SingleElement() { var ft = new FenwickTree(1); ft.Update(0, 42); Assert.Equal(42U, ft.Get(0)); Assert.Equal(42U, ft.Prefix(0)); Assert.Equal(42U, ft.Total()); }
    [Fact] public void FromValuesEmpty() { var ft = FenwickTree.FromValues([]); Assert.True(ft.IsEmpty); Assert.Equal(0, ft.Count); Assert.Equal(0U, ft.Total()); }
    [Fact] public void FromValuesSingle() { var ft = FenwickTree.FromValues([99]); Assert.Equal(99U, ft.Get(0)); Assert.Equal(99U, ft.Total()); }
    [Fact] public void UpdateLastElement() { var ft = new FenwickTree(5); ft.Update(4, 100); Assert.Equal(100U, ft.Get(4)); Assert.Equal(100U, ft.Total()); }
    [Fact] public void UpdateNegativeDelta() { var ft = FenwickTree.FromValues([10, 20, 30]); ft.Update(1, -5); Assert.Equal(15U, ft.Get(1)); Assert.Equal(55U, ft.Total()); }
    [Fact] public void UpdateWrapsBelowZero() { var ft = FenwickTree.FromValues([5]); ft.Update(0, -10); Assert.Equal(unchecked(5U + (uint)(-10)), ft.Get(0)); }
    [Fact] public void GetAtZeroSingle() { Assert.Equal(42U, FenwickTree.FromValues([42]).Get(0)); }
    [Fact] public void GetAfterMultipleUpdates() { var ft = new FenwickTree(3); ft.Update(1, 10); ft.Update(1, 20); ft.Update(1, -5); Assert.Equal(25U, ft.Get(1)); }
    [Fact] public void PrefixZeroAfterUpdate() { var ft = new FenwickTree(5); ft.Update(0, 7); Assert.Equal(7U, ft.Prefix(0)); }
    [Fact] public void RangeSingleElement() { Assert.Equal(20U, FenwickTree.FromValues([10, 20, 30]).Range(1, 1)); }
    [Fact] public void RangeFullEqualsTotal() { var ft = FenwickTree.FromValues([1, 2, 3, 4, 5]); Assert.Equal(ft.Total(), ft.Range(0, 4)); }
    [Fact] public void RangeAllZeros() { var ft = new FenwickTree(5); Assert.Equal(0U, ft.Range(0, 4)); Assert.Equal(0U, ft.Range(2, 3)); }
    [Fact] public void BatchUpdateEmpty() { var ft = FenwickTree.FromValues([1, 2, 3]); ft.BatchUpdate([]); Assert.Equal(6U, ft.Total()); }
    [Fact] public void BatchUpdateSameIndex() { var ft = new FenwickTree(3); ft.BatchUpdate([(0, 10), (0, 20), (0, -5)]); Assert.Equal(25U, ft.Get(0)); Assert.Equal(0U, ft.Get(1)); }
    [Fact] public void RebuildIdempotent() { var v = new uint[] { 5, 10, 15 }; var ft = FenwickTree.FromValues(v); var t = ft.Total(); ft.Rebuild(v); Assert.Equal(t, ft.Total()); for (int i = 0; i < 3; i++) Assert.Equal(v[i], ft.Get(i)); }
    [Fact] public void RebuildAllZeros() { var ft = FenwickTree.FromValues([10, 20, 30]); ft.Rebuild([0, 0, 0]); Assert.Equal(0U, ft.Total()); }
    [Fact] public void FindPrefixEmpty() { var ft = new FenwickTree(0); Assert.Null(ft.FindPrefix(0)); Assert.Null(ft.FindPrefix(100)); }
    [Fact] public void FindPrefixSingle() { var ft = FenwickTree.FromValues([10]); Assert.Null(ft.FindPrefix(0)); Assert.Equal(0, ft.FindPrefix(10)); Assert.Equal(0, ft.FindPrefix(100)); }
    [Fact] public void FindPrefixExceedsTotal() { Assert.Equal(2, FenwickTree.FromValues([1, 2, 3]).FindPrefix(1000)); }
    [Fact] public void FindPrefixEqualsTotal() { Assert.Equal(2, FenwickTree.FromValues([5, 5, 5]).FindPrefix(15)); }
    [Fact] public void FindPrefixExactBoundaries() { var ft = FenwickTree.FromValues([10, 10, 10]); Assert.Equal(0, ft.FindPrefix(10)); Assert.Equal(1, ft.FindPrefix(20)); Assert.Equal(2, ft.FindPrefix(30)); }
    [Fact] public void ResizeToZero() { var ft = FenwickTree.FromValues([1, 2, 3]); ft.Resize(0); Assert.True(ft.IsEmpty); Assert.Equal(0U, ft.Total()); }
    [Fact] public void ResizeSameSize() { var ft = FenwickTree.FromValues([1, 2, 3]); ft.Resize(3); Assert.Equal(3, ft.Count); Assert.Equal(6U, ft.Total()); }
    [Fact] public void ResizeGrowFromZero() { var ft = new FenwickTree(0); ft.Resize(3); Assert.Equal(3, ft.Count); Assert.Equal(0U, ft.Total()); ft.Update(0, 5); Assert.Equal(5U, ft.Get(0)); }
    [Fact] public void SetToZero() { var ft = FenwickTree.FromValues([10, 20, 30]); ft.Set(1, 0); Assert.Equal(0U, ft.Get(1)); Assert.Equal(40U, ft.Total()); }
    [Fact] public void SetSameValue() { var ft = FenwickTree.FromValues([10, 20, 30]); ft.Set(1, 20); Assert.Equal(20U, ft.Get(1)); Assert.Equal(60U, ft.Total()); }
    [Fact] public void SetToMaxU32() { var ft = FenwickTree.FromValues([0, 0, 0]); ft.Set(0, uint.MaxValue); Assert.Equal(uint.MaxValue, ft.Get(0)); }
    [Fact] public void TotalOnSingleElement() { Assert.Equal(42U, FenwickTree.FromValues([42]).Total()); }
    [Fact] public void FromValuesPreservesAll() { var v = Enumerable.Range(1, 20).Select(i => (uint)i).ToArray(); var ft = FenwickTree.FromValues(v); for (int i = 0; i < 20; i++) Assert.Equal(v[i], ft.Get(i)); Assert.Equal(210U, ft.Total()); }
    [Fact] public void RangeFirstToMiddle() { Assert.Equal(60U, FenwickTree.FromValues([10, 20, 30, 40, 50]).Range(0, 2)); }
    [Fact] public void RangeMiddleToEnd() { Assert.Equal(90U, FenwickTree.FromValues([10, 20, 30, 40, 50]).Range(3, 4)); }

    // ── Panic tests ──
    [Fact] public void UpdateOutOfBounds() { Assert.Throws<ArgumentOutOfRangeException>(() => new FenwickTree(3).Update(3, 1)); }
    [Fact] public void PrefixOutOfBounds() { Assert.Throws<ArgumentOutOfRangeException>(() => new FenwickTree(3).Prefix(3)); }
    [Fact] public void RangeLeftGreaterThanRight() { Assert.Throws<ArgumentException>(() => FenwickTree.FromValues([1, 2, 3]).Range(2, 1)); }
    [Fact] public void RebuildWrongSize() { Assert.Throws<ArgumentException>(() => new FenwickTree(3).Rebuild([1, 2])); }

    // ── Helpers ──
    [Fact] public void LowBitCorrectness() { Assert.Equal(1, FenwickTree.LowBit(1)); Assert.Equal(2, FenwickTree.LowBit(2)); Assert.Equal(1, FenwickTree.LowBit(3)); Assert.Equal(4, FenwickTree.LowBit(4)); Assert.Equal(2, FenwickTree.LowBit(6)); Assert.Equal(8, FenwickTree.LowBit(8)); Assert.Equal(4, FenwickTree.LowBit(12)); }
    [Fact] public void MsbCorrectness() { Assert.Equal(0, FenwickTree.MostSignificantBit(0)); Assert.Equal(1, FenwickTree.MostSignificantBit(1)); Assert.Equal(4, FenwickTree.MostSignificantBit(5)); Assert.Equal(8, FenwickTree.MostSignificantBit(8)); Assert.Equal(64, FenwickTree.MostSignificantBit(100)); Assert.Equal(512, FenwickTree.MostSignificantBit(1000)); }
}
