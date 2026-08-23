// SPDX-License-Identifier: Apache-2.0
// Tests adapted from crates/ftui-render/src/quotient_filter.rs at 15cc6543.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class QuotientFilterTests
{
    [Fact]
    public void EmptyFilterUsesDefaultConfiguration()
    {
        var filter = QuotientFilter.WithDefaults();

        Assert.True(filter.IsEmpty);
        Assert.Equal(0, filter.Count);
        Assert.Equal(1024, filter.Capacity);
        Assert.False(filter.Contains(42UL));
    }

    [Fact]
    public void RemovalNeverCreatesFalseNegativesInsideClusters()
    {
        for (ulong baseValue = 0; baseValue < 2000; baseValue += 97)
        {
            var filter = new QuotientFilter(new QuotientFilterConfig(4, 16));
            var items = Enumerable.Range(0, 12).Select(offset => baseValue + (ulong)offset).ToArray();
            foreach (var item in items)
            {
                filter.Insert(item);
            }

            for (var index = 0; index < items.Length; index += 2)
            {
                filter.Remove(items[index]);
            }

            for (var index = 1; index < items.Length; index += 2)
            {
                Assert.True(filter.Contains(items[index]),
                    $"false negative for {items[index]} after cluster deletions (base {baseValue})");
            }
        }
    }

    [Fact]
    public void InsertAndLookup()
    {
        var filter = QuotientFilter.WithDefaults();

        Assert.True(filter.Insert(100UL));
        Assert.True(filter.Contains(100UL));
        Assert.Equal(1, filter.Count);
    }

    [Fact]
    public void DuplicateInsertReturnsFalse()
    {
        var filter = QuotientFilter.WithDefaults();

        Assert.True(filter.Insert(42UL));
        Assert.False(filter.Insert(42UL));
        Assert.Equal(1, filter.Count);
    }

    [Fact]
    public void InsertMultipleHasNoFalseNegatives()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(8, 8));
        for (ulong item = 0; item < 50; item++)
        {
            filter.Insert(item);
        }

        Assert.Equal(50, filter.Count);
        for (ulong item = 0; item < 50; item++)
        {
            Assert.True(filter.Contains(item));
        }
    }

    [Fact]
    public void NoFalseNegativesForTwoHundredItems()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(10, 10));
        for (ulong item = 0; item < 200; item++)
        {
            filter.Insert(item);
        }

        for (ulong item = 0; item < 200; item++)
        {
            Assert.True(filter.Contains(item));
        }
    }

    [Fact]
    public void ObservedFalsePositiveRateIsBounded()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(12, 8));
        for (ulong item = 0; item < 500; item++)
        {
            filter.Insert(item);
        }

        var falsePositives = 0;
        for (ulong item = 10_000; item < 20_000; item++)
        {
            if (filter.Contains(item))
            {
                falsePositives++;
            }
        }

        Assert.True(falsePositives / 10_000d < 0.05,
            $"false positive rate was {falsePositives / 10_000d:P4}");
    }

    [Fact]
    public void RemoveElement()
    {
        var filter = QuotientFilter.WithDefaults();
        filter.Insert(42UL);

        Assert.True(filter.Remove(42UL));
        Assert.Equal(0, filter.Count);
        Assert.False(filter.Contains(42UL));
    }

    [Fact]
    public void RemoveNonexistentReturnsFalse() =>
        Assert.False(QuotientFilter.WithDefaults().Remove(42UL));

    [Fact]
    public void RemovalPreservesOtherProbeMembers()
    {
        var filter = QuotientFilter.WithDefaults();
        for (ulong item = 0; item < 20; item++)
        {
            filter.Insert(item);
        }

        for (ulong item = 0; item < 20; item += 2)
        {
            filter.Remove(item);
        }

        for (ulong item = 1; item < 20; item += 2)
        {
            Assert.True(filter.Contains(item));
        }

        for (ulong item = 0; item < 20; item += 2)
        {
            Assert.False(filter.Contains(item));
        }
    }

    [Fact]
    public void ClearFilter()
    {
        var filter = QuotientFilter.WithDefaults();
        for (ulong item = 0; item < 100; item++)
        {
            filter.Insert(item);
        }

        filter.Clear();

        Assert.True(filter.IsEmpty);
        Assert.Equal(0, filter.Count);
        for (ulong item = 0; item < 100; item++)
        {
            Assert.False(filter.Contains(item));
        }
    }

    [Fact]
    public void LoadFactorTracksOccupancy()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(4, 4));
        Assert.Equal(0.0, filter.LoadFactor);
        for (ulong item = 0; item < 8; item++)
        {
            filter.Insert(item);
        }

        Assert.Equal(0.5, filter.LoadFactor, 12);
    }

    [Fact]
    public void ConfigForCapacityMeetsRequestedShape()
    {
        var config = QuotientFilterConfig.ForCapacity(10_000, 0.01);

        Assert.True(config.R >= 7);
        Assert.True((1UL << (int)config.Q) >= 10_000);
    }

    [Fact]
    public void ConfigForCapacitySanitizesInvalidRates()
    {
        var zero = QuotientFilterConfig.ForCapacity(128, 0.0);
        var nan = QuotientFilterConfig.ForCapacity(128, double.NaN);

        Assert.True(zero.R >= 7);
        Assert.True(nan.R >= 7);
        Assert.True((1UL << (int)zero.Q) >= 128);
        Assert.True((1UL << (int)nan.Q) >= 128);
    }

    [Fact]
    public void StringKeysUseStableUtf8Hashing()
    {
        var filter = QuotientFilter.WithDefaults();
        filter.Insert("hello");
        filter.Insert("world");

        Assert.True(filter.Contains("hello"));
        Assert.True(filter.Contains("world"));
        Assert.False(filter.Contains("foo"));
    }

    [Theory]
    [InlineData(0UL, 926u, 69UL)]
    [InlineData(1UL, 557u, 217UL)]
    [InlineData(42UL, 991u, 81UL)]
    [InlineData(100UL, 171u, 113UL)]
    [InlineData(10_000UL, 872u, 166UL)]
    public void UnsignedIntegerFingerprintsMatchPinnedRustDefaultHasher(
        ulong value, uint expectedQuotient, ulong expectedRemainder)
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(10, 8));

        Assert.Equal((expectedQuotient, expectedRemainder), filter.FingerprintForTest(value));
    }

    [Theory]
    [InlineData("hello", 910u, 217UL)]
    [InlineData("world", 1004u, 36UL)]
    [InlineData("foo", 627u, 183UL)]
    public void StringFingerprintsMatchPinnedRustDefaultHasher(
        string value, uint expectedQuotient, ulong expectedRemainder)
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(10, 8));

        Assert.Equal((expectedQuotient, expectedRemainder), filter.FingerprintForTest(value));
    }

    [Fact]
    public void RowIdTracking()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(12, 8));
        uint[] dirtyRows = [5, 42, 100, 255, 1000];
        foreach (var row in dirtyRows)
        {
            filter.Insert(row);
        }

        foreach (var row in dirtyRows)
        {
            Assert.True(filter.Contains(row));
            filter.Remove(row);
        }

        Assert.True(filter.IsEmpty);
    }

    [Fact]
    public void MergeFiltersAddsNewFingerprints()
    {
        var config = new QuotientFilterConfig(8, 8);
        var left = new QuotientFilter(config);
        var right = new QuotientFilter(config);
        for (ulong item = 0; item < 10; item++) left.Insert(item);
        for (ulong item = 5; item < 15; item++) right.Insert(item);

        var added = left.Merge(right);

        Assert.True(added > 0);
        for (ulong item = 0; item < 15; item++) Assert.True(left.Contains(item));
    }

    [Fact]
    public void MergeWithMismatchedConfigIsNoop()
    {
        var left = new QuotientFilter(new QuotientFilterConfig(8, 8));
        var right = new QuotientFilter(new QuotientFilterConfig(10, 8));
        left.Insert(1UL);

        Assert.Equal(0, left.Merge(right));
        Assert.Equal(1, left.Count);
    }

    [Fact]
    public void TheoreticalRateIncreasesWithLoad()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(10, 8));
        var emptyRate = filter.TheoreticalFalsePositiveRate;
        for (ulong item = 0; item < 100; item++) filter.Insert(item);

        Assert.True(emptyRate < filter.TheoreticalFalsePositiveRate);
    }

    [Fact]
    public void SparseFilterShapeUsesFewerBitsThanMillionRowBitset()
    {
        var config = QuotientFilterConfig.ForCapacity(1000, 0.01);
        var filterBits = (config.R + 3) * (1UL << (int)config.Q);

        Assert.True(filterBits < 1_000_000);
    }

    [Fact]
    public void DefaultConfigIsEmpty() =>
        Assert.True(new QuotientFilter().IsEmpty);

    [Fact]
    public void InsertAfterRemoveReusesSlot()
    {
        var filter = QuotientFilter.WithDefaults();
        filter.Insert(1UL);
        filter.Remove(1UL);

        Assert.True(filter.Insert(1UL));
        Assert.True(filter.Contains(1UL));
        Assert.Equal(1, filter.Count);
    }

    [Fact]
    public void HeavyLoadHasNoFalseNegatives()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(10, 20));
        var inserted = 0;
        for (ulong item = 0; item < 500; item++)
        {
            if (filter.Insert(item)) inserted++;
        }

        Assert.Equal(500, inserted);
        for (ulong item = 0; item < 500; item++) Assert.True(filter.Contains(item));
    }

    [Fact]
    public void CloneIsIndependentAndCustomFullHashIsAccepted()
    {
        var filter = new QuotientFilter(new QuotientFilterConfig(4, 8));
        var value = new ExactHash(0x1234_5678_9ABC_DEF0);
        filter.Insert(value);

        var clone = filter.Clone();
        clone.Clear();

        Assert.True(filter.Contains(value));
        Assert.True(clone.IsEmpty);
    }

    private sealed record ExactHash(ulong Hash) : IQuotientFilterHashable
    {
        public ulong GetQuotientFilterHash() => Hash;
    }
}
