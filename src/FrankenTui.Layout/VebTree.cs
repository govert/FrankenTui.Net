// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/veb_tree.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust's T: Clone contract maps to ICloneable when implemented and
// normal C# value/reference semantics otherwise. Vec/HashMap storage maps to
// immutable array snapshots and Dictionary while preserving all observable
// vEB, child, and DFS ordering.

namespace FrankenTui.Layout;

/// <summary>A node in the logical tree before van Emde Boas layout.</summary>
public sealed class TreeNode<T> : ICloneable
{
    public TreeNode(uint id, T data, IEnumerable<uint> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        Id = id;
        Data = data;
        Children = Array.AsReadOnly(children.ToArray());
    }

    public uint Id { get; }

    public T Data { get; }

    public IReadOnlyList<uint> Children { get; }

    public static TreeNode<T> New(uint id, T data, IEnumerable<uint> children) =>
        new(id, data, children);

    public TreeNode<T> Clone() => new(Id, CloneValue(Data), Children);

    object ICloneable.Clone() => Clone();

    private static TValue CloneValue<TValue>(TValue value) =>
        value is ICloneable cloneable ? (TValue)cloneable.Clone() : value;
}

/// <summary>A node stored in the cache-oblivious flat-array order.</summary>
public sealed class VebEntry<T> : ICloneable
{
    public VebEntry(
        uint id,
        T data,
        IEnumerable<uint> childIndices,
        uint parentIndex,
        ushort depth)
    {
        Id = id;
        Data = data;
        ChildIndices = Array.AsReadOnly(childIndices.ToArray());
        ParentIndex = parentIndex;
        Depth = depth;
    }

    public uint Id { get; }

    public T Data { get; }

    public IReadOnlyList<uint> ChildIndices { get; }

    public uint ParentIndex { get; }

    public ushort Depth { get; }

    public override string ToString() =>
        $"VebEntry {{ Id = {Id}, Data = {Data}, ParentIndex = {ParentIndex}, Depth = {Depth} }}";

    public VebEntry<T> Clone() =>
        new(Id, CloneValue(Data), ChildIndices, ParentIndex, Depth);

    object ICloneable.Clone() => Clone();

    private static TValue CloneValue<TValue>(TValue value) =>
        value is ICloneable cloneable ? (TValue)cloneable.Clone() : value;
}

/// <summary>A logical tree flattened into recursive van Emde Boas memory order.</summary>
public sealed class VebTree<T> : ICloneable
{
    private readonly VebEntry<T>[] _nodes;
    private readonly Dictionary<uint, uint> _index;

    private VebTree(VebEntry<T>[] nodes, Dictionary<uint, uint> index)
    {
        _nodes = nodes;
        _index = index;
    }

    public static VebTree<T> Build(IEnumerable<TreeNode<T>> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TreeNode<T>[] logicalNodes = input.ToArray();
        if (logicalNodes.Length == 0)
            return new VebTree<T>([], []);

        var nodeMap = new Dictionary<uint, TreeNode<T>>(logicalNodes.Length);
        foreach (TreeNode<T> node in logicalNodes)
        {
            ArgumentNullException.ThrowIfNull(node);
            nodeMap[node.Id] = node;
        }

        var allChildren = new HashSet<uint>(
            logicalNodes.SelectMany(static node => node.Children));
        uint rootId = logicalNodes.FirstOrDefault(node => !allChildren.Contains(node.Id))?.Id
            ?? logicalNodes[0].Id;

        var depths = new Dictionary<uint, ushort>();
        var depthQueue = new Queue<(uint Id, ushort Depth)>();
        depthQueue.Enqueue((rootId, 0));
        while (depthQueue.Count > 0)
        {
            (uint id, ushort depth) = depthQueue.Dequeue();
            depths[id] = depth;
            if (!nodeMap.TryGetValue(id, out TreeNode<T>? node)) continue;
            foreach (uint childId in node.Children)
                depthQueue.Enqueue((childId, checked((ushort)(depth + 1))));
        }

        var dfsOrder = new List<uint>(logicalNodes.Length);
        var stack = new Stack<uint>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            uint id = stack.Pop();
            dfsOrder.Add(id);
            if (!nodeMap.TryGetValue(id, out TreeNode<T>? node)) continue;
            for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
                stack.Push(node.Children[childIndex]);
        }

        IReadOnlyList<uint> vebOrder = VebLayoutOrder(dfsOrder, nodeMap);
        var idToPosition = new Dictionary<uint, uint>(vebOrder.Count);
        for (int position = 0; position < vebOrder.Count; position++)
            idToPosition[vebOrder[position]] = checked((uint)position);

        var parentMap = new Dictionary<uint, uint>();
        foreach (TreeNode<T> node in logicalNodes)
        {
            foreach (uint childId in node.Children)
                parentMap[childId] = node.Id;
        }

        var entries = new VebEntry<T>[vebOrder.Count];
        for (int position = 0; position < vebOrder.Count; position++)
        {
            uint id = vebOrder[position];
            TreeNode<T> node = nodeMap[id];
            uint[] childIndices = node.Children
                .Where(idToPosition.ContainsKey)
                .Select(childId => idToPosition[childId])
                .ToArray();
            uint parentIndex = parentMap.TryGetValue(id, out uint parentId) &&
                               idToPosition.TryGetValue(parentId, out uint parentPosition)
                ? parentPosition
                : uint.MaxValue;
            entries[position] = new VebEntry<T>(
                id,
                CloneValue(node.Data),
                childIndices,
                parentIndex,
                depths.GetValueOrDefault(id));
        }

        return new VebTree<T>(entries, idToPosition);
    }

    public int Len() => _nodes.Length;

    public bool IsEmpty() => _nodes.Length == 0;

    public VebEntry<T>? Get(uint id) =>
        _index.TryGetValue(id, out uint position) ? _nodes[checked((int)position)] : null;

    public VebEntry<T>? GetByIndex(uint index) =>
        index < (uint)_nodes.Length ? _nodes[(int)index] : null;

    public IEnumerable<VebEntry<T>> Iter() => _nodes;

    public IReadOnlyList<VebEntry<T>> IterDfs()
    {
        if (_nodes.Length == 0) return Array.Empty<VebEntry<T>>();
        var result = new List<VebEntry<T>>(_nodes.Length);
        var stack = new Stack<uint>();
        stack.Push(0);
        while (stack.Count > 0)
        {
            uint index = stack.Pop();
            if (index >= (uint)_nodes.Length) continue;
            VebEntry<T> entry = _nodes[(int)index];
            result.Add(entry);
            for (int child = entry.ChildIndices.Count - 1; child >= 0; child--)
                stack.Push(entry.ChildIndices[child]);
        }
        return result;
    }

    public VebEntry<T>? Root() => _nodes.FirstOrDefault();

    public IReadOnlyList<VebEntry<T>> AsSlice() => Array.AsReadOnly(_nodes);

    public VebTree<T> Clone()
    {
        var nodes = new VebEntry<T>[_nodes.Length];
        for (int index = 0; index < _nodes.Length; index++)
        {
            VebEntry<T> entry = _nodes[index];
            nodes[index] = new VebEntry<T>(
                entry.Id,
                CloneValue(entry.Data),
                entry.ChildIndices,
                entry.ParentIndex,
                entry.Depth);
        }
        return new VebTree<T>(nodes, new Dictionary<uint, uint>(_index));
    }

    object ICloneable.Clone() => Clone();

    private static IReadOnlyList<uint> VebLayoutOrder(
        IReadOnlyList<uint> dfsOrder,
        IReadOnlyDictionary<uint, TreeNode<T>> nodeMap)
    {
        if (dfsOrder.Count <= 1) return dfsOrder.ToArray();

        uint root = dfsOrder[0];
        var depths = new Dictionary<uint, ushort>();
        var queue = new Queue<(uint Id, ushort Depth)>();
        var subtree = new HashSet<uint>(dfsOrder);
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            (uint id, ushort depth) = queue.Dequeue();
            depths[id] = depth;
            if (!nodeMap.TryGetValue(id, out TreeNode<T>? node)) continue;
            foreach (uint childId in node.Children)
            {
                if (subtree.Contains(childId))
                    queue.Enqueue((childId, checked((ushort)(depth + 1))));
            }
        }

        ushort maximumDepth = depths.Count == 0 ? (ushort)0 : depths.Values.Max();
        if (maximumDepth <= 1) return dfsOrder.ToArray();
        ushort middleDepth = (ushort)(maximumDepth / 2);

        var top = new List<uint>();
        var bottomRoots = new List<uint>();
        foreach (uint id in dfsOrder)
        {
            ushort depth = depths.GetValueOrDefault(id);
            if (depth > middleDepth) continue;
            top.Add(id);
            if (!nodeMap.TryGetValue(id, out TreeNode<T>? node)) continue;
            foreach (uint childId in node.Children)
            {
                if (subtree.Contains(childId) && depths.GetValueOrDefault(childId) > middleDepth)
                    bottomRoots.Add(childId);
            }
        }

        var bottomSubtrees = new Dictionary<uint, IReadOnlyList<uint>>();
        foreach (uint bottomRoot in bottomRoots)
        {
            var bottomOrder = new List<uint>();
            var bottomStack = new Stack<uint>();
            bottomStack.Push(bottomRoot);
            while (bottomStack.Count > 0)
            {
                uint id = bottomStack.Pop();
                if (!subtree.Contains(id)) continue;
                bottomOrder.Add(id);
                if (!nodeMap.TryGetValue(id, out TreeNode<T>? node)) continue;
                for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
                {
                    uint childId = node.Children[childIndex];
                    if (subtree.Contains(childId)) bottomStack.Push(childId);
                }
            }
            bottomSubtrees[bottomRoot] = bottomOrder;
        }

        var result = new List<uint>(dfsOrder.Count);
        result.AddRange(VebLayoutOrder(top, nodeMap));
        foreach (uint bottomRoot in bottomRoots)
        {
            if (bottomSubtrees.TryGetValue(bottomRoot, out IReadOnlyList<uint>? bottomOrder))
                result.AddRange(VebLayoutOrder(bottomOrder, nodeMap));
        }
        return result;
    }

    private static TValue CloneValue<TValue>(TValue value) =>
        value is ICloneable cloneable ? (TValue)cloneable.Clone() : value;
}
