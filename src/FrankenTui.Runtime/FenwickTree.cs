// Upstream source: crates/ftui-widgets/src/fenwick.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of FenwickTree (Binary Indexed Tree) for prefix sums.
// O(log n) point update and prefix query, zero allocations per operation.
//
// Designed for virtualized list height layout: each entry i stores the
// height of item i, and Prefix(i) gives the y-offset of item i+1.

namespace FrankenTui.Runtime;

/// <summary>
/// Fenwick tree (Binary Indexed Tree) for prefix sum queries over u32 values.
///
/// The tree is stored 1-indexed in a contiguous array of length n+1
/// (index 0 unused). This gives cache-friendly sequential access.
/// Invariants: tree[i] stores the sum of a range determined by LowBit(i);
/// after Rebuild, the tree exactly represents the given values;
/// BatchUpdate produces identical results to sequential Update calls.
/// </summary>
public sealed class FenwickTree
{
    /// <summary>1-indexed tree storage. tree[0] is unused.</summary>
    private uint[] _tree;
    /// <summary>Number of elements (not including index 0).</summary>
    private int _count;

    private FenwickTree() { _tree = []; }

    /// <summary>Create a Fenwick tree of size n initialised to all zeros.</summary>
    public FenwickTree(int count)
    {
        _tree = new uint[count + 1];
        _count = count;
    }

    /// <summary>
    /// Create from an initial array of values in O(n).
    /// Faster than calling Update n times (O(n log n)).
    /// </summary>
    public static FenwickTree FromValues(uint[] values)
    {
        int count = values.Length;
        var tree = new uint[count + 1];
        for (int i = 0; i < count; i++) tree[i + 1] = values[i];
        for (int i = 1; i <= count; i++)
        {
            int parent = i + LowBit(i);
            if (parent <= count)
                tree[parent] = unchecked(tree[parent] + tree[i]);
        }
        return new FenwickTree { _tree = tree, _count = count };
    }

    public int Count => _count;
    public bool IsEmpty => _count == 0;

    /// <summary>Add delta to element at position index (0-indexed). O(log n).</summary>
    public void Update(int index, int delta) => UpdateUnsigned(index, (uint)delta);

    /// <summary>Set element at position index to value (0-indexed). O(log n).</summary>
    public void Set(int index, uint value)
    {
        uint current = Get(index);
        UpdateUnsigned(index, unchecked(value - current));
    }

    /// <summary>Get value at position index: Prefix(index) - Prefix(index-1). O(log n).</summary>
    public uint Get(int index)
    {
        if (index == 0) return Prefix(0);
        return unchecked(Prefix(index) - Prefix(index - 1));
    }

    /// <summary>Prefix sum of elements [0..=index] (0-indexed). O(log n).</summary>
    public uint Prefix(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} out of bounds (count={_count})");
        uint sum = 0;
        int treeIndex = index + 1;
        while (treeIndex > 0) { sum = unchecked(sum + _tree[treeIndex]); treeIndex -= LowBit(treeIndex); }
        return sum;
    }

    /// <summary>Range sum [left..=right] (0-indexed). O(log n).</summary>
    public uint Range(int left, int right)
    {
        if (left > right) throw new ArgumentException($"left {left} > right {right}");
        if (left == 0) return Prefix(right);
        return unchecked(Prefix(right) - Prefix(left - 1));
    }

    /// <summary>Total sum of all elements. O(log n).</summary>
    public uint Total() => _count == 0 ? 0 : Prefix(_count - 1);

    /// <summary>Apply multiple updates. Produces identical results to sequential Update calls.</summary>
    public void BatchUpdate((int Index, int Delta)[] deltas)
    {
        foreach (var (index, delta) in deltas) Update(index, delta);
    }

    /// <summary>Rebuild from fresh values in O(n). Requires values.Length == Count.</summary>
    public void Rebuild(uint[] values)
    {
        if (values.Length != _count)
            throw new ArgumentException($"rebuild size mismatch: expected {_count}, got {values.Length}");
        Array.Clear(_tree);
        for (int i = 0; i < _count; i++) _tree[i + 1] = values[i];
        for (int i = 1; i <= _count; i++)
        {
            int parent = i + LowBit(i);
            if (parent <= _count)
                _tree[parent] = unchecked(_tree[parent] + _tree[i]);
        }
    }

    /// <summary>
    /// Find largest index i such that Prefix(i) &lt;= target.
    /// Returns null if all prefix sums exceed target. O(log n).
    /// </summary>
    public int? FindPrefix(uint target)
    {
        if (_count == 0) return null;
        int position = 0;
        uint remaining = target;
        int bitMask = MostSignificantBit(_count);
        while (bitMask > 0)
        {
            int next = position + bitMask;
            if (next <= _count && _tree[next] <= remaining)
            { remaining = unchecked(remaining - _tree[next]); position = next; }
            bitMask >>= 1;
        }
        if (position == 0 && _tree.Length > 1 && _tree[1] > target) return null;
        return Math.Max(0, position - 1);
    }

    /// <summary>Resize. Growing fills zeros; shrinking drops excess. O(newCount).</summary>
    public void Resize(int newCount)
    {
        if (newCount == _count) return;
        var values = new uint[_count];
        for (int i = 0; i < _count; i++) values[i] = Get(i);
        Array.Resize(ref values, newCount);
        _count = newCount;
        _tree = new uint[newCount + 1];
        Rebuild(values);
    }

    private void UpdateUnsigned(int index, uint delta)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index), $"index {index} out of bounds (count={_count})");
        int treeIndex = index + 1;
        while (treeIndex <= _count) { _tree[treeIndex] = unchecked(_tree[treeIndex] + delta); treeIndex += LowBit(treeIndex); }
    }

    /// <summary>Lowest set bit of x. E.g., LowBit(6) = 2.</summary>
    public static int LowBit(int x) => x & -x;

    /// <summary>Most significant bit that fits within n.</summary>
    public static int MostSignificantBit(int n)
    {
        if (n == 0) return 0;
        return 1 << (31 - System.Numerics.BitOperations.LeadingZeroCount((uint)n));
    }
}
