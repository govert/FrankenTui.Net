namespace FrankenTui.A11y;

public sealed class AccessibilitySnapshot
{
    private readonly List<AccessibleNode> _nodes = [];

    public IReadOnlyList<AccessibleNode> Nodes => _nodes;

    public AccessibilitySnapshot Add(string role, string label, string? description = null)
    {
        _nodes.Add(new AccessibleNode(role, label, description));
        return this;
    }
}
