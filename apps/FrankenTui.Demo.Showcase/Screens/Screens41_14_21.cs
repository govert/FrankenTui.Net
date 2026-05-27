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
        // Full port of performance.rs — side-by-side layout with dynamic viewport
        var severities = new[] { " INFO", "DEBUG", " WARN", "ERROR", "TRACE" };
        var modules = new[] { "server::http", "db::pool", "auth::jwt", "cache::redis", "queue::worker",
                              "api::handler", "core::runtime" };
        var totalItems = 10000;
        var selected = 1;
        var scrollOffset = 0;

        // Use observable widget to measure available height
        return new PerformancePanel(
            totalItems, selected, scrollOffset, severities, modules);
    }

    /// <summary>Observes its render area to compute dynamic viewport height.</summary>
    private sealed class PerformancePanel(
        int totalItems, int selected, int scrollOffset,
        string[] severities, string[] modules) : IWidget
    {
        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Layout: Fill(list+stats) + Fixed(1) status bar
            var mainHeight = area.Height - 1;
            if (mainHeight <= 0) return;

            var listWidth = area.Width - 35;
            if (listWidth <= 0) return;

            var viewportHeight = Math.Max(1, mainHeight - 2); // minus panel borders

            var visibleEnd = Math.Min(scrollOffset + viewportHeight, totalItems);

            // Render list panel
            var listArea = new Rect(area.X, area.Y, (ushort)listWidth, (ushort)mainHeight);
            var listBlock = new PanelWidget { Title = $"Virtualized List ({totalItems} items)", Child = new RawTextBlock(
                string.Join("\n", Enumerable.Range(0, viewportHeight).Select(i =>
                    $"[{i,5}] {severities[i % 5]} {modules[i % 7],-18} Event #{i:D5}: simulated log entry with payload data"))) };
            listBlock.Render(new RuntimeRenderContext(context.Buffer, listArea, context.Theme));

            // Render stats panel
            var statsArea = new Rect((ushort)(area.X + listWidth), area.Y, 35, (ushort)mainHeight);
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
                new string('░', 29));
            var statsBlock = new PanelWidget { Title = "Performance Stats", Child = new RawTextBlock(statsLines) };
            statsBlock.Render(new RuntimeRenderContext(context.Buffer, statsArea, context.Theme));

            // Render screen status bar
            var statusY = (ushort)(area.Y + mainHeight);
            var statusArea = new Rect(area.X, statusY, area.Width, 1);
            var statusText = $"Item {selected}/{totalItems} | j/k: scroll | Ctrl+D/U: page | g/G: jump";
            BufferPainter.WriteText(context.Buffer, area.X, statusY, statusText, context.Theme.Default.ToCell());
        }

        public Size Measure(Size available) => available;
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
