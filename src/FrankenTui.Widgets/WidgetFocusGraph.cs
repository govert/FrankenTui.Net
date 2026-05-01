using FrankenTui.Core;

namespace FrankenTui.Widgets;

public enum WidgetNavigationDirection
{
    Up,
    Down,
    Left,
    Right,
    Next,
    Prev
}

public sealed record WidgetFocusNode(string Id, Rect Bounds)
{
    public int TabIndex { get; init; }

    public bool IsFocusable { get; init; } = true;

    public string? GroupId { get; init; }

    public WidgetFocusNode WithTabIndex(int tabIndex) => this with { TabIndex = tabIndex };

    public WidgetFocusNode WithFocusable(bool isFocusable) => this with { IsFocusable = isFocusable };

    public WidgetFocusNode WithGroup(string? groupId) => this with { GroupId = groupId };
}

public readonly record struct WidgetFocusEdgeKey(string FromId, WidgetNavigationDirection Direction);

public sealed class WidgetFocusGraph
{
    private readonly Dictionary<string, WidgetFocusNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<WidgetFocusEdgeKey, string> _edges = new();

    public int NodeCount => _nodes.Count;

    public int EdgeCount => _edges.Count;

    public bool IsEmpty => _nodes.Count == 0;

    public string Insert(WidgetFocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentException.ThrowIfNullOrWhiteSpace(node.Id);

        _nodes[node.Id] = node;
        return node.Id;
    }

    public WidgetFocusNode? Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_nodes.Remove(id, out var node))
        {
            return null;
        }

        foreach (var direction in Enum.GetValues<WidgetNavigationDirection>())
        {
            _edges.Remove(new WidgetFocusEdgeKey(id, direction));
        }

        foreach (var edge in _edges.Where(pair => string.Equals(pair.Value, id, StringComparison.Ordinal)).ToArray())
        {
            _edges.Remove(edge.Key);
        }

        return node;
    }

    public void Connect(string fromId, WidgetNavigationDirection direction, string toId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toId);

        if (!_nodes.ContainsKey(fromId) || !_nodes.ContainsKey(toId))
        {
            return;
        }

        _edges[new WidgetFocusEdgeKey(fromId, direction)] = toId;
    }

    public void Disconnect(string fromId, WidgetNavigationDirection direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
        _edges.Remove(new WidgetFocusEdgeKey(fromId, direction));
    }

    public string? Navigate(string fromId, WidgetNavigationDirection direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromId);
        return _edges.TryGetValue(new WidgetFocusEdgeKey(fromId, direction), out var target) ? target : null;
    }

    public WidgetFocusNode? Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _nodes.TryGetValue(id, out var node) ? node : null;
    }

    public IReadOnlyList<string> NodeIds() =>
        _nodes.Keys
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> TabOrder() =>
        _nodes.Values
            .Where(static node => node.IsFocusable && node.TabIndex >= 0)
            .OrderBy(static node => node.TabIndex)
            .ThenBy(static node => node.Id, StringComparer.Ordinal)
            .Select(static node => node.Id)
            .ToArray();

    public IReadOnlyList<string> GroupTabOrder(string groupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        return _nodes.Values
            .Where(node => node.IsFocusable && node.TabIndex >= 0 && string.Equals(node.GroupId, groupId, StringComparison.Ordinal))
            .OrderBy(static node => node.TabIndex)
            .ThenBy(static node => node.Id, StringComparer.Ordinal)
            .Select(static node => node.Id)
            .ToArray();
    }

    public void BuildTabChain(bool wrap)
    {
        foreach (var key in _edges.Keys.Where(static key => key.Direction is WidgetNavigationDirection.Next or WidgetNavigationDirection.Prev).ToArray())
        {
            _edges.Remove(key);
        }

        var order = TabOrder();
        if (order.Count < 2)
        {
            return;
        }

        for (var index = 0; index < order.Count - 1; index++)
        {
            _edges[new WidgetFocusEdgeKey(order[index], WidgetNavigationDirection.Next)] = order[index + 1];
            _edges[new WidgetFocusEdgeKey(order[index + 1], WidgetNavigationDirection.Prev)] = order[index];
        }

        if (wrap)
        {
            _edges[new WidgetFocusEdgeKey(order[^1], WidgetNavigationDirection.Next)] = order[0];
            _edges[new WidgetFocusEdgeKey(order[0], WidgetNavigationDirection.Prev)] = order[^1];
        }
    }

    public void Clear()
    {
        _nodes.Clear();
        _edges.Clear();
    }
}
