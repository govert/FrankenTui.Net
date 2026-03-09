namespace FrankenTui.A11y;

public sealed record AccessibleNode(string Role, string Label, string? Description = null);
