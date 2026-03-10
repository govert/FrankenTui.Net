using System.Numerics;
using System.Runtime.InteropServices;
using FrankenTui.Render;

namespace FrankenTui.Simd;

internal sealed class SimdBufferDiffAccelerator : IBufferDiffAccelerator
{
    private const int UlongsPerCell = 2;

    public bool TryAppendRowChanges(
        ReadOnlySpan<Cell> oldRow,
        ReadOnlySpan<Cell> newRow,
        ushort row,
        List<CellPosition> changes)
    {
        if (oldRow.Length != newRow.Length)
        {
            return false;
        }

        var oldBits = MemoryMarshal.Cast<Cell, ulong>(oldRow);
        var newBits = MemoryMarshal.Cast<Cell, ulong>(newRow);
        if (oldBits.SequenceEqual(newBits))
        {
            return true;
        }

        var cellsPerVector = Math.Max(1, Vector<ulong>.Count / UlongsPerCell);
        var chunkUlongs = cellsPerVector * UlongsPerCell;
        var cellIndex = 0;
        var ulongIndex = 0;
        if (Vector.IsHardwareAccelerated && chunkUlongs > 0 && oldBits.Length >= chunkUlongs)
        {
            while (ulongIndex <= oldBits.Length - chunkUlongs)
            {
                var left = new Vector<ulong>(oldBits.Slice(ulongIndex, chunkUlongs));
                var right = new Vector<ulong>(newBits.Slice(ulongIndex, chunkUlongs));
                if (!Vector.EqualsAll(left, right))
                {
                    AppendChunk(oldBits, newBits, row, changes, cellIndex, cellsPerVector);
                }

                ulongIndex += chunkUlongs;
                cellIndex += cellsPerVector;
            }
        }

        for (; cellIndex < oldRow.Length; cellIndex++)
        {
            var bitIndex = cellIndex * UlongsPerCell;
            if (oldBits[bitIndex] != newBits[bitIndex] || oldBits[bitIndex + 1] != newBits[bitIndex + 1])
            {
                changes.Add(new CellPosition((ushort)cellIndex, row));
            }
        }

        return true;
    }

    private static void AppendChunk(
        ReadOnlySpan<ulong> oldBits,
        ReadOnlySpan<ulong> newBits,
        ushort row,
        List<CellPosition> changes,
        int cellStart,
        int count)
    {
        var end = cellStart + count;
        for (var cellIndex = cellStart; cellIndex < end; cellIndex++)
        {
            var bitIndex = cellIndex * UlongsPerCell;
            if (oldBits[bitIndex] != newBits[bitIndex] || oldBits[bitIndex + 1] != newBits[bitIndex + 1])
            {
                changes.Add(new CellPosition((ushort)cellIndex, row));
            }
        }
    }
}
