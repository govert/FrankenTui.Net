using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Screen 19: Responsive Layout. Ported from responsive_demo.rs.</summary>
internal static class Screen19Responsive
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        var breakpoint = "SM (60-89)";
        var lines = string.Join("\n",
            "Breakpoint: SM (60-89)",
            "Layout: stacked",
            "",
            "Sidebar: hidden (md+)",
            "Aside:   hidden (lg+)",
            "",
            "[b] Toggle BPs",
            "[Current: default]");
        var sidebar = ShowcaseSurface.Panel("Sidebar", lines);

        var contentLines = string.Join("\n",
            "Columns: 1",
            "",
            "Padding: 2",
            "Style: normal",
            "",
            "The layout adapts to the terminal width.",
            "Resize your terminal to see the layout switch",
            "between 1-column, 2-column, and 3-column modes.");

        var headerText = $"Breakpoint: {breakpoint} | Width: 80 | Thresholds: sm\u226560 md\u226590 lg\u2265120 xl\u2265160";
        var header = ShowcaseSurface.Panel(null, headerText + "\n" +
            $"Columns: 1 | Breakpoint: {breakpoint} | 80×24");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(2), new ParagraphWidget(headerText)),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Fixed(30), sidebar),
                        (LayoutConstraint.Fill(), ShowcaseSurface.Panel("Content", contentLines))
                    ]))
            ]);
    }
}

/// <summary>Screen 36: Inline Mode Story. Ported from inline_mode_story.rs.</summary>
internal static class Screen36InlineMode
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        var levels = new[] { "ERROR", "DEBUG", "INFO ", "WARN " };
        var modules = new[] { "render", "render", "runtime", "runtime", "runtime",
                              "widgets", "widgets", "widgets", "io", "io",
                              "io", "layout", "layout", "layout", "core",
                              "core", "core", "render", "render" };
        var msgs = new[] { "scrollback ok", "scrollback ok", "scrollback ok", "scrollback ok",
                           "inline anchor", "inline anchor", "inline anchor", "inline anchor",
                           "inline anchor", "inline anchor", "inline anchor", "budget check",
                           "budget check", "budget check", "budget check", "budget check",
                           "budget check", "budget check", "diff pass" };

        var logLines = new List<string>();
        for (var i = 0; i < 19; i++)
            logLines.Add($"[{94 + i:D6}] [{levels[i % 4]}] {modules[i],-7} {msgs[i]}");

        var header = ShowcaseSurface.Panel(null,
            "Mode: Inline  |  Compare: OFF  |  Anchor: Bottom  |  UI height: 2  |  Rate: 2/tick\n" +
            "Status: Live  |  Lines: 114  |  Scrollback preserved in inline mode");

        var log = ShowcaseSurface.Panel("Inline Mode Story", string.Join("\n", logLines));

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), header),
                (LayoutConstraint.Fill(), log)
            ]);
    }
}

/// <summary>Screen 27: Form Validation. Ported from form_validation.rs.</summary>
internal static class Screen27FormValidation
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        var formLines = string.Join("\n",
            "Username:         Enter username (required)",
            "Email:            user@example.com",
            "Password:         Min 8 characters",
            "Confirm Password: Re-enter password",
            "Age:                25",
            "Bio:              Max 100 characters",
            "Website:          https://example.com",
            "Role:               (Select a role)",
            "Accept Terms:     [ ]",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "");
        var form = ShowcaseSurface.Panel("Registration Form", formLines);

        var header = ShowcaseSurface.Panel(null, "Mode: Real-time [M to toggle]");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(1), header),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Fill(), form),
                        (LayoutConstraint.Fixed(10), ShowcaseSurface.Panel(null, "No validation\nerrors yet"))
                    ]))
            ]);
    }
}
