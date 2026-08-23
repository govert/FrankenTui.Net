// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/dep_graph.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust Result<(), CycleError> maps to a nullable CycleError return;
// null is success. TryAddEdge is also provided for conventional C# consumption.
// Invalid raw node handles throw ArgumentOutOfRangeException instead of allowing
// Rust indexing panics (and possible partial mutation). Traversal and invalidation
// ordering are otherwise preserved exactly.

using System.Runtime.InteropServices;

namespace FrankenTui.Layout;

/// <summary>A compact, stable handle into a <see cref="DepGraph"/>.</summary>
public readonly record struct NodeId(uint Value) : IComparable<NodeId>
{
    public static NodeId FromRaw(uint index) => new(index);

    public uint Raw() => Value;

    public int CompareTo(NodeId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"N{Value}";
}

/// <summary>The independently hashed input domain that changed on a layout node.</summary>
public enum InputKind
{
    Constraint,
    Content,
    Style,
}

/// <summary>Describes an edge insertion that would introduce a dependency cycle.</summary>
public sealed class CycleError : Exception, IEquatable<CycleError>
{
    public CycleError(NodeId from, NodeId to)
        : base($"layout cycle detected: {from} → {to} would create a cycle")
    {
        From = from;
        To = to;
    }

    public NodeId From { get; }

    public NodeId To { get; }

    public bool Equals(CycleError? other) =>
        other is not null && From == other.From && To == other.To;

    public override bool Equals(object? obj) => obj is CycleError other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(From, To);

    public override string ToString() => Message;
}

/// <summary>
/// Dependency graph for deterministic incremental-layout invalidation.
/// </summary>
public sealed class DepGraph
{
    private const uint NoParent = uint.MaxValue;

    private readonly List<DepNode> _nodes;
    private readonly List<List<NodeId>> _forwardAdjacency;
    private readonly List<List<NodeId>> _reverseAdjacency;
    private readonly List<NodeId> _pendingDirty;
    private readonly List<uint> _freeList;
    private uint _currentGeneration;

    public DepGraph()
        : this(0, 0)
    {
    }

    private DepGraph(int nodeCapacity, int edgeCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nodeCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(edgeCapacity);
        _nodes = new List<DepNode>(nodeCapacity);
        _forwardAdjacency = new List<List<NodeId>>(nodeCapacity);
        _reverseAdjacency = new List<List<NodeId>>(nodeCapacity);
        _pendingDirty = [];
        _freeList = [];
        _currentGeneration = 1;
    }

    public static DepGraph New() => new();

    public static DepGraph WithCapacity(int nodeCapacity, int edgeCapacity) =>
        new(nodeCapacity, edgeCapacity);

    public NodeId AddNode()
    {
        if (_freeList.Count > 0)
        {
            int last = _freeList.Count - 1;
            uint slot = _freeList[last];
            _freeList.RemoveAt(last);
            int index = CheckedIndex(slot);
            _nodes[index] = DepNode.New(_currentGeneration);
            _forwardAdjacency[index].Clear();
            _reverseAdjacency[index].Clear();
            return new NodeId(slot);
        }

        uint raw = checked((uint)_nodes.Count);
        _nodes.Add(DepNode.New(_currentGeneration));
        _forwardAdjacency.Add([]);
        _reverseAdjacency.Add([]);
        return new NodeId(raw);
    }

    public void RemoveNode(NodeId id)
    {
        int index = ToIndex(id);
        if (index < 0 || _nodes[index].Generation == 0)
            return;

        foreach (NodeId reverse in _reverseAdjacency[index])
        {
            int childIndex = ToIndex(reverse);
            if (childIndex < 0) continue;
            DepNode child = _nodes[childIndex];
            if (child.Parent == id.Value)
            {
                child.Parent = NoParent;
                _nodes[childIndex] = child;
            }
        }

        NodeId[] forwards = _forwardAdjacency[index].ToArray();
        _forwardAdjacency[index].Clear();
        foreach (NodeId forward in forwards)
        {
            int forwardIndex = ToIndex(forward);
            if (forwardIndex >= 0)
                _reverseAdjacency[forwardIndex].RemoveAll(candidate => candidate == id);
        }

        NodeId[] reverses = _reverseAdjacency[index].ToArray();
        _reverseAdjacency[index].Clear();
        foreach (NodeId reverse in reverses)
        {
            int reverseIndex = ToIndex(reverse);
            if (reverseIndex >= 0)
                _forwardAdjacency[reverseIndex].RemoveAll(candidate => candidate == id);
        }

        DepNode node = _nodes[index];
        node.Generation = 0;
        node.DirtyGeneration = 0;
        _nodes[index] = node;
        _freeList.Add(id.Value);
    }

    public int NodeCount() => _nodes.Count - _freeList.Count;

    public int EdgeCount() => _forwardAdjacency.Sum(static adjacency => adjacency.Count);

    public void SetParent(NodeId child, NodeId parent)
    {
        int childIndex = ToIndex(child);
        if (childIndex < 0) return;
        DepNode node = _nodes[childIndex];
        node.Parent = parent.Value;
        _nodes[childIndex] = node;
    }

    public NodeId? Parent(NodeId id)
    {
        int index = ToIndex(id);
        if (index < 0) return null;
        uint parent = _nodes[index].Parent;
        return parent == NoParent ? null : new NodeId(parent);
    }

    /// <summary>
    /// Add an edge indicating that <paramref name="from"/> depends on
    /// <paramref name="to"/>. Returns null on success or the rejected cycle.
    /// </summary>
    public CycleError? AddEdge(NodeId from, NodeId to)
    {
        RequireIndex(from, nameof(from));
        RequireIndex(to, nameof(to));
        if (from == to || CanReach(to, from))
            return new CycleError(from, to);

        _forwardAdjacency[CheckedIndex(from.Value)].Add(to);
        _reverseAdjacency[CheckedIndex(to.Value)].Add(from);
        return null;
    }

    public bool TryAddEdge(NodeId from, NodeId to, out CycleError? error)
    {
        error = AddEdge(from, to);
        return error is null;
    }

    public void MarkChanged(NodeId id, InputKind kind, ulong newHash)
    {
        int index = ToIndex(id);
        if (index < 0) return;
        DepNode node = _nodes[index];
        if (node.Generation == 0) return;

        ulong oldHash = kind switch
        {
            InputKind.Constraint => node.ConstraintHash,
            InputKind.Content => node.ContentHash,
            InputKind.Style => node.StyleHash,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        if (oldHash == newHash) return;

        switch (kind)
        {
            case InputKind.Constraint:
                node.ConstraintHash = newHash;
                break;
            case InputKind.Content:
                node.ContentHash = newHash;
                break;
            case InputKind.Style:
                node.StyleHash = newHash;
                break;
        }
        node.DirtyGeneration = _currentGeneration;
        _nodes[index] = node;
        _pendingDirty.Add(id);
    }

    public void MarkDirty(NodeId id)
    {
        int index = ToIndex(id);
        if (index < 0) return;
        DepNode node = _nodes[index];
        if (node.Generation == 0) return;
        node.DirtyGeneration = _currentGeneration;
        _nodes[index] = node;
        _pendingDirty.Add(id);
    }

    /// <summary>
    /// Propagate pending dirtiness through reverse edges and return all currently
    /// dirty nodes in deterministic DFS pre-order/topological order.
    /// </summary>
    public IReadOnlyList<NodeId> Propagate()
    {
        if (_pendingDirty.Count == 0)
            return Array.Empty<NodeId>();

        var queue = new Queue<NodeId>();
        var visited = new bool[_nodes.Count];
        foreach (NodeId id in _pendingDirty)
        {
            int index = ToIndex(id);
            if (index >= 0 && !visited[index])
            {
                visited[index] = true;
                queue.Enqueue(id);
            }
        }
        _pendingDirty.Clear();

        while (queue.Count > 0)
        {
            NodeId current = queue.Dequeue();
            int index = CheckedIndex(current.Value);
            DepNode node = _nodes[index];
            if (node.Generation == 0) continue;
            node.DirtyGeneration = _currentGeneration;
            _nodes[index] = node;

            foreach (NodeId dependent in _reverseAdjacency[index])
            {
                int dependentIndex = ToIndex(dependent);
                if (dependentIndex >= 0 && !visited[dependentIndex])
                {
                    visited[dependentIndex] = true;
                    queue.Enqueue(dependent);
                }
            }
        }

        return CollectDirtyDfsPreorder();
    }

    public bool IsDirty(NodeId id)
    {
        int index = ToIndex(id);
        return index >= 0 && _nodes[index].Generation != 0 && _nodes[index].IsDirty;
    }

    public void Clean(NodeId id)
    {
        int index = ToIndex(id);
        if (index < 0) return;
        DepNode node = _nodes[index];
        node.Generation = _currentGeneration;
        node.DirtyGeneration = 0;
        _nodes[index] = node;
    }

    public void CleanAll()
    {
        _currentGeneration = unchecked(_currentGeneration + 1);
        if (_currentGeneration == 0) _currentGeneration = 1;
        for (int index = 0; index < _nodes.Count; index++)
        {
            DepNode node = _nodes[index];
            if (node.Generation == 0) continue;
            node.Generation = _currentGeneration;
            node.DirtyGeneration = 0;
            _nodes[index] = node;
        }
        _pendingDirty.Clear();
    }

    public ulong? ConstraintHash(NodeId id) => Hash(id, InputKind.Constraint);

    public ulong? ContentHash(NodeId id) => Hash(id, InputKind.Content);

    public ulong? StyleHash(NodeId id) => Hash(id, InputKind.Style);

    public IEnumerable<NodeId> DirtyNodes()
    {
        for (int index = 0; index < _nodes.Count; index++)
        {
            DepNode node = _nodes[index];
            if (node.Generation != 0 && node.IsDirty)
                yield return new NodeId((uint)index);
        }
    }

    public int DirtyCount() => _nodes.Count(static node => node.Generation != 0 && node.IsDirty);

    public IReadOnlyList<NodeId> Dependencies(NodeId id)
    {
        int index = ToIndex(id);
        return index < 0 || _nodes[index].Generation == 0
            ? Array.Empty<NodeId>()
            : _forwardAdjacency[index].AsReadOnly();
    }

    public IReadOnlyList<NodeId> Dependents(NodeId id)
    {
        int index = ToIndex(id);
        return index < 0 || _nodes[index].Generation == 0
            ? Array.Empty<NodeId>()
            : _reverseAdjacency[index].AsReadOnly();
    }

    public void InvalidateAll()
    {
        for (int index = 0; index < _nodes.Count; index++)
        {
            DepNode node = _nodes[index];
            if (node.Generation == 0) continue;
            node.DirtyGeneration = _currentGeneration;
            _nodes[index] = node;
            _pendingDirty.Add(new NodeId((uint)index));
        }
    }

    private bool CanReach(NodeId from, NodeId to)
    {
        var visited = new bool[_nodes.Count];
        var stack = new Stack<NodeId>();
        stack.Push(from);
        while (stack.Count > 0)
        {
            NodeId current = stack.Pop();
            if (current == to) return true;
            int index = ToIndex(current);
            if (index < 0 || visited[index]) continue;
            visited[index] = true;
            if (_nodes[index].Generation == 0) continue;
            foreach (NodeId dependency in _forwardAdjacency[index])
            {
                int dependencyIndex = CheckedIndex(dependency.Value);
                if (!visited[dependencyIndex]) stack.Push(dependency);
            }
        }
        return false;
    }

    private IReadOnlyList<NodeId> CollectDirtyDfsPreorder()
    {
        var roots = new List<NodeId>();
        for (int index = 0; index < _nodes.Count; index++)
        {
            DepNode node = _nodes[index];
            if (node.Generation == 0 || !node.IsDirty) continue;
            bool hasDirtyDependency = _forwardAdjacency[index].Any(dependency =>
            {
                int dependencyIndex = ToIndex(dependency);
                return dependencyIndex >= 0 &&
                       _nodes[dependencyIndex].Generation != 0 &&
                       _nodes[dependencyIndex].IsDirty;
            });
            if (!hasDirtyDependency) roots.Add(new NodeId((uint)index));
        }
        roots.Sort();

        var result = new List<NodeId>();
        var visited = new bool[_nodes.Count];
        for (int index = roots.Count - 1; index >= 0; index--)
            DfsPostorder(roots[index], result, visited);
        result.Reverse();
        return result;
    }

    private void DfsPostorder(NodeId id, List<NodeId> result, bool[] visited)
    {
        int index = ToIndex(id);
        if (index < 0 || visited[index]) return;
        DepNode node = _nodes[index];
        if (node.Generation == 0 || !node.IsDirty) return;
        visited[index] = true;

        List<NodeId> children = _reverseAdjacency[index]
            .Where(child =>
            {
                int childIndex = ToIndex(child);
                return childIndex >= 0 && !visited[childIndex] &&
                       _nodes[childIndex].Generation != 0 && _nodes[childIndex].IsDirty;
            })
            .Order()
            .ToList();
        for (int childIndex = children.Count - 1; childIndex >= 0; childIndex--)
            DfsPostorder(children[childIndex], result, visited);
        result.Add(id);
    }

    private ulong? Hash(NodeId id, InputKind kind)
    {
        int index = ToIndex(id);
        if (index < 0 || _nodes[index].Generation == 0) return null;
        DepNode node = _nodes[index];
        return kind switch
        {
            InputKind.Constraint => node.ConstraintHash,
            InputKind.Content => node.ContentHash,
            InputKind.Style => node.StyleHash,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private int ToIndex(NodeId id) => id.Value < (uint)_nodes.Count ? (int)id.Value : -1;

    private int RequireIndex(NodeId id, string parameterName)
    {
        int index = ToIndex(id);
        return index >= 0
            ? index
            : throw new ArgumentOutOfRangeException(parameterName, id, "NodeId is outside this graph.");
    }

    private static int CheckedIndex(uint value) => checked((int)value);

    [StructLayout(LayoutKind.Sequential)]
    private struct DepNode
    {
        public uint Generation;
        public uint DirtyGeneration;
        public ulong ConstraintHash;
        public ulong ContentHash;
        public ulong StyleHash;
        public uint Parent;

        public readonly bool IsDirty => DirtyGeneration >= Generation;

        public static DepNode New(uint generation) => new()
        {
            Generation = generation,
            DirtyGeneration = 0,
            ConstraintHash = 0,
            ContentHash = 0,
            StyleHash = 0,
            Parent = NoParent,
        };
    }
}
