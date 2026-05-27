using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Screen 16: Mermaid Showcase — diagram renderer with controls + diagnostics.
/// Ported from mermaid_showcase.rs using existing MermaidEngine.</summary>
internal static class Screen16MermaidEngine
{
    public static IWidget Build(ShowcaseDemoState state) => new MermaidShowcaseWidget();

    private sealed class MermaidShowcaseWidget : IWidget
    {
        static readonly MermaidSample[] Samples =
        [
            new("flow-basic", "Flowchart Basic", "flowchart", ["flowchart", "demo"], "simple", 4, 3,
                "flowchart TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Process]\n    B -->|No| D[End]\n    C --> D", "Start ──▶ Decision ──▶ Process", "Start --> Decision --> Process"),
            new("flow-arch", "Service Architecture", "flowchart", ["flowchart", "architecture"], "medium", 5, 4,
                "flowchart LR\n    A[Client] --> B[API Gateway]\n    B --> C[Auth]\n    B --> D[Data]\n    C --> E[(DB)]\n    D --> E", "Client ──▶ Gateway ──▶ Auth", "Client --> Gateway --> Auth"),
            new("seq-basic", "Sequence Basic", "sequence", ["sequence", "demo"], "simple", 3, 3,
                "sequenceDiagram\n    Client->>Server: Request\n    Server->>Database: Query\n    Database-->>Server: Result\n    Server-->>Client: Response", "Client ──▶ Server", "Client --> Server"),
            new("state-basic", "State Machine", "state", ["state", "demo"], "simple", 5, 5,
                "stateDiagram\n    [*] --> Idle\n    Idle --> Running: start\n    Running --> Paused: pause\n    Paused --> Running: resume\n    Running --> Stopped: stop\n    Stopped --> [*]", "Idle → Running → Paused", "Idle -> Running -> Paused"),
            new("flow-styled", "Styled Flowchart", "flowchart", ["flowchart", "styles"], "simple", 4, 3,
                "flowchart TD\n    A[Start] --> B[Process]\n    B --> C[Validate]\n    C --> D[End]\n    style A fill:#4a9\n    style D fill:#f96", "Start ──▶ Process ──▶ Validate", "Start --> Process --> Validate"),
            new("seq-detailed", "Detailed Sequence", "sequence", ["sequence", "advanced"], "medium", 5, 4,
                "sequenceDiagram\n    participant U as User\n    participant F as Frontend\n    participant B as Backend\n    U->>F: Click\n    F->>B: POST /api\n    B-->>F: 201 OK\n    F-->>U: Success", "User ──▶ Frontend", "User --> Frontend"),
        ];

        readonly MermaidConfig _config = MermaidConfig.Default;
        MermaidShowcasePreferences _prefs = MermaidShowcasePreferences.Default;
        int _sampleIdx;
        string _status = "";
        List<MermaidDiagnostic> _diagnostics = [];

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty || area.Height < 6) return;

            // Layout: header(1) + diagram(fill) + status(1)
            int diagH = area.Height - 2;
            if (diagH <= 0) return;

            // Header: diagram type selector + controls
            var headerY = area.Y;
            var diagY = (ushort)(area.Y + 1);
            var statusY = (ushort)(area.Y + 1 + diagH);

            var sample = Samples[_sampleIdx % Samples.Length];
            var header = $"Mermaid Showcase | [{_sampleIdx+1}/{Samples.Length}] {sample.Category} | ←→:switch | g:glyph | w:wrap";
            BufferPainter.WriteText(context.Buffer, area.X, headerY, header, Cell.FromChar(' '));

            // Parse and render
            var diagram = MermaidEngine.Parse(sample, _config);
            _diagnostics = diagram.Diagnostics.ToList();

            var vp = MermaidEngine.Render(diagram, _config, _prefs, area.Width, (ushort)diagH);
            var rows = vp.Rows;

            // Render diagram area
            for (int i = 0; i < rows.Count && i < diagH; i++)
            {
                var line = rows[i];
                var y = (ushort)(diagY + i);
                for (int x = 0; x < line.Length && x < area.Width; x++)
                    context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(line[x]));
            }

            // Status bar: performance + diagnostics summary
            var nodes = diagram.Nodes.Count;
            var edges = diagram.Edges.Count;
            var errors = _diagnostics.Count(d => d.Severity == MermaidDiagnosticSeverity.Error);
            var glyph = _prefs.GlyphMode == MermaidGlyphMode.Unicode ? "Unicode" : "ASCII";
            _status = $"[{diagram.Kind}] Nodes:{nodes} Edges:{edges} Errors:{errors} | Glyph:{glyph} | Wrap:{_prefs.WrapMode}";
            BufferPainter.WriteText(context.Buffer, area.X, statusY, _status, Cell.FromChar(' '));
        }
    }
}
