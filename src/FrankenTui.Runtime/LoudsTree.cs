// Upstream source: crates/ftui-widgets/src/louds.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of LOUDS tree encoding with O(1) navigation via rank/select.
//
// Compacts a tree structure from O(n × ptr_size) to 2n+1 bits while
// supporting O(1) parent, first_child, next_sibling, and is_leaf
// navigation via rank/select on a bitvector.
//
// Encoding: Traverse the tree in level order (BFS). For each node with d
// children, emit d one-bits followed by a zero-bit. Prepend a sentinel
// "10" for the super-root. Total bits: 2n + 1.

namespace FrankenTui.Runtime;

/// <summary>
/// LOUDS-encoded tree with O(1) navigation.
/// Stores the tree structure in 2n+1 bits plus rank superblocks for fast
/// rank/select queries.
/// </summary>
public sealed class LoudsTree
{
    /// <summary>Number of u64 words per rank superblock (512 bits).</summary>
    private const int SuperblockWords = 8;

    /// <summary>The LOUDS bitvector (including super-root sentinel).</summary>
    private readonly ulong[] _bits;
    /// <summary>Cumulative popcount at superblock boundaries.</summary>
    private readonly ulong[] _rankSuperblocks;
    /// <summary>Total number of bits in the bitvector.</summary>
    private readonly int _bitLength;
    /// <summary>Number of tree nodes (excluding the super-root).</summary>
    private readonly int _nodeCount;

    private LoudsTree(ulong[] bits, ulong[] rankSuperblocks, int bitLength, int nodeCount)
    { _bits = bits; _rankSuperblocks = rankSuperblocks; _bitLength = bitLength; _nodeCount = nodeCount; }

    /// <summary>
    /// Build a LOUDS tree from BFS-order degree sequence.
    /// degrees[i] is the number of children of the i-th node in level-order.
    /// The root is degrees[0].
    /// </summary>
    public static LoudsTree FromDegrees(int[] degrees)
    {
        if (degrees.Length == 0)
            throw new ArgumentException("degree sequence must not be empty");

        int nodeCount = degrees.Length;
        int totalChildren = degrees.Sum();
        if (totalChildren != nodeCount - 1)
            throw new ArgumentException(
                $"degree sum ({totalChildren}) must equal n-1 ({nodeCount - 1}) for a tree with {nodeCount} nodes");

        int bitLength = 2 * nodeCount + 1;
        int wordCount = (bitLength + 63) / 64;
        var bits = new ulong[wordCount];

        // Sentinel super-root: bit 0 = 1, bit 1 = 0
        SetBit(bits, 0);
        // bit 1 is already 0

        int position = 2; // Start after sentinel "10"
        foreach (int degree in degrees)
        {
            for (int i = 0; i < degree; i++)
            {
                SetBit(bits, position);
                position++;
            }
            // Zero bit (separator) — already 0
            position++;
        }

        if (position != bitLength)
            throw new InvalidOperationException($"encoding used {position} bits but expected {bitLength}");

        var rankSuperblocks = BuildRankSuperblocks(bits);
        return new LoudsTree(bits, rankSuperblocks, bitLength, nodeCount);
    }

    /// <summary>
    /// Build a LOUDS tree from a pointer-based tree (children list per node).
    /// children[i] is an array of child node indices for node i.
    /// Nodes must be numbered 0..n in BFS order.
    /// </summary>
    public static LoudsTree FromChildren(int[][] children)
    {
        var degrees = children.Select(c => c.Length).ToArray();
        return FromDegrees(degrees);
    }

    /// <summary>Number of nodes in the tree.</summary>
    public int NodeCount => _nodeCount;

    /// <summary>Whether the tree is empty.</summary>
    public bool IsEmpty => _nodeCount == 0;

    /// <summary>Total memory usage in bytes (excluding struct overhead).</summary>
    public int SizeInBytes => _bits.Length * 8 + _rankSuperblocks.Length * 8;

    /// <summary>Degree (number of children) of node v.</summary>
    public int Degree(int v)
    {
        if ((uint)v >= (uint)_nodeCount)
            throw new ArgumentOutOfRangeException(nameof(v), $"node {v} out of bounds (n={_nodeCount})");
        int end = SelectZero(v + 1);
        int start = v == 0 ? SelectZero(0) + 1 : SelectZero(v) + 1;
        return end - start;
    }

    /// <summary>Parent of node v, or null for the root (node 0).</summary>
    public int? Parent(int v)
    {
        if ((uint)v >= (uint)_nodeCount)
            throw new ArgumentOutOfRangeException(nameof(v), $"node {v} out of bounds (n={_nodeCount})");
        if (v == 0) return null;
        int childBit = SelectOne(v);
        return RankZero(childBit) - 1;
    }

    /// <summary>First child of node v, or null if v is a leaf.</summary>
    public int? FirstChild(int v)
    {
        if ((uint)v >= (uint)_nodeCount)
            throw new ArgumentOutOfRangeException(nameof(v), $"node {v} out of bounds (n={_nodeCount})");
        int blockStart = SelectZero(v) + 1;
        if (blockStart >= _bitLength || !GetBit(_bits, blockStart))
            return null;
        return RankOne(blockStart + 1) - 1;
    }

    /// <summary>Next sibling of node v, or null if v is the last child.</summary>
    public int? NextSibling(int v)
    {
        if ((uint)v >= (uint)_nodeCount)
            throw new ArgumentOutOfRangeException(nameof(v), $"node {v} out of bounds (n={_nodeCount})");
        if (v == 0) return null;
        int vBit = SelectOne(v);
        int nextBit = vBit + 1;
        if (nextBit >= _bitLength || !GetBit(_bits, nextBit))
            return null;
        return v + 1;
    }

    /// <summary>Whether node v is a leaf (has no children).</summary>
    public bool IsLeaf(int v) => FirstChild(v) is null;

    /// <summary>Depth of node v (root has depth 0). O(depth).</summary>
    public int Depth(int v)
    {
        int depth = 0;
        int? current = v;
        while (current is not null)
        {
            var parent = Parent(current.Value);
            if (parent is null) break;
            depth++;
            current = parent;
        }
        return depth;
    }

    /// <summary>Enumerate children of node v.</summary>
    public IEnumerable<int> Children(int v)
    {
        var child = FirstChild(v);
        while (child is not null)
        {
            yield return child.Value;
            child = NextSibling(child.Value);
        }
    }

    /// <summary>Subtree size rooted at node v (including v itself). O(subtreeSize).</summary>
    public int SubtreeSize(int v)
    {
        if ((uint)v >= (uint)_nodeCount)
            throw new ArgumentOutOfRangeException(nameof(v), $"node {v} out of bounds (n={_nodeCount})");
        int count = 0;
        var queue = new Queue<int>();
        queue.Enqueue(v);
        while (queue.Count > 0)
        {
            int node = queue.Dequeue();
            count++;
            foreach (var child in Children(node))
                queue.Enqueue(child);
        }
        return count;
    }

    // ── Bitvector primitives ──

    private int RankOne(int position)
    {
        if (position == 0) return 0;
        int wordIndex = position / 64, bitIndex = position % 64;
        int sbIndex = wordIndex / SuperblockWords;
        ulong count = _rankSuperblocks[sbIndex];
        int sbStart = sbIndex * SuperblockWords;
        for (int i = sbStart; i < wordIndex && i < _bits.Length; i++)
            count += (ulong)System.Numerics.BitOperations.PopCount(_bits[i]);
        if (bitIndex > 0 && wordIndex < _bits.Length)
            count += (ulong)System.Numerics.BitOperations.PopCount(_bits[wordIndex] & ((1UL << bitIndex) - 1));
        return (int)count;
    }

    private int RankZero(int position) => position - RankOne(position);

    private int SelectOne(int k)
    {
        ulong target = (ulong)k;
        int lo = 0, hi = _rankSuperblocks.Length - 1;
        while (lo < hi) { int mid = lo + (hi - lo) / 2; if (_rankSuperblocks[mid + 1] <= target) lo = mid + 1; else hi = mid; }
        int remaining = k - (int)_rankSuperblocks[lo];
        for (int w = lo * SuperblockWords; w < _bits.Length; w++)
        {
            int ones = System.Numerics.BitOperations.PopCount(_bits[w]);
            if (remaining < ones) return w * 64 + SelectInWord(_bits[w], remaining);
            remaining -= ones;
        }
        throw new InvalidOperationException($"selectOne({k}): not enough 1-bits");
    }

    private int SelectZero(int k)
    {
        int lo = 0, hi = _rankSuperblocks.Length - 1;
        while (lo < hi) { int mid = lo + (hi - lo) / 2; int zeros = (mid + 1) * SuperblockWords * 64 - (int)_rankSuperblocks[mid + 1]; if (zeros <= k) lo = mid + 1; else hi = mid; }
        int remaining = k - (lo * SuperblockWords * 64 - (int)_rankSuperblocks[lo]);
        for (int w = lo * SuperblockWords; w < _bits.Length; w++)
        {
            int zeros = 64 - System.Numerics.BitOperations.PopCount(_bits[w]);
            if (remaining < zeros) return w * 64 + SelectZeroInWord(_bits[w], remaining);
            remaining -= zeros;
        }
        throw new InvalidOperationException($"selectZero({k}): not enough 0-bits");
    }

    // ── Static bit helpers ──

    private static void SetBit(ulong[] bits, int position)
    {
        bits[position / 64] |= 1UL << (position % 64);
    }

    private static bool GetBit(ulong[] bits, int position)
    {
        return (bits[position / 64] >> (position % 64) & 1) == 1;
    }

    private static ulong[] BuildRankSuperblocks(ulong[] bits)
    {
        int superblockCount = (bits.Length + SuperblockWords - 1) / SuperblockWords;
        var superblocks = new ulong[superblockCount + 1];
        ulong cumulative = 0;
        for (int sb = 0; sb < superblockCount; sb++)
        {
            int end = Math.Min((sb + 1) * SuperblockWords, bits.Length);
            for (int w = sb * SuperblockWords; w < end; w++)
                cumulative += (ulong)System.Numerics.BitOperations.PopCount(bits[w]);
            superblocks[sb + 1] = cumulative;
        }
        return superblocks;
    }

    private static int SelectInWord(ulong word, int k)
    {
        int remaining = k;
        ulong current = word;
        for (int bit = 0; bit < 64; bit++)
        {
            if ((current & 1) == 1) { if (remaining == 0) return bit; remaining--; }
            current >>= 1;
            if (current == 0) break;
        }
        throw new InvalidOperationException("selectInWord: not enough 1-bits");
    }

    private static int SelectZeroInWord(ulong word, int k)
    {
        int remaining = k;
        ulong inverted = ~word;
        for (int bit = 0; bit < 64; bit++)
        {
            if ((inverted & 1) == 1) { if (remaining == 0) return bit; remaining--; }
            inverted >>= 1;
        }
        throw new InvalidOperationException("selectZeroInWord: not enough 0-bits");
    }
}
