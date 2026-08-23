// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/incremental.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// The 31 upstream inline tests are represented one-for-one below. Two additional
// managed contract tests close public members not exercised by the inline Rust suite.

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class IncrementalTests
{
    [Fact]
    public void NewNodeIsDirty()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        Assert.True(incremental.IsDirty(node));
    }

    [Fact]
    public void GetOrComputeCaches()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();

        Rect area = Area(80, 24);
        int calls = 0;
        IReadOnlyList<Rect> first = incremental.GetOrCompute(node, area, value =>
        {
            calls++;
            return SplitEqual(value, 2);
        });
        IReadOnlyList<Rect> second = incremental.GetOrCompute(node, area, value =>
        {
            calls++;
            return SplitEqual(value, 2);
        });

        Assert.Equal(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DirtyNodeRecomputes()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));
        incremental.MarkDirty(node);
        incremental.Propagate();

        int calls = 0;
        incremental.GetOrCompute(node, area, value =>
        {
            calls++;
            return SplitEqual(value, 2);
        });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void AreaChangeTriggersRecompute()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();
        incremental.GetOrCompute(node, Area(80, 24), value => SplitEqual(value, 2));

        int calls = 0;
        incremental.GetOrCompute(node, Area(120, 40), value =>
        {
            calls++;
            return SplitEqual(value, 2);
        });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void SameAreaSameNodeCached()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));
        int calls = 0;
        incremental.GetOrCompute(node, area, _ =>
        {
            calls++;
            return Array.Empty<Rect>();
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DirtyParentDirtiesChild()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId root = incremental.AddNode(null);
        NodeId child = incremental.AddNode(root);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(root, area, value => SplitEqual(value, 1));
        incremental.GetOrCompute(child, area, value => SplitEqual(value, 2));
        incremental.MarkDirty(root);
        incremental.Propagate();

        Assert.True(incremental.IsDirty(root));
        Assert.True(incremental.IsDirty(child));
    }

    [Fact]
    public void CleanSiblingNotAffectedByDirtySibling()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(3);
        incremental.Propagate();
        Rect area = Area(120, 24);
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        incremental.MarkDirty(children[1]);
        incremental.Propagate();

        Assert.False(incremental.IsDirty(root));
        Assert.False(incremental.IsDirty(children[0]));
        Assert.True(incremental.IsDirty(children[1]));
        Assert.False(incremental.IsDirty(children[2]));
    }

    [Fact]
    public void FlexSiblingsDirtyViaParent()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(3);
        incremental.Propagate();
        Rect area = Area(120, 24);
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        incremental.MarkDirty(children[1]);
        incremental.MarkDirty(root);
        incremental.Propagate();

        Assert.True(incremental.IsDirty(root));
        Assert.All(children, child => Assert.True(incremental.IsDirty(child)));
    }

    [Fact]
    public void StatsTrackHitsAndMisses()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(3);
        incremental.Propagate();

        Rect area = Area(120, 24);
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        IncrementalStats stats = incremental.Stats();
        Assert.Equal(4, stats.Recomputed);
        Assert.Equal(0, stats.Cached);
        Assert.Equal(4, stats.Total);

        incremental.ResetStats();
        rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        stats = incremental.Stats();
        Assert.Equal(0, stats.Recomputed);
        Assert.Equal(4, stats.Cached);
        Assert.Equal(4, stats.Total);
        Assert.InRange(Math.Abs(stats.HitRate() - 1.0), 0.0, 0.000999);
    }

    [Fact]
    public void StatsPartialDirty()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(4);
        incremental.Propagate();

        Rect area = Area(160, 24);
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 4));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        incremental.ResetStats();
        incremental.MarkDirty(children[2]);
        incremental.Propagate();

        rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 4));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        IncrementalStats stats = incremental.Stats();
        Assert.Equal(1, stats.Recomputed);
        Assert.Equal(4, stats.Cached);
        Assert.Equal(5, stats.Total);
    }

    [Fact]
    public void ForceFullBypassesCache()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));
        incremental.SetForceFull(true);
        Assert.True(incremental.ForceFull());

        int calls = 0;
        incremental.GetOrCompute(node, area, value =>
        {
            calls++;
            return SplitEqual(value, 2);
        });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ForceFullProducesIdenticalResults()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(3);
        incremental.Propagate();
        Rect area = Area(120, 24);

        IReadOnlyList<Rect> rootIncremental = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        List<IReadOnlyList<Rect>> childrenIncremental = [];
        for (int index = 0; index < children.Length; index++)
        {
            childrenIncremental.Add(incremental.GetOrCompute(
                children[index], rootIncremental[index], value => SplitEqual(value, 2)));
        }

        incremental.SetForceFull(true);
        incremental.ResetStats();
        IReadOnlyList<Rect> rootFull = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        List<IReadOnlyList<Rect>> childrenFull = [];
        for (int index = 0; index < children.Length; index++)
        {
            childrenFull.Add(incremental.GetOrCompute(
                children[index], rootFull[index], value => SplitEqual(value, 2)));
        }

        Assert.Equal(rootIncremental, rootFull);
        AssertNestedRectsEqual(childrenIncremental, childrenFull);
    }

    [Fact]
    public void RemoveNodeEvictsCache()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId root = incremental.AddNode(null);
        NodeId child = incremental.AddNode(root);
        incremental.Propagate();

        incremental.GetOrCompute(child, Area(40, 24), value => SplitEqual(value, 1));
        Assert.NotNull(incremental.CachedRects(child));
        incremental.RemoveNode(child);
        Assert.Null(incremental.CachedRects(child));
    }

    [Fact]
    public void RemoveNodeDirtiesParent()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId root = incremental.AddNode(null);
        NodeId child = incremental.AddNode(root);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(root, area, value => SplitEqual(value, 1));
        incremental.GetOrCompute(child, area, value => SplitEqual(value, 1));
        incremental.RemoveNode(child);
        Assert.True(incremental.IsDirty(root));
    }

    [Fact]
    public void InvalidateAllForcesRecompute()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(3);
        incremental.Propagate();

        Rect area = Area(120, 24);
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        incremental.InvalidateAll();
        incremental.Propagate();
        Assert.True(incremental.IsDirty(root));
        Assert.All(children, child => Assert.True(incremental.IsDirty(child)));
    }

    [Fact]
    public void CleanAllResetsDirty()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(2);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(root, area, value => SplitEqual(value, 2));
        for (int index = 0; index < children.Length; index++)
        {
            Rect childArea = new((ushort)(index * 40), 0, 40, 24);
            incremental.GetOrCompute(children[index], childArea, value => SplitEqual(value, 1));
        }

        incremental.MarkDirty(root);
        incremental.Propagate();
        Assert.True(incremental.IsDirty(root));
        incremental.CleanAll();
        Assert.False(incremental.IsDirty(root));
    }

    [Fact]
    public void ClearCacheFreesMemory()
    {
        (IncrementalLayout incremental, NodeId root, _) = FlatTree(5);
        incremental.Propagate();
        incremental.GetOrCompute(root, Area(200, 24), value => SplitEqual(value, 5));
        Assert.True(incremental.CacheLen() > 0);

        incremental.ClearCache();
        Assert.Equal(0, incremental.CacheLen());
    }

    [Fact]
    public void MarkDirtyWithAncestorsPropagatesToSiblings()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] children) = FlatTree(3);
        incremental.Propagate();

        Rect area = Area(120, 24);
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 3));
        for (int index = 0; index < children.Length; index++)
        {
            incremental.GetOrCompute(children[index], rootRects[index], value => SplitEqual(value, 1));
        }

        incremental.MarkDirtyWithAncestors(children[1]);
        incremental.Propagate();
        Assert.True(incremental.IsDirty(root));
        Assert.All(children, child => Assert.True(incremental.IsDirty(child)));
    }

    [Fact]
    public void MarkDirtyWithAncestorsDeepChain()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId root = incremental.AddNode(null);
        NodeId a = incremental.AddNode(root);
        NodeId b = incremental.AddNode(a);
        NodeId c = incremental.AddNode(b);
        incremental.Propagate();

        Rect area = Area(80, 24);
        incremental.GetOrCompute(root, area, value => SplitEqual(value, 1));
        incremental.GetOrCompute(a, area, value => SplitEqual(value, 1));
        incremental.GetOrCompute(b, area, value => SplitEqual(value, 1));
        incremental.GetOrCompute(c, area, value => SplitEqual(value, 1));

        incremental.MarkDirtyWithAncestors(c);
        incremental.Propagate();
        Assert.True(incremental.IsDirty(root));
        Assert.True(incremental.IsDirty(a));
        Assert.True(incremental.IsDirty(b));
        Assert.True(incremental.IsDirty(c));
    }

    [Fact]
    public void MarkChangedDeduplicates()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();
        Rect area = Area(80, 24);
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));

        incremental.MarkChanged(node, InputKind.Constraint, 42);
        incremental.Propagate();
        Assert.True(incremental.IsDirty(node));
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));

        incremental.MarkChanged(node, InputKind.Constraint, 42);
        incremental.Propagate();
        Assert.False(incremental.IsDirty(node));
    }

    [Fact]
    public void DeepTreePartialDirty()
    {
        (IncrementalLayout incremental, NodeId root, NodeId[] leaves) = BinaryTree(4);
        incremental.Propagate();
        Walk(incremental, root, Area(160, 24));
        incremental.ResetStats();

        incremental.MarkDirty(leaves[7]);
        incremental.Propagate();
        Walk(incremental, root, Area(160, 24));

        IncrementalStats stats = incremental.Stats();
        Assert.Equal(1, stats.Recomputed);
        Assert.True(stats.Cached > 0);
    }

    [Fact]
    public void IncrementalEqualsFullLayout()
    {
        Rect area = Area(200, 60);
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId root = incremental.AddNode(null);
        List<NodeId> children = [];
        for (int outer = 0; outer < 5; outer++)
        {
            NodeId child = incremental.AddNode(root);
            children.Add(child);
            for (int inner = 0; inner < 3; inner++)
            {
                incremental.AddNode(child);
            }
        }

        incremental.Propagate();
        IReadOnlyList<Rect> rootIncremental = incremental.GetOrCompute(root, area, value => SplitEqual(value, 5));
        Assert.Equal(3_939_641_721_629_212_131UL, incremental.ResultHash(root));
        List<IReadOnlyList<Rect>> childIncremental = [];
        List<IReadOnlyList<Rect>> grandchildrenIncremental = [];
        for (int index = 0; index < children.Count; index++)
        {
            NodeId child = children[index];
            IReadOnlyList<Rect> childRects = incremental.GetOrCompute(
                child, rootIncremental[index], value => SplitEqual(value, 3));
            IReadOnlyList<NodeId> dependents = incremental.Graph().Dependents(child);
            for (int grandchildIndex = 0; grandchildIndex < dependents.Count && grandchildIndex < childRects.Count; grandchildIndex++)
            {
                grandchildrenIncremental.Add(incremental.GetOrCompute(
                    dependents[grandchildIndex], childRects[grandchildIndex], value => SplitEqual(value, 1)));
            }

            childIncremental.Add(childRects);
        }

        incremental.SetForceFull(true);
        incremental.ResetStats();
        IReadOnlyList<Rect> rootFull = incremental.GetOrCompute(root, area, value => SplitEqual(value, 5));
        List<IReadOnlyList<Rect>> childFull = [];
        List<IReadOnlyList<Rect>> grandchildrenFull = [];
        for (int index = 0; index < children.Count; index++)
        {
            NodeId child = children[index];
            IReadOnlyList<Rect> childRects = incremental.GetOrCompute(
                child, rootFull[index], value => SplitEqual(value, 3));
            IReadOnlyList<NodeId> dependents = incremental.Graph().Dependents(child);
            for (int grandchildIndex = 0; grandchildIndex < dependents.Count && grandchildIndex < childRects.Count; grandchildIndex++)
            {
                grandchildrenFull.Add(incremental.GetOrCompute(
                    dependents[grandchildIndex], childRects[grandchildIndex], value => SplitEqual(value, 1)));
            }

            childFull.Add(childRects);
        }

        Assert.Equal(rootIncremental, rootFull);
        AssertNestedRectsEqual(childIncremental, childFull);
        AssertNestedRectsEqual(grandchildrenIncremental, grandchildrenFull);
    }

    [Fact]
    public void EmptyGraph()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        Assert.Equal(0, incremental.NodeCount());
        Assert.Equal(0, incremental.CacheLen());
    }

    [Fact]
    public void SingleNodeGraph()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();
        IReadOnlyList<Rect> result = incremental.GetOrCompute(node, Area(80, 24), _ => Array.Empty<Rect>());

        Assert.Empty(result);
        IncrementalStats stats = incremental.Stats();
        Assert.Equal(1, stats.Total);
        Assert.Equal(1, stats.Recomputed);
    }

    [Fact]
    public void ZeroAreaStillCaches()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();
        Rect area = default;
        incremental.GetOrCompute(node, area, _ => Array.Empty<Rect>());

        int calls = 0;
        incremental.GetOrCompute(node, area, _ =>
        {
            calls++;
            return Array.Empty<Rect>();
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ResultHashConsistent()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        incremental.Propagate();
        Rect area = Area(80, 24);
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));
        ulong first = incremental.ResultHash(node)!.Value;

        incremental.MarkDirty(node);
        incremental.Propagate();
        incremental.GetOrCompute(node, area, value => SplitEqual(value, 2));
        ulong second = incremental.ResultHash(node)!.Value;
        Assert.Equal(first, second);
    }

    [Fact]
    public void DebugFormat()
    {
        string debug = IncrementalLayout.New().ToString();
        Assert.Contains("IncrementalLayout", debug, StringComparison.Ordinal);
        Assert.Contains("nodes", debug, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultImpl()
    {
        IncrementalLayout incremental = new();
        Assert.Equal(0, incremental.NodeCount());
        Assert.False(incremental.ForceFull());
    }

    [Fact]
    public void FromEnvDefaultIsNotForceFull()
    {
        IncrementalLayout incremental = IncrementalLayout.FromEnv();
        Assert.Equal(0, incremental.NodeCount());
    }

    [Fact]
    public void ParseEnvValues()
    {
        static bool Parse(string value) =>
            value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase);

        Assert.True(Parse("1"));
        Assert.True(Parse("true"));
        Assert.True(Parse("TRUE"));
        Assert.True(Parse("yes"));
        Assert.True(Parse("YES"));
        Assert.False(Parse("0"));
        Assert.False(Parse("false"));
        Assert.False(Parse("no"));
        Assert.False(Parse(string.Empty));
    }

    [Fact]
    public void ThousandNodeTreePartialDirty()
    {
        IncrementalLayout incremental = IncrementalLayout.WithCapacity(111);
        NodeId root = incremental.AddNode(null);
        List<NodeId> children = [];
        List<NodeId> grandchildren = [];
        for (int outer = 0; outer < 10; outer++)
        {
            NodeId child = incremental.AddNode(root);
            children.Add(child);
            for (int inner = 0; inner < 10; inner++)
            {
                grandchildren.Add(incremental.AddNode(child));
            }
        }

        incremental.Propagate();
        Rect area = Area(200, 60);
        WalkWideTree(incremental, root, children, area);
        IncrementalStats stats = incremental.Stats();
        Assert.Equal(111, stats.Recomputed);
        Assert.Equal(0, stats.Cached);

        incremental.ResetStats();
        incremental.MarkDirty(grandchildren[17]);
        incremental.MarkDirty(grandchildren[83]);
        incremental.Propagate();
        WalkWideTree(incremental, root, children, area);

        stats = incremental.Stats();
        Assert.Equal(2, stats.Recomputed);
        Assert.Equal(109, stats.Cached);
        Assert.True(stats.HitRate() > 0.95);
    }

    [Fact]
    public void PublicDependencyAndGraphMutationSurfaceIsPreserved()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId root = incremental.AddNode(null);
        NodeId child = incremental.AddNode(root);

        CycleError? cycle = incremental.AddDependency(root, child);
        Assert.NotNull(cycle);
        Assert.Same(incremental.Graph(), incremental.GraphMut());
        Assert.Equal(0, incremental.Stats().CacheEntries);
        Assert.Equal(2, incremental.NodeCount());
    }

    [Fact]
    public void ResultChangedAndCachedRectsFollowUpstreamOwnershipContract()
    {
        IncrementalLayout incremental = IncrementalLayout.New();
        NodeId node = incremental.AddNode(null);
        Assert.True(incremental.ResultChanged(node));
        Assert.Null(incremental.CachedRects(node));

        incremental.Propagate();
        IReadOnlyList<Rect> result = incremental.GetOrCompute(node, Area(80, 24), value => SplitEqual(value, 2));
        Assert.False(incremental.ResultChanged(node));
        IReadOnlyList<Rect> cached = Assert.IsAssignableFrom<IReadOnlyList<Rect>>(incremental.CachedRects(node));
        Assert.Equal(result, cached);

        Rect[] returnedArray = Assert.IsType<Rect[]>(result);
        returnedArray[0] = default;
        Assert.NotEqual(default, incremental.CachedRects(node)![0]);
    }

    private static (IncrementalLayout Incremental, NodeId Root, NodeId[] Leaves) BinaryTree(int depth)
    {
        int nodeCount = (1 << (depth + 1)) - 1;
        IncrementalLayout incremental = IncrementalLayout.WithCapacity(nodeCount);
        NodeId root = incremental.AddNode(null);
        List<NodeId> currentLevel = [root];

        for (int level = 0; level < depth; level++)
        {
            List<NodeId> nextLevel = [];
            foreach (NodeId parent in currentLevel)
            {
                nextLevel.Add(incremental.AddNode(parent));
                nextLevel.Add(incremental.AddNode(parent));
            }

            currentLevel = nextLevel;
        }

        return (incremental, root, currentLevel.ToArray());
    }

    private static (IncrementalLayout Incremental, NodeId Root, NodeId[] Children) FlatTree(int count)
    {
        IncrementalLayout incremental = IncrementalLayout.WithCapacity(count + 1);
        NodeId root = incremental.AddNode(null);
        NodeId[] children = Enumerable.Range(0, count)
            .Select(_ => incremental.AddNode(root))
            .ToArray();
        return (incremental, root, children);
    }

    private static Rect Area(ushort width, ushort height) => new(0, 0, width, height);

    private static IReadOnlyList<Rect> SplitEqual(Rect area, int count)
    {
        if (count == 0)
        {
            return Array.Empty<Rect>();
        }

        ushort width = (ushort)(area.Width / (ushort)count);
        return Enumerable.Range(0, count)
            .Select(index => new Rect(
                (ushort)(area.X + (index * width)),
                area.Y,
                width,
                area.Height))
            .ToArray();
    }

    private static void Walk(IncrementalLayout incremental, NodeId id, Rect area)
    {
        IReadOnlyList<Rect> rects = incremental.GetOrCompute(id, area, value => SplitEqual(value, 2));
        IReadOnlyList<NodeId> dependents = incremental.Graph().Dependents(id);
        for (int index = 0; index < dependents.Count && index < rects.Count; index++)
        {
            Walk(incremental, dependents[index], rects[index]);
        }
    }

    private static void WalkWideTree(
        IncrementalLayout incremental,
        NodeId root,
        IReadOnlyList<NodeId> children,
        Rect area)
    {
        IReadOnlyList<Rect> rootRects = incremental.GetOrCompute(root, area, value => SplitEqual(value, 10));
        for (int childIndex = 0; childIndex < children.Count; childIndex++)
        {
            NodeId child = children[childIndex];
            IReadOnlyList<Rect> childRects = incremental.GetOrCompute(
                child, rootRects[childIndex], value => SplitEqual(value, 10));
            IReadOnlyList<NodeId> dependents = incremental.Graph().Dependents(child);
            for (int grandchildIndex = 0; grandchildIndex < dependents.Count && grandchildIndex < childRects.Count; grandchildIndex++)
            {
                incremental.GetOrCompute(
                    dependents[grandchildIndex], childRects[grandchildIndex], value => SplitEqual(value, 1));
            }
        }
    }

    private static void AssertNestedRectsEqual(
        IReadOnlyList<IReadOnlyList<Rect>> expected,
        IReadOnlyList<IReadOnlyList<Rect>> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index], actual[index]);
        }
    }
}
