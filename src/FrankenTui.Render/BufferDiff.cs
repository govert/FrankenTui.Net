namespace FrankenTui.Render;

public sealed class BufferDiff
{
    private readonly List<CellPosition> _changes = [];

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
        diff.ComputeInto(oldBuffer, newBuffer);
        return diff;
    }

    public static BufferDiff ComputeDirty(Buffer oldBuffer, Buffer newBuffer)
    {
        var diff = new BufferDiff();
        diff.ComputeDirtyInto(oldBuffer, newBuffer);
        return diff;
    }

    public void ComputeInto(Buffer oldBuffer, Buffer newBuffer)
    {
        EnsureCompatible(oldBuffer, newBuffer);
        _changes.Clear();

        for (ushort y = 0; y < newBuffer.Height; y++)
        {
            var oldRow = oldBuffer.GetRow(y);
            var newRow = newBuffer.GetRow(y);
            if (oldRow.SequenceEqual(newRow))
            {
                continue;
            }

            for (ushort x = 0; x < newBuffer.Width; x++)
            {
                if (!oldRow[x].BitsEqual(newRow[x]))
                {
                    _changes.Add(new CellPosition(x, y));
                }
            }
        }
    }

    public void ComputeDirtyInto(Buffer oldBuffer, Buffer newBuffer)
    {
        EnsureCompatible(oldBuffer, newBuffer);
        _changes.Clear();

        for (ushort y = 0; y < newBuffer.Height; y++)
        {
            if (!newBuffer.IsRowDirty(y))
            {
                continue;
            }

            var oldRow = oldBuffer.GetRow(y);
            var newRow = newBuffer.GetRow(y);
            if (oldRow.SequenceEqual(newRow))
            {
                continue;
            }

            for (ushort x = 0; x < newBuffer.Width; x++)
            {
                if (!oldRow[x].BitsEqual(newRow[x]))
                {
                    _changes.Add(new CellPosition(x, y));
                }
            }
        }
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

    private static void EnsureCompatible(Buffer oldBuffer, Buffer newBuffer)
    {
        if (oldBuffer.Width != newBuffer.Width || oldBuffer.Height != newBuffer.Height)
        {
            throw new ArgumentException("Buffers must have identical dimensions.");
        }
    }
}
