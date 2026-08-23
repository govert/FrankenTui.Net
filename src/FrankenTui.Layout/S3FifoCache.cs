// SPDX-License-Identifier: Apache-2.0
// Internal specialization of .external/frankentui/crates/ftui-core/src/s3_fifo.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust's slab Vec<Option<Entry>> and VecDeque queues map to a managed
// List<Entry?> slab, free-index stack, and LinkedList queues. Queue order, admission,
// promotion, ghost readmission, capped frequency, and eviction semantics are preserved.
// This dependency remains internal because the bounded port unit is ftui-layout/cache.rs.

namespace FrankenTui.Layout;

internal sealed class S3FifoCache<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Location> _index;
    private readonly List<Entry?> _entries;
    private readonly Stack<int> _freeIndices = [];
    private readonly LinkedList<int> _small = [];
    private readonly LinkedList<int> _main = [];
    private readonly LinkedList<TKey> _ghost = [];
    private readonly int _smallCapacity;
    private readonly int _mainCapacity;
    private readonly int _ghostCapacity;

    public S3FifoCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        int actualCapacity = Math.Max(capacity, 2);
        _smallCapacity = Math.Max(actualCapacity / 10, 1);
        _mainCapacity = actualCapacity - _smallCapacity;
        _ghostCapacity = _smallCapacity;
        _index = new Dictionary<TKey, Location>(actualCapacity);
        _entries = new List<Entry?>(actualCapacity);
    }

    public int Count => _index.Count;

    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (!_index.TryGetValue(key, out Location location))
        {
            value = default;
            return false;
        }

        Entry entry = RequiredEntry(location.Index);
        entry.Frequency = (byte)Math.Min(entry.Frequency + 1, 3);
        value = entry.Value;
        return true;
    }

    public TValue? Insert(TKey key, TValue value)
    {
        if (_index.TryGetValue(key, out Location location))
        {
            Entry entry = RequiredEntry(location.Index);
            TValue previous = entry.Value;
            entry.Value = value;
            entry.Frequency = (byte)Math.Min(entry.Frequency + 1, 3);
            return previous;
        }

        bool inGhost = RemoveFromGhost(key);
        if (inGhost)
        {
            EvictMainIfFull();
            int index = AllocateEntry(key, value);
            _main.AddLast(index);
            _index[key] = new Location(index, IsMain: true);
        }
        else
        {
            EvictSmallIfFull();
            int index = AllocateEntry(key, value);
            _small.AddLast(index);
            _index[key] = new Location(index, IsMain: false);
        }

        return default;
    }

    public bool Remove(TKey key)
    {
        if (!_index.Remove(key, out Location location))
            return false;

        if (location.IsMain)
            _main.Remove(location.Index);
        else
            _small.Remove(location.Index);

        FreeEntry(location.Index);
        return true;
    }

    public void Clear()
    {
        _index.Clear();
        _entries.Clear();
        _freeIndices.Clear();
        _small.Clear();
        _main.Clear();
        _ghost.Clear();
    }

    private int AllocateEntry(TKey key, TValue value)
    {
        var entry = new Entry(key, value);
        if (_freeIndices.TryPop(out int freeIndex))
        {
            _entries[freeIndex] = entry;
            return freeIndex;
        }

        int index = _entries.Count;
        _entries.Add(entry);
        return index;
    }

    private void FreeEntry(int index)
    {
        _entries[index] = null;
        _freeIndices.Push(index);
    }

    private bool RemoveFromGhost(TKey key)
    {
        EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
        LinkedListNode<TKey>? current = _ghost.First;
        while (current is not null)
        {
            LinkedListNode<TKey>? next = current.Next;
            if (comparer.Equals(current.Value, key))
            {
                _ghost.Remove(current);
                return true;
            }

            current = next;
        }

        return false;
    }

    private void EvictSmallIfFull()
    {
        while (_small.Count >= _smallCapacity)
        {
            int index = _small.First!.Value;
            _small.RemoveFirst();
            Entry entry = RequiredEntry(index);

            if (entry.Frequency > 0)
            {
                entry.Frequency = 0;
                EvictMainIfFull();
                _index[entry.Key] = new Location(index, IsMain: true);
                _main.AddLast(index);
            }
            else
            {
                _entries[index] = null;
                _freeIndices.Push(index);
                _index.Remove(entry.Key);

                if (_ghost.Count >= _ghostCapacity)
                    _ghost.RemoveFirst();
                _ghost.AddLast(entry.Key);
            }
        }
    }

    private void EvictMainIfFull()
    {
        while (_main.Count >= _mainCapacity)
        {
            int index = _main.First!.Value;
            _main.RemoveFirst();
            Entry entry = RequiredEntry(index);

            if (entry.Frequency > 0)
            {
                entry.Frequency--;
                _main.AddLast(index);
            }
            else
            {
                _entries[index] = null;
                _freeIndices.Push(index);
                _index.Remove(entry.Key);
            }
        }
    }

    private Entry RequiredEntry(int index) =>
        _entries[index] ?? throw new InvalidOperationException(
            "S3-FIFO invariant violated: indexed slab entry must be occupied.");

    private sealed class Entry(TKey key, TValue value)
    {
        public TKey Key { get; } = key;

        public TValue Value { get; set; } = value;

        public byte Frequency { get; set; }
    }

    private readonly record struct Location(int Index, bool IsMain);
}
