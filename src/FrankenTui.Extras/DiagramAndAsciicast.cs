namespace FrankenTui.Extras;

/// <summary>Diagram support (DOT parser stub). Matches upstream diagram + dot_parser.</summary>
public static class DiagramSupport
{
    public static string ParseDot(string dot) => dot.Contains("digraph") ? "diagram:valid" : "diagram:parse_error";
}

/// <summary>Diagram layout engine stub. Matches upstream diagram_layout.</summary>
public sealed class DiagramLayout
{
    public IReadOnlyList<(double X, double Y, string Label)> Layout(string dot)
    {
        var lines = dot.Split('\n').Where(l => l.Contains("->")).ToList();
        var result = new List<(double, double, string)>();
        for (var i = 0; i < lines.Count; i++)
            result.Add((i * 10.0, i * 5.0, $"node_{i}"));
        return result;
    }
}

/// <summary>Asciicast recording format. Matches upstream asciicast.</summary>
public sealed class AsciicastRecorder
{
    private readonly List<string> _frames = [];
    private double _time;

    public void RecordFrame(string content)
    {
        _frames.Add($"[{_time:F6},\"o\",\"{EscapeJson(content)}\"]");
        _time += 0.016;
    }

    public string ToAsciicast(int width, int height) =>
        $"{{\"version\":2,\"width\":{width},\"height\":{height}}}\n" + string.Join("\n", _frames);

    private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
