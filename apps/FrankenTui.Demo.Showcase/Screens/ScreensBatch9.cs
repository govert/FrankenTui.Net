using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 9: remaining simple/medium screens

internal static class Screen36InlineMode { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::vertical([Fixed(2), Min(1)])
    var header = ShowcaseSurface.Panel(null, "Mode: Inline  |  Compare: OFF  |  Anchor: Bottom  |  UI height: 2  |  Rate: 2/tick\nStatus: Live  |  Lines: 114  |  Scrollback preserved in inline mode");
    var logLines = new List<string>();
    var levels = new[] { "ERROR", "DEBUG", "INFO ", "WARN " };
    var mods = new[] { "render", "render", "runtime", "runtime", "runtime", "widgets", "widgets", "widgets", "io", "io", "io", "layout", "layout", "layout", "core", "core", "core", "render" };
    var msgs = new[] { "scrollback ok", "scrollback ok", "scrollback ok", "scrollback ok", "inline anchor", "inline anchor", "inline anchor", "inline anchor", "inline anchor", "inline anchor", "inline anchor", "budget check", "budget check", "budget check", "budget check", "budget check", "budget check", "diff pass" };
    for (var i = 0; i < 18; i++) logLines.Add($"[{94+i:D6}] [{levels[i%4]}] {mods[i],-7} {msgs[i]}");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(2), header),
        (LayoutConstraint.Fill(), ShowcaseSurface.Panel("Inline Mode Story", string.Join("\n", logLines)))
    ]);
}}

internal static class Screen26MousePlayground { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::vertical([Fixed(2), Fill]) with hit targets
    var header = ShowcaseSurface.Panel(null, "Mouse Playground  |  Position: (40, 12)  |  Event: Move  |  Button: None  |  Scroll: 0");
    var targets = ShowcaseSurface.Panel("Hit Targets", string.Join("\n",
        "┌─ Button 1 ─┐  ┌─ Button 2 ─┐",
        "│   Click!   │  │   Click!   │",
        "└────────────┘  └────────────┘",
        "",
        "┌─ Toggle ───┐",
        "│  [ ] OFF   │",
        "└────────────┘",
        "",
        "Drag: idle  |  Hit-test: active"));
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(2), header),
        (LayoutConstraint.Fill(), targets)
    ]);
}}

internal static class Screen09FileBrowser { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::vertical([Fixed(1), Min(1)]) → Flex::horizontal([Percentage(48), Fill])
    var path = new ParagraphWidget("📁 src/  |  Files: 12  |  Modified: today  |  Sort: name");
    var tree = ShowcaseSurface.Panel("Files", "📁 FrankenTui.Core/\n  📄 Rect.cs\n  📄 Cell.cs\n📁 FrankenTui.Render/\n  📄 Buffer.cs\n  📄 Frame.cs\n📁 FrankenTui.Widgets/\n  📄 Block.cs\n  📄 Paragraph.cs\n  📄 Table.cs\n📁 FrankenTui.Runtime/\n  📄 Program.cs\n📄 FrankenTui.sln");
    var preview = ShowcaseSurface.Panel("Preview", "// FrankenTui.sln\n\nMicrosoft Visual Studio Solution File\nFormat Version 12.00\n# Visual Studio Version 17\n\nProject(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\")");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), path),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(48), tree),
            (LayoutConstraint.Fill(), preview)
        ]))
    ]);
}}

internal static class Screen15Markdown { public static IWidget Build(ShowcaseDemoState s) {
    // view(): renders multiple panels with markdown content
    return ShowcaseSurface.Panel("Markdown", string.Join("\n",
        "# Markdown Rendering",
        "## Headers",
        "### H3: Smaller header",
        "",
        "**Bold text** and *italic text* and `code`.",
        "> Blockquote: This is a quoted section.",
        "",
        "---",
        "- Unordered list item 1",
        "- Unordered list item 2",
        "",
        "1. Ordered item one",
        "2. Ordered item two",
        "",
        "| Col A | Col B |",
        "|-------|-------|",
        "|  a1   |  b1   |",
        "|  a2   |  b2   |"));
}}

internal static class Screen10Advanced { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::horizontal with error traceback + timers/spinners/state
    var trace = ShowcaseSurface.Panel("Error Traceback", string.Join("\n",
        "\"src/main.rs\", line 42, in main",
        "   40 │ fn main() -> Result<()> {",
        "   41 │     let config = Config::load()?;",
        " ❱ 42 │     app::run(config)?;",
        "   43 │     Ok(())",
        "   44 │ }",
        "  File \"src/app.rs\", line 128, in run",
        "   125 │ pub fn run(config: Config) -> Result<()> {",
        "   126 │     let db = Database::connect(&config.db_url)?;",
        "   127 │     let server = Server::new(db);",
        " ❱ 128 │     server.listen(config.port)?;",
        "   129 │     Ok(())",
        "   130 │ }"));
    var timers = ShowcaseSurface.Panel("Timers", "Compact timer: 4m57.9s (0%)\nClock timer:   01:59\n\nSpinners:\ndots        ⠧\nline        \\\nbraille     ⠧\nProgress: ░░░░░░░░░");
    var state = ShowcaseSurface.Panel("State", "State: Idle\nMacro: none\nEvents: 0\nDuration: -\nSpeed: 1.00x\nLoop: Off");
    var info = ShowcaseSurface.Panel(null, "FrankenTUI Demo Showcase\nFramework: ftui (Rust)");
    return new StackWidget(LayoutDirection.Horizontal, [
        (LayoutConstraint.Percentage(70), trace),
        (LayoutConstraint.Percentage(30), new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(10), timers),
            (LayoutConstraint.Fixed(8), state),
            (LayoutConstraint.Fill(), info)
        ]))
    ]);
}}


internal static class Screen19Responsive { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::vertical([Fixed(1), Min(1)]) with breakpoint detection
    var bp = new ParagraphWidget("Breakpoint: SM (60-89) | Width: 80 | Thresholds: sm≥60 md≥90 lg≥120 xl≥160");
    var sidebar = ShowcaseSurface.Panel("Sidebar", "Breakpoint: SM (60-89)\nLayout: stacked\n\nSidebar: hidden (md+)\nAside:   hidden (lg+)\n\n[b] Toggle BPs\n[Current: default]");
    var content = ShowcaseSurface.Panel("Content", "Columns: 1 | Breakpoint: SM (60-89) | 80×24\n\nPadding: 2 | Style: normal\n\nThe layout adapts to the terminal width.\nResize your terminal to see the layout switch\nbetween 1-column, 2-column, and 3-column modes.");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), bp),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Fixed(30), sidebar),
            (LayoutConstraint.Fill(), content)
        ]))
    ]);
}}

internal static class Screen24LayoutInspect { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::horizontal with inspector + layout + tree panels
    var inspector = ShowcaseSurface.Panel("Inspector", string.Join("\n",
        "Scenario: Flex Trio",
        "Details: Vertical flex: Fixed + Min + Max",
        "Step: Constraints - Inspect min/max bounds",
        "",
        "Overlay: on   Tree: on",
        "Keys: n/p scenario  [/ ] step  o overlay",
        "",
        "FlexRoot  req 44x15  got 44x15  min 0x0",
        "  Fixed  req 44x3  got 44x3  min 0x3  max ∞x3",
        "  Min  req 44x4  got 44x7  min 0x4  max ∞x∞",
        "  Max  req 44x6  got 44x3  min 0x0  max ∞x6"));
    var layout = ShowcaseSurface.Panel("Layout", ".Fixed 44x3 [0..∞ x 3..3]\n\n.Min 44x4→44x7 [0..∞ x 4..∞]\n\n.Max 44x6→44x3 [0..∞ x 0..6]");
    var tree = ShowcaseSurface.Panel("Tree", "FlexRoot req=44x15 got=44x15 min=0x0\n  Fixed req=44x3 got=44x3 min=0x3 max=∞x3\n  Min req=44x4 got=44x7 min=0x4 max=∞x∞\n  Max req=44x6 got=44x3 min=0x0 max=∞x6");
    return new StackWidget(LayoutDirection.Horizontal, [
        (LayoutConstraint.Fixed(40), inspector),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(20), layout),
            (LayoutConstraint.Fill(), tree)
        ]))
    ]);
}}

internal static class Screen23Intrinsic { public static IWidget Build(ShowcaseDemoState s) {
    // view(): Flex::horizontal with menu + content + chart
    var title = new ParagraphWidget(" Intrinsic Sizing Demo • Adaptive Sidebar (1/4)");
    var menu = ShowcaseSurface.Panel("Menu", "📊 Dashboard\n⚙️ Settings\n❓ Help\n🔔 Notifications\n👤 Profile");
    var content = ShowcaseSurface.Panel("Content", "Current width: 72\nSidebar mode: Full sidebar mode (width >= 60)\n\nThe sidebar adapts to terminal width:\n• Narrow: icon-only (4 cols)\n• Wide: full labels (20 cols)\n\nTry resizing your terminal!");
    var chart = ShowcaseSurface.Panel(null, "21px\n\n┃\n┃\n┃\n┃\n──\n");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), title),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Fixed(22), menu),
            (LayoutConstraint.Fill(), content),
            (LayoutConstraint.Fixed(8), chart)
        ]))
    ]);
}}

internal static class Screen08DataViz { public static IWidget Build(ShowcaseDemoState s) {
    // 3d_data.rs: chart rendering with blocks
    return ShowcaseSurface.Panel("Data Viz", string.Join("\n",
        "Chart: CPU Usage",
        "100% ┤     ▄▄▄▄",
        " 80% ┤   ▄▄████▄▄    ▄▄▄▄",
        " 60% ┤  ▄█████████▄ ▄████▄",
        " 40% ┤ ▄█████████████████▄",
        " 20% ┤ ▄███████████████████▄",
        "  0% └────────────────────────────",
        "",
        "Chart: Memory",
        "100% ┤ ████████████████████",
        " 80% ┤ ████████████████████",
        " 60% ┤ ██████████▌",
        " 40% ┤ ██████████▌",
        " 20% ┤ █████▌",
        "  0% └────────────────────────────"));
}}
