// Upstream source: crates/ftui-widgets/src/louds.rs (tests module)
// Full 1-1 port of all upstream LOUDS tests. Proptests converted to explicit loops.

using FrankenTui.Runtime;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class LoudsTreeTests
{
    [Fact] public void SingleNodeTree()
    {
        var tree = LoudsTree.FromDegrees([0]);
        Assert.Equal(1, tree.NodeCount);
        Assert.True(tree.IsLeaf(0));
        Assert.Null(tree.Parent(0));
        Assert.Null(tree.FirstChild(0));
        Assert.Equal(0, tree.Depth(0));
    }

    [Fact] public void LinearChain()
    {
        var tree = LoudsTree.FromDegrees([1, 1, 1, 0]);
        Assert.Equal(4, tree.NodeCount);
        Assert.Null(tree.Parent(0));
        Assert.Equal(0, tree.Parent(1));
        Assert.Equal(1, tree.Parent(2));
        Assert.Equal(2, tree.Parent(3));
        Assert.Equal(1, tree.FirstChild(0));
        Assert.Equal(2, tree.FirstChild(1));
        Assert.Equal(3, tree.FirstChild(2));
        Assert.True(tree.IsLeaf(3));
        Assert.Equal(0, tree.Depth(0));
        Assert.Equal(1, tree.Depth(1));
        Assert.Equal(2, tree.Depth(2));
        Assert.Equal(3, tree.Depth(3));
    }

    [Fact] public void BinaryTree()
    {
        var tree = LoudsTree.FromDegrees([2, 2, 0, 0, 0]);
        Assert.Equal(5, tree.NodeCount);
        Assert.Equal(1, tree.FirstChild(0));
        Assert.Equal(2, tree.NextSibling(1));
        Assert.Null(tree.NextSibling(2));
        Assert.Equal(3, tree.FirstChild(1));
        Assert.Equal(4, tree.NextSibling(3));
        Assert.True(tree.IsLeaf(3));
        Assert.True(tree.IsLeaf(4));
        Assert.True(tree.IsLeaf(2));
        Assert.Equal(0, tree.Parent(1));
        Assert.Equal(0, tree.Parent(2));
        Assert.Equal(1, tree.Parent(3));
        Assert.Equal(1, tree.Parent(4));
    }

    [Fact] public void WideTree()
    {
        var tree = LoudsTree.FromDegrees([4, 0, 0, 0, 0]);
        Assert.Equal(5, tree.NodeCount);
        Assert.Equal(1, tree.FirstChild(0));
        Assert.Equal(2, tree.NextSibling(1));
        Assert.Equal(3, tree.NextSibling(2));
        Assert.Equal(4, tree.NextSibling(3));
        Assert.Null(tree.NextSibling(4));
        for (int i = 1; i < 5; i++) { Assert.True(tree.IsLeaf(i)); Assert.Equal(0, tree.Parent(i)); Assert.Equal(1, tree.Depth(i)); }
    }

    [Fact] public void ThreeLevelTree()
    {
        var tree = LoudsTree.FromDegrees([2, 1, 2, 0, 0, 0]);
        Assert.Equal(6, tree.NodeCount);
        Assert.Equal(1, tree.FirstChild(0));
        Assert.Equal(2, tree.NextSibling(1));
        Assert.Equal(3, tree.FirstChild(1));
        Assert.Equal(4, tree.FirstChild(2));
        Assert.Equal(5, tree.NextSibling(4));
        Assert.Equal(1, tree.Parent(3));
        Assert.Equal(2, tree.Parent(4));
        Assert.Equal(2, tree.Parent(5));
        Assert.Equal(2, tree.Depth(4));
        Assert.Equal(2, tree.Depth(5));
    }

    [Fact] public void DegreeMatchesInput()
    {
        int[] degrees = [2, 1, 2, 0, 0, 0];
        var tree = LoudsTree.FromDegrees(degrees);
        for (int i = 0; i < degrees.Length; i++)
            Assert.Equal(degrees[i], tree.Degree(i));
    }

    [Fact] public void ChildrenIter()
    {
        var tree = LoudsTree.FromDegrees([3, 0, 1, 0, 0]);
        Assert.Equal(new[] { 1, 2, 3 }, tree.Children(0).ToList());
        Assert.Equal(new[] { 4 }, tree.Children(2).ToList());
        Assert.Empty(tree.Children(1));
    }

    [Fact] public void SubtreeSizeRoot() => Assert.Equal(5, LoudsTree.FromDegrees([2, 2, 0, 0, 0]).SubtreeSize(0));
    [Fact] public void SubtreeSizeLeaf() => Assert.Equal(1, LoudsTree.FromDegrees([2, 2, 0, 0, 0]).SubtreeSize(3));
    [Fact] public void SubtreeSizeInternal() => Assert.Equal(3, LoudsTree.FromDegrees([2, 2, 0, 0, 0]).SubtreeSize(1));

    [Fact] public void FromChildrenMatchesDegrees()
    {
        var children = new int[][] { [1, 2], [3, 4], [], [], [] };
        var tree = LoudsTree.FromChildren(children);
        Assert.Equal(5, tree.NodeCount);
        Assert.Equal(1, tree.FirstChild(0));
        Assert.Equal(3, tree.FirstChild(1));
    }

    [Fact] public void MemoryMuchLessThanPointers()
    {
        int n = 1000;
        int total = 2 * n - 1;
        var degrees = new int[total];
        for (int i = 0; i < n - 1; i++) degrees[i] = 2;
        var tree = LoudsTree.FromDegrees(degrees);
        int loudsBytes = tree.SizeInBytes;
        int pointerBytes = total * 3 * 8;
        Assert.True(loudsBytes < pointerBytes / 10, $"LOUDS ({loudsBytes}B) should be < 10% of pointer tree ({pointerBytes}B)");
    }

    [Fact] public void MemoryScaling()
    {
        foreach (int n in new[] { 100, 1000, 10_000 })
        {
            int total = 2 * n - 1;
            var degrees = new int[total];
            for (int i = 0; i < n - 1; i++) degrees[i] = 2;
            var tree = LoudsTree.FromDegrees(degrees);
            double bitsPerNode = (tree.SizeInBytes * 8.0) / total;
            Assert.True(bitsPerNode < 4.0, $"n={n}: {bitsPerNode:F1} bits/node exceeds 4.0");
        }
    }

    [Fact] public void RootNoSiblings() => Assert.Null(LoudsTree.FromDegrees([2, 0, 0]).NextSibling(0));

    [Fact] public void InvalidDegreeSumPanics()
    {
        var ex = Assert.Throws<ArgumentException>(() => LoudsTree.FromDegrees([3, 0, 0]));
        Assert.Contains("degree sum", ex.Message);
    }

    [Fact] public void EmptyDegreesPanics()
    {
        var ex = Assert.Throws<ArgumentException>(() => LoudsTree.FromDegrees([]));
        Assert.Contains("must not be empty", ex.Message);
    }

    [Fact] public void ParentChildRoundtrip()
    {
        var tree = LoudsTree.FromDegrees([3, 2, 0, 1, 0, 0, 0]);
        for (int v = 1; v < tree.NodeCount; v++)
        {
            int p = tree.Parent(v)!.Value;
            var children = tree.Children(p).ToList();
            Assert.Contains(v, children);
        }
    }

    [Fact] public void AllNodesReachableFromRoot()
    {
        var tree = LoudsTree.FromDegrees([3, 2, 0, 1, 0, 0, 0]);
        var visited = new bool[tree.NodeCount];
        var stack = new Stack<int>(); stack.Push(0);
        while (stack.Count > 0) { int v = stack.Pop(); visited[v] = true; foreach (var c in tree.Children(v)) stack.Push(c); }
        Assert.All(visited, Assert.True);
    }

    // ── Property test equivalents ──

    private static int[] GenerateDegreeSequence(int nodeCount, Random rng)
    {
        var degrees = new int[nodeCount];
        int totalChildren = 0;
        int target = nodeCount - 1;
        for (int i = 0; i < nodeCount; i++)
        {
            int remaining = target - totalChildren;
            if (remaining == 0) break;
            int minNeeded = Math.Max(0, (i + 1) - totalChildren);
            int maxAllowed = Math.Min(remaining, Math.Max(minNeeded, rng.Next(0, 5)));
            int d = Math.Max(minNeeded, maxAllowed);
            degrees[i] = d;
            totalChildren += d;
        }
        int deficit = target - totalChildren;
        if (deficit > 0) degrees[0] += deficit;
        return degrees;
    }

    [Fact] public void NavigationConsistentProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 20; trial++)
        {
            int nodeCount = rng.Next(2, 50);
            var degrees = GenerateDegreeSequence(nodeCount, rng);
            var tree = LoudsTree.FromDegrees(degrees);
            Assert.Equal(nodeCount, tree.NodeCount);

            for (int v = 1; v < nodeCount; v++)
            {
                var p = tree.Parent(v);
                Assert.NotNull(p);
                Assert.True(p!.Value < v);
            }

            for (int v = 0; v < nodeCount; v++)
            {
                foreach (var child in tree.Children(v))
                    Assert.Equal(v, tree.Parent(child));
                int childCount = tree.Children(v).Count();
                Assert.Equal(degrees[v], tree.Degree(v));
                Assert.Equal(degrees[v], childCount);
            }
        }
    }

    [Fact] public void SubtreeSizesSumProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 10; trial++)
        {
            int nodeCount = rng.Next(2, 30);
            var degrees = GenerateDegreeSequence(nodeCount, rng);
            var tree = LoudsTree.FromDegrees(degrees);
            Assert.Equal(tree.NodeCount, tree.SubtreeSize(0));
        }
    }

    [Fact] public void DepthMatchesParentChainProperty()
    {
        var rng = new Random(42);
        for (int trial = 0; trial < 20; trial++)
        {
            int nodeCount = rng.Next(2, 50);
            var degrees = GenerateDegreeSequence(nodeCount, rng);
            var tree = LoudsTree.FromDegrees(degrees);
            for (int v = 0; v < tree.NodeCount; v++)
            {
                int d = tree.Depth(v);
                if (v == 0) Assert.Equal(0, d);
                else Assert.Equal(d, tree.Depth(tree.Parent(v)!.Value) + 1);
            }
        }
    }

    [Fact] public void MemorySublinearProperty()
    {
        var rng = new Random(42);
        foreach (int n in rng.Next(50, 500) is int nn ? new[] { nn } : new[] { 100 })
        {
            int total = 2 * n - 1;
            var degrees = new int[total];
            for (int i = 0; i < n - 1; i++) degrees[i] = 2;
            var tree = LoudsTree.FromDegrees(degrees);
            double bitsPerNode = (tree.SizeInBytes * 8.0) / total;
            Assert.True(bitsPerNode < 5.0, $"n={n}: {bitsPerNode:F1} bits/node exceeds 5.0");
        }
    }
}
