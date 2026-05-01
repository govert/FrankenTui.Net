namespace FrankenTui.Render;

public sealed class GraphemeRegistry
{
    private readonly List<GraphemeEntry?> _entries = [];
    private readonly List<ushort> _generations = [];
    private readonly Dictionary<string, GraphemeId> _lookup = new(StringComparer.Ordinal);
    private readonly List<int> _freeList = [];

    public GraphemeId Intern(string text, byte width)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (width > GraphemeId.MaxWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (_lookup.TryGetValue(text, out var existing))
        {
            Retain(existing);
            return existing;
        }

        var slot = AllocateSlot();
        ushort generation;
        if (slot < _generations.Count)
        {
            _generations[slot] = (ushort)((_generations[slot] + 1) & GraphemeId.MaxGeneration);
            generation = _generations[slot];
        }
        else
        {
            generation = 0;
            _generations.Add(0);
        }

        var id = new GraphemeId((uint)slot, generation, width);
        var entry = new GraphemeEntry(text, width, 1);
        if (slot < _entries.Count)
        {
            _entries[slot] = entry;
        }
        else
        {
            _entries.Add(entry);
        }

        _lookup[text] = id;
        return id;
    }

    public string? Resolve(GraphemeId id)
    {
        if (!IsGenerationMatch(id))
        {
            return null;
        }

        return _entries[id.Slot]?.Text;
    }

    public void Retain(GraphemeId id)
    {
        if (!IsGenerationMatch(id))
        {
            return;
        }

        var entry = _entries[id.Slot];
        if (entry is null)
        {
            return;
        }

        entry.RefCount = entry.RefCount == uint.MaxValue ? uint.MaxValue : entry.RefCount + 1;
    }

    public void Release(GraphemeId id)
    {
        if (!IsGenerationMatch(id))
        {
            return;
        }

        var entry = _entries[id.Slot];
        if (entry is null || entry.RefCount == 0)
        {
            return;
        }

        entry.RefCount--;
        if (entry.RefCount != 0)
        {
            return;
        }

        _lookup.Remove(entry.Text);
        _entries[id.Slot] = null;
        _freeList.Add(id.Slot);
    }

    public void Clear()
    {
        _entries.Clear();
        _generations.Clear();
        _lookup.Clear();
        _freeList.Clear();
    }

    private int AllocateSlot()
    {
        if (_freeList.Count == 0)
        {
            if (_entries.Count > GraphemeId.MaxSlot)
            {
                throw new InvalidOperationException("Grapheme registry capacity exceeded.");
            }

            return _entries.Count;
        }

        var lastIndex = _freeList.Count - 1;
        var slot = _freeList[lastIndex];
        _freeList.RemoveAt(lastIndex);
        return slot;
    }

    private bool IsGenerationMatch(GraphemeId id) =>
        id.Slot < _generations.Count &&
        _generations[id.Slot] == id.Generation &&
        id.Slot < _entries.Count;

    private sealed class GraphemeEntry(string text, byte width, uint refCount)
    {
        public string Text { get; } = text;

        public byte Width { get; } = width;

        public uint RefCount { get; set; } = refCount;
    }
}
