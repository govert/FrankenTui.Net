// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/grapheme_pool.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using System.Diagnostics;

namespace FrankenTui.Render;

/// <summary>Reference-counted, generation-safe interning for complex grapheme clusters.</summary>
public sealed class GraphemePool
{
    private sealed class Slot(string text, byte width, uint refCount)
    {
        public string Text { get; } = text;
        public byte Width { get; } = width;
        public uint RefCount { get; set; } = refCount;
        public Slot Clone() => new(Text, Width, RefCount);
    }

    private readonly List<Slot?> _slots;
    private readonly List<ushort> _generations;
    private readonly Dictionary<string, GraphemeId> _lookup;
    private readonly List<int> _freeList;

    public GraphemePool() : this(0) { }

    private GraphemePool(int capacity)
    {
        _slots = new List<Slot?>(capacity);
        _generations = new List<ushort>(capacity);
        _lookup = new Dictionary<string, GraphemeId>(capacity, StringComparer.Ordinal);
        _freeList = [];
    }

    private GraphemePool(GraphemePool source)
    {
        _slots = new List<Slot?>(source._slots.Count);
        _slots.AddRange(source._slots.Select(slot => slot?.Clone()));
        _generations = [.. source._generations];
        _lookup = new Dictionary<string, GraphemeId>(source._lookup, StringComparer.Ordinal);
        _freeList = [.. source._freeList];
    }

    public static GraphemePool WithCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        return new GraphemePool(capacity);
    }

    public int Count => _slots.Count - _freeList.Count;
    public bool IsEmpty => Count == 0;
    public int Capacity => _slots.Capacity;

    public GraphemeId Intern(string text, byte width)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (width > GraphemeId.MaxWidth)
            throw new ArgumentOutOfRangeException(nameof(width), "width overflow");

        if (_lookup.TryGetValue(text, out var existing))
        {
            Debug.Assert(existing.Generation == _generations[existing.Slot]);
            Debug.Assert(existing.Width == width,
                $"intern called with a different width for {text}: existing={existing.Width}, new={width}");
            Retain(existing);
            return existing;
        }

        var slotIndex = AllocateSlot();
        ushort generation;
        if (slotIndex < _generations.Count)
        {
            generation = (ushort)((_generations[slotIndex] + 1) & GraphemeId.MaxGeneration);
            _generations[slotIndex] = generation;
        }
        else
        {
            generation = 0;
            _generations.Add(0);
        }

        var id = new GraphemeId((uint)slotIndex, generation, width);
        var slot = new Slot(text, width, 1);
        if (slotIndex < _slots.Count) _slots[slotIndex] = slot;
        else _slots.Add(slot);
        _lookup[text] = id;
        return id;
    }

    public string? Get(GraphemeId id) => IsCurrent(id) ? _slots[id.Slot]!.Text : null;

    public void Retain(GraphemeId id)
    {
        if (!IsCurrent(id)) return;
        var slot = _slots[id.Slot]!;
        slot.RefCount = slot.RefCount == uint.MaxValue ? uint.MaxValue : slot.RefCount + 1;
    }

    public void Release(GraphemeId id)
    {
        if (!IsCurrent(id)) return;
        var slot = _slots[id.Slot]!;
        if (slot.RefCount == 0) return;
        slot.RefCount--;
        if (slot.RefCount != 0) return;
        _lookup.Remove(slot.Text);
        _slots[id.Slot] = null;
        _freeList.Add(id.Slot);
    }

    public uint RefCount(GraphemeId id) => IsCurrent(id) ? _slots[id.Slot]!.RefCount : 0;

    /// <summary>Invalidate all current IDs while retaining slots for deterministic reuse.</summary>
    public void Clear()
    {
        _lookup.Clear();
        _freeList.Clear();
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i] = null;
            _generations[i] = (ushort)((_generations[i] + 1) & GraphemeId.MaxGeneration);
        }

        for (var i = _slots.Count - 1; i >= 0; i--) _freeList.Add(i);
    }

    /// <summary>Recompute references from the positive closed set of supplied buffers.</summary>
    public void Gc(params Buffer[] buffers) => Gc((IEnumerable<Buffer>)buffers);

    public void Gc(IEnumerable<Buffer> buffers)
    {
        ArgumentNullException.ThrowIfNull(buffers);
        foreach (var slot in _slots)
            if (slot is not null) slot.RefCount = 0;

        foreach (var buffer in buffers)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            foreach (var cell in buffer.Cells)
            {
                if (cell.Content.GraphemeId is not { } id || !IsCurrent(id)) continue;
                var slot = _slots[id.Slot]!;
                slot.RefCount = slot.RefCount == uint.MaxValue ? uint.MaxValue : slot.RefCount + 1;
            }
        }

        var deadKeys = new List<string>();
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot is null || slot.RefCount != 0) continue;
            deadKeys.Add(slot.Text);
            _slots[i] = null;
            _generations[i] = (ushort)((_generations[i] + 1) & GraphemeId.MaxGeneration);
            _freeList.Add(i);
        }

        foreach (var key in deadKeys) _lookup.Remove(key);
    }

    public GraphemePool Clone() => new(this);

    public override string ToString() =>
        $"GraphemePool {{ slots = {_slots.Count}, entries = {Count}, free = {_freeList.Count} }}";

    private int AllocateSlot()
    {
        if (_freeList.Count > 0)
        {
            var last = _freeList.Count - 1;
            var slot = _freeList[last];
            _freeList.RemoveAt(last);
            return slot;
        }

        if (_slots.Count > GraphemeId.MaxSlot)
            throw new InvalidOperationException("grapheme pool capacity exceeded");
        return _slots.Count;
    }

    private bool IsCurrent(GraphemeId id) =>
        id.Slot >= 0 && id.Slot < _slots.Count &&
        id.Slot < _generations.Count &&
        _generations[id.Slot] == id.Generation &&
        _slots[id.Slot] is not null;
}
