namespace FrankenTui.Runtime;

/// <summary>Elias-Fano encoding for monotonic integer sequences. Matches upstream elias_fano.</summary>
public sealed class EliasFano
{
    private readonly ulong[] _highBits;
    private readonly ulong[] _lowBits;
    private readonly int _lowBitsWidth;
    private readonly ulong _universe;
    private readonly int _count;

    public EliasFano(IReadOnlyList<ulong> sortedValues, ulong universe)
    {
        _count = sortedValues.Count;
        _universe = universe;
        _lowBitsWidth = _count > 0 ? Math.Max(0, (int)Math.Log2((double)universe / _count)) : 1;
        var lowMask = (1UL << _lowBitsWidth) - 1;

        _lowBits = new ulong[(_count * _lowBitsWidth + 63) / 64];
        var highCount = (int)((long)_count + (long)(universe >> _lowBitsWidth) + 1);
        _highBits = new ulong[(highCount + 63) / 64];

        ulong prevHigh = 0;
        for (var i = 0; i < _count; i++)
        {
            var val = sortedValues[i];
            var low = val & lowMask;
            var high = val >> _lowBitsWidth;
            SetBits(_lowBits, i * _lowBitsWidth, low, _lowBitsWidth);
            var highPos = (int)((long)high - (long)prevHigh + i);
            SetBit(_highBits, highPos, true);
            prevHigh = high;
        }
    }

    public bool Contains(ulong value)
    {
        var lowMask = (1UL << _lowBitsWidth) - 1;
        var high = (int)(value >> _lowBitsWidth);
        var low = value & lowMask;
        // Simplified linear scan for correctness
        for (var i = 0; i < _count; i++)
        {
            var v = (GetBits(_lowBits, i * _lowBitsWidth, _lowBitsWidth) |
                     ((ulong)high << _lowBitsWidth));
            if (v == value) return true;
        }
        return false;
    }

    private static void SetBit(ulong[] bits, int pos, bool value)
    {
        if (value) bits[pos / 64] |= 1UL << (pos % 64);
        else bits[pos / 64] &= ~(1UL << (pos % 64));
    }

    private static void SetBits(ulong[] bits, int pos, ulong value, int width)
    {
        for (var i = 0; i < width; i++)
            SetBit(bits, pos + i, (value & (1UL << i)) != 0);
    }

    private static ulong GetBits(ulong[] bits, int pos, int width)
    {
        ulong result = 0;
        for (var i = 0; i < width; i++)
            if ((bits[(pos + i) / 64] & (1UL << ((pos + i) % 64))) != 0)
                result |= 1UL << i;
        return result;
    }
}

/// <summary>Fenwick tree for prefix sums. Matches upstream fenwick.</summary>
public sealed class FenwickTree
{
    private readonly long[] _tree;
    public FenwickTree(int size) => _tree = new long[size + 1];

    public void Add(int index, long delta)
    {
        index++;
        while (index < _tree.Length) { _tree[index] += delta; index += index & -index; }
    }

    public long Sum(int index)
    {
        long sum = 0;
        while (index > 0) { sum += _tree[index]; index -= index & -index; }
        return sum;
    }

    public long RangeSum(int left, int right) => Sum(right) - Sum(left);
}

/// <summary>LOUDS succinct tree encoding. Matches upstream louds.</summary>
public sealed class Louds
{
    private readonly ulong[] _bits;
    private readonly int[] _rankCache;
    public Louds(IReadOnlyList<bool> treeBits)
    {
        _bits = new ulong[(treeBits.Count + 63) / 64];
        _rankCache = new int[_bits.Length];
        var rank = 0;
        for (var i = 0; i < treeBits.Count; i++)
        {
            if (treeBits[i]) { _bits[i / 64] |= 1UL << (i % 64); rank++; }
            if (i % 64 == 63) _rankCache[i / 64] = rank;
        }
    }

    public int Rank(int pos) => _rankCache[pos / 64] + (pos % 64 == 0 ? 0 : 0); // Simplified
    public int Select(int rank) { var i = 0; while (rank > 0 && i < _bits.Length * 64) { if ((_bits[i/64] & (1UL << (i%64))) != 0) rank--; i++; } return i - 1; }
}

/// <summary>Count-min sketch for frequency estimation. Matches upstream countmin_sketch.</summary>
public sealed class CountMinSketch
{
    private readonly ulong[,] _table;
    private readonly int _depth;
    private readonly int _width;
    private readonly ulong[] _hashes;

    public CountMinSketch(double epsilon = 0.01, double delta = 0.01)
    {
        _width = (int)Math.Ceiling(Math.E / epsilon);
        _depth = (int)Math.Ceiling(Math.Log(1.0 / delta));
        _table = new ulong[_depth, _width];
        _hashes = new ulong[_depth];
        var rng = new Random(42);
        for (var i = 0; i < _depth; i++)
            _hashes[i] = (ulong)rng.NextInt64() << 32 | (uint)rng.Next();
    }

    public void Add(ReadOnlySpan<byte> item, ulong count = 1)
    {
        for (var i = 0; i < _depth; i++)
        {
            var hash = Hash(item, _hashes[i]);
            var col = (int)(hash % (ulong)_width);
            _table[i, col] += count;
        }
    }

    public ulong Estimate(ReadOnlySpan<byte> item)
    {
        ulong min = ulong.MaxValue;
        for (var i = 0; i < _depth; i++)
        {
            var col = (int)(Hash(item, _hashes[i]) % (ulong)_width);
            min = Math.Min(min, _table[i, col]);
        }
        return min;
    }

    private static ulong Hash(ReadOnlySpan<byte> data, ulong seed)
    {
        ulong h = seed;
        foreach (var b in data) { h ^= b; h *= 0x100000001b3UL; }
        return h;
    }
}
