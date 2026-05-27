using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Faithful ports of Rust screen view() methods with exact Flex constraints

/// <summary>Screen 22: Action Timeline. Ported from action_timeline.rs.</summary>
internal static class Screen22ActionTimeline
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // view(): Flex::vertical([Fixed(3), Min(1)]) → Flex::horizontal([Min(45), Min(30)])
        var filters = ShowcaseSurface.Panel("Filters + Follow",
            "Follow[F]: ON  Component[C]: all  Severity[S]: all  Type[T]: all  Clear[X]");

        var timeline = ShowcaseSurface.Panel("Event Timeline", string.Join("\n",
            "   4 ERROR render  cmd     Command dispatched to model",
            "   3 WARN  render  model   State update queued",
            "   2 INFO  render  effect  Side-effect emitted",
            "   1 DEBUG render  diff    Frame diff computed",
            "   0 INFO  runtime start   Runtime initialized",
            "",
            "Active filter: all  |  Selected event: 0",
            "Focus: 0 focus",
            "Max events: 500  |  Burst: every 2 ticks  |  Initial events: 12"));

        var details = ShowcaseSurface.Panel("Event Detail", string.Join("\n",
            "ID: 25",
            "Tick: 4",
            "Severity: ERROR",
            "Component: render",
            "Type: cmd",
            "",
            "Action: Command dispatched to model"));

        return new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(3), filters),
            (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Horizontal, [
                (LayoutConstraint.Minimum(45), timeline),
                (LayoutConstraint.Minimum(30), details)
            ]))
        ]);
    }
}

/// <summary>Screen 27: Form Validation. Ported from form_validation.rs.</summary>


internal static class Screen20LogSearch
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // view(): Flex::vertical([Fixed(4), Min(1)])
        var search = ShowcaseSurface.Panel("Search", "Search: [type to filter]  |  Regex: OFF  |  Case: sensitive");

        var controls = ShowcaseSurface.Panel("Controls",
            "n/N next/prev  |  f toggle follow  |  r toggle regex  |  c toggle case  |  x clear");

        var logLines = new List<string>();
        var levels = new[] { " INFO", "DEBUG", " WARN", "ERROR", "TRACE" };
        var mods = new[] { "server::http", "db::pool", "auth::jwt", "cache::redis", "queue::worker" };
        for (var i = 1; i <= 10; i++)
            logLines.Add($"[{i:D4}] {levels[i % 5]} {mods[i % 5],-18} Event #{i:D5}: log entry with payload data");

        var logs = ShowcaseSurface.Panel("Log Entries", string.Join("\n", logLines));

        return new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(2), search),
            (LayoutConstraint.Fixed(2), controls),
            (LayoutConstraint.Fill(), logs)
        ]);
    }
}

/// <summary>Screen 29: Async Tasks. Ported from async_tasks.rs.</summary>
internal static class Screen29AsyncTasks
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // view(): Flex::vertical([Min(1), Fixed(1)])
        var jobs = ShowcaseSurface.Panel("Job Queue", string.Join("\n",
            "● Task 1: Port widgets      [████████░░] 80% ACTIVE",
            "● Task 2: Add test suite    [██████░░░░] 60% ACTIVE",
            "● Task 3: Fix layout bug    [██████████] 100% DONE",
            "○ Task 4: Deploy to staging [░░░░░░░░░░] 0% PENDING",
            "○ Task 5: Run benchmarks    [░░░░░░░░░░] 0% PENDING"));

        var status = ShowcaseSurface.Panel(null,
            "Workers: 4 active  |  Queue: 2 pending  |  Throughput: 142/min  |  Avg latency: 34ms");

        return new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fill(), jobs),
            (LayoutConstraint.Fixed(2), status)
        ]);
    }
}

/// <summary>Screen 28: Virtualized Search. Ported from virtualized_search.rs.</summary>
internal static class Screen28Virtualized
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // view(): Flex::vertical([Fixed(2), Min(1)])
        var search = ShowcaseSurface.Panel("Search", "Search: [type to filter]  |  Showing 0-19 of 10,000  |  Scroll: 0");

        var items = new List<string>();
        for (var i = 0; i < 15; i++)
            items.Add($"[{i:D4}] Item #{i:D5} — Virtualized list entry {i}");

        var list = ShowcaseSurface.Panel("Virtualized List", string.Join("\n", items));

        return new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(2), search),
            (LayoutConstraint.Fill(), list)
        ]);
    }
}

/// <summary>Screen 30: Theme Studio. Ported from theme_studio.rs.</summary>
internal static class Screen30ThemeStudio
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // view(): Flex::horizontal([Min(40), Fixed(35)])
        var presets = ShowcaseSurface.Panel("Theme Presets", string.Join("\n",
            "Current: Cyberpunk Aurora",
            "",
            "  [1] Cyberpunk Aurora",
            "  [2] Solarized Dark",
            "  [3] Dracula",
            "  [4] Nord",
            "  [5] Monokai",
            "  [6] GitHub Dark",
            "  [7] One Dark",
            "",
            "Press 1-7 to switch themes"));

        var palette = ShowcaseSurface.Panel("Palette", string.Join("\n",
            "Background:  #0A0E14",
            "Foreground:  #C8D2DC",
            "Primary:     #5CCFE6",
            "Secondary:   #B8CFE6",
            "Accent:      #FFCC66",
            "Success:     #A6E3A1",
            "Warning:     #F9E2AF",
            "Error:       #F38BA8"));

        return new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Minimum(40), presets),
            (LayoutConstraint.Fixed(35), palette)
        ]);
    }
}
