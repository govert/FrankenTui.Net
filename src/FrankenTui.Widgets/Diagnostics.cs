namespace FrankenTui.Widgets;

/// <summary>Diagnostic information for widget debugging. Matches upstream diagnostics.</summary>
public sealed record WidgetDiagnostic(string WidgetName, string Property, string Value, DateTimeOffset Timestamp)
{
    public override string ToString() => $"[{Timestamp:HH:mm:ss}] {WidgetName}.{Property} = {Value}";
}

/// <summary>Diagnostics collector for widget inspection. Matches upstream diagnostics module.</summary>
public sealed class DiagnosticsCollector
{
    private readonly List<WidgetDiagnostic> _entries = [];
    public bool Enabled { get; set; }
    public IReadOnlyList<WidgetDiagnostic> Entries => _entries;
    public int MaxEntries { get; init; } = 100;

    public void Record(string widget, string property, string value)
    {
        if (!Enabled) return;
        _entries.Add(new WidgetDiagnostic(widget, property, value, DateTimeOffset.UtcNow));
        while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
    }

    public void Clear() => _entries.Clear();

    public IEnumerable<string> Recent(int count = 20) =>
        _entries.TakeLast(count).Select(d => d.ToString());
}
