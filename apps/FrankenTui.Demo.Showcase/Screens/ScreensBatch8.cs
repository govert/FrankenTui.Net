using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 8: screens 13,21,31,32,33,34,35,38,39,40,42,43,44 — proper Flex constraints

internal static class Screen13MacroRecorder { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(6), Min(1)]) → Flex::horizontal([Percentage(60), Percentage(40)])
    var controls = ShowcaseSurface.Panel("Macro Controls", "Status: Idle\nMode: Record\nBuffer: 0 events\n\n  r  Record  p  Play  s  Stop\n  e  Export  l  Load  c  Clear");
    var editor = ShowcaseSurface.Panel("Macro Editor", "No macro recorded yet.\n\nEvents will appear here as key/mouse/tick.\n\nFormat: JSONL");
    var history = ShowcaseSurface.Panel("History", "(empty)\n\nExport path: macros/");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(6), controls),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(60), editor),
            (LayoutConstraint.Percentage(40), history)
        ]))
    ]);
}}

internal static class Screen21Notifications { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::horizontal([Percentage(40), Min(1)])
    var instructions = ShowcaseSurface.Panel("Notification Demo", string.Join("\n",
        "Press keys to trigger notifications:", "",
        "  s  Success", "  e  Error with Retry",
        "  w  Warning", "  i  Info",
        "  u  Urgent with Ack/Snooze",
        "  d  Dismiss all", "",
        "Queue: 0 visible, 0 pending",
        "Total shown: 0", "Last action: (none)"));
    var stack = ShowcaseSurface.Panel("Notification Stack", "No active notifications.\n\nNotifications appear here with\nswipe-to-dismiss and action buttons.");
    return new StackWidget(LayoutDirection.Horizontal, [
        (LayoutConstraint.Percentage(40), instructions),
        (LayoutConstraint.Fill(), stack)
    ]);
}}

internal static class Screen31SnapshotPlayer { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(1), Min(1)]) → render_main_layout
    var title = new ParagraphWidget("Time-Travel Studio  |  Snapshots: 42  |  Frame: 24/42  |  ▶ Playing (1.0x)");
    var timeline = ShowcaseSurface.Panel("Timeline", "[0]──[10]──[20]──▶[24]──[30]──[40]\n\nControls:\n  Space play/pause  ←→ step  Home/End first/last\n  r record  c clear  e export");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), title),
        (LayoutConstraint.Fill(), timeline)
    ]);
}}

internal static class Screen32PerfHud { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(1), Min(8), Fixed(1)]) with metrics + sparklines
    var title = new ParagraphWidget("PERFORMANCE CHALLENGE MODE — DEGRADATION TIERS");
    var metrics = ShowcaseSurface.Panel("Real-Time Metrics", string.Join("\n",
        "  Sim FPS:         9.0 (Minimum viable)", "  Obs FPS:         9.0",
        "  Frame time:      110.99ms avg", "  Tick rate:        9.0 tps",
        "  Diff:             0.992ms (+0.0ms)", "  Render:          1.667ms (+0.0ms)",
        "  Present:          0.672ms", "  Budget Usage:     16.7%",
        "",
        "  avg:    110.99   p50:    110.87   p95:    113.04   p99:    115.47",
        "  min:    109.68   max:    115.47"));
    var sparkline = ShowcaseSurface.Panel("Tick Intervals (µs)", "█  █  █  █  ▅  █ ▆█▇  ▄  █ ███ ▄ █ ▆ ▅  █ ████ ██ █ █ █▇▇ █  ██  █ ████ ██████▅████ █ ███  █▆████ ███████████▅█▅ ███  ██████ ██████████████ ████");
    var status = new ParagraphWidget("0/120 budget:17ms | samples:2/120");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), title),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(60), metrics),
            (LayoutConstraint.Percentage(40), sparkline)
        ])),
        (LayoutConstraint.Fixed(1), status)
    ]);
}}

internal static class Screen33Explainability { public static IWidget Build(ShowcaseDemoState s) {
    // view() → render(frame, area, CockpitMode::Full)
    return ShowcaseSurface.Panel("Explainability Cockpit", string.Join("\n",
        "Diff Decision Evidence  |  Frame: 42  |  Mode: Full",
        "",
        "Strategy: Dirty Rows (3 rows, 15 cells)",
        "Cost: 250µs (vs 500µs full redraw)",
        "",
        "Why dirty rows?",
        "  Posterior mean: 0.87  Risk tolerance: 0.10",
        "  Budget pressure: low (12.6%)",
        "  Hysteresis: not triggered  Cooldown: 0 frames",
        "",
        "Budget: Used 2.1ms/16.7ms (12.6%)",
        "Degradation: None  Recovery: N/A"));
}}

internal static class Screen34I18n { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(3), Fill, Fixed(1)]) with locale bar + panels
    var locale = ShowcaseSurface.Panel(null, "Language: en-US  |  Panel: Overview  |  Flow: LTR  |  Tab: cycle panels  |  1-4: jump");
    var overview = ShowcaseSurface.Panel("Overview", string.Join("\n",
        "English:    Hello, World!",
        "Spanish:    ¡Hola, Mundo!",
        "French:     Bonjour le monde!",
        "German:     Hallo, Welt!",
        "Japanese:   こんにちは世界",
        "Korean:     안녕하세요 세계",
        "Arabic:     مرحبا بالعالم",
        "Hebrew:     שלום עולם",
        "Russian:    Привет, мир!",
        "Chinese:    你好，世界",
        "",
        "RTL:  مرحبا بالعالم — هذا نص تجريبي",
        "Emoji: 🎉🚀💻🎨🔧📊"));
    var status = new ParagraphWidget("Width test: ＡＢＣＤＥ vs ABCDE  |  Emoji width: 🎉=2  |  Grapheme clusters: e\u0301=1");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(3), locale),
        (LayoutConstraint.Fill(), overview),
        (LayoutConstraint.Fixed(1), status)
    ]);
}}

internal static class Screen35VoiOverlay { public static IWidget Build(ShowcaseDemoState s) {
    // view() → decision + posterior + observation + ledger sections
    return ShowcaseSurface.Panel("VOI Overlay", string.Join("\n",
        "Value of Information Analysis  |  focus: Decision  |  Tab:section  v:detail  n/p:ledger",
        "",
        "Decision: Render strategy",
        "  Alternatives: Full redraw (500µs) | Dirty rows (250µs) | Skip (0µs)",
        "  Expected Value: Full=0.85  Dirty=0.92  Skip=0.45",
        "  Selected: Dirty rows  Confidence: 0.92",
        "",
        "Posterior: Beta(α=42, β=8)  Mean: 0.84  Variance: 0.003",
        "  Evidence: 50 observations  |  Prior: Beta(1,1)"));
}}

internal static class Screen38WidgetBuilder { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(2), Fill, Fixed(6)]) → Flex::horizontal([Fixed(28), Fill])
    var header = ShowcaseSurface.Panel(null, "Widget Builder  |  Type: Paragraph  |  Ctrl+S save  |  Ctrl+E export");
    var props = ShowcaseSurface.Panel("Properties", "Type: Paragraph\nText: \"Hello, FrankenTUI!\"\nAlignment: Left\nWrap: Word\nStyle: Default");
    var preview = ShowcaseSurface.Panel("Preview", "Hello, FrankenTUI!\n\nThis is a live preview of the\nwidget with the current properties.\n\nOutput updates in real-time.");
    var footer = ShowcaseSurface.Panel("Diagnostics", "Render time: 0.3ms  |  Cells: 120  |  Graphemes: 18  |  Cache: warm");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(2), header),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Fixed(28), props),
            (LayoutConstraint.Fill(), preview)
        ])),
        (LayoutConstraint.Fixed(6), footer)
    ]);
}}

internal static class Screen39CmdPalette { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(2), Min(6)]) → Flex::horizontal([Percentage(55), Fill])
    var header = ShowcaseSurface.Panel(null, "Match Mode: 0 All  1 Exact  2 Prefix  3 WordStart  4 Substring  5 Fuzzy  |  Type to filter  |  ↑↓ navigate  |  Enter execute");
    var palette = ShowcaseSurface.Panel("Command Palette", ">log\n> [Navigation] Go to Log Search\n> log_search  screen");
    var evidence = ShowcaseSurface.Panel("Evidence", string.Join("\n",
        "Selected: Go to Log Search",
        "Match: Substring on \"log\"",
        "MatchType: BestFirst",
        "Position: 6", "WordBoundary: false",
        "TitleLength: 22", "Score: 0.87"));
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(2), header),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(55), palette),
            (LayoutConstraint.Fill(), evidence)
        ]))
    ]);
}}

internal static class Screen40Determinism { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(2), Fill]) → Flex::horizontal([Percentage(60), Percentage(40)])
    var header = ShowcaseSurface.Panel(null, "Determinism Lab  |  Seed: 42  |  Frames: 1000  |  Hash: SHA-256  |  Deterministic: YES");
    var equivalence = ShowcaseSurface.Panel("Equivalence", "Checksum: 0xDEADBEEF\nFrames verified: 1000/1000\n\nFrame 0: ✓ 0xDEADBEEF\nFrame 1: ✓ 0xCAFEBABE\nFrame 2: ✓ 0x8BADF00D\n...\nFrame 999: ✓ 0xFEEDFACE\n\nAll frames match reference.");
    var trace = ShowcaseSurface.Panel("Trace", "Test: ftui_determinism_checksum_equivalence\nStatus: PASS\nDuration: 1.2s\n\nReproducibility:\n  --seed=42  deterministic\n  --seed=0   random\n\nEvidence: checksums matched");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(2), header),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(60), equivalence),
            (LayoutConstraint.Percentage(40), trace)
        ]))
    ]);
}}

internal static class Screen42Kanban { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::horizontal([Ratio(1,3), Ratio(1,3), Ratio(1,3)])
    var todo = ShowcaseSurface.Panel("To Do", "Task 1: Setup project\nTask 2: Add CI/CD\nTask 3: Write tests\n\nDrag cards between columns.\nClick to select.");
    var inprog = ShowcaseSurface.Panel("In Progress", "Task 4: Port widgets\nTask 5: Add docs\n\nSelected: none");
    var done = ShowcaseSurface.Panel("Done", "Task 6: Release v1.0\n\nKeyboard: Tab columns\nArrows move cards");
    return new StackWidget(LayoutDirection.Horizontal, [
        (LayoutConstraint.Percentage(33), todo),
        (LayoutConstraint.Percentage(33), inprog),
        (LayoutConstraint.Percentage(34), done)
    ]);
}}

internal static class Screen43MarkdownLive { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(3), Min(1)]) → Flex::horizontal([Percentage(50), Percentage(50)])
    var search = ShowcaseSurface.Panel("Search", "Find: []  Replace: []  |  Mode: Edit  |  Sync: ON");
    var editor = ShowcaseSurface.Panel("Editor", "# Hello World\n\nThis is **bold** and *italic*.\n\n- List item 1\n- List item 2\n\n`inline code`\n\n```\ncode block\n```");
    var preview = ShowcaseSurface.Panel("Preview", "Hello World\n\nThis is bold and italic.\n\n• List item 1\n• List item 2\n\ninline code\n\ncode block");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(3), search),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(50), editor),
            (LayoutConstraint.Percentage(50), preview)
        ]))
    ]);
}}

internal static class Screen44DragDrop { public static IWidget Build(ShowcaseDemoState s) {
    // Flex::vertical([Fixed(1), Min(3), Fixed(2)]) with mode tabs + sortable + cross-container
    var tabs = new ParagraphWidget("Drag & Drop Lab  |  [Sortable List]  cross-container  keyboard drag  |  DemoMode: sortable");
    var sortable = ShowcaseSurface.Panel("Sortable List", "> Item 1\n  Item 2\n  Item 3\n  Item 4\n  Item 5\n  Item 6\n  Item 7\n  Item 8\n\nLIST_SIZE=8 deterministic");
    var cross = ShowcaseSurface.Panel("Cross-Container", "Drop here\n\ntarget A\ntarget B\n\nDrag items between lists.");
    var keyboard = ShowcaseSurface.Panel("Keyboard Drag", "Tab: focus\nSpace: pick up\nArrows: move\nEnter: drop");
    var instructions = new ParagraphWidget("Tab / Shift+Tab cycles DemoMode  |  mouse down on layout_tabs selects mode");
    return new StackWidget(LayoutDirection.Vertical, [
        (LayoutConstraint.Fixed(1), tabs),
        (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Fill(), sortable),
            (LayoutConstraint.Fixed(25), cross),
            (LayoutConstraint.Fixed(20), keyboard)
        ])),
        (LayoutConstraint.Fixed(2), instructions)
    ]);
}}
