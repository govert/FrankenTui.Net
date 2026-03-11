using System.Text;

namespace FrankenTui.Extras;

public enum MermaidDiagramKind
{
    Flowchart,
    Sequence,
    State,
    Unknown
}

public enum MermaidDiagnosticSeverity
{
    Info,
    Warn,
    Error
}

public sealed record MermaidDiagnostic(
    string Code,
    MermaidDiagnosticSeverity Severity,
    string Message);

public sealed record MermaidNode(
    string Id,
    string Label);

public sealed record MermaidEdge(
    string FromId,
    string ToId,
    string? Label = null);

public sealed record MermaidDiagram(
    MermaidDiagramKind Kind,
    string Direction,
    IReadOnlyList<MermaidNode> Nodes,
    IReadOnlyList<MermaidEdge> Edges,
    IReadOnlyList<MermaidDiagnostic> Diagnostics);

public sealed record MermaidViewport(
    IReadOnlyList<string> Rows,
    IReadOnlyList<MermaidDiagnostic> Diagnostics);

public sealed record MermaidShowcasePreferences(
    int SelectedSampleIndex = 0,
    MermaidLayoutMode LayoutMode = MermaidLayoutMode.Auto,
    MermaidTier Fidelity = MermaidTier.Auto,
    MermaidGlyphMode GlyphMode = MermaidGlyphMode.Unicode,
    MermaidWrapMode WrapMode = MermaidWrapMode.WordChar,
    bool StylesEnabled = true,
    bool MetricsVisible = true,
    bool ControlsVisible = true)
{
    public static MermaidShowcasePreferences Default { get; } = new();
}

internal static class MermaidEngine
{
    public static MermaidDiagram Parse(MermaidSample sample, MermaidConfig config)
    {
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentNullException.ThrowIfNull(config);

        var diagnostics = new List<MermaidDiagnostic>();
        var lines = sample.Source.ReplaceLineEndings("\n").Split('\n');
        var header = lines
            .Select(static line => line.Trim())
            .FirstOrDefault(static line => !string.IsNullOrWhiteSpace(line)) ?? string.Empty;

        var kind = header.StartsWith("graph", StringComparison.OrdinalIgnoreCase) ||
                   header.StartsWith("flowchart", StringComparison.OrdinalIgnoreCase)
            ? MermaidDiagramKind.Flowchart
            : header.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase)
                ? MermaidDiagramKind.Sequence
                : header.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase)
                    ? MermaidDiagramKind.State
                    : MermaidDiagramKind.Unknown;

        if (kind == MermaidDiagramKind.Unknown)
        {
            diagnostics.Add(new MermaidDiagnostic(
                "mermaid/unsupported/diagram",
                MermaidDiagnosticSeverity.Error,
                $"Unsupported diagram header: {header}."));
            return new MermaidDiagram(kind, "TB", [], [], diagnostics);
        }

        var direction = ResolveDirection(header);
        var nodes = new List<MermaidNode>();
        var edges = new List<MermaidEdge>();
        var nodeIndex = new Dictionary<string, MermaidNode>(StringComparer.Ordinal);
        var participantOrder = new List<string>();

        foreach (var rawLine in lines.Skip(1))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("%%{", StringComparison.Ordinal))
            {
                if (!config.EnableInitDirectives)
                {
                    diagnostics.Add(new MermaidDiagnostic(
                        "mermaid/unsupported/directive",
                        MermaidDiagnosticSeverity.Warn,
                        "Init directives are disabled by config."));
                }

                continue;
            }

            if (line.StartsWith("classDef", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("style ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("class ", StringComparison.OrdinalIgnoreCase))
            {
                if (!config.EnableStyles)
                {
                    diagnostics.Add(new MermaidDiagnostic(
                        "mermaid/unsupported/style",
                        MermaidDiagnosticSeverity.Warn,
                        "Style directives are disabled by config."));
                }

                continue;
            }

            if (line.StartsWith("click ", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new MermaidDiagnostic(
                    config.EnableLinks ? "mermaid/unsupported/link" : "mermaid/sanitized/input",
                    MermaidDiagnosticSeverity.Warn,
                    config.EnableLinks ? "Link directives are not rendered in the terminal baseline." : "Link directives were blocked by config."));
                continue;
            }

            if (TryParseFlowOrState(line, nodeIndex, nodes, edges, out var flowDiagnostic))
            {
                if (flowDiagnostic is not null)
                {
                    diagnostics.Add(flowDiagnostic);
                }

                continue;
            }

            if (TryParseSequence(line, participantOrder, nodeIndex, nodes, edges, out var sequenceDiagnostic))
            {
                if (sequenceDiagnostic is not null)
                {
                    diagnostics.Add(sequenceDiagnostic);
                }

                continue;
            }

            diagnostics.Add(new MermaidDiagnostic(
                "mermaid/unsupported/feature",
                MermaidDiagnosticSeverity.Warn,
                $"Ignored line: {line}"));
        }

        if (nodes.Count > config.MaxNodes || edges.Count > config.MaxEdges)
        {
            diagnostics.Add(new MermaidDiagnostic(
                "mermaid/limit/exceeded",
                MermaidDiagnosticSeverity.Warn,
                $"Diagram exceeds configured limits ({nodes.Count} nodes / {edges.Count} edges)."));
        }

        return new MermaidDiagram(kind, direction, nodes, edges, diagnostics);
    }

    public static MermaidViewport Render(
        MermaidDiagram diagram,
        MermaidConfig config,
        MermaidShowcasePreferences preferences,
        ushort width,
        ushort height)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(preferences);

        if (!config.Enabled)
        {
            return new MermaidViewport(["Mermaid disabled by config."], [
                new MermaidDiagnostic("mermaid/disabled", MermaidDiagnosticSeverity.Info, "Mermaid rendering is disabled.")
            ]);
        }

        if (diagram.Kind == MermaidDiagramKind.Unknown)
        {
            return new MermaidViewport(
                ["Unsupported Mermaid diagram."],
                diagram.Diagnostics);
        }

        var rows = diagram.Kind switch
        {
            MermaidDiagramKind.Flowchart => RenderFlowchart(diagram, preferences),
            MermaidDiagramKind.Sequence => RenderSequence(diagram, preferences),
            MermaidDiagramKind.State => RenderState(diagram, preferences),
            _ => ["Unsupported Mermaid diagram."]
        };

        var wrapped = rows
            .Select(row => WrapRow(row, width, preferences.WrapMode))
            .SelectMany(static row => row)
            .Take(Math.Max(height - 2, 1))
            .ToArray();

        if (wrapped.Length == 0)
        {
            wrapped = [string.Empty];
        }

        return new MermaidViewport(wrapped, diagram.Diagnostics);
    }

    private static string ResolveDirection(string header)
    {
        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[^1].ToUpperInvariant() : "TB";
    }

    private static bool TryParseFlowOrState(
        string line,
        IDictionary<string, MermaidNode> nodeIndex,
        ICollection<MermaidNode> nodes,
        ICollection<MermaidEdge> edges,
        out MermaidDiagnostic? diagnostic)
    {
        diagnostic = null;

        var arrow = line.Contains("-->", StringComparison.Ordinal)
            ? "-->"
            : line.Contains("->>", StringComparison.Ordinal)
                ? "->>"
                : line.Contains("-->>", StringComparison.Ordinal)
                    ? "-->>"
                    : null;
        if (arrow is null)
        {
            return false;
        }

        var parts = line.Split(arrow, 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            diagnostic = new MermaidDiagnostic(
                "mermaid/parse/error",
                MermaidDiagnosticSeverity.Error,
                $"Could not parse edge line: {line}");
            return true;
        }

        var from = ParseNode(parts[0], nodeIndex, nodes);
        var toText = parts[1];
        string? label = null;
        if (toText.Contains(':', StringComparison.Ordinal))
        {
            var messageParts = toText.Split(':', 2, StringSplitOptions.TrimEntries);
            toText = messageParts[0];
            label = messageParts[1];
        }

        var to = ParseNode(toText, nodeIndex, nodes);
        edges.Add(new MermaidEdge(from.Id, to.Id, label));
        return true;
    }

    private static bool TryParseSequence(
        string line,
        ICollection<string> participantOrder,
        IDictionary<string, MermaidNode> nodeIndex,
        ICollection<MermaidNode> nodes,
        ICollection<MermaidEdge> edges,
        out MermaidDiagnostic? diagnostic)
    {
        diagnostic = null;

        var arrow = line.Contains("->>", StringComparison.Ordinal)
            ? "->>"
            : line.Contains("-->>", StringComparison.Ordinal)
                ? "-->>"
                : null;
        if (arrow is null)
        {
            return false;
        }

        var parts = line.Split(arrow, 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            diagnostic = new MermaidDiagnostic(
                "mermaid/parse/error",
                MermaidDiagnosticSeverity.Error,
                $"Could not parse sequence line: {line}");
            return true;
        }

        var targetParts = parts[1].Split(':', 2, StringSplitOptions.TrimEntries);
        var from = ParseNode(parts[0], nodeIndex, nodes);
        var to = ParseNode(targetParts[0], nodeIndex, nodes);
        if (!participantOrder.Contains(from.Id, StringComparer.Ordinal))
        {
            participantOrder.Add(from.Id);
        }

        if (!participantOrder.Contains(to.Id, StringComparer.Ordinal))
        {
            participantOrder.Add(to.Id);
        }

        edges.Add(new MermaidEdge(from.Id, to.Id, targetParts.Length == 2 ? targetParts[1] : null));
        return true;
    }

    private static MermaidNode ParseNode(
        string text,
        IDictionary<string, MermaidNode> nodeIndex,
        ICollection<MermaidNode> nodes)
    {
        var trimmed = text.Trim();
        var id = new string(trimmed.TakeWhile(static ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-').ToArray());
        if (string.IsNullOrWhiteSpace(id))
        {
            id = trimmed;
        }

        var label = ExtractLabel(trimmed);
        if (!nodeIndex.TryGetValue(id, out var node))
        {
            node = new MermaidNode(id, label);
            nodeIndex[id] = node;
            nodes.Add(node);
        }

        return node;
    }

    private static string ExtractLabel(string text)
    {
        var bracketStart = text.IndexOfAny(['[', '{', '(']);
        if (bracketStart < 0)
        {
            return text.Trim();
        }

        var closing = text.LastIndexOfAny([']', '}', ')']);
        if (closing <= bracketStart)
        {
            return text.Trim();
        }

        return text[(bracketStart + 1)..closing].Trim();
    }

    private static IReadOnlyList<string> RenderFlowchart(MermaidDiagram diagram, MermaidShowcasePreferences preferences)
    {
        var arrow = preferences.GlyphMode == MermaidGlyphMode.Unicode ? "──▶" : "-->";
        var rows = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var edge in diagram.Edges)
        {
            var from = diagram.Nodes.First(node => node.Id == edge.FromId);
            var to = diagram.Nodes.First(node => node.Id == edge.ToId);
            rows.Add($"{from.Label} {arrow} {to.Label}{FormatLabel(edge.Label)}");
            visited.Add(from.Id);
            visited.Add(to.Id);
        }

        foreach (var node in diagram.Nodes.Where(node => !visited.Contains(node.Id)))
        {
            rows.Add(node.Label);
        }

        return rows;
    }

    private static IReadOnlyList<string> RenderSequence(MermaidDiagram diagram, MermaidShowcasePreferences preferences)
    {
        var arrow = preferences.GlyphMode == MermaidGlyphMode.Unicode ? "──▶" : "-->";
        var labels = diagram.Nodes.Select(static node => node.Label).ToArray();
        var rows = new List<string>
        {
            string.Join("  ", labels)
        };

        rows.AddRange(diagram.Edges.Select(edge =>
        {
            var from = diagram.Nodes.First(node => node.Id == edge.FromId);
            var to = diagram.Nodes.First(node => node.Id == edge.ToId);
            return $"{from.Label} {arrow} {to.Label}{FormatLabel(edge.Label)}";
        }));

        return rows;
    }

    private static IReadOnlyList<string> RenderState(MermaidDiagram diagram, MermaidShowcasePreferences preferences)
    {
        var arrow = preferences.GlyphMode == MermaidGlyphMode.Unicode ? "→" : "->";
        return diagram.Edges.Select(edge =>
        {
            var from = diagram.Nodes.First(node => node.Id == edge.FromId);
            var to = diagram.Nodes.First(node => node.Id == edge.ToId);
            return $"{from.Label} {arrow} {to.Label}";
        }).ToArray();
    }

    private static string FormatLabel(string? label) =>
        string.IsNullOrWhiteSpace(label) ? string.Empty : $"  {label}";

    private static IReadOnlyList<string> WrapRow(string row, ushort width, MermaidWrapMode wrapMode)
    {
        if (width == 0)
        {
            return [string.Empty];
        }

        if (row.Length <= width || wrapMode == MermaidWrapMode.None)
        {
            return [row];
        }

        var chunks = new List<string>();
        var remaining = row;
        while (remaining.Length > width)
        {
            var slice = remaining[..width];
            if (wrapMode is MermaidWrapMode.Word or MermaidWrapMode.WordChar)
            {
                var breakAt = slice.LastIndexOf(' ');
                if (breakAt > width / 4)
                {
                    chunks.Add(slice[..breakAt].TrimEnd());
                    remaining = remaining[(breakAt + 1)..];
                    continue;
                }
            }

            chunks.Add(slice);
            remaining = remaining[width..];
        }

        if (remaining.Length > 0)
        {
            chunks.Add(remaining);
        }

        return chunks;
    }
}
