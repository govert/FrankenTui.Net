// SPDX-License-Identifier: Apache-2.0
// Tests ported from crates/ftui-render/src/grapheme_pool.rs at 15cc6543.

using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class GraphemePoolTests
{
    [Fact]
    public void InternGetAndWidthRoundTrip()
    {
        var pool = new GraphemePool();
        var id = pool.Intern("👨‍👩‍👧‍👦", 2);
        Assert.Equal("👨‍👩‍👧‍👦", pool.Get(id));
        Assert.Equal(2, id.Width);
    }

    [Fact]
    public void DeduplicationReturnsSameIdAndRetains()
    {
        var pool = new GraphemePool();
        var first = pool.Intern("🎉", 2);
        var second = pool.Intern("🎉", 2);
        Assert.Equal(first, second);
        Assert.Equal((uint)2, pool.RefCount(first));
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void RetainReleaseAndDoubleReleaseAreSafe()
    {
        var pool = new GraphemePool();
        var id = pool.Intern("🚀", 2);
        pool.Retain(id);
        Assert.Equal((uint)2, pool.RefCount(id));
        pool.Release(id);
        Assert.Equal((uint)1, pool.RefCount(id));
        pool.Release(id);
        pool.Release(id);
        pool.Retain(id);
        Assert.Null(pool.Get(id));
        Assert.Equal((uint)0, pool.RefCount(id));
        Assert.True(pool.IsEmpty);
    }

    [Fact]
    public void SlotReuseAdvancesGeneration()
    {
        var pool = new GraphemePool();
        var old = pool.Intern("A", 1);
        pool.Release(old);
        var current = pool.Intern("B", 1);
        Assert.Equal(old.Slot, current.Slot);
        Assert.NotEqual(old.Generation, current.Generation);
        Assert.Null(pool.Get(old));
        Assert.Equal("B", pool.Get(current));
    }

    [Fact]
    public void MultipleGraphemesHaveDistinctIds()
    {
        var pool = new GraphemePool();
        var ids = new[] { pool.Intern("👨‍💻", 2), pool.Intern("👩‍🔬", 2), pool.Intern("🧑🏽‍🚀", 2) };
        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(new[] { "👨‍💻", "👩‍🔬", "🧑🏽‍🚀" }, ids.Select(pool.Get));
    }

    [Theory]
    [InlineData("zero-width", 0)]
    [InlineData("A", 1)]
    [InlineData("日", 2)]
    [InlineData("max-width", GraphemeId.MaxWidth)]
    [InlineData("", 0)]
    public void ValidWidthsAndTextArePreserved(string text, byte width)
    {
        var pool = new GraphemePool();
        var id = pool.Intern(text, width);
        Assert.Equal(width, id.Width);
        Assert.Equal(text, pool.Get(id));
    }

    [Fact]
    public void WidthOverflowIsRejected()
    {
        var pool = new GraphemePool();
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            pool.Intern("X", GraphemeId.MaxWidth + 1));
        Assert.Contains("width overflow", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CapacityAndLongString()
    {
        var pool = GraphemePool.WithCapacity(100);
        Assert.True(pool.Capacity >= 100);
        var text = new string('a', 1_000);
        Assert.Equal(text, pool.Get(pool.Intern(text, 1)));
    }

    [Fact]
    public void InvalidIdsAreInert()
    {
        var pool = new GraphemePool();
        var invalid = new GraphemeId(999, 0, 1);
        Assert.Null(pool.Get(invalid));
        Assert.Equal((uint)0, pool.RefCount(invalid));
        pool.Retain(invalid);
        pool.Release(invalid);
        Assert.True(pool.IsEmpty);
    }

    [Fact]
    public void ClearInvalidatesIdsAndReusesFromSlotZero()
    {
        var pool = new GraphemePool();
        var old = pool.Intern("A", 1);
        pool.Intern("B", 1);
        pool.Clear();
        var current = pool.Intern("C", 1);
        Assert.Equal(0, current.Slot);
        Assert.NotEqual(old.Generation, current.Generation);
        Assert.Null(pool.Get(old));
        Assert.Equal("C", pool.Get(current));
    }

    [Fact]
    public void FreeListIsLifo()
    {
        var pool = new GraphemePool();
        var zero = pool.Intern("A", 1);
        pool.Intern("B", 1);
        var two = pool.Intern("C", 1);
        pool.Release(zero);
        pool.Release(two);
        Assert.Equal(2, pool.Intern("D", 1).Slot);
        Assert.Equal(0, pool.Intern("E", 1).Slot);
    }

    [Fact]
    public void CloneIsIndependent()
    {
        var pool = new GraphemePool();
        var id = pool.Intern("shared", 1);
        var clone = pool.Clone();
        pool.Release(id);
        Assert.Null(pool.Get(id));
        Assert.Equal("shared", clone.Get(id));
        clone.Retain(id);
        Assert.Equal((uint)2, clone.RefCount(id));
        Assert.Contains("GraphemePool", clone.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GenerationOverflowDoesNotCorruptWidth()
    {
        var pool = new GraphemePool();
        var initial = pool.Intern("initial", 0);
        pool.Release(initial);
        for (var i = 0; i <= GraphemeId.MaxGeneration; i++)
        {
            var id = pool.Intern($"g{i}", 0);
            pool.Release(id);
        }
        var overflow = pool.Intern("overflow", 0);
        Assert.Equal(0, overflow.Width);
        Assert.Equal("overflow", pool.Get(overflow));
    }

    [Fact]
    public void GcRetainsReferencedAndFreesUnreferenced()
    {
        var pool = new GraphemePool();
        var keep = pool.Intern("🎉", 2);
        var drop = pool.Intern("🧪", 2);
        var buffer = BufferWith(keep);
        pool.Gc(buffer);
        Assert.Equal("🎉", pool.Get(keep));
        Assert.Null(pool.Get(drop));
        Assert.Equal((uint)1, pool.RefCount(keep));
    }

    [Fact]
    public void GcCountsMultipleBuffersAndReferences()
    {
        var pool = new GraphemePool();
        var first = pool.Intern("🎉", 2);
        var second = pool.Intern("🔥", 2);
        var one = BufferWith(first, first);
        var two = BufferWith(second);
        pool.Gc(one, two);
        Assert.Equal((uint)2, pool.RefCount(first));
        Assert.Equal((uint)1, pool.RefCount(second));
        Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void GcEmptyClosedSetFreesEverything()
    {
        var pool = new GraphemePool();
        var id = pool.Intern("test", 1);
        pool.Gc([]);
        Assert.Null(pool.Get(id));
        Assert.True(pool.IsEmpty);
    }

    [Fact]
    public void GcResetsInflatedRefcountsAndIsIdempotent()
    {
        var pool = new GraphemePool();
        var keep = pool.Intern("keep", 1);
        pool.Retain(keep);
        pool.Retain(keep);
        var buffer = BufferWith(keep);
        pool.Gc(buffer);
        pool.Gc(buffer);
        Assert.Equal((uint)1, pool.RefCount(keep));
        Assert.Equal("keep", pool.Get(keep));
    }

    [Fact]
    public void GcFreedSlotsRemainReusableAndLookupConsistent()
    {
        var pool = new GraphemePool();
        var drop = pool.Intern("A", 1);
        var keep = pool.Intern("B", 1);
        pool.Gc(BufferWith(keep));
        var restored = pool.Intern("A", 1);
        Assert.Equal(drop.Slot, restored.Slot);
        Assert.Equal("A", pool.Get(restored));
        Assert.Equal(keep, pool.Intern("B", 1));
    }

    [Fact]
    public void SharedFramePoolResolvesComplexGraphemesThroughBufferAndCopy()
    {
        var pool = new GraphemePool();
        var frame = new Frame(8, 1, pool);
        var id = frame.InternWithWidth("👨‍👩‍👧‍👦", 2);
        frame.Buffer.Set(0, 0, Cell.Empty.WithContent(CellContent.FromGrapheme(id)));
        Assert.Equal("👨‍👩‍👧‍👦", frame.Buffer.ResolveText(frame.Buffer.Get(0, 0)!.Value));

        var copy = new RenderBuffer(8, 1, pool);
        copy.CopyFrom(frame.Buffer);
        Assert.Equal("👨‍👩‍👧‍👦", copy.ResolveText(copy.Get(0, 0)!.Value));
    }

    [Fact]
    public void BufferOverrideMigratesLocalGraphemesIntoFramePool()
    {
        var local = new RenderBuffer(8, 1);
        local.SetText(0, 0, "👨‍👩‍👧‍👦", Cell.Empty);
        var pool = new GraphemePool();
        var frame = new Frame(8, 1, pool) { BufferOverride = local };
        Assert.Equal("👨‍👩‍👧‍👦", frame.Buffer.ResolveText(frame.Buffer.Get(0, 0)!.Value));
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void LocalBufferRegistryClearAlsoInvalidatesStaleIds()
    {
        var buffer = new RenderBuffer(4, 1);
        var old = buffer.Graphemes.Intern("A", 1);
        buffer.Clear();
        var current = buffer.Graphemes.Intern("B", 1);
        Assert.Equal(old.Slot, current.Slot);
        Assert.NotEqual(old.Generation, current.Generation);
        Assert.Null(buffer.Graphemes.Resolve(old));
        Assert.Equal("B", buffer.Graphemes.Resolve(current));
    }

    [Fact]
    public void DeterministicPropertyCorpusPreservesCoreInvariants()
    {
        var random = new Random(0x15CC6543);
        for (var sample = 0; sample < 256; sample++)
        {
            var text = $"g{sample}-{random.NextInt64():x}";
            var width = (byte)random.Next(0, GraphemeId.MaxWidth + 1);
            var pool = new GraphemePool();
            var id = pool.Intern(text, width);
            Assert.Equal(text, pool.Get(id));
            Assert.Equal(width, id.Width);
            Assert.Equal(id, pool.Intern(text, width));
            Assert.Equal((uint)2, pool.RefCount(id));
            pool.Release(id);
            Assert.Equal((uint)1, pool.RefCount(id));
            pool.Release(id);
            Assert.Null(pool.Get(id));
        }
    }

    private static RenderBuffer BufferWith(params GraphemeId[] ids)
    {
        var buffer = new RenderBuffer((ushort)Math.Max(ids.Length * 2, 1), 1);
        for (var i = 0; i < ids.Length; i++)
            buffer.Set((ushort)(i * 2), 0, Cell.Empty.WithContent(CellContent.FromGrapheme(ids[i])));
        return buffer;
    }
}
