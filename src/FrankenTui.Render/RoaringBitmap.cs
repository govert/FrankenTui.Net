// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/roaring_bitmap.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using System.Collections;
using System.Numerics;

namespace FrankenTui.Render;

/// <summary>
/// Minimal Roaring bitmap for cell-level dirty-region tracking. Sparse
/// high-16-bit partitions promote from sorted arrays to 65,536-bit bitmaps.
/// </summary>
public sealed class RoaringBitmap : IEnumerable<uint>
{
    internal const int ArrayToBitmapThreshold = 4096;

    private readonly List<ContainerEntry> _containers;

    public RoaringBitmap()
    {
        _containers = [];
    }

    private RoaringBitmap(List<ContainerEntry> containers)
    {
        _containers = containers;
    }

    /// <summary>Inserts a value and returns true only when it was new.</summary>
    public bool Insert(uint value)
    {
        var key = (ushort)(value >> 16);
        var low = (ushort)value;
        var index = FindContainerIndex(key);
        if (index >= 0)
        {
            return _containers[index].Insert(low);
        }

        var entry = new ContainerEntry(key, new ArrayContainer());
        entry.Insert(low);
        _containers.Insert(~index, entry);
        return true;
    }

    public bool Contains(uint value)
    {
        var index = FindContainerIndex((ushort)(value >> 16));
        return index >= 0 && _containers[index].Container.Contains((ushort)value);
    }

    public long Cardinality
    {
        get
        {
            long count = 0;
            foreach (var entry in _containers)
            {
                count += entry.Container.Cardinality;
            }

            return count;
        }
    }

    public bool IsEmpty
    {
        get
        {
            foreach (var entry in _containers)
            {
                if (entry.Container.Cardinality != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void Clear() => _containers.Clear();

    public RoaringBitmap Clone()
    {
        var containers = new List<ContainerEntry>(_containers.Count);
        foreach (var entry in _containers)
        {
            containers.Add(new ContainerEntry(entry.Key, entry.Container.Clone()));
        }

        return new RoaringBitmap(containers);
    }

    public RoaringBitmap Union(RoaringBitmap other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = Clone();
        foreach (var value in other)
        {
            result.Insert(value);
        }

        return result;
    }

    public RoaringBitmap Intersection(RoaringBitmap other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = new RoaringBitmap();
        var smaller = Cardinality <= other.Cardinality ? this : other;
        var larger = ReferenceEquals(smaller, this) ? other : this;
        foreach (var value in smaller)
        {
            if (larger.Contains(value))
            {
                result.Insert(value);
            }
        }

        return result;
    }

    /// <summary>Inserts the half-open range [start, end).</summary>
    public void InsertRange(uint start, uint end)
    {
        for (var value = start; value < end; value++)
        {
            Insert(value);
        }
    }

    public IEnumerator<uint> GetEnumerator()
    {
        foreach (var entry in _containers)
        {
            foreach (var low in entry.Container.Values())
            {
                yield return ((uint)entry.Key << 16) | low;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal bool IsBitmapContainerFor(ushort key)
    {
        var index = FindContainerIndex(key);
        return index >= 0 && _containers[index].Container is BitmapContainer;
    }

    private int FindContainerIndex(ushort key)
    {
        var low = 0;
        var high = _containers.Count - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var comparison = _containers[middle].Key.CompareTo(key);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return ~low;
    }

    private sealed class ContainerEntry(ushort key, Container container)
    {
        public ushort Key { get; } = key;

        public Container Container { get; set; } = container;

        public bool Insert(ushort value)
        {
            var inserted = Container.Insert(value);
            if (Container is ArrayContainer array &&
                array.Cardinality >= ArrayToBitmapThreshold)
            {
                Container = array.ToBitmap();
            }

            return inserted;
        }
    }

    private abstract class Container
    {
        public abstract int Cardinality { get; }

        public abstract bool Contains(ushort value);

        public abstract IEnumerable<ushort> Values();

        public abstract Container Clone();

        public bool Insert(ushort value)
        {
            var inserted = InsertCore(value);
            return inserted;
        }

        protected abstract bool InsertCore(ushort value);
    }

    private sealed class ArrayContainer : Container
    {
        private readonly List<ushort> _values;

        public ArrayContainer()
        {
            _values = [];
        }

        private ArrayContainer(List<ushort> values)
        {
            _values = values;
        }

        public override int Cardinality => _values.Count;

        public override bool Contains(ushort value) => _values.BinarySearch(value) >= 0;

        public override IEnumerable<ushort> Values() => _values;

        public override Container Clone() => new ArrayContainer([.. _values]);

        protected override bool InsertCore(ushort value)
        {
            var index = _values.BinarySearch(value);
            if (index >= 0)
            {
                return false;
            }

            _values.Insert(~index, value);
            return true;
        }

        public BitmapContainer ToBitmap()
        {
            var bitmap = new BitmapContainer();
            foreach (var value in _values)
            {
                bitmap.Insert(value);
            }

            return bitmap;
        }
    }

    private sealed class BitmapContainer : Container
    {
        private readonly ulong[] _words;
        private int _count;

        public BitmapContainer()
        {
            _words = new ulong[1024];
        }

        private BitmapContainer(ulong[] words, int count)
        {
            _words = words;
            _count = count;
        }

        public override int Cardinality => _count;

        public override bool Contains(ushort value)
        {
            var wordIndex = value >> 6;
            var bit = 1UL << (value & 63);
            return (_words[wordIndex] & bit) != 0;
        }

        public override IEnumerable<ushort> Values()
        {
            for (var wordIndex = 0; wordIndex < _words.Length; wordIndex++)
            {
                var word = _words[wordIndex];
                while (word != 0)
                {
                    var bit = BitOperations.TrailingZeroCount(word);
                    yield return (ushort)((wordIndex * 64) + bit);
                    word &= word - 1;
                }
            }
        }

        public override Container Clone() => new BitmapContainer((ulong[])_words.Clone(), _count);

        protected override bool InsertCore(ushort value)
        {
            var wordIndex = value >> 6;
            var bit = 1UL << (value & 63);
            if ((_words[wordIndex] & bit) != 0)
            {
                return false;
            }

            _words[wordIndex] |= bit;
            _count++;
            return true;
        }
    }
}
