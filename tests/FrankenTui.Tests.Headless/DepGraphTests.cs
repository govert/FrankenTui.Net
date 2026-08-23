// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/dep_graph.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using System.Reflection;
using System.Runtime.InteropServices;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class DepGraphTests
{
    [Fact]
    public void AddNodeReturnsSequentialIds()
    {
        var graph = new DepGraph();
        Assert.Equal(0u, graph.AddNode().Raw());
        Assert.Equal(1u, graph.AddNode().Raw());
        Assert.Equal(2u, graph.AddNode().Raw());
        Assert.Equal(3, graph.NodeCount());
    }

    [Fact]
    public void RemoveNodeRecyclesSlot()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        graph.AddNode();
        graph.RemoveNode(first);
        Assert.Equal(1, graph.NodeCount());
        Assert.Equal(0u, graph.AddNode().Raw());
        Assert.Equal(2, graph.NodeCount());
    }

    [Fact]
    public void AddEdgeCreatesDependency()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        Assert.Null(graph.AddEdge(first, second));
        Assert.Equal(new[] { second }, graph.Dependencies(first));
        Assert.Equal(new[] { first }, graph.Dependents(second));
        Assert.Equal(1, graph.EdgeCount());
    }

    [Fact]
    public void SelfLoopDetected()
    {
        var graph = new DepGraph();
        NodeId node = graph.AddNode();
        CycleError error = graph.AddEdge(node, node)!;
        Assert.Equal(new CycleError(node, node), error);
        Assert.Equal("N0", node.ToString());
        Assert.Equal(
            "layout cycle detected: N0 → N0 would create a cycle",
            error.ToString());
    }

    [Fact]
    public void TwoNodeCycleDetected()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        Assert.Null(graph.AddEdge(first, second));
        Assert.Equal(new CycleError(second, first), graph.AddEdge(second, first));
    }

    [Fact]
    public void ThreeNodeCycleDetected()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        Assert.Null(graph.AddEdge(first, second));
        Assert.Null(graph.AddEdge(second, third));
        Assert.Equal(new CycleError(third, first), graph.AddEdge(third, first));
    }

    [Fact]
    public void DagAllowsDiamond()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        NodeId fourth = graph.AddNode();
        Assert.Null(graph.AddEdge(first, second));
        Assert.Null(graph.AddEdge(first, third));
        Assert.Null(graph.AddEdge(second, fourth));
        Assert.Null(graph.AddEdge(third, fourth));
        Assert.Equal(4, graph.EdgeCount());
    }

    [Fact]
    public void MarkChangedDeduplicates()
    {
        var graph = new DepGraph();
        NodeId node = graph.AddNode();
        graph.MarkChanged(node, InputKind.Constraint, 42);
        Assert.True(graph.IsDirty(node));
        graph.Clean(node);
        graph.MarkChanged(node, InputKind.Constraint, 42);
        Assert.False(graph.IsDirty(node));
    }

    [Fact]
    public void MarkChangedDifferentHash()
    {
        var graph = new DepGraph();
        NodeId node = graph.AddNode();
        graph.MarkChanged(node, InputKind.Constraint, 42);
        graph.Clean(node);
        graph.MarkChanged(node, InputKind.Constraint, 99);
        Assert.True(graph.IsDirty(node));
    }

    [Fact]
    public void PropagateSingleNode()
    {
        var graph = new DepGraph();
        NodeId node = graph.AddNode();
        graph.MarkDirty(node);
        Assert.Equal(new[] { node }, graph.Propagate());
    }

    [Fact]
    public void PropagateParentToChild()
    {
        var graph = new DepGraph();
        NodeId parent = graph.AddNode();
        NodeId child = graph.AddNode();
        Assert.Null(graph.AddEdge(child, parent));
        graph.SetParent(child, parent);
        graph.MarkDirty(parent);
        Assert.Equal(new[] { parent, child }, graph.Propagate());
    }

    [Fact]
    public void PropagateChain()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        Assert.Null(graph.AddEdge(second, first));
        Assert.Null(graph.AddEdge(third, second));
        graph.SetParent(second, first);
        graph.SetParent(third, second);
        graph.MarkDirty(first);
        Assert.Equal(new[] { first, second, third }, graph.Propagate());
    }

    [Fact]
    public void PropagateOnlyAffectedSubtree()
    {
        var graph = new DepGraph();
        NodeId root = graph.AddNode();
        NodeId firstChild = graph.AddNode();
        NodeId secondChild = graph.AddNode();
        NodeId independent = graph.AddNode();
        Assert.Null(graph.AddEdge(firstChild, root));
        Assert.Null(graph.AddEdge(secondChild, root));
        graph.MarkDirty(root);
        IReadOnlyList<NodeId> dirty = graph.Propagate();
        Assert.Contains(root, dirty);
        Assert.Contains(firstChild, dirty);
        Assert.Contains(secondChild, dirty);
        Assert.DoesNotContain(independent, dirty);
    }

    [Fact]
    public void PropagateDiamondDeduplicates()
    {
        var graph = new DepGraph();
        NodeId root = graph.AddNode();
        NodeId left = graph.AddNode();
        NodeId right = graph.AddNode();
        NodeId bottom = graph.AddNode();
        Assert.Null(graph.AddEdge(left, root));
        Assert.Null(graph.AddEdge(right, root));
        Assert.Null(graph.AddEdge(bottom, left));
        Assert.Null(graph.AddEdge(bottom, right));
        graph.MarkDirty(root);
        IReadOnlyList<NodeId> dirty = graph.Propagate();
        Assert.Equal(4, dirty.Count);
        Assert.Equal(4, dirty.Distinct().Count());
    }

    [Fact]
    public void CleanAllResets()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        Assert.Null(graph.AddEdge(second, first));
        graph.MarkDirty(first);
        graph.Propagate();
        Assert.True(graph.IsDirty(first));
        Assert.True(graph.IsDirty(second));
        graph.CleanAll();
        Assert.False(graph.IsDirty(first));
        Assert.False(graph.IsDirty(second));
        Assert.Equal(0, graph.DirtyCount());
    }

    [Fact]
    public void InvalidateAllDirtiesEverything()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        graph.InvalidateAll();
        Assert.Equal(new[] { first, second, third }, graph.Propagate());
    }

    [Fact]
    public void ParentChildRelationship()
    {
        var graph = new DepGraph();
        NodeId parent = graph.AddNode();
        NodeId child = graph.AddNode();
        Assert.Null(graph.Parent(parent));
        graph.SetParent(child, parent);
        Assert.Equal(parent, graph.Parent(child));
    }

    [Fact]
    public void InputHashesStoredIndependently()
    {
        var graph = new DepGraph();
        NodeId node = graph.AddNode();
        graph.MarkChanged(node, InputKind.Constraint, 1);
        graph.MarkChanged(node, InputKind.Content, 2);
        graph.MarkChanged(node, InputKind.Style, 3);
        Assert.Equal(1ul, graph.ConstraintHash(node));
        Assert.Equal(2ul, graph.ContentHash(node));
        Assert.Equal(3ul, graph.StyleHash(node));
    }

    [Fact]
    public void DeadNodeIsNotDirty()
    {
        var graph = new DepGraph();
        NodeId node = graph.AddNode();
        graph.MarkDirty(node);
        graph.RemoveNode(node);
        Assert.False(graph.IsDirty(node));
    }

    [Fact]
    public void PropagateSkipsDeadNodes()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        Assert.Null(graph.AddEdge(second, first));
        Assert.Null(graph.AddEdge(third, second));
        graph.RemoveNode(second);
        graph.MarkDirty(first);
        IReadOnlyList<NodeId> dirty = graph.Propagate();
        Assert.Contains(first, dirty);
        Assert.DoesNotContain(third, dirty);
    }

    [Fact]
    public void PropagateEmptyReturnsEmpty()
    {
        var graph = new DepGraph();
        graph.AddNode();
        Assert.Empty(graph.Propagate());
    }

    [Fact]
    public void LargeTreePropagation()
    {
        DepGraph graph = DepGraph.WithCapacity(1000, 1000);
        NodeId root = graph.AddNode();
        var leaves = new List<NodeId>();
        for (int childIndex = 0; childIndex < 10; childIndex++)
        {
            NodeId child = graph.AddNode();
            Assert.Null(graph.AddEdge(child, root));
            graph.SetParent(child, root);
            for (int grandchildIndex = 0; grandchildIndex < 10; grandchildIndex++)
            {
                NodeId grandchild = graph.AddNode();
                Assert.Null(graph.AddEdge(grandchild, child));
                graph.SetParent(grandchild, child);
                leaves.Add(grandchild);
            }
        }
        Assert.Equal(111, graph.NodeCount());
        graph.MarkDirty(root);
        Assert.Equal(111, graph.Propagate().Count);
        graph.CleanAll();
        graph.MarkDirty(leaves[42]);
        Assert.Equal(new[] { leaves[42] }, graph.Propagate());
    }

    [Fact]
    public void NodeSizeUnder64Bytes() => Assert.True(ManagedDepNodeSize() <= 64);

    [Fact]
    public void NodeSizeExactly40Bytes() => Assert.Equal(40, ManagedDepNodeSize());

    [Fact]
    public void MultipleInputChangesSinglePropagation()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        Assert.Null(graph.AddEdge(second, first));
        graph.MarkChanged(first, InputKind.Constraint, 10);
        graph.MarkChanged(first, InputKind.Content, 20);
        Assert.Equal(2, graph.Propagate().Count);
    }

    [Fact]
    public void PropagateDfsPreorder()
    {
        var graph = new DepGraph();
        NodeId root = graph.AddNode();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        Assert.Null(graph.AddEdge(first, root));
        Assert.Null(graph.AddEdge(second, first));
        Assert.Null(graph.AddEdge(third, first));
        graph.SetParent(first, root);
        graph.SetParent(second, first);
        graph.SetParent(third, first);
        graph.MarkDirty(root);
        Assert.Equal(new[] { root, first, second, third }, graph.Propagate());
    }

    [Fact]
    public void DirtyCountAccurate()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        graph.AddNode();
        Assert.Equal(0, graph.DirtyCount());
        graph.MarkDirty(first);
        graph.MarkDirty(second);
        graph.Propagate();
        Assert.Equal(2, graph.DirtyCount());
        graph.Clean(first);
        Assert.Equal(1, graph.DirtyCount());
        graph.CleanAll();
        Assert.Equal(0, graph.DirtyCount());
    }

    [Fact]
    public void DependenciesAndDependentsApi()
    {
        var graph = new DepGraph();
        NodeId first = graph.AddNode();
        NodeId second = graph.AddNode();
        NodeId third = graph.AddNode();
        Assert.Null(graph.AddEdge(second, first));
        Assert.Null(graph.AddEdge(third, first));
        Assert.Equal(new[] { first }, graph.Dependencies(second));
        Assert.Equal(new[] { first }, graph.Dependencies(third));
        Assert.Equal(2, graph.Dependents(first).Count);
    }

    private static int ManagedDepNodeSize()
    {
        Type nodeType = typeof(DepGraph).GetNestedType("DepNode", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DepGraph.DepNode not found.");
        return Marshal.SizeOf(nodeType);
    }
}
