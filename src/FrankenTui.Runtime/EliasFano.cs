// Upstream source: crates/ftui-widgets/src/elias_fano.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of EliasFano encoded monotone integer sequence.
//
// Provides space-efficient representation of sorted non-decreasing sequences
// with O(1) access, rank, and select operations. Designed for height prefix
// sums in virtualized lists, enabling "which row is at pixel offset Y?"
// queries with ~10x less memory than a dense array.
//
// Encoding: Each value is split into low and high parts.
// Low bits: floor(log2(U/n)) bits per element, packed in a dense array.
// High bits: unary encoding in a bitvector with precomputed rank superblocks.

namespace FrankenTui.Runtime;

/// <summary>
/// Elias-Fano encoded monotone integer sequence.
/// O(1) access, rank, select, and nextGeq queries on sorted u64 values.
/// </summary>
public sealed class EliasFano
{
    private const int SuperblockWords = 8;

    private readonly ulong[] _lowBits;
    private readonly ulong[] _highBits;
    private readonly ulong[] _rankSuperblocks;
    private readonly int _count;
    private readonly uint _lowWidth;
    private readonly ulong _universe;

    private EliasFano(ulong[] lowBits, ulong[] highBits, ulong[] rankSuperblocks,
        int count, uint lowWidth, ulong universe)
    { _lowBits = lowBits; _highBits = highBits; _rankSuperblocks = rankSuperblocks; _count = count; _lowWidth = lowWidth; _universe = universe; }

    /// <summary>Encode a sorted non-decreasing sequence. Throws if not monotonic.</summary>
    public static EliasFano Encode(ulong[] values)
    {
        int count = values.Length;
        if (count == 0) return new EliasFano([], [], [0], 0, 0, 0);

        for (int i = 1; i < count; i++)
            if (values[i] < values[i - 1])
                throw new ArgumentException($"sequence must be non-decreasing: values[{i}]={values[i]} < values[{i - 1}]={values[i - 1]}");

        ulong universe = values[count - 1];
        uint lowWidth = 0;
        if (universe > 0 && count > 1) { ulong ratio = universe / (ulong)count; if (ratio > 0) lowWidth = (uint)FloorLog2(ratio); }
        ulong lowMask = lowWidth == 0 ? 0 : lowWidth >= 64 ? ulong.MaxValue : (1UL << (int)lowWidth) - 1;

        int lowWords = (int)DivCeil((ulong)count * lowWidth, 64);
        var lowBits = new ulong[lowWords];
        ulong maxHigh = universe >> (int)lowWidth;
        int highWords = (int)DivCeil((ulong)count + maxHigh + 1, 64);
        var highBits = new ulong[highWords];

        for (int i = 0; i < count; i++)
        {
            ulong value = values[i];
            if (lowWidth > 0) { SetBits(lowBits, (ulong)i * lowWidth, lowWidth, value & lowMask); }
            ulong high = value >> (int)lowWidth;
            ulong bitPosition = high + (ulong)i;
            highBits[bitPosition / 64] |= 1UL << (int)(bitPosition % 64);
        }

        int superblockCount = DivCeil(highWords, SuperblockWords);
        var rankSuperblocks = new ulong[superblockCount + 1];
        ulong cumulative = 0;
        for (int sb = 0; sb < superblockCount; sb++)
        {
            int end = Math.Min((sb + 1) * SuperblockWords, highWords);
            for (int w = sb * SuperblockWords; w < end; w++)
                cumulative += (ulong)System.Numerics.BitOperations.PopCount(highBits[w]);
            rankSuperblocks[sb + 1] = cumulative;
        }

        return new EliasFano(lowBits, highBits, rankSuperblocks, count, lowWidth, universe);
    }

    /// <summary>Access value at index. Reconstructs from low and high parts.</summary>
    public ulong Access(int index)
    {
        if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index), $"index {index} out of bounds (count={_count})");
        ulong low = _lowWidth > 0 ? GetBits(_lowBits, (ulong)index * _lowWidth, _lowWidth) : 0;
        int position = SelectOne(index);
        return ((ulong)position - (ulong)index << (int)_lowWidth) | low;
    }

    /// <summary>Number of elements &le; query.</summary>
    public int Rank(ulong query)
    {
        if (_count == 0 || query < Access(0)) return 0;
        if (query >= _universe) return _count;
        ulong high = query >> (int)_lowWidth;
        ulong low = _lowWidth > 0 ? query & ((1UL << (int)_lowWidth) - 1) : 0;
        int bucketStart = high == 0 ? 0 : SelectZero((int)high - 1) + 1;
        int bucketEnd = (int)high < _highBits.Length * 64 ? TrySelectZero((int)high) ?? _highBits.Length * 64 : _highBits.Length * 64;
        int baseRank = (int)RankOne(bucketStart);
        int count = baseRank;
        int position = bucketStart;
        while (position < bucketEnd)
        {
            int wi = position / 64; if (wi >= _highBits.Length) break;
            if ((_highBits[wi] >> (position % 64) & 1) == 1)
            {
                if (count >= _count) break;
                ulong elemLow = _lowWidth > 0 ? GetBits(_lowBits, (ulong)count * _lowWidth, _lowWidth) : 0;
                if (elemLow <= low) count++; else break;
            }
            else break;
            position++;
        }
        return count;
    }

    /// <summary>Value at given rank (equivalent to Access).</summary>
    public ulong Select(int rank) => Access(rank);

    /// <summary>First element &ge; query. Returns (index, value) or null.</summary>
    public (int Index, ulong Value)? NextGeq(ulong query)
    {
        if (_count == 0) return null;
        if (query == 0) return (0, Access(0));
        if (query > _universe) return null;
        int idx = Rank(query - 1);
        if (idx >= _count) return null;
        return (idx, Access(idx));
    }

    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public int SizeInBytes => _lowBits.Length * 8 + _highBits.Length * 8 + _rankSuperblocks.Length * 8;
    public int OptimalSizeInBytes
    {
        get
        {
            if (_count == 0) return 0;
            double bpe = _universe > 0 && _count > 1 ? Math.Max(Math.Log2((double)_universe / _count), 0.0) + 2.0 : 2.0;
            return (int)Math.Ceiling(_count * bpe / 8.0);
        }
    }

    private ulong RankOne(int position)
    {
        if (position == 0) return 0;
        int wordIndex = position / 64, bitIndex = position % 64;
        int sbIndex = wordIndex / SuperblockWords;
        ulong count = _rankSuperblocks[sbIndex];
        for (int i = sbIndex * SuperblockWords; i < wordIndex && i < _highBits.Length; i++)
            count += (ulong)System.Numerics.BitOperations.PopCount(_highBits[i]);
        if (bitIndex > 0 && wordIndex < _highBits.Length)
            count += (ulong)System.Numerics.BitOperations.PopCount(_highBits[wordIndex] & ((1UL << bitIndex) - 1));
        return count;
    }

    private int SelectOne(int k)
    {
        ulong target = (ulong)k;
        int lo = 0, hi = _rankSuperblocks.Length - 1;
        while (lo < hi) { int mid = lo + (hi - lo) / 2; if (_rankSuperblocks[mid + 1] <= target) lo = mid + 1; else hi = mid; }
        int remaining = k - (int)_rankSuperblocks[lo];
        for (int w = lo * SuperblockWords; w < _highBits.Length; w++)
        {
            int ones = System.Numerics.BitOperations.PopCount(_highBits[w]);
            if (remaining < ones) return w * 64 + SelectInWord(_highBits[w], remaining);
            remaining -= ones;
        }
        throw new InvalidOperationException($"selectOne({k}): not enough 1-bits");
    }

    private int SelectZero(int k) => TrySelectZero(k) ?? throw new InvalidOperationException($"selectZero({k}): not enough 0-bits");

    private int? TrySelectZero(int k)
    {
        int lo = 0, hi = _rankSuperblocks.Length - 1;
        while (lo < hi) { int mid = lo + (hi - lo) / 2; int zeros = (mid + 1) * SuperblockWords * 64 - (int)_rankSuperblocks[mid + 1]; if (zeros <= k) lo = mid + 1; else hi = mid; }
        int remaining = k - (lo * SuperblockWords * 64 - (int)_rankSuperblocks[lo]);
        for (int w = lo * SuperblockWords; w < _highBits.Length; w++)
        {
            int zeros = 64 - System.Numerics.BitOperations.PopCount(_highBits[w]);
            if (remaining < zeros) return w * 64 + SelectZeroInWord(_highBits[w], remaining);
            remaining -= zeros;
        }
        return null;
    }

    public static ulong GetBits(ulong[] words, ulong bitPosition, uint width)
    {
        if (width == 0) return 0;
        int wordIndex = (int)(bitPosition / 64), bitOffset = (int)(bitPosition % 64);
        ulong mask = width >= 64 ? ulong.MaxValue : (1UL << (int)width) - 1;
        if (bitOffset + width <= 64) return (words[wordIndex] >> bitOffset) & mask;
        return ((words[wordIndex] >> bitOffset) | (words[wordIndex + 1] << (64 - bitOffset))) & mask;
    }

    public static void SetBits(ulong[] words, ulong bitPosition, uint width, ulong value)
    {
        if (width == 0) return;
        int wordIndex = (int)(bitPosition / 64), bitOffset = (int)(bitPosition % 64);
        ulong mask = width >= 64 ? ulong.MaxValue : (1UL << (int)width) - 1;
        value &= mask;
        words[wordIndex] &= ~(mask << bitOffset);
        words[wordIndex] |= value << bitOffset;
        if (bitOffset + width > 64) { int overflow = bitOffset + (int)width - 64; words[wordIndex + 1] &= ~((1UL << overflow) - 1); words[wordIndex + 1] |= value >> (64 - bitOffset); }
    }

    public static int SelectInWord(ulong word, int k)
    {
        int remaining = k; ulong current = word;
        for (int bit = 0; bit < 64; bit++) { if ((current & 1) == 1) { if (remaining == 0) return bit; remaining--; } current >>= 1; if (current == 0) break; }
        throw new InvalidOperationException("selectInWord: not enough 1-bits");
    }

    private static int SelectZeroInWord(ulong word, int k)
    {
        int remaining = k; ulong inverted = ~word;
        for (int bit = 0; bit < 64; bit++) { if ((inverted & 1) == 1) { if (remaining == 0) return bit; remaining--; } inverted >>= 1; }
        throw new InvalidOperationException("selectZeroInWord: not enough 0-bits");
    }

    private static int DivCeil(int a, int b) => (a + b - 1) / b;
    private static ulong DivCeil(ulong a, ulong b) => (a + b - 1) / b;
    private static int FloorLog2(ulong x) => 63 - System.Numerics.BitOperations.LeadingZeroCount(x);
}
