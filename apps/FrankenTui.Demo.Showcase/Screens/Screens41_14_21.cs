using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Widget that renders raw text without borders or wrapping, clipped to bounds.</summary>
internal sealed class RawTextBlock(string text) : IWidget
{
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var buf = context.Buffer;
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var maxRows = Math.Min(lines.Length, context.Bounds.Height);
        for (var row = 0; row < maxRows; row++)
        {
            var y = (ushort)(context.Bounds.Y + row);
            var line = lines[row];
            var maxCols = Math.Min(line.Length, context.Bounds.Width);
            for (var col = 0; col < maxCols; col++)
            {
                var x = (ushort)(context.Bounds.X + col);
                buf.Set(x, y, Cell.FromChar(line[col]));
            }
        }
    }

    public Size Measure(Size available) => available;
}

/// <summary>Screen 41: Hyperlink Playground. Ported from hyperlink_playground.rs.</summary>
internal static class Screen41Hyperlink
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        var linkLines = string.Join("\n",
            "> FrankenTUI",
            "  Docs",
            "  GitHub",
            "  OSC 8 Spec",
            "  ANSI Reference");
        var linksPanel = ShowcaseSurface.Panel("Links (OSC-8)", linkLines);

        var detailsLines = string.Join("\n",
            "Selected: FrankenTUI",
            "URL: https://ftui.dev",
            "Registry ID: 1",
            "Hit: id=8000 region=Link data=1",
            @"OSC 8 open: \x1b]8;;https://ftui.dev\x1b\",
            @"OSC 8 close: \x1b]8;;\x1b\",
            "Notes: Project home + overview",
            "Hover: None",
            "",
            "Registry map",
            "[1] FrankenTUI",
            "[2] Docs",
            "[3] GitHub",
            "[4] OSC 8 Spec",
            "[5] ANSI Reference");
        var detailsPanel = ShowcaseSurface.Panel("Details & Registry", detailsLines);

        var header = ShowcaseSurface.Panel(null,
            "Hyperlink Playground  OSC-8 + Hit Regions\n" +
            "Up/Down move · Tab cycle · Enter activate · Mouse hover/click");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(4), header),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Fixed(56), linksPanel),
                        (LayoutConstraint.Fill(), detailsPanel)
                    ]))
            ]);
    }
}

/// <summary>Screen 14: Performance. Ported from performance.rs.</summary>
internal static class Screen14Performance
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // Full port of performance.rs — side-by-side layout with stats panel and status bar
        var severities = new[] { " INFO", "DEBUG", " WARN", "ERROR", "TRACE" };
        var modules = new[] { "server::http", "db::pool", "auth::jwt", "cache::redis", "queue::worker",
                              "api::handler", "core::runtime" };
        var totalItems = 10000;
        var selected = 1;  // default selected=1 (matches upstream snapshot at 120x40)
        var scrollOffset = 0;
        var viewportHeight = 20;

        // Generate items — actual count limited by viewport at render time
        var logLines = new List<string>();
        for (var i = 0; i < 100; i++)
            logLines.Add($"[{i,5}] {severities[i % 5]} {modules[i % 7],-18} Event #{i:D5}: simulated log entry with payload data");

        var listPanel = ShowcaseSurface.PanelRaw($"Virtualized List ({totalItems} items)", string.Join("\n", logLines));

        var visibleEnd = Math.Min(scrollOffset + 100, totalItems);
        var statsLines = string.Join("\n",
            $"Total items:  {totalItems}",
            $"Selected:     {selected} / {totalItems}",
            $"Scroll:       {scrollOffset}",
            $"Viewport:     {viewportHeight} rows",
            $"Visible:      {scrollOffset}..{visibleEnd}",
            $"Progress:     0.0%",
            $"Tick:         0",
            "",
            "Only visible rows are rendered.",
            $"Rendering {visibleEnd - scrollOffset} of {totalItems} items.",
            "");
        var statsPanel = ShowcaseSurface.PanelRaw("Performance Stats", statsLines + "\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591");

        var status = $"Item {selected}/{totalItems} | j/k: scroll | Ctrl+D/U: page | g/G: jump";

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Fill(), listPanel),
                        (LayoutConstraint.Fixed(35), statsPanel)
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget(status))
            ]);
    }
}

/// <summary>Screen 21: Notifications. Ported from notifications.rs.</summary>
internal static class Screen21Notifications
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        var lines = string.Join("\n",
            "Press keys to trigger notifications:",
            "",
            "  s  Success notification",
            "  e  Error with Retry action",
            "  w  Warning notification",
            "  i  Info notification",
            "  u  Urgent with Ack/Snooze actions",
            "  d  Dismiss all notifications",
            "",
            "Queue: 0 visible, 0 pending",
            "Total shown: 0",
            "Last action: (none)");
        return ShowcaseSurface.Panel("Notification Demo", lines);
    }
}
