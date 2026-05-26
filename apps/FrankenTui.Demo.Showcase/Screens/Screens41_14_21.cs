using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

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
        var levels = new[] { "INFO", "DEBUG", "WARN", "ERROR", "TRACE" };
        var modules = new[] { "server::http", "db::pool", "auth::jwt", "cache::redis", "queue::worker",
                              "api::handler", "core::runtime", "server::http", "db::pool", "auth::jwt",
                              "cache::redis", "queue::worker", "api::handler", "core::runtime", "server::http",
                              "db::pool", "auth::jwt", "cache::redis", "queue::worker", "api::handler" };

        var logLines = new List<string>();
        for (var i = 0; i < 20; i++)
        {
            var level = levels[i % 5];
            var mod = modules[i];
            var pad = level.Length == 4 ? " " : "";
            logLines.Add($"[{i,5}] {pad}{level} {mod,-15} Event #{i:D5}: simulated log entry with payload");
        }
        var panel = ShowcaseSurface.Panel("Virtualized List (10000 items)", string.Join("\n", logLines));
        return panel;
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
