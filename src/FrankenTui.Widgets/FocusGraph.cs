// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/focus/graph.rs (861L)
// Directed graph for focus navigation: nodes, edges, directional nav, tab order, cycle detection.

using FrankenTui.Core;

namespace FrankenTui.Widgets.Focus;

public enum NavDirection { Up, Down, Left, Right, Next, Prev }

public static class NavDirections
{
    public static readonly NavDirection[] All = { NavDirection.Up, NavDirection.Down, NavDirection.Left, NavDirection.Right, NavDirection.Next, NavDirection.Prev };
}

public sealed class FocusNode
{
    public ulong Id { get; set; }
    public Rect Bounds { get; set; }
    public int TabIndex { get; set; }
    public bool IsFocusable { get; set; } = true;
    public uint? GroupId { get; set; }

    public FocusNode(ulong id, Rect bounds) => (Id, Bounds) = (id, bounds);
    public FocusNode WithTabIndex(int idx) { TabIndex = idx; return this; }
    public FocusNode WithFocusable(bool f) { IsFocusable = f; return this; }
    public FocusNode WithGroup(uint g) { GroupId = g; return this; }
}

public sealed class FocusGraph
{
    readonly Dictionary<ulong, FocusNode> _nodes = new();
    readonly Dictionary<(ulong, NavDirection), ulong> _edges = new();

    public FocusGraph() { }

    public ulong Insert(FocusNode node)
    {
        _nodes[node.Id] = node;
        return node.Id;
    }

    public FocusNode? Remove(ulong id)
    {
        if (!_nodes.Remove(id, out var node)) return null;
        foreach (var dir in NavDirections.All) _edges.Remove((id, dir));
        var toRemove = new List<(ulong, NavDirection)>();
        foreach (var (key, target) in _edges) if (target == id) toRemove.Add(key);
        foreach (var k in toRemove) _edges.Remove(k);
        return node;
    }

    public void Connect(ulong from, NavDirection dir, ulong to)
    {
        if (_nodes.ContainsKey(from) && _nodes.ContainsKey(to))
            _edges[(from, dir)] = to;
    }

    public void Disconnect(ulong from, NavDirection dir) => _edges.Remove((from, dir));

    public ulong? Navigate(ulong from, NavDirection dir) =>
        _edges.TryGetValue((from, dir), out var to) ? to : null;

    public FocusNode? Get(ulong id) => _nodes.TryGetValue(id, out var n) ? n : null;
    public int NodeCount => _nodes.Count;
    public int EdgeCount => _edges.Count;
    public bool IsEmpty => _nodes.Count == 0;
    public IEnumerable<ulong> NodeIds => _nodes.Keys;

    public List<ulong> TabOrder()
    {
        var ordered = _nodes.Values.Where(n => n.IsFocusable && n.TabIndex >= 0)
            .OrderBy(n => n.TabIndex).ThenBy(n => n.Id).Select(n => n.Id).ToList();
        return ordered;
    }

    public List<ulong> GroupTabOrder(uint group)
    {
        return _nodes.Values.Where(n => n.IsFocusable && n.TabIndex >= 0 && n.GroupId == group)
            .OrderBy(n => n.TabIndex).ThenBy(n => n.Id).Select(n => n.Id).ToList();
    }

    public List<ulong>? FindCycle(ulong start)
    {
        // Floyd's tortoise-and-hare
        ulong? slow = start, fast = start;
        while (true)
        {
            slow = Navigate(slow!.Value, NavDirection.Next); if (slow == null) return null;
            fast = Navigate(fast!.Value, NavDirection.Next); if (fast == null) return null;
            fast = Navigate(fast.Value, NavDirection.Next); if (fast == null) return null;
            if (slow == fast) break;
        }
        ulong p1 = start, p2 = slow!.Value;
        while (p1 != p2) { p1 = Navigate(p1, NavDirection.Next)!.Value; p2 = Navigate(p2, NavDirection.Next)!.Value; }
        var cycle = new List<ulong> { p1 };
        ulong cur = Navigate(p1, NavDirection.Next)!.Value;
        while (cur != p1) { cycle.Add(cur); cur = Navigate(cur, NavDirection.Next)!.Value; }
        cycle.Add(p1);
        return cycle;
    }

    public List<ulong>? FindCycleInDirection(ulong start, NavDirection dir)
    {
        ulong? slow = start, fast = start;
        while (true)
        {
            slow = Navigate(slow!.Value, dir); if (slow == null) return null;
            fast = Navigate(fast!.Value, dir); if (fast == null) return null;
            fast = Navigate(fast.Value, dir); if (fast == null) return null;
            if (slow == fast) break;
        }
        ulong p1 = start, p2 = slow!.Value;
        while (p1 != p2) { p1 = Navigate(p1, dir)!.Value; p2 = Navigate(p2, dir)!.Value; }
        var cycle = new List<ulong> { p1 };
        ulong cur = Navigate(p1, dir)!.Value;
        while (cur != p1) { cycle.Add(cur); cur = Navigate(cur, dir)!.Value; }
        cycle.Add(p1);
        return cycle;
    }

    public void BuildTabChain(bool wrap)
    {
        _edges.Where(kv => kv.Key.Item2 is NavDirection.Next or NavDirection.Prev).ToList()
            .ForEach(kv => _edges.Remove(kv.Key));
        var order = TabOrder();
        if (order.Count < 2) return;
        for (int i = 0; i < order.Count - 1; i++) { _edges[(order[i], NavDirection.Next)] = order[i + 1]; _edges[(order[i + 1], NavDirection.Prev)] = order[i]; }
        if (wrap) { _edges[(order[^1], NavDirection.Next)] = order[0]; _edges[(order[0], NavDirection.Prev)] = order[^1]; }
    }

    public void Clear() { _nodes.Clear(); _edges.Clear(); }
}
