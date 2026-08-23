// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/veb_tree.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class VebTreeTests
{
    [Fact]
    public void EmptyTree()
    {
        VebTree<string> tree = VebTree<string>.Build([]);
        Assert.True(tree.IsEmpty());
        Assert.Equal(0, tree.Len());
        Assert.Null(tree.Root());
    }

    [Fact]
    public void SingleNode()
    {
        VebTree<string> tree = VebTree<string>.Build([new TreeNode<string>(42, "solo", [])]);
        Assert.Equal(1, tree.Len());
        VebEntry<string> root = tree.Root()!;
        Assert.Equal(42u, root.Id);
        Assert.Equal("solo", root.Data);
        Assert.Empty(root.ChildIndices);
        Assert.Equal(uint.MaxValue, root.ParentIndex);
    }

    [Fact]
    public void ThreeNodeTree()
    {
        VebTree<string> tree = VebTree<string>.Build([
            new TreeNode<string>(0, "root", [1, 2]),
            new TreeNode<string>(1, "left", []),
            new TreeNode<string>(2, "right", []),
        ]);
        Assert.Equal(3, tree.Len());
        Assert.Equal("root", tree.Get(0)!.Data);
        Assert.Equal("left", tree.Get(1)!.Data);
        Assert.Equal("right", tree.Get(2)!.Data);
    }

    [Fact]
    public void LookupById()
    {
        VebTree<string> tree = VebTree<string>.Build([
            new TreeNode<string>(10, "a", [20, 30]),
            new TreeNode<string>(20, "b", []),
            new TreeNode<string>(30, "c", []),
        ]);
        Assert.Equal("a", tree.Get(10)!.Data);
        Assert.Equal("b", tree.Get(20)!.Data);
        Assert.Equal("c", tree.Get(30)!.Data);
        Assert.Null(tree.Get(99));
    }

    [Fact]
    public void ParentIndicesCorrect()
    {
        VebTree<string> tree = VebTree<string>.Build([
            new TreeNode<string>(0, "r", [1, 2]),
            new TreeNode<string>(1, "l", [3]),
            new TreeNode<string>(2, "r2", []),
            new TreeNode<string>(3, "ll", []),
        ]);
        VebEntry<string>[] entries = tree.Iter().ToArray();
        Assert.Equal(uint.MaxValue, tree.Get(0)!.ParentIndex);
        Assert.Equal((uint)Array.FindIndex(entries, static entry => entry.Id == 0), tree.Get(1)!.ParentIndex);
        Assert.Equal((uint)Array.FindIndex(entries, static entry => entry.Id == 1), tree.Get(3)!.ParentIndex);
    }

    [Fact]
    public void ChildIndicesCorrect()
    {
        VebTree<string> tree = VebTree<string>.Build([
            new TreeNode<string>(0, "r", [1, 2]),
            new TreeNode<string>(1, "l", []),
            new TreeNode<string>(2, "r2", []),
        ]);
        VebEntry<string> root = tree.Get(0)!;
        Assert.Equal(2, root.ChildIndices.Count);
        Assert.All(root.ChildIndices, index => Assert.Contains(tree.GetByIndex(index)!.Id, new uint[] { 1, 2 }));
    }

    [Fact]
    public void DfsIterationPreservesAllNodes()
    {
        TreeNode<string>[] nodes = MakeBinaryTree(3);
        VebTree<string> tree = VebTree<string>.Build(nodes);
        IReadOnlyList<VebEntry<string>> dfs = tree.IterDfs();
        Assert.Equal(nodes.Length, dfs.Count);
        Assert.Equal(Enumerable.Range(0, nodes.Length).Select(static id => (uint)id),
            dfs.Select(static entry => entry.Id).Order());
    }

    [Fact]
    public void DfsRootFirst()
    {
        VebTree<string> tree = VebTree<string>.Build(MakeBinaryTree(3));
        Assert.Equal(0u, tree.IterDfs()[0].Id);
    }

    [Fact]
    public void VebOrderContainsAllNodes()
    {
        TreeNode<string>[] nodes = MakeBinaryTree(4);
        VebTree<string> tree = VebTree<string>.Build(nodes);
        uint[] ids = tree.Iter().Select(static entry => entry.Id).ToArray();
        Assert.Equal(nodes.Length, tree.Len());
        Assert.Equal(nodes.Length, ids.Length);
        Assert.Equal(nodes.Length, ids.Distinct().Count());
    }

    [Fact]
    public void DepthValuesCorrect()
    {
        VebTree<string> tree = VebTree<string>.Build([
            new TreeNode<string>(0, "d0", [1, 2]),
            new TreeNode<string>(1, "d1a", [3]),
            new TreeNode<string>(2, "d1b", []),
            new TreeNode<string>(3, "d2", []),
        ]);
        Assert.Equal((ushort)0, tree.Get(0)!.Depth);
        Assert.Equal((ushort)1, tree.Get(1)!.Depth);
        Assert.Equal((ushort)1, tree.Get(2)!.Depth);
        Assert.Equal((ushort)2, tree.Get(3)!.Depth);
    }

    [Fact]
    public void LargeTree1000Nodes()
    {
        TreeNode<uint>[] nodes = Enumerable.Range(0, 1000)
            .Select(index => new TreeNode<uint>(
                (uint)index,
                (uint)index,
                index < 999 ? [(uint)(index + 1)] : []))
            .ToArray();
        VebTree<uint> tree = VebTree<uint>.Build(nodes);
        Assert.Equal(1000, tree.Len());
        Assert.Equal((ushort)0, tree.Get(0)!.Depth);
        Assert.Equal((ushort)999, tree.Get(999)!.Depth);
    }

    [Fact]
    public void LayoutResultsIdentical()
    {
        VebTree<string> tree = VebTree<string>.Build(MakeBinaryTree(4));
        var vebIds = tree.Iter().Select(static entry => entry.Id).ToHashSet();
        var dfsIds = tree.IterDfs().Select(static entry => entry.Id).ToHashSet();
        Assert.True(vebIds.SetEquals(dfsIds));
    }

    [Fact]
    public void WideTree()
    {
        var nodes = new List<TreeNode<uint>>
        {
            new(0, 0, Enumerable.Range(1, 100).Select(static id => (uint)id)),
        };
        nodes.AddRange(Enumerable.Range(1, 100)
            .Select(static id => new TreeNode<uint>((uint)id, (uint)id, [])));
        VebTree<uint> tree = VebTree<uint>.Build(nodes);
        Assert.Equal(101, tree.Len());
        Assert.Equal(100, tree.Get(0)!.ChildIndices.Count);
    }

    [Fact]
    public void RebuildProducesSameResult()
    {
        TreeNode<string>[] nodes = MakeBinaryTree(3);
        VebTree<string> first = VebTree<string>.Build(nodes);
        VebTree<string> second = VebTree<string>.Build(nodes);
        Assert.Equal(
            first.Iter().Select(static entry => entry.Id),
            second.Iter().Select(static entry => entry.Id));

        VebTree<string> clone = first.Clone();
        Assert.Equal(
            first.Iter().Select(static entry => entry.Id),
            clone.Iter().Select(static entry => entry.Id));
        Assert.NotSame(first.Root(), clone.Root());
        Assert.NotSame(nodes[0], nodes[0].Clone());
        Assert.NotSame(first.Root(), first.Root()!.Clone());
    }

    // Port of tests/proptest_veb_tree_invariants.rs. The source uses 200
    // proptest cases; the managed projection uses 200 reproducible seeded cases.

    [Fact]
    public void VebNodeCountConservationProperty() => ForRandomTrees(100, nodes =>
        Assert.Equal(nodes.Length, VebTree<uint>.Build(nodes).Len()));

    [Fact]
    public void VebIdUniquenessProperty() => ForRandomTrees(100, nodes =>
    {
        uint[] ids = VebTree<uint>.Build(nodes).Iter().Select(static entry => entry.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    });

    [Fact]
    public void VebRootIsFirstProperty() => ForRandomTrees(100, nodes =>
    {
        VebEntry<uint> root = VebTree<uint>.Build(nodes).Root()!;
        Assert.Equal(0u, root.Id);
        Assert.Equal(uint.MaxValue, root.ParentIndex);
    });

    [Fact]
    public void VebParentChildConsistencyProperty() => ForRandomTrees(50, nodes =>
    {
        VebTree<uint> tree = VebTree<uint>.Build(nodes);
        IReadOnlyList<VebEntry<uint>> entries = tree.AsSlice();
        for (int position = 0; position < entries.Count; position++)
        {
            VebEntry<uint> entry = entries[position];
            foreach (uint childIndex in entry.ChildIndices)
                Assert.Equal((uint)position, tree.GetByIndex(childIndex)!.ParentIndex);
            if (entry.ParentIndex != uint.MaxValue)
                Assert.Contains((uint)position, tree.GetByIndex(entry.ParentIndex)!.ChildIndices);
        }
    });

    [Fact]
    public void VebDepthCorrectnessProperty() => ForRandomTrees(50, nodes =>
    {
        VebTree<uint> tree = VebTree<uint>.Build(nodes);
        Assert.Equal((ushort)0, tree.Root()!.Depth);
        foreach (VebEntry<uint> entry in tree.Iter())
        {
            foreach (uint childIndex in entry.ChildIndices)
                Assert.Equal((ushort)(entry.Depth + 1), tree.GetByIndex(childIndex)!.Depth);
        }
    });

    [Fact]
    public void VebDfsCompletenessProperty() => ForRandomTrees(100, nodes =>
    {
        VebTree<uint> tree = VebTree<uint>.Build(nodes);
        IReadOnlyList<VebEntry<uint>> dfs = tree.IterDfs();
        Assert.Equal(nodes.Length, dfs.Count);
        Assert.True(dfs.Select(static entry => entry.Id).ToHashSet()
            .SetEquals(tree.Iter().Select(static entry => entry.Id)));
    });

    [Fact]
    public void VebLookupCorrectnessProperty() => ForRandomTrees(100, nodes =>
    {
        VebTree<uint> tree = VebTree<uint>.Build(nodes);
        foreach (TreeNode<uint> node in nodes)
            Assert.Equal(node.Data, tree.Get(node.Id)!.Data);
    });

    [Fact]
    public void VebDeterministicProperty() => ForRandomTrees(50, nodes =>
    {
        VebTree<uint> first = VebTree<uint>.Build(nodes);
        VebTree<uint> second = VebTree<uint>.Build(nodes);
        Assert.Equal(
            first.Iter().Select(static entry => entry.Id),
            second.Iter().Select(static entry => entry.Id));
    });

    [Fact]
    public void VebBinaryTreeInvariantsProperty()
    {
        var random = new Random(0x15cc_0009);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            TreeNode<uint>[] nodes = MakeBinaryValueTree((ushort)random.Next(1, 8));
            VebTree<uint> tree = VebTree<uint>.Build(nodes);
            Assert.Equal(nodes.Length, tree.Len());
            Assert.All(tree.Iter(), entry =>
                Assert.True(entry.ChildIndices.Count is 0 or 2));
        }
    }

    private static TreeNode<string>[] MakeBinaryTree(ushort depth)
    {
        var nodes = new List<TreeNode<string>>();
        uint nextId = 1;

        void Build(uint id, ushort currentDepth, ushort remaining)
        {
            string label = $"node_{id}_d{currentDepth}";
            if (remaining == 0)
            {
                nodes.Add(new TreeNode<string>(id, label, []));
                return;
            }
            uint left = nextId++;
            uint right = nextId++;
            nodes.Add(new TreeNode<string>(id, label, [left, right]));
            Build(left, (ushort)(currentDepth + 1), (ushort)(remaining - 1));
            Build(right, (ushort)(currentDepth + 1), (ushort)(remaining - 1));
        }

        Build(0, 0, depth);
        return nodes.ToArray();
    }

    private static void ForRandomTrees(int maximumNodes, Action<TreeNode<uint>[]> assertion)
    {
        var random = new Random(unchecked(0x15cc_2026 + maximumNodes));
        for (int iteration = 0; iteration < 200; iteration++)
            assertion(GenerateTree(random, maximumNodes));
    }

    private static TreeNode<uint>[] GenerateTree(Random random, int maximumNodes)
    {
        int count = random.Next(2, maximumNodes + 1);
        var children = Enumerable.Range(0, count)
            .Select(static _ => new List<uint>())
            .ToArray();
        for (int child = 1; child < count; child++)
            children[random.Next(0, child)].Add((uint)child);
        return Enumerable.Range(0, count)
            .Select(id => new TreeNode<uint>((uint)id, (uint)id, children[id]))
            .ToArray();
    }

    private static TreeNode<uint>[] MakeBinaryValueTree(ushort depth)
    {
        var nodes = new List<TreeNode<uint>>();
        uint nextId = 1;

        void Build(uint id, ushort remaining)
        {
            if (remaining == 0)
            {
                nodes.Add(new TreeNode<uint>(id, id, []));
                return;
            }
            uint left = nextId++;
            uint right = nextId++;
            nodes.Add(new TreeNode<uint>(id, id, [left, right]));
            Build(left, (ushort)(remaining - 1));
            Build(right, (ushort)(remaining - 1));
        }

        Build(0, depth);
        return nodes.ToArray();
    }
}
