namespace FrankenTui.Render;

public enum DiffSkipHintKind
{
    FullDiff,
    SkipDiff,
    NarrowToRows
}

public sealed class DiffSkipHint
{
    private DiffSkipHint(DiffSkipHintKind kind, IReadOnlyList<ushort>? rows = null)
    {
        Kind = kind;
        Rows = rows ?? Array.Empty<ushort>();
    }

    public DiffSkipHintKind Kind { get; }

    public IReadOnlyList<ushort> Rows { get; }

    public bool SkipsWork => Kind is not DiffSkipHintKind.FullDiff;

    public string Label => Kind switch
    {
        DiffSkipHintKind.FullDiff => "full-diff",
        DiffSkipHintKind.SkipDiff => "skip-diff",
        _ => "narrow-to-rows"
    };

    public static DiffSkipHint FullDiff { get; } = new(DiffSkipHintKind.FullDiff);

    public static DiffSkipHint SkipDiff { get; } = new(DiffSkipHintKind.SkipDiff);

    public static DiffSkipHint NarrowToRows(IEnumerable<ushort> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new DiffSkipHint(DiffSkipHintKind.NarrowToRows, rows.Distinct().Order().ToArray());
    }
}

public sealed class BufferDiff
{
    private readonly List<CellPosition> _changes = [];

    public static IBufferDiffAccelerator? Accelerator { get; set; }

    public IReadOnlyList<CellPosition> Changes => _changes;

    public int Count => _changes.Count;

    public bool IsEmpty => _changes.Count == 0;

    public static BufferDiff Full(ushort width, ushort height)
    {
        var diff = new BufferDiff();
        for (ushort y = 0; y < height; y++)
        {
            for (ushort x = 0; x < width; x++)
            {
                diff._changes.Add(new CellPosition(x, y));
            }
        }

        return diff;
    }

    public static BufferDiff Compute(Buffer oldBuffer, Buffer newBuffer)
    {
        var diff = new BufferDiff();
        diff.ComputeRowsInto(oldBuffer, newBuffer, significantOnly: false, dirtyOnly: false);
        return diff;
    }

    public static BufferDiff ComputeDirty(Buffer oldBuffer, Buffer newBuffer)
    {
        var diff = new BufferDiff();
        diff.ComputeRowsInto(oldBuffer, newBuffer, significantOnly: false, dirtyOnly: true);
        return diff;
    }

    public static BufferDiff ComputeFull(Buffer oldBuffer, Buffer newBuffer)
    {
        var diff = new BufferDiff();
        diff.ComputeFullInto(oldBuffer, newBuffer);
        return diff;
    }

    public static BufferDiff ComputeCertified(Buffer oldBuffer, Buffer newBuffer, DiffSkipHint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);

        var diff = new BufferDiff();
        diff.ComputeCertifiedInto(oldBuffer, newBuffer, hint);
        return diff;
    }

    public static BufferDiff ComputeSignificantDirty(Buffer oldBuffer, Buffer newBuffer)
    {
        var diff = new BufferDiff();
        diff.ComputeRowsInto(oldBuffer, newBuffer, significantOnly: true, dirtyOnly: true);
        return diff;
    }

    public void ComputeInto(Buffer oldBuffer, Buffer newBuffer)
    {
        ComputeRowsInto(oldBuffer, newBuffer, significantOnly: false, dirtyOnly: false);
    }

    public void ComputeDirtyInto(Buffer oldBuffer, Buffer newBuffer)
    {
        ComputeRowsInto(oldBuffer, newBuffer, significantOnly: false, dirtyOnly: true);
    }

    public void ComputeFullInto(Buffer oldBuffer, Buffer newBuffer)
    {
        EnsureCompatible(oldBuffer, newBuffer);
        _changes.Clear();

        for (ushort y = 0; y < newBuffer.Height; y++)
        {
            var oldRow = oldBuffer.GetRow(y);
            var newRow = newBuffer.GetRow(y);
            AppendFullRowChanges(oldRow, newRow, y, _changes);
        }
    }

    public void ComputeCertifiedInto(Buffer oldBuffer, Buffer newBuffer, DiffSkipHint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);
        EnsureCompatible(oldBuffer, newBuffer);
        _changes.Clear();

        switch (hint.Kind)
        {
            case DiffSkipHintKind.FullDiff:
                ComputeDirtyInto(oldBuffer, newBuffer);
                return;
            case DiffSkipHintKind.SkipDiff:
                return;
            case DiffSkipHintKind.NarrowToRows:
                foreach (var row in hint.Rows)
                {
                    if (row >= newBuffer.Height)
                    {
                        continue;
                    }

                    var oldRow = oldBuffer.GetRow(row);
                    var newRow = newBuffer.GetRow(row);
                    AppendRowChanges(oldRow, newRow, row, _changes);
                }

                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(hint));
        }
    }

    public static int CountDirtyRows(Buffer oldBuffer, Buffer newBuffer)
    {
        EnsureCompatible(oldBuffer, newBuffer);

        var count = 0;
        for (ushort y = 0; y < newBuffer.Height; y++)
        {
            if (!oldBuffer.GetRow(y).SequenceEqual(newBuffer.GetRow(y)))
            {
                count++;
            }
        }

        return count;
    }

    public static IReadOnlyList<ushort> CollectDirtyRows(Buffer oldBuffer, Buffer newBuffer)
    {
        EnsureCompatible(oldBuffer, newBuffer);

        var rows = new List<ushort>();
        for (ushort y = 0; y < newBuffer.Height; y++)
        {
            if (!oldBuffer.GetRow(y).SequenceEqual(newBuffer.GetRow(y)))
            {
                rows.Add(y);
            }
        }

        return rows;
    }

    public IReadOnlyList<ChangeRun> Runs()
    {
        if (_changes.Count == 0)
        {
            return Array.Empty<ChangeRun>();
        }

        var runs = new List<ChangeRun>(_changes.Count);
        var index = 0;
        while (index < _changes.Count)
        {
            var start = _changes[index];
            var x1 = start.X;
            index++;

            while (index < _changes.Count)
            {
                var next = _changes[index];
                if (next.Y != start.Y || next.X != x1 + 1)
                {
                    break;
                }

                x1 = next.X;
                index++;
            }

            runs.Add(new ChangeRun(start.Y, start.X, x1));
        }

        return runs;
    }

    public void Clear() => _changes.Clear();

    internal static void AppendRowChanges(
        ReadOnlySpan<Cell> oldRow,
        ReadOnlySpan<Cell> newRow,
        ushort y,
        List<CellPosition> changes)
    {
        if (oldRow.SequenceEqual(newRow))
        {
            return;
        }

        for (ushort x = 0; x < newRow.Length; x++)
        {
            if (!oldRow[x].BitsEqual(newRow[x]))
            {
                changes.Add(new CellPosition(x, y));
            }
        }
    }

    internal static void AppendSignificantRowChanges(
        ReadOnlySpan<Cell> oldRow,
        ReadOnlySpan<Cell> newRow,
        ushort y,
        List<CellPosition> changes)
    {
        if (RowsEqualBySignificance(oldRow, newRow))
        {
            return;
        }

        for (ushort x = 0; x < newRow.Length; x++)
        {
            if (!oldRow[x].SignificantEqual(newRow[x]))
            {
                changes.Add(new CellPosition(x, y));
            }
        }
    }

    internal static void AppendFullRowChanges(
        ReadOnlySpan<Cell> oldRow,
        ReadOnlySpan<Cell> newRow,
        ushort y,
        List<CellPosition> changes)
    {
        for (ushort x = 0; x < newRow.Length; x++)
        {
            if (!oldRow[x].BitsEqual(newRow[x]))
            {
                changes.Add(new CellPosition(x, y));
            }
        }
    }

    private void ComputeRowsInto(Buffer oldBuffer, Buffer newBuffer, bool significantOnly, bool dirtyOnly)
    {
        EnsureCompatible(oldBuffer, newBuffer);
        _changes.Clear();

        for (ushort y = 0; y < newBuffer.Height; y++)
        {
            if (dirtyOnly && oldBuffer.GetRow(y).SequenceEqual(newBuffer.GetRow(y)))
            {
                continue;
            }

            var oldRow = oldBuffer.GetRow(y);
            var newRow = newBuffer.GetRow(y);
            if (!significantOnly && Accelerator?.TryAppendRowChanges(oldRow, newRow, y, _changes) == true)
            {
                continue;
            }

            if (significantOnly)
            {
                AppendSignificantRowChanges(oldRow, newRow, y, _changes);
            }
            else
            {
                AppendRowChanges(oldRow, newRow, y, _changes);
            }
        }
    }

    private static bool RowsEqualBySignificance(ReadOnlySpan<Cell> oldRow, ReadOnlySpan<Cell> newRow)
    {
        for (var index = 0; index < newRow.Length; index++)
        {
            if (!oldRow[index].SignificantEqual(newRow[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureCompatible(Buffer oldBuffer, Buffer newBuffer)
    {
        if (oldBuffer.Width != newBuffer.Width || oldBuffer.Height != newBuffer.Height)
        {
            throw new ArgumentException("Buffers must have identical dimensions.");
        }
    }
}
