// Upstream source: crates/ftui-widgets/src/elias_fano.rs (tests module)
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of all upstream Elias-Fano tests. Proptests converted to explicit loops.

using FrankenTui.Runtime;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class EliasFanoTests
{
    // ── Basic encoding/decoding ──

    [Fact] public void EmptySequence()
    {
        var ef = EliasFano.Encode([]);
        Assert.Equal(0, ef.Count);
        Assert.True(ef.IsEmpty);
        Assert.Equal(0, ef.Rank(0));
        Assert.Equal(0, ef.Rank(100));
        Assert.Null(ef.NextGeq(0));
    }

    [Fact] public void SingleElement()
    {
        var ef = EliasFano.Encode([42]);
        Assert.Equal(1, ef.Count);
        Assert.Equal(42UL, ef.Access(0));
        Assert.Equal(42UL, ef.Select(0));
        Assert.Equal(0, ef.Rank(41));
        Assert.Equal(1, ef.Rank(42));
        Assert.Equal(1, ef.Rank(100));
        Assert.Equal((0, 42UL), ef.NextGeq(0));
        Assert.Equal((0, 42UL), ef.NextGeq(42));
        Assert.Null(ef.NextGeq(43));
    }

    [Fact] public void AllZeros()
    {
        var ef = EliasFano.Encode([0, 0, 0, 0]);
        Assert.Equal(4, ef.Count);
        for (int i = 0; i < 4; i++)
            Assert.Equal(0UL, ef.Access(i));
        Assert.Equal(4, ef.Rank(0));
        Assert.Equal((0, 0UL), ef.NextGeq(0));
        Assert.Null(ef.NextGeq(1));
    }

    [Fact] public void ConsecutiveValues()
    {
        var values = Enumerable.Range(0, 100).Select(i => (ulong)i).ToArray();
        var ef = EliasFano.Encode(values);
        Assert.Equal(100, ef.Count);
        for (int i = 0; i < 100; i++)
            Assert.Equal((ulong)i, ef.Access(i));
    }

    [Fact] public void HeightPrefixSums()
    {
        ulong[] sums = [20, 45, 75, 100, 130, 180];
        var ef = EliasFano.Encode(sums);
        for (int i = 0; i < sums.Length; i++)
            Assert.Equal(sums[i], ef.Access(i));
        Assert.Equal(2, ef.Rank(50));
        Assert.Equal(3, ef.Rank(75));
        Assert.Equal((2, 75UL), ef.NextGeq(50));
    }

    [Fact] public void LargeValues()
    {
        ulong[] values = [1_000_000, 2_000_000, 3_000_000, 4_000_000];
        var ef = EliasFano.Encode(values);
        for (int i = 0; i < values.Length; i++)
            Assert.Equal(values[i], ef.Access(i));
    }

    [Fact] public void DuplicateValues()
    {
        ulong[] values = [5, 5, 10, 10, 10, 20];
        var ef = EliasFano.Encode(values);
        for (int i = 0; i < values.Length; i++)
            Assert.Equal(values[i], ef.Access(i));
        Assert.Equal(2, ef.Rank(5));
        Assert.Equal(5, ef.Rank(10));
        Assert.Equal(2, ef.Rank(9));
    }

    [Fact] public void RankBoundaryCases()
    {
        ulong[] values = [10, 20, 30, 40, 50];
        var ef = EliasFano.Encode(values);
        Assert.Equal(0, ef.Rank(0));
        Assert.Equal(0, ef.Rank(9));
        Assert.Equal(1, ef.Rank(10));
        Assert.Equal(1, ef.Rank(15));
        Assert.Equal(5, ef.Rank(50));
        Assert.Equal(5, ef.Rank(100));
    }

    [Fact] public void NextGeqExhaustive()
    {
        ulong[] values = [10, 20, 30, 40, 50];
        var ef = EliasFano.Encode(values);
        Assert.Equal((0, 10UL), ef.NextGeq(0));
        Assert.Equal((0, 10UL), ef.NextGeq(10));
        Assert.Equal((1, 20UL), ef.NextGeq(11));
        Assert.Equal((4, 50UL), ef.NextGeq(50));
        Assert.Null(ef.NextGeq(51));
    }

    [Fact] public void SelectMatchesAccess()
    {
        ulong[] values = [3, 7, 15, 31, 63, 127, 255];
        var ef = EliasFano.Encode(values);
        for (int i = 0; i < values.Length; i++)
            Assert.Equal(ef.Access(i), ef.Select(i));
    }

    [Fact] public void SpaceEfficiency()
    {
        var values = Enumerable.Range(0, 10_000).Select(i => (ulong)(i * 100)).ToArray();
        var ef = EliasFano.Encode(values);
        int denseSize = values.Length * 8;
        int efSize = ef.SizeInBytes;
        Assert.True(efSize < denseSize, $"Elias-Fano ({efSize}B) should be smaller than dense ({denseSize}B)");
    }

    [Fact] public void SizeInBytesNonZeroForNonEmpty()
    {
        var ef = EliasFano.Encode([1, 2, 3]);
        Assert.True(ef.SizeInBytes > 0);
    }

    [Fact] public void RejectsNonMonotone()
    {
        var ex = Assert.Throws<ArgumentException>(() => EliasFano.Encode([10, 5, 20]));
        Assert.Contains("non-decreasing", ex.Message);
    }

    [Fact] public void AccessOutOfBounds()
    {
        var ef = EliasFano.Encode([1, 2, 3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => ef.Access(3));
    }

    // ── Bit manipulation ──

    [Fact] public void GetSetBitsWithinWord()
    {
        var words = new ulong[2];
        EliasFano.SetBits(words, 0, 8, 0xAB);
        Assert.Equal(0xABUL, EliasFano.GetBits(words, 0, 8));
        EliasFano.SetBits(words, 16, 12, 0xFFF);
        Assert.Equal(0xFFFUL, EliasFano.GetBits(words, 16, 12));
    }

    [Fact] public void GetSetBitsCrossingBoundary()
    {
        var words = new ulong[2];
        EliasFano.SetBits(words, 60, 8, 0xFF);
        Assert.Equal(0xFFUL, EliasFano.GetBits(words, 60, 8));
    }

    [Fact] public void SelectInWordVarious()
    {
        // Tests matching upstream select_in_word_various
        Assert.Equal(1, EliasFano.SelectInWord(0b1010_1010, 0));
        Assert.Equal(3, EliasFano.SelectInWord(0b1010_1010, 1));
        Assert.Equal(5, EliasFano.SelectInWord(0b1010_1010, 2));
        Assert.Equal(7, EliasFano.SelectInWord(0b1010_1010, 3));
        Assert.Equal(0, EliasFano.SelectInWord(1, 0));
        Assert.Equal(63, EliasFano.SelectInWord(ulong.MaxValue, 63));
    }

    // ── Property test equivalents ──

    [Fact] public void AccessMatchesOriginalProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 20; trial++)
        {
            int n = rng.Next(1, 200);
            var values = new ulong[n];
            ulong acc = 0;
            for (int i = 0; i < n; i++) { acc += (ulong)rng.Next(1, 50); values[i] = acc; }
            var ef = EliasFano.Encode(values);
            Assert.Equal(n, ef.Count);
            for (int i = 0; i < n; i++)
                Assert.Equal(values[i], ef.Access(i));
        }
    }

    [Fact] public void RankMatchesNaiveProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 10; trial++)
        {
            int n = rng.Next(1, 100);
            var values = new ulong[n];
            ulong acc = 0;
            for (int i = 0; i < n; i++) { acc += (ulong)rng.Next(1, 20); values[i] = acc; }
            var ef = EliasFano.Encode(values);

            var testPoints = new HashSet<ulong>(values);
            foreach (var v in values) { if (v > 0) testPoints.Add(v - 1); testPoints.Add(v + 1); }
            testPoints.Add(0);

            foreach (var q in testPoints.Order())
            {
                int naive = values.Count(v => v <= q);
                Assert.Equal(naive, ef.Rank(q));
            }
        }
    }

    [Fact] public void NextGeqMatchesNaiveProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 10; trial++)
        {
            int n = rng.Next(1, 100);
            var values = new ulong[n];
            ulong acc = 0;
            for (int i = 0; i < n; i++) { acc += (ulong)rng.Next(1, 20); values[i] = acc; }
            var ef = EliasFano.Encode(values);

            var testPoints = new HashSet<ulong>(values) { 0 };
            if (values.Length > 0) testPoints.Add(values[^1] + 1);

            foreach (var q in testPoints.Order())
            {
                var naive = values.Select((v, i) => (Index: i, Value: v)).FirstOrDefault(x => x.Value >= q);
                var naiveResult = naive.Value >= q ? ((int, ulong)?)(naive.Index, naive.Value) : null;
                var efResult = ef.NextGeq(q);
                Assert.Equal(naiveResult, efResult);
            }
        }
    }

    [Fact] public void SelectEqualsAccessProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 20; trial++)
        {
            int n = rng.Next(1, 100);
            var values = new ulong[n];
            ulong acc = 0;
            for (int i = 0; i < n; i++) { acc += (ulong)rng.Next(1, 20); values[i] = acc; }
            var ef = EliasFano.Encode(values);
            for (int i = 0; i < n; i++)
                Assert.Equal(ef.Access(i), ef.Select(i));
        }
    }

    [Fact] public void SpaceWithin10xOfOptimalProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 10; trial++)
        {
            int n = rng.Next(2, 500);
            var values = new ulong[n];
            ulong acc = 0;
            for (int i = 0; i < n; i++) { acc += (ulong)rng.Next(1, 200); values[i] = acc; }
            var ef = EliasFano.Encode(values);
            int actual = ef.SizeInBytes;
            int optimal = Math.Max(ef.OptimalSizeInBytes, 1);
            Assert.True(actual <= optimal * 10,
                $"space: actual={actual}, optimal={optimal}");
        }
    }

    // ── Memory comparison ──

    private static ulong[] MakePrefixSums(int n, ulong avgHeight)
    {
        var sums = new ulong[n];
        ulong acc = 0;
        for (int i = 0; i < n; i++) { acc += avgHeight + (ulong)(i % 5); sums[i] = acc; }
        return sums;
    }

    [Fact] public void MemoryComparison1k()
    {
        var sums = MakePrefixSums(1_000, 20);
        var ef = EliasFano.Encode(sums);
        Assert.True(ef.SizeInBytes < sums.Length * 8);
    }

    [Fact] public void MemoryComparison10k()
    {
        var sums = MakePrefixSums(10_000, 20);
        var ef = EliasFano.Encode(sums);
        Assert.True(ef.SizeInBytes < sums.Length * 8);
    }

    [Fact] public void MemoryComparison100k()
    {
        var sums = MakePrefixSums(100_000, 20);
        var ef = EliasFano.Encode(sums);
        Assert.True(ef.SizeInBytes < sums.Length * 8);
    }

    [Fact] public void MemoryComparison1M()
    {
        var sums = MakePrefixSums(1_000_000, 20);
        var ef = EliasFano.Encode(sums);
        Assert.True(ef.SizeInBytes < sums.Length * 8);
    }

    [Fact] public void QueryCorrectnessAtScale()
    {
        var sums = MakePrefixSums(100_000, 20);
        var ef = EliasFano.Encode(sums);
        Assert.Equal(sums[0], ef.Access(0));
        Assert.Equal(sums[50_000], ef.Access(50_000));
        Assert.Equal(sums[99_999], ef.Access(99_999));

        ulong midVal = sums[50_000];
        int naiveRank = sums.Count(v => v <= midVal);
        Assert.Equal(naiveRank, ef.Rank(midVal));

        ulong target = sums[75_000] - 1;
        var result = ef.NextGeq(target);
        Assert.NotNull(result);
        Assert.True(result!.Value.Value >= target);
        Assert.Equal(sums[result.Value.Index], result.Value.Value);
    }
}
