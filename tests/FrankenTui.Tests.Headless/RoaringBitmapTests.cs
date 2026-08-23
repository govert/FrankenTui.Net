// SPDX-License-Identifier: Apache-2.0
// Tests adapted from crates/ftui-render/src/roaring_bitmap.rs at 15cc6543.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class RoaringBitmapTests
{
    [Fact]
    public void EmptyBitmap()
    {
        var bitmap = new RoaringBitmap();

        Assert.Equal(0, bitmap.Cardinality);
        Assert.True(bitmap.IsEmpty);
        Assert.False(bitmap.Contains(0));
        Assert.Empty(bitmap);
    }

    [Fact]
    public void InsertAndContains()
    {
        var bitmap = new RoaringBitmap();

        Assert.True(bitmap.Insert(42));
        Assert.False(bitmap.Insert(42));
        Assert.True(bitmap.Contains(42));
        Assert.False(bitmap.Contains(43));
        Assert.Equal(1, bitmap.Cardinality);
    }

    [Fact]
    public void InsertMultipleContainers()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(0);
        bitmap.Insert(65_536);
        bitmap.Insert(131_072);

        Assert.Equal(3, bitmap.Cardinality);
        Assert.True(bitmap.Contains(0));
        Assert.True(bitmap.Contains(65_536));
        Assert.True(bitmap.Contains(131_072));
    }

    [Fact]
    public void IterationIsSorted()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(100);
        bitmap.Insert(5);
        bitmap.Insert(50);
        bitmap.Insert(1);

        Assert.Equal([1u, 5u, 50u, 100u], bitmap.ToArray());
    }

    [Fact]
    public void IterationAcrossContainersIsSorted()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(65_537);
        bitmap.Insert(10);
        bitmap.Insert(65_536);

        Assert.Equal([10u, 65_536u, 65_537u], bitmap.ToArray());
    }

    [Fact]
    public void ClearRemovesAllContainers()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(1);
        bitmap.Insert(2);
        bitmap.Insert(3);

        bitmap.Clear();

        Assert.Equal(0, bitmap.Cardinality);
        Assert.True(bitmap.IsEmpty);
    }

    [Fact]
    public void UnionDoesNotMutateInputs()
    {
        var left = new RoaringBitmap();
        left.Insert(1);
        left.Insert(3);
        var right = new RoaringBitmap();
        right.Insert(2);
        right.Insert(3);

        var result = left.Union(right);

        Assert.Equal([1u, 2u, 3u], result.ToArray());
        Assert.Equal([1u, 3u], left.ToArray());
        Assert.Equal([2u, 3u], right.ToArray());
    }

    [Fact]
    public void IntersectionUsesCommonValuesOnly()
    {
        var left = new RoaringBitmap();
        left.InsertRange(1, 4);
        var right = new RoaringBitmap();
        right.InsertRange(2, 5);

        var result = left.Intersection(right);

        Assert.Equal([2u, 3u], result.ToArray());
        Assert.False(result.Contains(1));
        Assert.False(result.Contains(4));
    }

    [Fact]
    public void IntersectionWithEmptyIsEmpty()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(1);

        Assert.True(bitmap.Intersection(new RoaringBitmap()).IsEmpty);
    }

    [Fact]
    public void ArrayPromotesAtExactly4096Entries()
    {
        var bitmap = new RoaringBitmap();
        for (uint value = 0; value < RoaringBitmap.ArrayToBitmapThreshold; value++)
        {
            bitmap.Insert(value);
        }

        Assert.Equal(RoaringBitmap.ArrayToBitmapThreshold, bitmap.Cardinality);
        Assert.True(bitmap.IsBitmapContainerFor(0));
        for (uint value = 0; value < RoaringBitmap.ArrayToBitmapThreshold; value++)
        {
            Assert.True(bitmap.Contains(value));
        }
    }

    [Fact]
    public void CellIndexDirtyTracking()
    {
        const uint width = 80;
        static uint Cell(uint x, uint y) => (y * width) + x;
        var dirty = new RoaringBitmap();
        dirty.Insert(Cell(0, 0));
        dirty.Insert(Cell(79, 0));
        dirty.Insert(Cell(40, 12));

        Assert.Equal(3, dirty.Cardinality);
        Assert.True(dirty.Contains(Cell(0, 0)));
        Assert.True(dirty.Contains(Cell(79, 0)));
        Assert.True(dirty.Contains(Cell(40, 12)));
        Assert.False(dirty.Contains(Cell(1, 0)));
    }

    [Fact]
    public void LargeScreenDirtyTracking()
    {
        const uint width = 300;
        const uint height = 100;
        var dirty = new RoaringBitmap();
        for (uint x = 0; x < width; x++)
        {
            dirty.Insert((10 * width) + x);
        }

        Assert.Equal((long)width, dirty.Cardinality);
        dirty.Clear();
        for (uint y = 0; y < height; y++)
        {
            dirty.InsertRange(y * width, (y + 1) * width);
        }

        Assert.Equal((long)(width * height), dirty.Cardinality);
    }

    [Fact]
    public void InsertRangeIsHalfOpen()
    {
        var bitmap = new RoaringBitmap();
        bitmap.InsertRange(10, 20);

        Assert.Equal(10, bitmap.Cardinality);
        Assert.Equal(Enumerable.Range(10, 10).Select(value => (uint)value), bitmap);
        Assert.False(bitmap.Contains(9));
        Assert.False(bitmap.Contains(20));
    }

    [Fact]
    public void UnionAcrossContainers()
    {
        var left = new RoaringBitmap();
        left.Insert(100);
        var right = new RoaringBitmap();
        right.Insert(65_636);

        Assert.Equal([100u, 65_636u], left.Union(right).ToArray());
    }

    [Fact]
    public void BitmapIterationHandlesWordAndContainerEdges()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(0);
        bitmap.Insert(63);
        bitmap.Insert(64);
        bitmap.Insert(65_535);
        for (uint value = 1; value < RoaringBitmap.ArrayToBitmapThreshold; value++)
        {
            bitmap.Insert(value);
        }

        Assert.True(bitmap.IsBitmapContainerFor(0));
        var values = bitmap.ToArray();
        Assert.Equal(0u, values[0]);
        Assert.Contains(63u, values);
        Assert.Contains(64u, values);
        Assert.Equal(65_535u, values[^1]);
        Assert.True(values.SequenceEqual(values.Order()));
    }

    [Fact]
    public void DefaultConstructionIsEmpty() => Assert.True(new RoaringBitmap().IsEmpty);

    [Fact]
    public void SupportsEntireUnsignedDomainAndDeepClone()
    {
        var bitmap = new RoaringBitmap();
        bitmap.Insert(uint.MaxValue);
        bitmap.Insert(0);

        var clone = bitmap.Clone();
        clone.Insert(1);

        Assert.Equal([0u, uint.MaxValue], bitmap.ToArray());
        Assert.Equal([0u, 1u, uint.MaxValue], clone.ToArray());
    }
}
