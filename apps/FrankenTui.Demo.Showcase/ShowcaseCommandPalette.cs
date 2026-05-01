using FrankenTui.Extras;

namespace FrankenTui.Demo.Showcase;

internal static class ShowcaseCommandPalette
{
    public static IReadOnlyList<CommandPaletteEntry> Entries() =>
        ShowcaseCatalog.Screens
            .Select(static screen => new CommandPaletteEntry(
                $"screen:{screen.Number:D2}",
                $"{screen.Number:00} {screen.Title}",
                screen.Blurb,
                MapCategory(screen.Category),
                [
                    screen.Number.ToString("D2"),
                    screen.Number.ToString(),
                    screen.Id,
                    screen.Slug,
                    screen.ShortLabel,
                    screen.Category.ToString()
                ],
                screen.Number <= 10 ? (screen.Number == 10 ? "0" : screen.Number.ToString()) : null,
                screen.Number,
                screen.Slug,
                screen.Category.ToString()))
            .ToArray();

    public static IReadOnlyList<CommandPaletteEntry> EvidenceLabEntries() =>
    [
        new("cmd:open", "Open File", "Open a file from disk", CommandPaletteCategory.Actions, ["file", "open"]),
        new("cmd:save", "Save File", "Save current buffer", CommandPaletteCategory.Actions, ["file", "save"]),
        new("cmd:find", "Find in Files", "Search across project", CommandPaletteCategory.Help, ["search", "grep", "rg"]),
        new("cmd:palette", "Open Command Palette", "Quick actions and navigation", CommandPaletteCategory.Navigation, ["palette", "command", "search"]),
        new("cmd:markdown", "Go to Markdown", "Switch to Markdown screen", CommandPaletteCategory.Navigation, ["markdown", "docs"]),
        new("cmd:logs", "Go to Log Search", "Filter live logs", CommandPaletteCategory.Navigation, ["logs", "search"]),
        new("cmd:perf", "Toggle Performance HUD", "Show render budget overlay", CommandPaletteCategory.Settings, ["perf", "hud"]),
        new("cmd:inline", "Inline Mode", "Switch to inline mode story", CommandPaletteCategory.Settings, ["inline", "scrollback"]),
        new("cmd:theme", "Cycle Theme", "Rotate theme palette", CommandPaletteCategory.Settings, ["theme", "colors"]),
        new("cmd:help", "Show Help", "Display keybinding overlay", CommandPaletteCategory.Actions, ["help", "keys"]),
        new("cmd:quit", "Quit", "Exit the application", CommandPaletteCategory.Actions, ["exit"]),
        new("cmd:reload", "Reload Workspace", "Refresh indexes and caches", CommandPaletteCategory.Debug, ["reload", "refresh"])
    ];

    public static bool TryResolveScreen(CommandPaletteExecution execution, out int screenNumber)
    {
        screenNumber = 0;
        const string prefix = "screen:";
        if (!execution.CommandId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(execution.CommandId[prefix.Length..], out screenNumber) &&
            screenNumber >= 1 &&
            screenNumber <= ShowcaseCatalog.Screens.Count;
    }

    private static CommandPaletteCategory MapCategory(ShowcaseScreenCategory category) =>
        category switch
        {
            ShowcaseScreenCategory.Tour => CommandPaletteCategory.Navigation,
            ShowcaseScreenCategory.Core => CommandPaletteCategory.Actions,
            ShowcaseScreenCategory.Visuals => CommandPaletteCategory.Settings,
            ShowcaseScreenCategory.Interaction => CommandPaletteCategory.Actions,
            ShowcaseScreenCategory.Text => CommandPaletteCategory.Help,
            ShowcaseScreenCategory.Systems => CommandPaletteCategory.Debug,
            _ => CommandPaletteCategory.Actions
        };
}
