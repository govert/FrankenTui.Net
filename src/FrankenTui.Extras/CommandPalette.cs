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

public sealed record CommandPaletteState(
    bool IsOpen = false,
    string Query = "",
    int SelectedIndex = 0,
    bool PreviewFocused = false,
    string Status = "Type to search...",
    string? LastExecutedCommandId = null)
{
    public static CommandPaletteState Closed { get; } = new();
}

public sealed record CommandPaletteExecution(
    string CommandId,
    string Status);

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
            var score = baseScore
                        + (tagMatch ? 0.15 : 0)
                        + PositionBonus(contiguous, title.Length)
                        + WordBoundaryBonus(title, contiguous);
            return new CommandPaletteSearchResult(entry, score, contiguous >= 0 ? MatchPositions(title, query) : []);
        }

        if (TryFuzzyMatch(title, query, out var positions, out var gaps))
        {
            var score = 0.6 - gaps * 0.02 + PositionBonus(positions[0], title.Length);
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

    private static double PositionBonus(int contiguousIndex, int titleLength)
    {
        if (contiguousIndex < 0 || titleLength <= 0)
        {
            return 0;
        }

        return Math.Max(0.05 - contiguousIndex / (double)Math.Max(titleLength, 1) * 0.05, 0);
    }

    private static double WordBoundaryBonus(string title, int contiguousIndex)
    {
        if (contiguousIndex <= 0 || contiguousIndex > title.Length - 1)
        {
            return contiguousIndex == 0 ? 0.1 : 0;
        }

        return char.IsWhiteSpace(title[contiguousIndex - 1]) ? 0.1 : 0;
    }
}

public static class CommandPaletteController
{
    public static IReadOnlyList<CommandPaletteSearchResult> Results(CommandPaletteState state, IReadOnlyList<CommandPaletteEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(entries);

        return CommandPaletteSearch.Search(entries, state.Query);
    }

    public static CommandPaletteState Toggle(CommandPaletteState state) =>
        state.IsOpen
            ? CommandPaletteState.Closed with { LastExecutedCommandId = state.LastExecutedCommandId }
            : state with
            {
                IsOpen = true,
                Query = string.Empty,
                SelectedIndex = 0,
                PreviewFocused = false,
                Status = "Type to search..."
            };

    public static CommandPaletteState Close(CommandPaletteState state, string status = "Palette closed.") =>
        state with
        {
            IsOpen = false,
            Query = string.Empty,
            SelectedIndex = 0,
            PreviewFocused = false,
            Status = status
        };

    public static (CommandPaletteState State, CommandPaletteExecution? Execution) Apply(
        CommandPaletteState state,
        KeyTerminalEvent keyEvent,
        IReadOnlyList<CommandPaletteEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(keyEvent);
        ArgumentNullException.ThrowIfNull(entries);

        var results = Results(state, entries);
        var selectedIndex = results.Count == 0
            ? -1
            : Math.Clamp(state.SelectedIndex, 0, results.Count - 1);
        var gesture = keyEvent.Gesture;
        if (gesture.Key == TerminalKey.Escape && gesture.Modifiers == TerminalModifiers.None)
        {
            if (!string.IsNullOrEmpty(state.Query))
            {
                return (state with { Query = string.Empty, SelectedIndex = 0, Status = "Query cleared." }, null);
            }

            return (Close(state), null);
        }

        if (gesture.Key == TerminalKey.Enter && gesture.Modifiers == TerminalModifiers.None)
        {
            if (selectedIndex < 0 || selectedIndex >= results.Count)
            {
                return (state with { Status = "No command selected." }, null);
            }

            var selected = results[selectedIndex].Entry;
            return (
                Close(state with
                {
                    LastExecutedCommandId = selected.Id,
                    Status = $"Executed {selected.Title}."
                }, $"Executed {selected.Title}."),
                new CommandPaletteExecution(selected.Id, $"Executed {selected.Title}."));
        }

        if (gesture.Key == TerminalKey.Tab && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { PreviewFocused = !state.PreviewFocused }, null);
        }

        if (gesture.Key == TerminalKey.Up && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { SelectedIndex = Move(selectedIndex, results.Count, -1) }, null);
        }

        if (gesture.Key == TerminalKey.Down && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { SelectedIndex = Move(selectedIndex, results.Count, 1) }, null);
        }

        if (gesture.Key == TerminalKey.PageUp && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { SelectedIndex = Move(selectedIndex, results.Count, -10) }, null);
        }

        if (gesture.Key == TerminalKey.PageDown && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { SelectedIndex = Move(selectedIndex, results.Count, 10) }, null);
        }

        if (gesture.Key == TerminalKey.Home && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { SelectedIndex = 0 }, null);
        }

        if (gesture.Key == TerminalKey.End && gesture.Modifiers == TerminalModifiers.None)
        {
            return (state with { SelectedIndex = Math.Max(results.Count - 1, 0) }, null);
        }

        if (gesture.Key == TerminalKey.Backspace && gesture.Modifiers == TerminalModifiers.None)
        {
            var query = state.Query.Length == 0 ? string.Empty : state.Query[..^1];
            return (state with { Query = query, SelectedIndex = 0 }, null);
        }

        if (gesture.IsCharacter && gesture.Character is { } rune && gesture.Modifiers == TerminalModifiers.None)
        {
            var query = state.Query;
            if (rune.Value == '\u0001')
            {
                return (state with { SelectedIndex = 0 }, null);
            }

            query += rune.ToString();
            return (state with { Query = query, SelectedIndex = 0 }, null);
        }

        return (state, null);
    }

    private static int Move(int index, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        return Math.Clamp((index < 0 ? 0 : index) + delta, 0, count - 1);
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
