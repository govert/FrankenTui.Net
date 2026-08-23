// SPDX-License-Identifier: Apache-2.0
// Tests adapted from crates/ftui-render/src/arena.rs at 15cc6543.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class ArenaTests
{
    [Fact]
    public void AllocFmtFormatsInvariantly()
    {
        var arena = new FrameArena(4096);
        Assert.Equal("hello 42 world", arena.AllocFmt($"hello {42} {"world"}"));
        Assert.Equal("value 7", arena.AllocFmt("value {0}", 7));
    }

    [Fact]
    public void AllocIterCollectsAndHandlesEmpty()
    {
        var arena = new FrameArena(4096);
        Assert.Equal(Enumerable.Range(0, 10), arena.AllocIter(Enumerable.Range(0, 10)));
        Assert.Empty(arena.AllocIter(Array.Empty<byte>()));
    }

    [Fact]
    public void NewVecSupportsPushAndCapacity()
    {
        var arena = new FrameArena(4096);
        var values = arena.NewVec<uint>();
        values.AddRange([1, 2, 3]);
        Assert.Equal(new uint[] { 1, 2, 3 }, values);
        var reserved = arena.NewVecWithCapacity<ulong>(100);
        Assert.True(reserved.Capacity >= 100);
        Assert.Empty(reserved);
    }

    [Theory]
    [InlineData("hello, world!")]
    [InlineData("")]
    [InlineData("こんにちは 🎉")]
    public void AllocStrPreservesContent(string value)
    {
        var arena = new FrameArena(4096);
        Assert.Equal(value, arena.AllocStr(value));
    }

    [Fact]
    public void AllocSliceCopiesAndHandlesEmpty()
    {
        var arena = new FrameArena(4096);
        var source = new uint[] { 1, 2, 3, 4, 5 };
        var copy = arena.AllocSlice<uint>(source);
        source[0] = 9;
        Assert.Equal(new uint[] { 1, 2, 3, 4, 5 }, copy);
        Assert.Empty(arena.AllocSlice<byte>([]));
    }

    [Fact]
    public void AllocAndAllocWithReturnMutableBoxes()
    {
        var arena = new FrameArena(4096);
        var direct = arena.Alloc(100);
        direct.Value = 200;
        Assert.Equal(200, direct.Value);
        Assert.Equal((ulong)42, arena.AllocWith(() => 42UL).Value);
    }

    [Fact]
    public void MultipleAllocationsCoexistUntilReset()
    {
        var arena = new FrameArena(4096);
        var first = arena.AllocStr("hello");
        var second = arena.AllocStr("world");
        var slice = arena.AllocSlice<uint>([1, 2, 3]);
        var value = arena.Alloc(42UL);
        Assert.Equal("hello", first);
        Assert.Equal("world", second);
        Assert.Equal(new uint[] { 1, 2, 3 }, slice);
        Assert.Equal((ulong)42, value.Value);
        Assert.Equal(4, arena.AllocationCount);
    }

    [Fact]
    public void ResetClearsOwnershipButRetainsCapacityHighWater()
    {
        var arena = new FrameArena(128);
        arena.AllocSlice<byte>(new byte[32 * 1024]);
        var grown = arena.AllocatedBytes;
        Assert.True(grown >= 32 * 1024);
        arena.Reset();
        Assert.Equal(0, arena.AllocationCount);
        Assert.Equal(0, arena.LiveBytes);
        Assert.Equal(1, arena.Generation);
        Assert.Equal(grown, arena.AllocatedBytes);
        arena.AllocSlice<byte>(new byte[32 * 1024]);
        Assert.Equal(grown, arena.AllocatedBytes);
    }

    [Fact]
    public void DefaultCapacityAndGrowthMatchSourcePolicyShape()
    {
        var arena = new FrameArena();
        Assert.Equal(FrameArena.DefaultArenaCapacity, arena.InitialCapacity);
        arena.AllocSlice<byte>(new byte[FrameArena.DefaultArenaCapacity + (64 * 1024)]);
        Assert.True(arena.AllocatedBytes >= FrameArena.DefaultArenaCapacity);
        Assert.True(arena.AllocatedBytesIncludingMetadata >= arena.AllocatedBytes);
    }

    [Fact]
    public void HeavyResetReuseIsStable()
    {
        var arena = new FrameArena(4096);
        for (var frame = 0; frame < 100; frame++)
        {
            Assert.StartsWith("frame ", arena.AllocStr($"frame {frame}"), StringComparison.Ordinal);
            Assert.Equal(50, arena.AllocIter(Enumerable.Range(0, 50)).Length);
            arena.Reset();
        }
        Assert.Equal(100, arena.Generation);
    }

    [Fact]
    public void DeterministicOperationCorpusNeverLosesValues()
    {
        var random = new Random(0x15CC6543);
        var arena = new FrameArena(256);
        for (var i = 0; i < 300; i++)
        {
            var size = random.Next(1, 1024);
            switch (random.Next(4))
            {
                case 0:
                    Assert.Equal(size, arena.AllocStr(new string('x', size)).Length);
                    break;
                case 1:
                    Assert.Equal(size, arena.AllocSlice<int>(new int[size]).Length);
                    break;
                case 2:
                    Assert.Equal(size, arena.Alloc(size).Value);
                    break;
                default:
                    arena.Reset();
                    break;
            }
        }
    }

    [Fact]
    public void FrameExposesExplicitArenaLifecycle()
    {
        var frame = new Frame(4, 1, new GraphemePool());
        Assert.Null(frame.Arena);
        var arena = new FrameArena(1024);
        frame.SetArena(arena);
        Assert.Same(arena, frame.Arena);
        frame.ClearArena();
        Assert.Null(frame.Arena);
    }

    [Fact]
    public void DebugViewStatesCapacityAndGeneration()
    {
        var arena = new FrameArena(1024);
        Assert.Contains("FrameArena", arena.ToString(), StringComparison.Ordinal);
        Assert.Contains("retained_bytes = 1024", arena.ToString(), StringComparison.Ordinal);
    }
}
