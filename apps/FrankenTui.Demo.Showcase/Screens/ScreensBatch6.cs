using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 6: final batch — screens 2, 16, 17, 18, 20, 25, 26, 28, 29, 30, 31, 33, 34, 45

internal static class Screen02Dashboard { public static IWidget Build(ShowcaseDemoState s) { var tiles = string.Join("\n", "╭─ System Health ─────╮  ╭─ Recent Logs ───────────────────╮", "│ CPU:   42%  ████░░ │  │ 08:00 INFO  Showcase booted      │", "│ MEM:   68%  ██████░│  │ 08:01 DEBUG Diff decision FULL    │", "│ DISK:  23%  ██░░░░ │  │ 08:02 WARN  Replay ledger missing │", "│ NET:   12%  █░░░░░ │  │ 08:03 INFO  Command palette ranked│", "╰─────────────────────╯  ╰────────────────────────────────╯", "", "╭─ Quick Stats ───────╮  ╭─ Active Tasks ────────────────╮", "│ FPS:      60.0      │  │ ● Port widgets       [████░░] │", "│ Frame:    0ms       │  │ ● Add tests          [██░░░░] │", "│ Uptime:   2m 34s    │  │ ● Fix layout         [██████] │", "│ Screens:  45        │  │ ○ Deploy              [░░░░░░] │", "╰─────────────────────╯  ╰──────────────────────────────╯"); return ShowcaseSurface.Panel("Dashboard", tiles); } }

internal static class Screen16Mermaid { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Mermaid Showcase", "graph TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Process]\n    B -->|No| D[End]\n    C --> D\n\nflowchart LR\n    subgraph Frontend\n        UI --> API\n    end\n    subgraph Backend\n        API --> DB[(Database)]\n        API --> Cache[(Redis)]\n    end\n\nsequenceDiagram\n    Client->>Server: Request\n    Server->>DB: Query\n    DB-->>Server: Result\n    Server-->>Client: Response"); } }

internal static class Screen17MermaidMega { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Mermaid Mega Showcase", "Diagram Types:\n  flowchart  • sequence  • class\n  state      • er        • gantt\n  pie        • git       • mindmap\n  timeline   • sankey    • block\n\nControls:\n  ↑/↓ select type\n  ←/→ adjust zoom\n  r reset view\n  e export SVG\n  d toggle debug\n\nCurrent: flowchart\nNodes: 12  Edges: 15\nRender time: 2.3ms\nCache: warm (12 diagrams)"); } }

internal static class Screen18VisualEffects { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Visual Effects", "Effect: Matrix Rain\nFPS: 60  |  Particles: 500\nSpeed: 1.0x  |  Density: high\nColor: #00FF00\n\nControls:\n  Space  play/pause\n  ←/→    cycle effects\n  ↑/↓    adjust speed\n  r      reset\n  c      cycle colors\n  d      toggle debug\n\nEffects:\n  Matrix Rain  •  Fire  •  Snow\n  Plasma  •  Starfield  •  Conway\n  Particles  •  Wave  •  Vortex"); } }



internal static class Screen26MousePlayground { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Mouse Playground", "Mouse Position: (40, 12)\nLast Event: Move\nButton: None\n\nClick targets:\n\n┌─ Button 1 ─┐  ┌─ Button 2 ─┐\n│   Click!   │  │   Click!   │\n└────────────┘  └────────────┘\n\n┌─ Toggle ───┐\n│  [ ] OFF   │\n└────────────┘\n\nScroll: 0\nDrag: idle\nHit-test: active"); } }






internal static class Screen45Quake { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Quake E1M1 (Easter Egg)", "WASD move · Arrows look · Space jump · F fire · V quality [Reduced] · R reset\n\n                    ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿\n                    ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿\n                    ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿\n   Retro FPS renderer using braille and box-drawing characters.\n   Runs at ~30 FPS in terminal. Use V to toggle quality.\n\n   \"The slipgate complex is the largest installation of its kind...\""); } }
