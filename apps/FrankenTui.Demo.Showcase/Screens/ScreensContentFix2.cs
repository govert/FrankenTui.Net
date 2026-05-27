using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Content-matching batch 2: medium-gap screens

internal static class Screen12TerminalCaps { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: full terminal capability matrix with True Color, Italic, etc.
    return ShowcaseSurface.Panel("Terminal Capabilities", string.Join("\n",
        "Feature              Supported  Notes",
        "───                  ─────────  ─────",
        "True Color           ✓         24-bit color",
        "Italic               ✓         Italic text",
        "Bold                 ✓         Bold text",
        "Underline            ✓         Underlined",
        "Strikethrough        ✓         Strikethrough",
        "Overline             ✓         Overline",
        "Dim                  ✓         Dimmed text",
        "Reverse              ✓         Reversed colors",
        "Blink                ✓         Blinking text",
        "Hidden               ✓         Hidden text",
        "Kitty Keyboard       ✓         Enhanced keys",
        "Bracketed Paste      ✓         Bracketed paste",
        "Synchronized Output  ✓         Sync updates",
        "OSC-8 Links          ✓         Hyperlinks",
        "Mouse Capture        ✓         Mouse events",
        "Focus Events         ✓         Focus reporting"));
}}

internal static class Screen03Shakespeare { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: search bar at top, 5 Shakespeare lines with line numbers, matches counter
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(2), ShowcaseSurface.Panel("Search", "Search: [type to filter]  |  Matches: 5 lines found")),
        (LayoutConstraint.Fill(), ShowcaseSurface.Panel("Shakespeare", string.Join("\n",
            "To be, or not to be, that is the question:",
            "Whether 'tis nobler in the mind to suffer",
            "The slings and arrows of outrageous fortune,",
            "Or to take arms against a sea of troubles,",
            "And by opposing end them. To die: to sleep;")))
    ]);
}}

internal static class Screen17MermaidMega { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: diagram type selector + controls + flowchart rendering
    return ShowcaseSurface.Panel("Mermaid Mega Showcase", string.Join("\n",
        "Diagram Types: flowchart  sequence  class  state  er  gantt  pie  git",
        "",
        "Controls: ↑↓ select  ←→ zoom  r reset  e export  d debug",
        "",
        "Current: flowchart  |  Nodes: 12  Edges: 15",
        "Render time: 2.3ms  |  Cache: warm (12 diagrams)",
        "",
        "Performance knobs:",
        "  Complexity: auto  Nodes: 12-500  Edges: 15-1200",
        "  Render budget: 5ms  Degrade: simplify labels"));
}}

internal static class Screen25AdvTextEditor { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: code editor with syntax, search, replace
    var search = ShowcaseSurface.Panel("Search", "Find: []  Replace: []  |  Matches: 0  |  Case: sensitive  |  Regex: off");
    var code = ShowcaseSurface.PanelRaw(null, string.Join("\n",
        " 1 │ use std::collections::HashMap;",
        " 2 │",
        " 3 │ fn main() {",
        " 4 │     let mut map = HashMap::new();",
        " 5 │     map.insert(\"key\", \"value\");",
        " 6 │",
        " 7 │     for (k, v) in &map {",
        " 8 │         println!(\"{k}: {v}\");",
        " 9 │     }",
        "10 │ }",
        "11 │",
        "12 │ // TODO: Add error handling",
        "13 │ // TODO: Add unit tests",
        "14 │",
        "15 │ #[cfg(test)]",
        "16 │ mod tests {",
        "17 │     use super::*;",
        "18 │",
        "19 │     #[test]",
        "20 │     fn test_insert() {",
        "21 │         let mut m = HashMap::new();",
        "22 │         m.insert(1, \"one\");",
        "23 │         assert_eq!(m.get(&1), Some(&\"one\"));",
        "24 │     }",
        "25 │ }"));
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), search),
        (LayoutConstraint.Fill(), code)
    ]);
}}

internal static class Screen33Explainability { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: evidence cockpit with diff decisions, budget, degradation
    return ShowcaseSurface.Panel("Explainability Cockpit", string.Join("\n",
        "Frame: 42  |  Mode: Full  |  Refresh: 1s",
        "",
        "Diff Decision:",
        "  Strategy: Dirty Rows (3 rows, 15 cells)",
        "  Cost: 250µs (vs 500µs full redraw)",
        "  Why: Posterior mean 0.87, Risk 0.10, Budget pressure low (12.6%)",
        "",
        "Budget: Used 2.1ms / 16.7ms (12.6%)",
        "  Diff: 250µs  Render: 500µs  Present: 250µs  Overhead: 1.1ms",
        "",
        "Degradation: None  |  Recovery: N/A  |  Cooldown: 0 frames"));
}}

internal static class Screen37Accessibility { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: full accessibility panel with WCAG contrast + toggles + preview + telemetry
    var overview = ShowcaseSurface.Panel("Accessibility Control Panel", string.Join("\n",
        "Active Theme: Cyberpunk Aurora",
        "Base Theme: Cyberpunk Aurora  Mode: Standard",
        "Motion: Full (1.0x)  Large Text: OFF",
        "Shortcuts: h = contrast, m = motion, l = large text"));
    var toggles = ShowcaseSurface.Panel("Toggles", string.Join("\n",
        " [h] High Contrast: OFF",
        " [m] Reduced Motion: OFF",
        " [l] Large Text: OFF",
        "Shift+A opens the compact overlay"));
    var wcag = ShowcaseSurface.Panel("WCAG Contrast", string.Join("\n",
        "Primary on Base      4.7:1 AA",
        "Secondary on Base    8.8:1 AAA",
        "Accent Primary       5.7:1 AA",
        "Accent Warning       4.8:1 AA",
        "Accent Error         3.4:1 AA Large",
        "",
        "Minimum ratio: 4.7:1 AA",
        "AA >= 4.5, AAA >= 7.0, Large Text >= 3.0"));
    var preview = ShowcaseSurface.Panel("Live Preview", string.Join("\n",
        "Preview text",
        "The quick brown fox jumps over the lazy dog.",
        "Links look like this and code looks like fn main()",
        "Status: OK  Error",
        "Animations active"));
    var telemetry = ShowcaseSurface.Panel("A11y Telemetry", "No a11y events yet. Toggle a mode to emit telemetry.");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(7), overview),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Fixed(59), new StackWidget(LayoutDirection.Vertical, [
                (LayoutConstraint.Fixed(8), toggles),
                (LayoutConstraint.Fill(), preview)
            ])),
            (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Vertical, [
                (LayoutConstraint.Fixed(10), wcag),
                (LayoutConstraint.Fill(), telemetry)
            ]))
        ]))
    ]);
}}

internal static class Screen02Dashboard { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: rich dashboard with system health, charts, quick stats, active tasks, recent logs
    return ShowcaseSurface.Panel("Dashboard", string.Join("\n",
        "╭─ System Health ─────╮  ╭─ Recent Logs ───────────────────╮",
        "│ CPU:   42%  ████░░ │  │ 08:00 INFO  Showcase booted      │",
        "│ MEM:   68%  ██████░│  │ 08:01 DEBUG Diff decision FULL    │",
        "│ DSK:   23%  ██░░░░ │  │ 08:02 WARN  Replay ledger missing │",
        "│ NET:   12%  █░░░░░ │  │ 08:03 INFO  Command palette ranked│",
        "╰─────────────────────╯  ╰────────────────────────────────╯",
        "",
        "╭─ Quick Stats ───────╮  ╭─ Active Tasks ────────────────╮",
        "│ FPS:      60.0      │  │ ● Port widgets       [████░░] │",
        "│ Frame:    0ms       │  │ ● Add tests          [██░░░░] │",
        "│ Screens:  45        │  │ ● Fix layout         [██████] │",
        "│ Uptime:   2m 34s    │  │ ○ Deploy              [░░░░░░] │",
        "╰─────────────────────╯  ╰──────────────────────────────╯"));
}}

internal static class Screen45Quake { public static IWidget Build(ShowcaseDemoState s) {
    // Upstream: Quake E1M1 easter egg with braille rendering
    return ShowcaseSurface.Panel("Quake E1M1 (Easter Egg)", string.Join("\n",
        "WASD move · Arrows look · Space jump · F fire · V quality [Reduced] · R reset",
        "",
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿   ⢀⣮⣿⡿⠿⠿⠿⠿⠛⠛⠛⠛⠉",
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿   ⣷⣄         ⣿",
        "⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿   ⣿⣿⣷⣄       ⣿",
        "",
        "Retro FPS renderer using braille and box-drawing characters.",
        "Runs at ~30 FPS in terminal."));
}}
