namespace FrankenTui.Testing.Harness;

public sealed record RenderSnapshot(IReadOnlyList<string> Rows, string Text, string? Output = null);
