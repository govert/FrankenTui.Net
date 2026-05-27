using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Widget that renders raw text without borders or wrapping, clipped to bounds.</summary>
internal sealed class RawTextBlock(string text) : IWidget, IMeasurableWidget
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

    public Size Measure(Size available)
    {
        var lines = text.Split('\n');
        return new Size(
            (ushort)(lines.Length > 0 ? Math.Min(lines.Max(l => l.Length), available.Width) : 0),
            (ushort)Math.Min(lines.Length, available.Height));
    }

    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        var lines = text.Split('\n');
        if (direction == LayoutDirection.Vertical)
            return SizeHint.Fixed((ushort)lines.Length);
        else
            return SizeHint.Fixed((ushort)(lines.Length > 0 ? lines.Max(l => l.Length) : 0));
    }
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
        var severities = new[] { " INFO", "DEBUG", " WARN", "ERROR", "TRACE" };
        var modules = new[] { "server::http", "db::pool", "auth::jwt", "cache::redis", "queue::worker",
                              "api::handler", "core::runtime" };
        var totalItems = 10000;

        var logLines = new List<string>();
        for (var i = 0; i < 40; i++)
            logLines.Add($"[{i,5}] {severities[i % 5]} {modules[i % 7],-18} Event #{i:D5}: simulated log entry with payload data");

        var listPanel = ShowcaseSurface.PanelRaw($"Virtualized List ({totalItems} items)", string.Join("\n", logLines));

        var statsLines = string.Join("\n",
            $"Total items:  {totalItems}",
            $"Selected:     1 / {totalItems}",
            $"Scroll:       0",
            $"Viewport:     40 rows",
            $"Visible:      0..40",
            $"Progress:     0.0%",
            $"Tick:         0",
            "",
            "Only visible rows are rendered.",
            $"Rendering 40 of {totalItems} items.",
            new string('░', 29));
        var statsPanel = ShowcaseSurface.PanelRaw("Performance Stats", statsLines);

        var status = $"Item 1/{totalItems} | j/k: scroll | Ctrl+D/U: page | g/G: jump";

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.FitContent, new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.FitContent, new PanelWidget
                        {
                            Title = $"Virtualized List ({totalItems} items)",
                            Child = new RawTextBlock(string.Join("\n", logLines))
                        }),
                        (LayoutConstraint.Fixed(35), statsPanel)
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget(status))
            ]);
    }
}

/// <summary>Screen 21: Notifications. Ported from notifications.rs.</summary>
