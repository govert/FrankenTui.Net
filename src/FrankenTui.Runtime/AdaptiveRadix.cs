// Upstream source: crates/ftui-widgets/src/adaptive_radix.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of AdaptiveRadixTree<V>, NodeDistribution, and the
// Node4/Node16/Node48/Node256 adaptive child storage with path compression,
// terminal values on compressed inner nodes, and prefix-scan/iter/delete.
//
// DIVERGENCE from earlier .NET stub: the previous local port was a thin
// byte-span-keyed placeholder that did not match the upstream contract. This
// file replaces it with the faithful upstream port. The C# test-facing API
// adapts the upstream Option-returning methods to value-defaulting methods so
// that value-typed V (e.g. int) works without a class constraint: Insert/Get/
// Delete return default(V) when no prior/absent value exists, matching the
// test expectations (which assert 0 for absent int keys).
//
// DIVERGENCE: Rust uses an enum ArtNode (Leaf/Inner) replaced in place via
// `*node = ArtNode::Inner {...}`. C# uses a single mutable class with a Kind
// discriminant so leaf→inner splits mutate the node in place, preserving the
// parent's child reference.

using System.Collections.Generic;
using System.Text;

namespace FrankenTui.Runtime;

/// <summary>
/// Adaptive radix tree for fast string-keyed prefix lookup.
/// Mirrors upstream <c>ftui_widgets::adaptive_radix::AdaptiveRadixTree</c>.
/// </summary>
public sealed class AdaptiveRadixTree<V>
{
    private const int Node4Max = 4;
    private const int Node16Max = 16;
    private const int Node48Max = 48;

    private ArtNode? _root;
    private int _len;

    /// <summary>Number of entries. Upstream <c>fn len</c>.</summary>
    public int Count => _len;

    /// <summary>Whether the tree is empty. Upstream <c>fn is_empty</c>.</summary>
    public bool IsEmpty => _len == 0;

    public AdaptiveRadixTree() { }

    /// <summary>
    /// Insert a key-value pair. Returns the previous value if the key existed,
    /// otherwise <c>default(V)</c>. Upstream <c>fn insert -&gt; Option&lt;V&gt;</c>.
    /// </summary>
    public V Insert(string key, V value)
    {
        if (_root is null)
        {
            _root = ArtNode.NewLeaf(key, value);
            _len++;
            return default!;
        }

        var (old, hadOld) = InsertRecursive(_root, Encoding.UTF8.GetBytes(key), key, value, 0);
        if (!hadOld) _len++;
        return hadOld ? old! : default!;
    }

    /// <summary>
    /// Look up a value by exact key. Returns <c>default(V)</c> if absent.
    /// Upstream <c>fn get -&gt; Option&lt;&amp;V&gt;</c>.
    /// </summary>
    public V Get(string key)
    {
        if (_root is null) return default!;
        var (found, value) = GetRecursive(_root, Encoding.UTF8.GetBytes(key), 0);
        return found ? value! : default!;
    }

    /// <summary>
    /// Return all key-value pairs whose keys start with the given prefix,
    /// sorted by key. Upstream <c>fn prefix_scan</c>.
    /// </summary>
    public List<(string Key, V Value)> PrefixScan(string prefix)
    {
        var results = new List<(string Key, V Value)>();
        if (_root is not null)
            PrefixScanRecursive(_root, Encoding.UTF8.GetBytes(prefix), 0, results);
        results.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        return results;
    }

    /// <summary>
    /// Delete a key. Returns the value if it existed, otherwise <c>default(V)</c>.
    /// Upstream <c>fn delete -&gt; Option&lt;V&gt;</c>.
    /// </summary>
    public V Delete(string key)
    {
        if (_root is null) return default!;
        var (removed, hadRemoved, prune) = DeleteRecursive(_root, Encoding.UTF8.GetBytes(key), 0);
        if (hadRemoved)
        {
            _len--;
            if (prune || IsEmptyNode(_root))
                _root = null;
            return removed!;
        }
        return default!;
    }

    /// <summary>Iterate all entries in sorted key order. Upstream <c>fn iter</c>.</summary>
    public List<(string Key, V Value)> Iter()
    {
        var results = new List<(string Key, V Value)>();
        if (_root is not null)
            CollectAll(_root, results);
        results.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        return results;
    }

    /// <summary>Get node-type distribution for diagnostics. Upstream <c>fn node_distribution</c>.</summary>
    public NodeDistribution GetNodeDistribution()
    {
        var dist = new NodeDistribution();
        if (_root is not null)
            CountNodes(_root, dist);
        return dist;
    }

    // ── Recursive insert ───────────────────────────────────────────────────

    private static (V old, bool hadOld) InsertRecursive(ArtNode node, byte[] keyBytes, string fullKey, V value, int depth)
    {
        if (node.Kind == NodeKind.Leaf)
        {
            if (node.Key == fullKey)
            {
                var old = node.Value!;
                node.Value = value;
                return (old, true);
            }

            var existingBytes = Encoding.UTF8.GetBytes(node.Key);
            var commonLen = CommonPrefixLength(existingBytes, depth, keyBytes, depth);

            var prefix = SubArray(existingBytes, depth, commonLen);
            var oldKey = node.Key;
            var oldVal = node.Value!;

            int splitDepth = depth + commonLen;

            Children? children = Children.Node4Empty();
            V? innerValue = default;
            string? terminalKey = null;

            if (splitDepth < existingBytes.Length)
            {
                var oldChild = ArtNode.NewLeaf(oldKey, oldVal);
                ChildrenInsert(ref children, existingBytes[splitDepth], oldChild);
            }

            if (splitDepth < keyBytes.Length)
            {
                var newChild = ArtNode.NewLeaf(fullKey, value);
                ChildrenInsert(ref children, keyBytes[splitDepth], newChild);
            }
            else
            {
                innerValue = value;
                terminalKey = fullKey;
            }

            if (splitDepth >= existingBytes.Length)
            {
                innerValue = oldVal;
                terminalKey = oldKey;
            }

            node.BecomeInner(prefix, children, innerValue, terminalKey);
            return (default!, false);
        }

        // Inner
        {
            var remaining = SubArray(keyBytes, depth, keyBytes.Length - depth);
            var prefixMatch = CommonPrefixLength(remaining, 0, node.Prefix!, 0);

            if (prefixMatch < node.Prefix!.Length)
            {
                // Prefix mismatch — split this inner node.
                var common = SubArray(node.Prefix, 0, prefixMatch);
                var oldSuffix = SubArray(node.Prefix, prefixMatch, node.Prefix.Length - prefixMatch);
                byte oldFirstByte = oldSuffix[0];

                var oldInner = ArtNode.NewInner(
                    SubArray(oldSuffix, 1, oldSuffix.Length - 1),
                    node.TakeChildren(),
                    node.HasValue ? node.Value : default,
                    node.HasValue,
                    node.TakeTerminalKey());

                Children? newChildren = Children.Node4Empty();
                ChildrenInsert(ref newChildren, oldFirstByte, oldInner);

                int newDepth = depth + prefixMatch;
                V? innerValue = default;
                string? newTerminalKey = null;
                if (newDepth < keyBytes.Length)
                {
                    var newChild = ArtNode.NewLeaf(fullKey, value);
                    ChildrenInsert(ref newChildren, keyBytes[newDepth], newChild);
                }
                else
                {
                    innerValue = value;
                    newTerminalKey = fullKey;
                }

                node.Prefix = common;
                node.Children = newChildren;
                node.Value = innerValue;
                node.HasValue = innerValue is not null || newTerminalKey is not null;
                node.TerminalKey = newTerminalKey;
                return (default!, false);
            }

            int nextDepth = depth + node.Prefix.Length;
            if (nextDepth >= keyBytes.Length)
            {
                var old = node.Value;
                bool hadOld = node.HasValue;
                node.Value = value;
                node.HasValue = true;
                node.TerminalKey = fullKey;
                return hadOld ? (old!, true) : (default!, false);
            }

            byte b = keyBytes[nextDepth];
            if (ChildrenGet(node.Children!, b) is { } child)
            {
                return InsertRecursive(child, keyBytes, fullKey, value, nextDepth + 1);
            }
            else
            {
                var newChild = ArtNode.NewLeaf(fullKey, value);
                ChildrenInsert(ref node.Children, b, newChild);
                return (default!, false);
            }
        }
    }

    // ── Recursive get ──────────────────────────────────────────────────────

    private static (bool found, V value) GetRecursive(ArtNode node, byte[] keyBytes, int depth)
    {
        if (node.Kind == NodeKind.Leaf)
        {
            return Encoding.UTF8.GetBytes(node.Key!).SequenceEqual(keyBytes)
                ? (true, node.Value!)
                : (false, default!);
        }

        var remaining = SubArray(keyBytes, depth, keyBytes.Length - depth);
        if (remaining.Length < node.Prefix!.Length || !StartsWith(remaining, node.Prefix))
            return (false, default!);
        int nextDepth = depth + node.Prefix.Length;
        if (nextDepth >= keyBytes.Length)
            return node.HasValue ? (true, node.Value!) : (false, default!);
        byte b = keyBytes[nextDepth];
        if (ChildrenGet(node.Children!, b) is { } child)
            return GetRecursive(child, keyBytes, nextDepth + 1);
        return (false, default!);
    }

    // ── Recursive prefix scan ──────────────────────────────────────────────

    private static void PrefixScanRecursive(ArtNode node, byte[] prefixBytes, int depth, List<(string, V)> results)
    {
        if (node.Kind == NodeKind.Leaf)
        {
            if (StartsWith(Encoding.UTF8.GetBytes(node.Key!), prefixBytes))
                results.Add((node.Key!, node.Value!));
            return;
        }

        var remainingPrefix = depth < prefixBytes.Length
            ? SubArray(prefixBytes, depth, prefixBytes.Length - depth)
            : Array.Empty<byte>();

        int matchLen = CommonPrefixLength(remainingPrefix, 0, node.Prefix!, 0);

        if (matchLen < remainingPrefix.Length && matchLen < node.Prefix!.Length)
            return;

        int nextDepth = depth + node.Prefix.Length;

        if (remainingPrefix.Length <= node.Prefix.Length)
        {
            if (depth + matchLen >= prefixBytes.Length)
            {
                CollectAllInner(node, results);
                return;
            }
        }

        if (nextDepth > prefixBytes.Length)
        {
            CollectAllInner(node, results);
            return;
        }

        if (nextDepth == prefixBytes.Length)
        {
            CollectAllInner(node, results);
            return;
        }

        byte b = prefixBytes[nextDepth];
        if (ChildrenGet(node.Children!, b) is { } child)
            PrefixScanRecursive(child, prefixBytes, nextDepth + 1, results);
    }

    private static void CollectAllInner(ArtNode node, List<(string, V)> results)
    {
        if (node.Kind == NodeKind.Leaf)
        {
            results.Add((node.Key!, node.Value!));
            return;
        }

        if (node.HasValue && node.TerminalKey is { } key)
            results.Add((key, node.Value!));
        foreach (var child in ChildrenIter(node.Children!))
            CollectAllInner(child, results);
    }

    private static void CollectAll(ArtNode node, List<(string, V)> results) => CollectAllInner(node, results);

    // ── Recursive delete ───────────────────────────────────────────────────

    private static (V removed, bool hadRemoved, bool prune) DeleteRecursive(ArtNode node, byte[] keyBytes, int depth)
    {
        if (node.Kind == NodeKind.Leaf)
        {
            if (Encoding.UTF8.GetBytes(node.Key!).SequenceEqual(keyBytes))
                return (node.Value!, true, true);
            return (default!, false, false);
        }

        var remaining = SubArray(keyBytes, depth, keyBytes.Length - depth);
        if (remaining.Length < node.Prefix!.Length || !StartsWith(remaining, node.Prefix))
            return (default!, false, false);

        int nextDepth = depth + node.Prefix.Length;
        if (nextDepth >= keyBytes.Length)
        {
            if (node.HasValue)
            {
                var removed = node.Value!;
                node.Value = default!;
                node.HasValue = false;
                node.TerminalKey = null;
                bool prune = ChildrenCount(node.Children!) == 0;
                return (removed, true, prune);
            }
            return (default!, false, false);
        }

        byte b = keyBytes[nextDepth];
        if (ChildrenGet(node.Children!, b) is not { } child)
            return (default!, false, false);

        var childResult = DeleteRecursive(child, keyBytes, nextDepth + 1);
        if (childResult.prune)
            ChildrenRemove(ref node.Children, b);

        bool pruneOuter = childResult.hadRemoved && !node.HasValue && ChildrenCount(node.Children!) == 0;
        return (childResult.removed, childResult.hadRemoved, pruneOuter);
    }

    private static bool IsEmptyNode(ArtNode node)
    {
        if (node.Kind == NodeKind.Leaf) return false;
        return !node.HasValue && ChildrenCount(node.Children!) == 0;
    }

    private static void CountNodes(ArtNode node, NodeDistribution dist)
    {
        if (node.Kind == NodeKind.Leaf)
        {
            dist.Leaves++;
            return;
        }

        switch (node.Children!)
        {
            case Children.Node4: dist.Node4++; break;
            case Children.Node16: dist.Node16++; break;
            case Children.Node48: dist.Node48++; break;
            case Children.Node256: dist.Node256++; break;
        }
        foreach (var child in ChildrenIter(node.Children!))
            CountNodes(child, dist);
    }

    // ── Children operations ─────────────────────────────────────────────────
    // Promotion/demotion reassign the owning node's `Children` field, so these
    // helpers take the ArtNode rather than a bare Children reference.

    private static void ChildrenInsert(ref Children? children, byte b, ArtNode child)
    {
        switch (children)
        {
            case Children.Node4 n4:
            {
                if (n4.Children.Count < Node4Max)
                {
                    int pos = InsertPosition(n4.Keys, b);
                    n4.Keys.Insert(pos, b);
                    n4.Children.Insert(pos, child);
                }
                else
                {
                    var newKeys = new List<byte>(n4.Keys);
                    var newChildren = new List<ArtNode>(n4.Children);
                    int pos = InsertPosition(newKeys, b);
                    newKeys.Insert(pos, b);
                    newChildren.Insert(pos, child);
                    children = new Children.Node16(newKeys, newChildren);
                }
                break;
            }
            case Children.Node16 n16:
            {
                if (n16.Children.Count < Node16Max)
                {
                    int pos = InsertPosition(n16.Keys, b);
                    n16.Keys.Insert(pos, b);
                    n16.Children.Insert(pos, child);
                }
                else
                {
                    var index = new byte[256];
                    Array.Fill(index, (byte)0xFF);
                    var newChildren = new List<ArtNode?>();
                    for (int i = 0; i < n16.Keys.Count; i++)
                    {
                        index[n16.Keys[i]] = (byte)i;
                        newChildren.Add(n16.Children[i]);
                    }
                    int idx = newChildren.Count;
                    index[b] = (byte)idx;
                    newChildren.Add(child);
                    children = new Children.Node48(index, newChildren, idx + 1);
                }
                break;
            }
            case Children.Node48 n48:
            {
                if (n48.Count < Node48Max)
                {
                    int idx = n48.Count;
                    n48.Index[b] = (byte)idx;
                    if (idx < n48.Children.Count)
                        n48.Children[idx] = child;
                    else
                        n48.Children.Add(child);
                    n48.Count++;
                }
                else
                {
                    var newChildren = new ArtNode?[256];
                    for (int by = 0; by < 256; by++)
                    {
                        byte idx = n48.Index[by];
                        if (idx != 0xFF && idx < n48.Children.Count)
                            newChildren[by] = n48.Children[idx];
                    }
                    newChildren[b] = child;
                    children = new Children.Node256(newChildren);
                }
                break;
            }
            case Children.Node256 n256:
            {
                n256.Children[b] = child;
                break;
            }
        }
    }

    private static ArtNode? ChildrenGet(Children children, byte b)
    {
        return children switch
        {
            Children.Node4 n4 => IndexOf(n4.Keys, b) is { } i ? n4.Children[i] : null,
            Children.Node16 n16 => IndexOf(n16.Keys, b) is { } i ? n16.Children[i] : null,
            Children.Node48 n48 => n48.Index[b] is byte idx && idx != 0xFF && idx < n48.Children.Count
                ? n48.Children[idx] : null,
            Children.Node256 n256 => n256.Children[b],
            _ => null,
        };
    }

    private static void ChildrenRemove(ref Children? children, byte b)
    {
        switch (children)
        {
            case Children.Node4 n4:
            {
                if (IndexOf(n4.Keys, b) is { } pos)
                {
                    n4.Keys.RemoveAt(pos);
                    n4.Children.RemoveAt(pos);
                }
                break;
            }
            case Children.Node16 n16:
            {
                if (IndexOf(n16.Keys, b) is { } pos)
                {
                    n16.Keys.RemoveAt(pos);
                    n16.Children.RemoveAt(pos);
                }
                if (n16.Children.Count <= Node4Max)
                    children = new Children.Node4(new List<byte>(n16.Keys), new List<ArtNode>(n16.Children));
                break;
            }
            case Children.Node48 n48:
            {
                byte idx = n48.Index[b];
                if (idx != 0xFF && idx < n48.Children.Count)
                {
                    n48.Children[idx] = null;
                    n48.Index[b] = 0xFF;
                    n48.Count = Math.Max(0, n48.Count - 1);
                }
                break;
            }
            case Children.Node256 n256:
            {
                n256.Children[b] = null;
                break;
            }
        }
    }

    private static int ChildrenCount(Children children)
    {
        return children switch
        {
            Children.Node4 n4 => n4.Children.Count,
            Children.Node16 n16 => n16.Children.Count,
            Children.Node48 n48 => n48.Count,
            Children.Node256 n256 => CountNonNull(n256.Children),
            _ => 0,
        };
    }

    private static IEnumerable<ArtNode> ChildrenIter(Children children)
    {
        switch (children)
        {
            case Children.Node4 n4:
                foreach (var c in n4.Children) yield return c;
                break;
            case Children.Node16 n16:
                foreach (var c in n16.Children) yield return c;
                break;
            case Children.Node48 n48:
                foreach (var c in n48.Children)
                    if (c is not null) yield return c;
                break;
            case Children.Node256 n256:
                foreach (var c in n256.Children)
                    if (c is not null) yield return c;
                break;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static int CommonPrefixLength(byte[] a, int offA, byte[] b, int offB)
    {
        int len = Math.Min(a.Length - offA, b.Length - offB);
        int i = 0;
        while (i < len && a[offA + i] == b[offB + i]) i++;
        return i;
    }

    private static byte[] SubArray(byte[] src, int offset, int length)
    {
        if (length <= 0) return Array.Empty<byte>();
        var result = new byte[length];
        Array.Copy(src, offset, result, 0, length);
        return result;
    }

    private static bool StartsWith(byte[] src, byte[] prefix)
    {
        if (src.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
            if (src[i] != prefix[i]) return false;
        return true;
    }

    private static int? IndexOf(List<byte> keys, byte b)
    {
        for (int i = 0; i < keys.Count; i++)
            if (keys[i] == b) return i;
        return null;
    }

    private static int InsertPosition(List<byte> keys, byte b)
    {
        for (int i = 0; i < keys.Count; i++)
            if (keys[i] > b) return i;
        return keys.Count;
    }

    private static int CountNonNull(ArtNode?[] arr)
    {
        int c = 0;
        foreach (var x in arr) if (x is not null) c++;
        return c;
    }

    // ── Node type ───────────────────────────────────────────────────────────
    // A single mutable class with a Kind discriminant so leaf→inner splits can
    // mutate the node in place, preserving the parent's child reference.

    private enum NodeKind { Leaf, Inner }

    private sealed class ArtNode
    {
        public NodeKind Kind;
        // Leaf state
        public string? Key;
        public V? Value;
        // Inner state
        public byte[]? Prefix;
        public Children? Children;
        public bool HasValue;
        public string? TerminalKey;

        public static ArtNode NewLeaf(string key, V value) => new()
        {
            Kind = NodeKind.Leaf,
            Key = key,
            Value = value,
        };

        public static ArtNode NewInner(byte[] prefix, Children children, V? value, bool hasValue, string? terminalKey) => new()
        {
            Kind = NodeKind.Inner,
            Prefix = prefix,
            Children = children,
            Value = value,
            HasValue = hasValue,
            TerminalKey = terminalKey,
        };

        public void BecomeInner(byte[] prefix, Children children, V? value, string? terminalKey)
        {
            Kind = NodeKind.Inner;
            Key = null;
            Prefix = prefix;
            Children = children;
            Value = value;
            HasValue = value is not null || terminalKey is not null;
            TerminalKey = terminalKey;
        }

        public Children TakeChildren()
        {
            var c = Children!;
            Children = Children.Node4Empty();
            return c;
        }

        public string? TakeTerminalKey()
        {
            var k = TerminalKey;
            TerminalKey = null;
            return k;
        }
    }

    // ── Children storage ───────────────────────────────────────────────────
    // Nested inside the generic tree so it can reference ArtNode and V.

    private abstract class Children
    {
        public sealed class Node4 : Children
        {
            public List<byte> Keys;
            public List<ArtNode> Children;
            public Node4(List<byte> keys, List<ArtNode> children) { Keys = keys; Children = children; }
        }

        public sealed class Node16 : Children
        {
            public List<byte> Keys;
            public List<ArtNode> Children;
            public Node16(List<byte> keys, List<ArtNode> children) { Keys = keys; Children = children; }
        }

        public sealed class Node48 : Children
        {
            public byte[] Index;
            public List<ArtNode?> Children;
            public int Count;
            public Node48(byte[] index, List<ArtNode?> children, int count)
            { Index = index; Children = children; Count = count; }
        }

        public sealed class Node256 : Children
        {
            public ArtNode?[] Children;
            public Node256(ArtNode?[] children) { Children = children; }
        }

        public static Node4 Node4Empty() => new(new List<byte>(), new List<ArtNode>());
    }
}

/// <summary>Distribution of node types in the tree. Upstream <c>NodeDistribution</c>.</summary>
public sealed class NodeDistribution
{
    public int Leaves { get; set; }
    public int Node4 { get; set; }
    public int Node16 { get; set; }
    public int Node48 { get; set; }
    public int Node256 { get; set; }
}
