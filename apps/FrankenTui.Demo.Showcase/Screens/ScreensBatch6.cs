using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 6: final batch — screens 2, 16, 17, 18, 20, 25, 26, 28, 29, 30, 31, 33, 34, 45


internal static class Screen16Mermaid { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Mermaid Showcase", "graph TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Process]\n    B -->|No| D[End]\n    C --> D\n\nflowchart LR\n    subgraph Frontend\n        UI --> API\n    end\n    subgraph Backend\n        API --> DB[(Database)]\n        API --> Cache[(Redis)]\n    end\n\nsequenceDiagram\n    Client->>Server: Request\n    Server->>DB: Query\n    DB-->>Server: Result\n    Server-->>Client: Response"); } }


internal static class Screen18VisualEffects { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Visual Effects", "Effect: Matrix Rain\nFPS: 60  |  Particles: 500\nSpeed: 1.0x  |  Density: high\nColor: #00FF00\n\nControls:\n  Space  play/pause\n  ←/→    cycle effects\n  ↑/↓    adjust speed\n  r      reset\n  c      cycle colors\n  d      toggle debug\n\nEffects:\n  Matrix Rain  •  Fire  •  Snow\n  Plasma  •  Starfield  •  Conway\n  Particles  •  Wave  •  Vortex"); } }




