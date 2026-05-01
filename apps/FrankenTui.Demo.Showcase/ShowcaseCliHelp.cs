namespace FrankenTui.Demo.Showcase;

public static class ShowcaseCliHelp
{
    public static readonly IReadOnlyList<string> Lines =
    [
        "FrankenTui.Net demo showcase",
        "  --screen N          start on screen N",
        "  --tour              start guided tour",
        "  --screen-mode MODE  alt|inline|inline-auto",
        "  --ui-height N --ui-min-height N --ui-max-height N",
        "  --mouse MODE        on|off|auto",
        "  --no-mouse          disable mouse tracking",
        "  --tick-ms N         interactive tick cadence",
        "  --exit-after-ms N   auto-exit after elapsed milliseconds",
        "  --exit-after-ticks N auto-exit after N ticks",
        "  --deterministic --seed N",
        "  --pane-workspace PATH",
        "  --evidence-jsonl PATH",
        "  --vfx-harness --vfx-effect NAME --vfx-size COLSxROWS",
        "  --vfx-cols N --vfx-rows N --vfx-seed N",
        "  --vfx-tick-ms N --vfx-frames N --vfx-jsonl PATH",
        "  --vfx-run-id ID --vfx-exit-after-ms N",
        "  --vfx-perf --vfx-golden PATH --vfx-update-golden",
        "  --mermaid-harness --mermaid-tick-ms N --mermaid-jsonl PATH",
        "  --mermaid-cols N --mermaid-rows N --mermaid-seed N",
        "  --mermaid-run-id ID",
        "  --frames N          scripted render frames (omit for interactive)",
        "  --width N --height N",
        "",
        "Host guidance:",
        "  Windows uses the managed Windows console backend; Unix-native TTY paths are not selected on Windows.",
        "  This mirrors upstream crossterm-compat fallback guidance for Windows native-backend users."
    ];

    public static string Text => string.Join(Environment.NewLine, Lines);
}
