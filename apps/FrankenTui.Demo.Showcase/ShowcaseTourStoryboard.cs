using System.Globalization;
using FrankenTui.Core;

namespace FrankenTui.Demo.Showcase;

internal sealed record ShowcaseTourCallout(
    int ScreenNumber,
    string StepId,
    string Title,
    string Body,
    string Hint,
    string Highlight);

internal static class ShowcaseTourStoryboard
{
    private static readonly IReadOnlyList<ShowcaseTourCallout> Steps =
    [
        Step(2, "dashboard:overview", "Dashboard", "This is the home screen. Every tile is meant to be clicked.", "Click a tile or press Enter to jump in.", "0.03,0.12,0.94,0.72"),
        Step(2, "dashboard:palette", "Dashboard", "Navigation is instant: everything is searchable and tagged.", "Press Ctrl+K to open the command palette.", "0.02,0.00,0.96,0.14"),
        Step(16, "mermaid:mermaid", "Mermaid Showcase", "Mermaid diagrams render deterministically with layout metrics and live controls.", "Press m for metrics, t for tier, and j/k to change samples.", "0.40,0.18,0.58,0.72"),
        Step(36, "inline:scrollback", "Inline Mode", "Inline mode keeps terminal scrollback while the UI stays pinned.", "Scroll up: the UI should not steal history.", "0.00,0.76,1.00,0.24"),
        Step(36, "inline:mouse_policy", "Inline Mode", "Mouse capture is explicit. Inline mode stays scrollback-first by default.", "Toggle mouse and watch what changes.", "0.02,0.76,0.96,0.22"),
        Step(40, "determinism:checksums", "Determinism Lab", "Determinism is proven with checksums and repeatable output.", "Run a check twice; the checksum should match.", "0.04,0.20,0.92,0.62"),
        Step(40, "determinism:shortcuts", "Determinism Lab", "The demo is built for shortcuts and evidence instead of hidden state.", "Try a shortcut and watch evidence update.", "0.00,0.00,1.00,0.20"),
        Step(31, "snapshot:replay", "Time-Travel Studio", "Replay frames, inspect diffs, and keep terminal UI behavior deterministic.", "Use j/k or arrows to scrub the timeline.", "0.04,0.72,0.92,0.22"),
        Step(31, "snapshot:diff", "Time-Travel Studio", "Diff mode shows what changed between frames.", "Toggle diff view and inspect render deltas.", "0.04,0.08,0.92,0.62"),
        Step(41, "hyperlink:hover_click", "Hyperlink Playground", "OSC-8 hyperlinks and hit regions make terminal-native interactivity visible.", "Hover a link, then click it.", "0.06,0.18,0.88,0.64"),
        Step(24, "layout:hit_testing", "Layout Inspector", "Hit testing is first-class: inspect the region under interaction.", "Open the inspector overlay and click around.", "0.00,0.00,1.00,1.00"),
        Step(33, "explainability:evidence", "Explainability Cockpit", "Evidence-led debugging brings diffs, resizes, budgets, and checksums together.", "Toggle a knob and inspect recorded evidence.", "0.04,0.18,0.92,0.66"),
        Step(32, "performance:budgets", "Performance Challenge", "Frame budgets are enforced; expensive frames degrade intentionally.", "Cycle tiers and watch what drops first.", "0.62,0.00,0.38,0.30"),
        Step(32, "performance:stress", "Performance Challenge", "Stress the system and verify recovery without flicker or cursor corruption.", "Use stress controls, then reset.", "0.04,0.24,0.56,0.68"),
        Step(18, "vfx:vfx", "Visual Effects", "Terminal visuals can be deterministic and fast.", "Switch effects and watch the perf HUD.", "0.04,0.14,0.92,0.74"),
        Step(18, "vfx:determinism", "Visual Effects", "Even visual effects should be deterministic under fixed seeds and ticks.", "Reseed deterministically and compare hashes.", "0.62,0.00,0.38,0.26")
    ];

    public static IReadOnlyList<ShowcaseTourCallout> All => Steps;

    public static int Count => Steps.Count;

    public static ShowcaseTourCallout ForScreen(ShowcaseScreen screen)
    {
        return Steps.FirstOrDefault(step => step.ScreenNumber == screen.Number) ??
            Step(screen.Number, $"{screen.Slug}:local", screen.Title, screen.Blurb, "Use n/p to step the tour or Space to pause.", "0.04,0.16,0.92,0.68");
    }

    public static ShowcaseTourCallout At(int index) =>
        Steps[Math.Clamp(index, 0, Steps.Count - 1)];

    public static int FirstIndexForScreen(int screenNumber)
    {
        for (var index = 0; index < Steps.Count; index++)
        {
            if (Steps[index].ScreenNumber == screenNumber)
            {
                return index;
            }
        }

        return 0;
    }

    public static Rect ResolveHighlight(ShowcaseTourCallout callout, Size viewport)
    {
        var contentHeight = viewport.Height > 3 ? (ushort)(viewport.Height - 3) : (ushort)1;
        var content = new Rect(0, 2, Math.Max(viewport.Width, (ushort)1), contentHeight);
        var parts = callout.Highlight.Split(',');
        if (parts.Length != 4 ||
            !TryParse(parts[0], out var xPct) ||
            !TryParse(parts[1], out var yPct) ||
            !TryParse(parts[2], out var wPct) ||
            !TryParse(parts[3], out var hPct))
        {
            return content;
        }

        var width = ClampToUShort((int)Math.Round(content.Width * wPct), 1, content.Width);
        var height = ClampToUShort((int)Math.Round(content.Height * hPct), 1, content.Height);
        var x = (ushort)(content.X + Math.Min(
            Math.Max((int)Math.Round(content.Width * xPct), 0),
            Math.Max(content.Width - width, 0)));
        var y = (ushort)(content.Y + Math.Min(
            Math.Max((int)Math.Round(content.Height * yPct), 0),
            Math.Max(content.Height - height, 0)));
        return new Rect(x, y, width, height);
    }

    public static string FormatRect(Rect rect) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{rect.X},{rect.Y},{rect.Width},{rect.Height}");

    private static ShowcaseTourCallout Step(
        int screenNumber,
        string stepId,
        string title,
        string body,
        string hint,
        string highlight) =>
        new(screenNumber, stepId, title, body, hint, highlight);

    private static bool TryParse(string value, out double parsed) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);

    private static ushort ClampToUShort(int value, int min, int max) =>
        (ushort)Math.Clamp(value, min, Math.Max(min, max));
}
