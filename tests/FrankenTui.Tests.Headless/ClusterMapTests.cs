// Upstream source: crates/ftui-text/src/cluster_map.rs — tests
// Source basis: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class ClusterMapTests
{
    [Fact]
    public void EmptyText()
    {
        var map = ClusterMap.FromText(string.Empty);

        Assert.True(map.IsEmpty);
        Assert.Equal(0, map.TotalCells);
        Assert.Equal(0, map.TotalBytes);
        Assert.Equal(0, map.ClusterCount);
    }

    [Fact]
    public void AsciiText()
    {
        var map = ClusterMap.FromText("Hello");

        Assert.Equal(5, map.ClusterCount);
        Assert.Equal(5, map.TotalCells);
        Assert.Equal(5, map.TotalBytes);
        for (var i = 0; i < 5; i++)
        {
            var entry = AssertEntry(map.Get(i));
            Assert.Equal((uint)i, entry.ByteStart);
            Assert.Equal((uint)(i + 1), entry.ByteEnd);
            Assert.Equal((uint)i, entry.CellStart);
            Assert.Equal((byte)1, entry.CellWidth);
        }
    }

    [Fact]
    public void WideCharactersUseUtf8ByteCoordinates()
    {
        var map = ClusterMap.FromText("世界");

        Assert.Equal(2, map.ClusterCount);
        Assert.Equal(6, map.TotalBytes);
        Assert.Equal(4, map.TotalCells);
        Assert.Equal(new ClusterEntry(0, 3, 0, 0, 2), AssertEntry(map.Get(0)));
        Assert.Equal(new ClusterEntry(3, 6, 1, 2, 2), AssertEntry(map.Get(1)));
    }

    [Fact]
    public void MixedAsciiAndWideText()
    {
        var map = ClusterMap.FromText("Hi世界!");

        Assert.Equal(5, map.ClusterCount);
        Assert.Equal(9, map.TotalBytes);
        Assert.Equal(7, map.TotalCells);
        Assert.Equal(new uint[] { 0, 1, 2, 4, 6 }, map.Entries.Select(entry => entry.CellStart));
    }

    [Fact]
    public void ZeroWidthClustersRoundTripToFirstOwner()
    {
        var map = ClusterMap.FromText("a\u200B\u200Bb");

        Assert.Contains(map.Entries, entry => entry.CellWidth == 0);
        Assert.Contains(
            map.Entries.Zip(map.Entries.Skip(1)),
            pair => pair.First.CellStart == pair.Second.CellStart);

        foreach (var entry in map.Entries)
        {
            var cell = map.ByteToCell((int)entry.ByteStart);
            var back = map.CellToByte(cell);
            Assert.True(back <= entry.ByteStart,
                $"round-trip overshoot: byte {entry.ByteStart} -> cell {cell} -> byte {back}");
        }

        var firstAtColumnOne = map.Entries.First(entry => entry.CellStart == 1);
        Assert.Equal((int)firstAtColumnOne.ByteStart, map.CellToByte(1));
        Assert.Equal(firstAtColumnOne.ByteStart, AssertEntry(map.CellToEntry(1)).ByteStart);
    }

    [Fact]
    public void FuzzCrashControlCharactersRoundTripToFirstOwner()
    {
        // Exact minimized input from the upstream fuzz failure fixed by 01356940:
        // '2' + U+0003 (zero width) + newline (width one).
        var map = ClusterMap.FromText("2\u0003\n");

        foreach (var entry in map.Entries)
        {
            var cell = map.ByteToCell((int)entry.ByteStart);
            var back = map.CellToByte(cell);
            Assert.True(back <= entry.ByteStart,
                $"round-trip overshoot: byte {entry.ByteStart} -> cell {cell} -> byte {back}");
        }

        Assert.Equal(1, map.CellToByte(1));
        Assert.Equal((uint)1, AssertEntry(map.CellToEntry(1)).ByteStart);
    }

    [Fact]
    public void CombiningMarksFormOneCluster()
    {
        var map = ClusterMap.FromText("e\u0301");

        Assert.Equal(1, map.ClusterCount);
        Assert.Equal(3, map.TotalBytes);
        Assert.Equal(1, map.TotalCells);
        Assert.Equal(new ClusterEntry(0, 3, 0, 0, 1), AssertEntry(map.Get(0)));
    }

    [Fact]
    public void ByteToCellHandlesWideAndMidClusterOffsets()
    {
        var map = ClusterMap.FromText("Hi世界!");

        Assert.Equal(0, map.ByteToCell(0));
        Assert.Equal(1, map.ByteToCell(1));
        Assert.Equal(2, map.ByteToCell(2));
        Assert.Equal(2, map.ByteToCell(3));
        Assert.Equal(2, map.ByteToCell(4));
        Assert.Equal(4, map.ByteToCell(5));
        Assert.Equal(6, map.ByteToCell(8));
        Assert.Equal(7, map.ByteToCell(9));
    }

    [Fact]
    public void ByteToEntryHandlesMidClusterAndPastEnd()
    {
        var map = ClusterMap.FromText("AB世C");

        Assert.Equal((uint)0, AssertEntry(map.ByteToEntry(0)).ByteStart);
        Assert.Equal((uint)2, AssertEntry(map.ByteToEntry(2)).ByteStart);
        Assert.Equal((uint)2, AssertEntry(map.ByteToEntry(3)).ByteStart);
        Assert.Null(map.ByteToEntry(100));
    }

    [Fact]
    public void CellToByteMapsContinuationCellsToClusterStart()
    {
        var map = ClusterMap.FromText("Hi世界!");

        Assert.Equal(new[] { 0, 1, 2, 2, 5, 5, 8 },
            Enumerable.Range(0, 7).Select(map.CellToByte));
        Assert.Equal(9, map.CellToByte(7));
    }

    [Fact]
    public void CellToEntryMapsBothWideCellsToOneEntry()
    {
        var map = ClusterMap.FromText("世");

        var first = AssertEntry(map.CellToEntry(0));
        var continuation = AssertEntry(map.CellToEntry(1));
        Assert.Equal(first, continuation);
        Assert.Equal((byte)2, first.CellWidth);
    }

    [Fact]
    public void ByteRangesMapToCellRanges()
    {
        Assert.Equal((0, 5), ClusterMap.FromText("Hello World").ByteRangeToCellRange(0, 5));
        Assert.Equal((6, 11), ClusterMap.FromText("Hello World").ByteRangeToCellRange(6, 11));
        Assert.Equal((2, 6), ClusterMap.FromText("Hi世界!").ByteRangeToCellRange(2, 8));
    }

    [Fact]
    public void CellRangesMapToCompleteUtf8Clusters()
    {
        var map = ClusterMap.FromText("Hi世界!");

        Assert.Equal((2, 8), map.CellRangeToByteRange(2, 6));
        Assert.Equal((2, 8), map.CellRangeToByteRange(3, 5));
        Assert.Equal((0, 5), ClusterMap.FromText("Hello World").CellRangeToByteRange(0, 5));
    }

    [Fact]
    public void GraphemeAccessorsRoundTrip()
    {
        var map = ClusterMap.FromText("Hi世界!");

        Assert.Equal(0, map.GraphemeToCell(0));
        Assert.Equal(2, map.GraphemeToCell(2));
        Assert.Equal(6, map.GraphemeToCell(4));
        Assert.Equal(7, map.GraphemeToCell(5));
        Assert.Equal(0, map.CellToGrapheme(0));
        Assert.Equal(2, map.CellToGrapheme(2));
        Assert.Equal(2, map.CellToGrapheme(3));

        var byteMap = ClusterMap.FromText("A世B");
        Assert.Equal(new[] { 0, 1, 4 }, Enumerable.Range(0, 3).Select(byteMap.GraphemeToByte));
        Assert.Equal(new[] { 0, 1, 2 }, new[] { 0, 1, 4 }.Select(byteMap.ByteToGrapheme));
    }

    [Fact]
    public void ExtractTextForCellsPreservesCanonicalText()
    {
        const string text = "Hi世界!";
        var map = ClusterMap.FromText(text);

        Assert.Equal("世界", map.ExtractTextForCells(text, 2, 6));
        Assert.Equal("世界", map.ExtractTextForCells(text, 3, 5));
        Assert.Equal(string.Empty, map.ExtractTextForCells(text, 3, 3));
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("世界")]
    [InlineData("Hi世界!")]
    [InlineData("e\u0301f")]
    [InlineData("שלום")]
    [InlineData("")]
    public void ByteCellRoundTripPreservesClusterStarts(string text)
    {
        var map = ClusterMap.FromText(text);

        foreach (var entry in map.Entries)
        {
            var byteOffset = (int)entry.ByteStart;
            Assert.Equal(byteOffset, map.CellToByte(map.ByteToCell(byteOffset)));
        }
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("世界")]
    [InlineData("Hi世界!")]
    [InlineData("e\u0301f")]
    public void CellByteRoundTripPreservesClusterStarts(string text)
    {
        var map = ClusterMap.FromText(text);

        foreach (var entry in map.Entries)
        {
            var cell = (int)entry.CellStart;
            Assert.Equal(cell, map.ByteToCell(map.CellToByte(cell)));
        }
    }

    [Fact]
    public void EntriesAreMonotonicAndContiguous()
    {
        var map = ClusterMap.FromText("Hi世界!");

        foreach (var pair in map.Entries.Zip(map.Entries.Skip(1)))
        {
            Assert.True(pair.First.ByteStart < pair.Second.ByteStart);
            Assert.True(pair.First.CellStart < pair.Second.CellStart);
            Assert.Equal(pair.First.ByteEnd, pair.Second.ByteStart);
            Assert.Equal(pair.First.CellEnd, pair.Second.CellStart);
        }

        Assert.Equal((uint)0, map.Entries[0].ByteStart);
        Assert.Equal((uint)0, map.Entries[0].CellStart);
        Assert.Equal((uint)map.TotalBytes, map.Entries[^1].ByteEnd);
        Assert.Equal((uint)map.TotalCells, map.Entries[^1].CellEnd);
    }

    [Fact]
    public void EmptyShapedRunProducesEmptyMap()
    {
        var map = ClusterMap.FromShapedRun(string.Empty, new ShapedRun([], 0));

        Assert.True(map.IsEmpty);
    }

    [Fact]
    public void ShapedLigatureUsesGlyphClusterBoundaries()
    {
        var run = new ShapedRun(
            [
                new ShapedGlyph(1, 0, 2, 0, 0, 0),
                new ShapedGlyph(2, 2, 1, 0, 0, 0),
                new ShapedGlyph(3, 3, 1, 0, 0, 0),
            ],
            4);
        var map = ClusterMap.FromShapedRun("file", run);

        Assert.Equal(3, map.ClusterCount);
        Assert.Equal(4, map.TotalCells);
        Assert.Equal(0, map.ByteToCell(0));
        Assert.Equal(0, map.ByteToCell(1));
        Assert.Equal(2, map.ByteToCell(2));
        Assert.Equal(0, map.CellToByte(1));
        Assert.Equal(2, map.CellToByte(2));
        Assert.Equal("fi", map.ExtractTextForCells("file", 0, 2));
    }

    [Fact]
    public void ShapedGlyphsSharingAClusterAccumulateAndClampAdvance()
    {
        var run = new ShapedRun(
            [
                new ShapedGlyph(1, 0, 200, 0, 0, 0),
                new ShapedGlyph(2, 0, 100, 0, 0, 0),
                new ShapedGlyph(3, 1, -2, 0, 0, 0),
            ],
            298);
        var map = ClusterMap.FromShapedRun("ab", run);

        Assert.Equal((byte)255, map.Entries[0].CellWidth);
        Assert.Equal((byte)2, map.Entries[1].CellWidth);
    }

    private static ClusterEntry AssertEntry(ClusterEntry? entry)
    {
        Assert.True(entry.HasValue);
        return entry.GetValueOrDefault();
    }
}
