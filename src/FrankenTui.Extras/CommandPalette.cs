using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum CommandPaletteCategory
{
    Navigation,
    Settings,
    Actions,
    Help,
    Debug
}

public sealed record CommandPaletteEntry(
    string Id,
    string Title,
    string Description,
    CommandPaletteCategory Category,
    IReadOnlyList<string> Tags,
    string? Keybinding = null);

public sealed record CommandPaletteSearchResult(
    CommandPaletteEntry Entry,
    double Score,
    IReadOnlyList<int> MatchPositions);

public static class CommandPaletteRegistry
{
    public static IReadOnlyList<CommandPaletteEntry> DefaultEntries() =>
    [
        new("goto-dashboard", "Go to Dashboard", "Open the hosted parity overview surface.", CommandPaletteCategory.Navigation, ["overview", "dashboard"], "g d"),
        new("toggle-tree", "Toggle Tree View", "Show or hide the integration plan tree.", CommandPaletteCategory.Actions, ["overlay", "tree"], "Esc Esc"),
        new("run-doctor", "Run Doctor", "Refresh runtime capture and evidence artifacts.", CommandPaletteCategory.Debug, ["doctor", "evidence", "replay"], "Ctrl+D"),
        new("open-log-search", "Open Log Search", "Filter live log lines with context merging.", CommandPaletteCategory.Help, ["log", "search", "filter"], "/"),
        new("show-perf-hud", "Show Performance HUD", "Display render budget and present health.", CommandPaletteCategory.Debug, ["hud", "budget", "performance"], "Ctrl+P"),
        new("record-macro", "Record Macro", "Capture the current session input stream.", CommandPaletteCategory.Actions, ["macro", "record", "scenario"], "r"),
        new("switch-language", "Switch Language", "Toggle demo localization and directionality.", CommandPaletteCategory.Settings, ["language", "rtl", "i18n"], "Alt+L")
    ];
}

public static class CommandPaletteSearch
{
    public static IReadOnlyList<CommandPaletteSearchResult> Search(
        IReadOnlyList<CommandPaletteEntry> entries,
        string query)
    {
        ArgumentNullException.ThrowIfNull(entries);
        query ??= string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            return entries
                .OrderBy(static entry => entry.Category)
                .ThenBy(static entry => entry.Title, StringComparer.OrdinalIgnoreCase)
                .Select(static entry => new CommandPaletteSearchResult(entry, 0, []))
                .ToArray();
        }

        var normalized = query.Trim();
        return entries
            .Select(entry => Score(entry, normalized))
            .Where(static result => result is not null)
            .Select(static result => result!)
            .OrderByDescending(static result => result.Score)
            .ThenBy(static result => result.Entry.Title.Length)
            .ThenBy(static result => result.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CommandPaletteSearchResult? Score(CommandPaletteEntry entry, string query)
    {
        var title = entry.Title;
        if (title.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return new CommandPaletteSearchResult(entry, 1.0, MatchPositions(title, query));
        }

        if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return new CommandPaletteSearchResult(entry, 0.9, MatchPositions(title, query));
        }

        if (title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(word => word.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
        {
            return new CommandPaletteSearchResult(entry, 0.8, MatchPositions(title, query));
        }

        var contiguous = title.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        var tagMatch = entry.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (contiguous >= 0 || tagMatch)
        {
            var baseScore = contiguous >= 0 ? 0.7 : 0.55;
            var score = baseScore + (tagMatch ? 0.15 : 0);
            return new CommandPaletteSearchResult(entry, score, contiguous >= 0 ? MatchPositions(title, query) : []);
        }

        if (TryFuzzyMatch(title, query, out var positions, out var gaps))
        {
            var score = 0.6 - gaps * 0.02;
            return new CommandPaletteSearchResult(entry, score, positions);
        }

        return null;
    }

    private static IReadOnlyList<int> MatchPositions(string title, string query)
    {
        var index = title.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return [];
        }

        return Enumerable.Range(index, query.Length).ToArray();
    }

    private static bool TryFuzzyMatch(string title, string query, out IReadOnlyList<int> positions, out int gaps)
    {
        var matchPositions = new List<int>(query.Length);
        var titleIndex = 0;
        gaps = 0;
        foreach (var rune in query)
        {
            var matched = false;
            while (titleIndex < title.Length)
            {
                if (char.ToLowerInvariant(title[titleIndex]) == char.ToLowerInvariant(rune))
                {
                    if (matchPositions.Count > 0)
                    {
                        gaps += Math.Max(titleIndex - matchPositions[^1] - 1, 0);
                    }

                    matchPositions.Add(titleIndex);
                    titleIndex++;
                    matched = true;
                    break;
                }

                titleIndex++;
            }

            if (!matched)
            {
                positions = [];
                return false;
            }
        }

        positions = matchPositions;
        return true;
    }
}

public sealed class CommandPaletteWidget : IWidget
{
    public string Query { get; init; } = string.Empty;

    public IReadOnlyList<CommandPaletteSearchResult> Results { get; init; } = [];

    public int SelectedIndex { get; init; }

    public bool ShowPreview { get; init; } = true;

    public void Render(RuntimeRenderContext context)
    {
        var columns = ShowPreview
            ? LayoutSolver.Split(context.Bounds, LayoutDirection.Horizontal, [LayoutConstraint.Percentage(58), LayoutConstraint.Fill()])
            : [context.Bounds];
        RenderList(context.WithBounds(columns[0]));

        if (ShowPreview && columns.Count > 1)
        {
            RenderPreview(context.WithBounds(columns[1]));
        }
    }

    private void RenderList(RuntimeRenderContext context)
    {
        new BlockWidget
        {
            Title = "Command Palette",
            Child = new PaddingWidget(
                new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(1), new ParagraphWidget($"> {Query}_")),
                        (LayoutConstraint.Fill(), new ListWidget
                        {
                            Items = Results.Count == 0
                                ? ["No results"]
                                : Results.Take(Math.Max(context.Bounds.Height - 3, 1))
                                    .Select(result => $"{result.Entry.Title} [{result.Entry.Keybinding ?? "-"}]")
                                    .ToArray(),
                            SelectedIndex = Results.Count == 0 ? -1 : Math.Min(SelectedIndex, Results.Count - 1),
                            FocusedIndex = Results.Count == 0 ? -1 : Math.Min(SelectedIndex, Results.Count - 1)
                        })
                    ]),
                Sides.All(1))
        }.Render(context);
    }

    private void RenderPreview(RuntimeRenderContext context)
    {
        var selected = Results.Count == 0 ? null : Results[Math.Min(SelectedIndex, Results.Count - 1)];
        var body = selected is null
            ? "Type to search..."
            : $"{selected.Entry.Title}\n{selected.Entry.Description}\nCategory: {selected.Entry.Category}\nKey: {selected.Entry.Keybinding ?? "n/a"}";

        new PanelWidget
        {
            Title = "Preview",
            Child = new ParagraphWidget(body)
        }.Render(context);
    }
}
