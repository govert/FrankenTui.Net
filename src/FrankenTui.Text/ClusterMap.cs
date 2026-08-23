// Upstream source: crates/ftui-text/src/cluster_map.rs
// Source basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Direct port. Byte coordinates intentionally remain UTF-8 byte offsets, as
// in Rust; they are not .NET UTF-16 string indices.

using System.Collections.ObjectModel;
using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Text;

/// <summary>A single grapheme or shaped-glyph cluster in a <see cref="ClusterMap"/>.</summary>
public readonly record struct ClusterEntry(
    uint ByteStart,
    uint ByteEnd,
    uint GraphemeIndex,
    uint CellStart,
    byte CellWidth)
{
    public Range ByteRange => (int)ByteStart..(int)ByteEnd;

    public Range CellRange => (int)CellStart..(int)(CellStart + CellWidth);

    public uint CellEnd => CellStart + CellWidth;
}

/// <summary>
/// Precomputed bidirectional mapping between UTF-8 source-byte offsets,
/// grapheme indices, and terminal cell columns.
/// </summary>
public sealed class ClusterMap
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    private readonly ClusterEntry[] _entries;
    private readonly ReadOnlyCollection<ClusterEntry> _entriesView;
    private readonly uint _totalCells;
    private readonly uint _totalBytes;

    private ClusterMap(ClusterEntry[] entries, uint totalCells, uint totalBytes)
    {
        _entries = entries;
        _entriesView = Array.AsReadOnly(entries);
        _totalCells = totalCells;
        _totalBytes = totalBytes;
    }

    /// <summary>Build a terminal/monospace map from extended grapheme clusters.</summary>
    public static ClusterMap FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return new ClusterMap([], 0, 0);
        }

        var entries = new List<ClusterEntry>(text.Length);
        uint byteOffset = 0;
        uint cellOffset = 0;
        uint graphemeIndex = 0;

        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(text))
        {
            var byteWidth = checked((uint)Utf8.GetByteCount(grapheme));
            var cellWidth = checked((byte)TerminalTextWidth.TextElementWidth(grapheme));

            entries.Add(new ClusterEntry(
                byteOffset,
                checked(byteOffset + byteWidth),
                graphemeIndex,
                cellOffset,
                cellWidth));

            byteOffset = checked(byteOffset + byteWidth);
            cellOffset = checked(cellOffset + cellWidth);
            graphemeIndex++;
        }

        return new ClusterMap(entries.ToArray(), cellOffset, byteOffset);
    }

    /// <summary>Build a map from glyph cluster byte offsets and advances.</summary>
    public static ClusterMap FromShapedRun(string text, ShapedRun run)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(run);
        if (text.Length == 0 || run.IsEmpty)
        {
            return new ClusterMap([], 0, 0);
        }

        var entries = new List<ClusterEntry>();
        uint cellOffset = 0;
        uint graphemeIndex = 0;
        var totalBytes = checked((uint)Utf8.GetByteCount(text));

        var i = 0;
        while (i < run.Glyphs.Count)
        {
            var clusterByte = run.Glyphs[i].Cluster;
            var clusterAdvance = 0;
            var j = i;
            while (j < run.Glyphs.Count && run.Glyphs[j].Cluster == clusterByte)
            {
                clusterAdvance = unchecked(clusterAdvance + run.Glyphs[j].XAdvance);
                j++;
            }

            var nextByte = j < run.Glyphs.Count ? run.Glyphs[j].Cluster : totalBytes;
            var absoluteAdvance = (ulong)Math.Abs((long)clusterAdvance);
            var width = (byte)Math.Min(absoluteAdvance, byte.MaxValue);

            entries.Add(new ClusterEntry(
                clusterByte,
                nextByte,
                graphemeIndex,
                cellOffset,
                width));

            cellOffset = checked(cellOffset + width);
            graphemeIndex++;
            i = j;
        }

        return new ClusterMap(entries.ToArray(), cellOffset, totalBytes);
    }

    public IReadOnlyList<ClusterEntry> Entries => _entriesView;

    public int TotalCells => checked((int)_totalCells);

    public int TotalBytes => checked((int)_totalBytes);

    public int ClusterCount => _entries.Length;

    public bool IsEmpty => _entries.Length == 0;

    public ClusterEntry? Get(int graphemeIndex) =>
        (uint)graphemeIndex < (uint)_entries.Length ? _entries[graphemeIndex] : null;

    /// <summary>Map a UTF-8 byte offset to its owning cluster's start cell.</summary>
    public int ByteToCell(int byteOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteOffset);
        if (_entries.Length == 0 || (uint)byteOffset >= _totalBytes)
        {
            return TotalCells;
        }

        var index = LowerBoundByteStart((uint)byteOffset);
        if (index < _entries.Length && _entries[index].ByteStart == (uint)byteOffset)
        {
            return checked((int)_entries[index].CellStart);
        }

        return index > 0 ? checked((int)_entries[index - 1].CellStart) : 0;
    }

    /// <summary>Find the cluster containing a UTF-8 byte offset.</summary>
    public ClusterEntry? ByteToEntry(int byteOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteOffset);
        if (_entries.Length == 0)
        {
            return null;
        }

        var offset = (uint)byteOffset;
        var index = LowerBoundByteStart(offset);
        if (index < _entries.Length && _entries[index].ByteStart == offset)
        {
            return _entries[index];
        }

        return index > 0 && offset < _entries[index - 1].ByteEnd
            ? _entries[index - 1]
            : null;
    }

    public (int Start, int End) ByteRangeToCellRange(int byteStart, int byteEnd)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteStart);
        ArgumentOutOfRangeException.ThrowIfNegative(byteEnd);
        if (_entries.Length == 0 || byteStart >= byteEnd)
        {
            return (0, 0);
        }

        var startCell = ByteToCell(byteStart);
        int endCell;
        if ((uint)byteEnd >= _totalBytes)
        {
            endCell = TotalCells;
        }
        else
        {
            var index = LowerBoundByteStart((uint)byteEnd);
            endCell = index < _entries.Length && _entries[index].ByteStart == (uint)byteEnd
                ? checked((int)_entries[index].CellStart)
                : index > 0
                    ? checked((int)_entries[index - 1].CellEnd)
                    : 0;
        }

        return (startCell, endCell);
    }

    /// <summary>
    /// Map a cell column to a UTF-8 byte offset. Duplicate cell starts caused
    /// by zero-width clusters resolve to the first owning cluster.
    /// </summary>
    public int CellToByte(int cellColumn)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellColumn);
        if (_entries.Length == 0 || (uint)cellColumn >= _totalCells)
        {
            return TotalBytes;
        }

        var index = LowerBoundCellStart((uint)cellColumn);
        return index < _entries.Length && _entries[index].CellStart == (uint)cellColumn
            ? checked((int)_entries[index].ByteStart)
            : checked((int)_entries[index - 1].ByteStart);
    }

    public ClusterEntry? CellToEntry(int cellColumn)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellColumn);
        if (_entries.Length == 0 || (uint)cellColumn >= _totalCells)
        {
            return null;
        }

        var column = (uint)cellColumn;
        var index = LowerBoundCellStart(column);
        if (index < _entries.Length && _entries[index].CellStart == column)
        {
            return _entries[index];
        }

        var entry = _entries[index - 1];
        return column < entry.CellEnd ? entry : null;
    }

    public (int Start, int End) CellRangeToByteRange(int cellStart, int cellEnd)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellStart);
        ArgumentOutOfRangeException.ThrowIfNegative(cellEnd);
        if (_entries.Length == 0 || cellStart >= cellEnd)
        {
            return (0, 0);
        }

        var startByte = CellToByte(cellStart);
        int endByte;
        if ((uint)cellEnd >= _totalCells)
        {
            endByte = TotalBytes;
        }
        else
        {
            endByte = CellToEntry(cellEnd - 1) is { } entry
                ? checked((int)entry.ByteEnd)
                : TotalBytes;
        }

        return (startByte, Math.Max(endByte, startByte));
    }

    public int GraphemeToCell(int graphemeIndex) =>
        Get(graphemeIndex) is { } entry ? checked((int)entry.CellStart) : TotalCells;

    public int CellToGrapheme(int cellColumn) =>
        CellToEntry(cellColumn) is { } entry ? checked((int)entry.GraphemeIndex) : _entries.Length;

    public int GraphemeToByte(int graphemeIndex) =>
        Get(graphemeIndex) is { } entry ? checked((int)entry.ByteStart) : TotalBytes;

    public int ByteToGrapheme(int byteOffset) =>
        ByteToEntry(byteOffset) is { } entry ? checked((int)entry.GraphemeIndex) : _entries.Length;

    public string ExtractTextForCells(string source, int cellStart, int cellEnd)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (byteStart, byteEnd) = CellRangeToByteRange(cellStart, cellEnd);
        var bytes = Utf8.GetBytes(source);
        if (byteStart >= bytes.Length)
        {
            return string.Empty;
        }

        var end = Math.Min(byteEnd, bytes.Length);
        return Utf8.GetString(bytes.AsSpan(byteStart, end - byteStart));
    }

    private int LowerBoundByteStart(uint byteOffset)
    {
        var low = 0;
        var high = _entries.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (_entries[middle].ByteStart < byteOffset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private int LowerBoundCellStart(uint cellColumn)
    {
        var low = 0;
        var high = _entries.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (_entries[middle].CellStart < cellColumn)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
