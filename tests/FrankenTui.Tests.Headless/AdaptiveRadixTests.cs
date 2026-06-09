// Upstream source: crates/ftui-widgets/src/adaptive_radix.rs (tests module)
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of all upstream adaptive_radix tests.

using FrankenTui.Runtime;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class AdaptiveRadixTests
{
    [Fact] public void EmptyTree()
    {
        var art = new AdaptiveRadixTree<int>();
        Assert.True(art.IsEmpty);
        Assert.Equal(0, art.Count);
        Assert.Equal(0, art.Get("anything"));
    }

    [Fact] public void SingleInsertAndGet()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("hello", 42);
        Assert.Equal(42, art.Get("hello"));
        Assert.Equal(0, art.Get("hell"));
        Assert.Equal(0, art.Get("helloo"));
        Assert.Equal(1, art.Count);
    }

    [Fact] public void MultipleInserts()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("file:open", 1);
        art.Insert("file:save", 2);
        art.Insert("file:close", 3);
        art.Insert("edit:undo", 4);
        art.Insert("edit:redo", 5);

        Assert.Equal(5, art.Count);
        Assert.Equal(1, art.Get("file:open"));
        Assert.Equal(2, art.Get("file:save"));
        Assert.Equal(3, art.Get("file:close"));
        Assert.Equal(4, art.Get("edit:undo"));
        Assert.Equal(5, art.Get("edit:redo"));
    }

    [Fact] public void PrefixScanBasic()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("file:open", 1);
        art.Insert("file:save", 2);
        art.Insert("file:close", 3);
        art.Insert("edit:undo", 4);

        var results = art.PrefixScan("file:");
        Assert.Equal(3, results.Count);

        var editResults = art.PrefixScan("edit:");
        Assert.Equal(1, editResults.Count);
    }

    [Fact] public void PrefixScanSorted()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("c", 3);
        art.Insert("b", 2);
        art.Insert("a", 1);

        var results = art.PrefixScan("");
        var keys = results.Select(r => r.Item1).ToList();
        Assert.Equal(new[] { "a", "b", "c" }, keys);
    }

    [Fact] public void UpdateExistingKey()
    {
        var art = new AdaptiveRadixTree<int>();
        Assert.Equal(0, art.Insert("key", 1));
        Assert.Equal(1, art.Insert("key", 2));
        Assert.Equal(2, art.Get("key"));
        Assert.Equal(1, art.Count);
    }

    [Fact] public void DeleteExisting()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("hello", 42);
        Assert.Equal(42, art.Delete("hello"));
        Assert.Equal(0, art.Get("hello"));
        Assert.Equal(0, art.Count);
    }

    [Fact] public void DeleteNonexistent()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("hello", 42);
        Assert.Equal(0, art.Delete("world"));
        Assert.Equal(1, art.Count);
    }

    [Fact] public void ManyInsertsPromoteNodeTypes()
    {
        var art = new AdaptiveRadixTree<int>();
        for (int i = 0; i < 50; i++)
            art.Insert($"key_{i:D3}", i);

        Assert.Equal(50, art.Count);

        for (int i = 0; i < 50; i++)
            Assert.Equal(i, art.Get($"key_{i:D3}"));

        var dist = art.GetNodeDistribution();
        Assert.True(dist.Leaves >= 50);
    }

    [Fact] public void IterReturnsAllSorted()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("z", 26);
        art.Insert("a", 1);
        art.Insert("m", 13);

        var entries = art.Iter();
        var keys = entries.Select(e => e.Item1).ToList();
        Assert.Equal(new[] { "a", "m", "z" }, keys);
    }

    [Fact] public void SharedPrefixKeys()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("test", 1);
        art.Insert("testing", 2);
        art.Insert("tested", 3);
        art.Insert("tester", 4);

        Assert.Equal(4, art.Count);
        Assert.Equal(1, art.Get("test"));
        Assert.Equal(2, art.Get("testing"));
        Assert.Equal(3, art.Get("tested"));
        Assert.Equal(4, art.Get("tester"));

        var scan = art.PrefixScan("test");
        Assert.Equal(4, scan.Count);
    }

    [Fact] public void IterIncludesKeysStoredOnCompressedInnerNodes()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("file:save", 1);
        art.Insert("file:save-as", 2);

        var entries = art.Iter();
        var keys = entries.Select(e => e.Item1).ToList();
        Assert.Equal(new[] { "file:save", "file:save-as" }, keys);
    }

    [Fact] public void DeletingPrefixKeyPreservesLongerDescendants()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("test", 1);
        art.Insert("testing", 2);
        art.Insert("tester", 3);

        Assert.Equal(1, art.Delete("test"));
        Assert.Equal(0, art.Get("test"));
        Assert.Equal(2, art.Get("testing"));
        Assert.Equal(3, art.Get("tester"));

        var keys = art.PrefixScan("test").Select(r => r.Item1).ToList();
        Assert.Equal(new[] { "tester", "testing" }, keys);
    }

    [Fact] public void EmptyPrefixScanReturnsAll()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("a", 1);
        art.Insert("b", 2);

        var results = art.PrefixScan("");
        Assert.Equal(2, results.Count);
    }

    [Fact] public void NodeDistribution()
    {
        var art = new AdaptiveRadixTree<int>();
        art.Insert("a", 1);
        art.Insert("b", 2);
        art.Insert("c", 3);

        var dist = art.GetNodeDistribution();
        Assert.True(dist.Leaves >= 3);
    }

    [Fact] public void CommandPaletteScenario()
    {
        var art = new AdaptiveRadixTree<int>();
        var commands = new[]
        {
            "file:open", "file:save", "file:save-as", "file:close", "file:new",
            "edit:undo", "edit:redo", "edit:cut", "edit:copy", "edit:paste",
            "view:sidebar", "view:terminal", "view:explorer", "view:minimap",
            "go:line", "go:file", "go:symbol", "go:definition",
        };
        for (int i = 0; i < commands.Length; i++)
            art.Insert(commands[i], i);

        Assert.Equal(5, art.PrefixScan("file:").Count);
        Assert.Equal(5, art.PrefixScan("edit:").Count);
        Assert.Equal(4, art.PrefixScan("view:").Count);
        Assert.Equal(4, art.PrefixScan("go:").Count);
        Assert.Equal(5, art.PrefixScan("f").Count);
    }
}
