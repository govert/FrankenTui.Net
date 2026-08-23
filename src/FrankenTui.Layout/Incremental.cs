// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/incremental.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust usize counters/capacities map to non-negative Int32 values.
// Rust Result<(), CycleError> maps to a nullable CycleError return (null is success),
// matching the managed DepGraph port. Owned Rects map to IReadOnlyList<Rect> with
// defensive arrays so callers cannot mutate cached state. graph()/graph_mut() both
// return the same managed DepGraph reference because C# has no borrow-mode distinction.
// Rust Debug maps to ToString(); the 64-bit rustc_hash 2.1 FxHasher algorithm used for
// observable result hashes is reproduced exactly.

using System.Numerics;
using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>Statistics for a single incremental-layout pass.</summary>
public record struct IncrementalStats
{
    /// <summary>Nodes recomputed in this pass.</summary>
    public int Recomputed { get; set; }

    /// <summary>Nodes returned from cache.</summary>
    public int Cached { get; set; }

    /// <summary>Total <see cref="IncrementalLayout.GetOrCompute"/> calls in this pass.</summary>
    public int Total { get; set; }

    /// <summary>Current number of entries in the cache.</summary>
    public int CacheEntries { get; set; }

    /// <summary>Cache hit rate as a fraction from 0.0 through 1.0.</summary>
    public readonly double HitRate() => Total == 0 ? 0.0 : (double)Cached / Total;
}

/// <summary>
/// Dependency-driven incremental layout with a per-node result cache.
/// Clean nodes in the same area return their cached rectangles; dirty nodes recompute.
/// </summary>
public sealed class IncrementalLayout
{
    private readonly DepGraph _graph;
    private readonly Dictionary<NodeId, CachedNodeLayout> _cache;
    private IncrementalStats _stats;
    private bool _forceFull;

    /// <summary>Create an empty incremental-layout engine.</summary>
    public IncrementalLayout()
        : this(0)
    {
    }

    private IncrementalLayout(int nodeCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nodeCapacity);

        _graph = nodeCapacity == 0
            ? DepGraph.New()
            : DepGraph.WithCapacity(nodeCapacity, nodeCapacity);
        _cache = new Dictionary<NodeId, CachedNodeLayout>(nodeCapacity);
    }

    /// <summary>Create an empty incremental-layout engine.</summary>
    public static IncrementalLayout New() => new();

    /// <summary>Create an engine with preallocated node and cache capacity.</summary>
    public static IncrementalLayout WithCapacity(int nodeCapacity) => new(nodeCapacity);

    /// <summary>
    /// Create an engine configured from <c>FRANKENTUI_FULL_LAYOUT</c>.
    /// Values <c>1</c>, <c>true</c>, and <c>yes</c> enable force-full mode,
    /// using case-insensitive comparison.
    /// </summary>
    public static IncrementalLayout FromEnv()
    {
        IncrementalLayout layout = new();
        layout._forceFull = ParseForceFull(Environment.GetEnvironmentVariable("FRANKENTUI_FULL_LAYOUT"));
        return layout;
    }

    /// <summary>Add a layout node, optionally dependent on a parent.</summary>
    public NodeId AddNode(NodeId? parent)
    {
        NodeId id = _graph.AddNode();
        if (parent is NodeId parentId)
        {
            _graph.SetParent(id, parentId);
            _ = _graph.AddEdge(id, parentId);
        }

        _graph.MarkDirty(id);
        return id;
    }

    /// <summary>Remove a node, evict its cache entry, and dirty its ancestor chain.</summary>
    public void RemoveNode(NodeId id)
    {
        _cache.Remove(id);
        if (_graph.Parent(id) is NodeId parent)
        {
            MarkDirtyWithAncestors(parent);
        }

        _graph.RemoveNode(id);
    }

    /// <summary>Add a custom edge indicating that <paramref name="from"/> depends on <paramref name="to"/>.</summary>
    /// <returns><see langword="null"/> on success, or the detected cycle.</returns>
    public CycleError? AddDependency(NodeId from, NodeId to) => _graph.AddEdge(from, to);

    /// <summary>Hash-deduplicated dirty marking for one input dimension.</summary>
    public void MarkChanged(NodeId id, InputKind kind, ulong newHash) =>
        _graph.MarkChanged(id, kind, newHash);

    /// <summary>Force-mark a node dirty without hash comparison.</summary>
    public void MarkDirty(NodeId id) => _graph.MarkDirty(id);

    /// <summary>Mark a node and every parent through the root dirty.</summary>
    public void MarkDirtyWithAncestors(NodeId id)
    {
        _graph.MarkDirty(id);
        NodeId current = id;
        while (_graph.Parent(current) is NodeId parent)
        {
            _graph.MarkDirty(parent);
            current = parent;
        }
    }

    /// <summary>Propagate pending dirtiness and return dirty nodes in DFS pre-order.</summary>
    public IReadOnlyList<NodeId> Propagate() => _graph.Propagate();

    /// <summary>Return whether a node is currently dirty.</summary>
    public bool IsDirty(NodeId id) => _graph.IsDirty(id);

    /// <summary>Return the cached layout or compute, store, and return a new result.</summary>
    public IReadOnlyList<Rect> GetOrCompute(
        NodeId id,
        Rect area,
        Func<Rect, IReadOnlyList<Rect>> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        _stats.Total++;

        if (!_forceFull &&
            !_graph.IsDirty(id) &&
            _cache.TryGetValue(id, out CachedNodeLayout? cached) &&
            cached.Area == area)
        {
            _stats.Cached++;
            return cached.Rects.ToArray();
        }

        IReadOnlyList<Rect> computed = compute(area)
            ?? throw new InvalidOperationException("The incremental-layout computation returned null.");
        Rect[] result = computed.ToArray();
        _cache[id] = new CachedNodeLayout(area, result.ToArray(), HashRects(result));
        _graph.Clean(id);
        _stats.Recomputed++;
        return result;
    }

    /// <summary>Return a read-only view of a cached result without recomputing.</summary>
    public IReadOnlyList<Rect>? CachedRects(NodeId id) =>
        _cache.TryGetValue(id, out CachedNodeLayout? cached)
            ? Array.AsReadOnly(cached.Rects)
            : null;

    /// <summary>
    /// Return whether the node has no previously computed result. This preserves the
    /// upstream method's current behavior exactly.
    /// </summary>
    public bool ResultChanged(NodeId id) => !_cache.ContainsKey(id);

    /// <summary>Return the 64-bit FxHash of the node's last computed rectangles.</summary>
    public ulong? ResultHash(NodeId id) =>
        _cache.TryGetValue(id, out CachedNodeLayout? cached) ? cached.ResultHash : null;

    /// <summary>Enable or disable full recomputation on every call.</summary>
    public void SetForceFull(bool force) => _forceFull = force;

    /// <summary>Return whether force-full mode is active.</summary>
    public bool ForceFull() => _forceFull;

    /// <summary>Return a value snapshot of the current-pass statistics.</summary>
    public IncrementalStats Stats()
    {
        IncrementalStats snapshot = _stats;
        snapshot.CacheEntries = _cache.Count;
        return snapshot;
    }

    /// <summary>Reset per-pass statistics.</summary>
    public void ResetStats() => _stats = default;

    /// <summary>Mark all graph nodes clean and advance the graph generation.</summary>
    public void CleanAll() => _graph.CleanAll();

    /// <summary>Mark every graph node dirty.</summary>
    public void InvalidateAll() => _graph.InvalidateAll();

    /// <summary>Clear all cached layout results.</summary>
    public void ClearCache() => _cache.Clear();

    /// <summary>Return the underlying dependency graph for read-oriented use.</summary>
    public DepGraph Graph() => _graph;

    /// <summary>Return the underlying dependency graph for mutation.</summary>
    public DepGraph GraphMut() => _graph;

    /// <summary>Return the number of cached node results.</summary>
    public int CacheLen() => _cache.Count;

    /// <summary>Return the number of live graph nodes.</summary>
    public int NodeCount() => _graph.NodeCount();

    /// <inheritdoc />
    public override string ToString() =>
        $"IncrementalLayout {{ nodes: {_graph.NodeCount()}, cache_entries: {_cache.Count}, force_full: {_forceFull.ToString().ToLowerInvariant()} }}";

    private static bool ParseForceFull(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase));

    private static ulong HashRects(IEnumerable<Rect> rects)
    {
        const ulong multiplier = 0xf1357aea2e62a9c5UL;
        ulong hash = 0;

        foreach (Rect rect in rects)
        {
            hash = AddToFxHash(hash, rect.X, multiplier);
            hash = AddToFxHash(hash, rect.Y, multiplier);
            hash = AddToFxHash(hash, rect.Width, multiplier);
            hash = AddToFxHash(hash, rect.Height, multiplier);
        }

        return BitOperations.RotateLeft(hash, 26);
    }

    private static ulong AddToFxHash(ulong hash, ushort value, ulong multiplier) =>
        unchecked((hash + value) * multiplier);

    private sealed record CachedNodeLayout(Rect Area, Rect[] Rects, ulong ResultHash);
}
