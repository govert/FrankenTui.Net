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
    string? Keybinding = null,
    int? ScreenNumber = null,
    string? ScreenSlug = null,
    string? ScreenCategory = null);

public enum CommandPaletteMatchKind
{
    NoMatch,
    Fuzzy,
    Substring,
    WordStart,
    Prefix,
    Exact
}

public sealed record CommandPaletteEvidenceEntry(
    string Kind,
    double Factor,
    string Description);

public sealed record CommandPaletteSearchResult(
    CommandPaletteEntry Entry,
    double Score,
    IReadOnlyList<int> MatchPositions,
    CommandPaletteMatchKind MatchKind = CommandPaletteMatchKind.NoMatch,
    IReadOnlyList<CommandPaletteEvidenceEntry>? Evidence = null)
{
    public IReadOnlyList<CommandPaletteEvidenceEntry> Evidence { get; init; } = Evidence ?? [];
}

public sealed record CommandPaletteState(
    bool IsOpen = false,
    string Query = "",
    int SelectedIndex = 0,
    bool PreviewFocused = false,
    string Status = "Type to search...",
    string? LastExecutedCommandId = null,
    IReadOnlyList<string>? FavoriteEntryIds = null,
    bool FavoritesOnly = false,
    CommandPaletteCategory? CategoryFilter = null)
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
                .Select(static entry => new CommandPaletteSearchResult(
                    entry,
                    ScoreEmpty(entry.Title),
                    [],
                    CommandPaletteMatchKind.NoMatch,
                    [new CommandPaletteEvidenceEntry("match_type", 1.0, "empty query matches all")]))
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
        var normalizedQuery = query.Trim();
        var titleLower = title.ToLowerInvariant();
        var queryLower = normalizedQuery.ToLowerInvariant();
        var match = DetectMatch(titleLower, queryLower);
        if (match.Kind == CommandPaletteMatchKind.NoMatch)
        {
            return null;
        }

        var scored = BayesianScore(match.Kind, match.Positions, queryLower.Length, titleLower);
        var score = scored.Score;
        var evidence = scored.Evidence.ToList();
        if (entry.Tags.Any(tag => tag.Contains(queryLower, StringComparison.OrdinalIgnoreCase)) &&
            score is > 0 and < 1)
        {
            var odds = score / (1.0 - score);
            score = odds * 3.0 / (1.0 + odds * 3.0);
            evidence.Add(new CommandPaletteEvidenceEntry("tag_match", 3.0, "query matches tag"));
        }

        return new CommandPaletteSearchResult(entry, score, match.Positions, match.Kind, evidence);
    }

    private static (CommandPaletteMatchKind Kind, IReadOnlyList<int> Positions) DetectMatch(
        string titleLower,
        string queryLower)
    {
        if (queryLower == titleLower)
        {
            return (CommandPaletteMatchKind.Exact, Enumerable.Range(0, titleLower.Length).ToArray());
        }

        if (titleLower.StartsWith(queryLower, StringComparison.Ordinal))
        {
            return (CommandPaletteMatchKind.Prefix, Enumerable.Range(0, queryLower.Length).ToArray());
        }

        if (TryWordStartMatch(titleLower, queryLower, out var wordStartPositions))
        {
            return (CommandPaletteMatchKind.WordStart, wordStartPositions);
        }

        var contiguous = titleLower.IndexOf(queryLower, StringComparison.Ordinal);
        if (contiguous >= 0)
        {
            return (CommandPaletteMatchKind.Substring, Enumerable.Range(contiguous, queryLower.Length).ToArray());
        }

        if (TryFuzzyMatch(titleLower, queryLower, out var fuzzyPositions, out _))
        {
            return (CommandPaletteMatchKind.Fuzzy, fuzzyPositions);
        }

        return (CommandPaletteMatchKind.NoMatch, []);
    }

    private static bool TryWordStartMatch(string titleLower, string queryLower, out IReadOnlyList<int> positions)
    {
        var matchPositions = new List<int>(queryLower.Length);
        var queryIndex = 0;
        for (var index = 0; index < titleLower.Length && queryIndex < queryLower.Length; index++)
        {
            var isWordStart = index == 0 || titleLower[index - 1] is ' ' or '-' or '_';
            if (isWordStart && titleLower[index] == queryLower[queryIndex])
            {
                matchPositions.Add(index);
                queryIndex++;
            }
        }

        positions = queryIndex == queryLower.Length ? matchPositions : [];
        return queryIndex == queryLower.Length;
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

    private static (double Score, IReadOnlyList<CommandPaletteEvidenceEntry> Evidence) BayesianScore(
        CommandPaletteMatchKind kind,
        IReadOnlyList<int> positions,
        int queryLength,
        string titleLower)
    {
        var combinedOdds = PriorOdds(kind);
        var evidence = new List<CommandPaletteEvidenceEntry>
        {
            new("match_type", combinedOdds, $"{kind.ToString().ToLowerInvariant()} match")
        };
        if (positions.Count > 0)
        {
            var positionFactor = 1.0 + 1.0 / (positions[0] + 1.0) * 0.5;
            combinedOdds *= positionFactor;
            evidence.Add(new CommandPaletteEvidenceEntry(
                "position",
                positionFactor,
                $"first match at position {positions[0]}"));
        }

        var wordBoundaryCount = positions.Count(position => position == 0 || titleLower[position - 1] is ' ' or '-' or '_');
        if (wordBoundaryCount > 0)
        {
            var boundaryFactor = 1.0 + wordBoundaryCount * 0.3;
            combinedOdds *= boundaryFactor;
            evidence.Add(new CommandPaletteEvidenceEntry(
                "word_boundary",
                boundaryFactor,
                $"{wordBoundaryCount} word boundary matches"));
        }

        if (kind == CommandPaletteMatchKind.Fuzzy && positions.Count > 1)
        {
            var totalGap = TotalGap(positions);
            var gapFactor = 1.0 / (1.0 + totalGap * 0.1);
            combinedOdds *= gapFactor;
            evidence.Add(new CommandPaletteEvidenceEntry(
                "gap_penalty",
                gapFactor,
                $"total gap of {totalGap} characters"));
        }

        var lengthFactor = 1.0 + queryLength / (double)Math.Max(titleLower.Length, 1) * 0.2;
        combinedOdds *= lengthFactor;
        evidence.Add(new CommandPaletteEvidenceEntry(
            "title_length",
            lengthFactor,
            $"title length {titleLower.Length} chars"));
        return (combinedOdds / (1.0 + combinedOdds), evidence);
    }

    private static double ScoreEmpty(string title) =>
        1.0 / (title.Length + 1.0) * 0.1;

    private static double PriorOdds(CommandPaletteMatchKind kind) =>
        kind switch
        {
            CommandPaletteMatchKind.Exact => 99.0,
            CommandPaletteMatchKind.Prefix => 9.0,
            CommandPaletteMatchKind.WordStart => 4.0,
            CommandPaletteMatchKind.Substring => 2.0,
            CommandPaletteMatchKind.Fuzzy => 0.333,
            _ => 0.0
        };

    private static int TotalGap(IReadOnlyList<int> positions)
    {
        var total = 0;
        for (var index = 1; index < positions.Count; index++)
        {
            total += Math.Max(positions[index] - positions[index - 1] - 1, 0);
        }

        return total;
    }
}

public static class CommandPaletteController
{
    public static IReadOnlyList<CommandPaletteSearchResult> Results(CommandPaletteState state, IReadOnlyList<CommandPaletteEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(entries);

        var results = CommandPaletteSearch.Search(entries, state.Query);
        if (state.CategoryFilter is { } categoryFilter)
        {
            results = results
                .Where(result => result.Entry.Category == categoryFilter)
                .ToArray();
        }

        if (!state.FavoritesOnly)
        {
            return results;
        }

        var favorites = FavoriteSet(state);
        return results
            .Where(result => favorites.Contains(result.Entry.Id))
            .ToArray();
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

        if (IsCtrlF(gesture, requireShift: true))
        {
            var nextFavoritesOnly = !state.FavoritesOnly;
            return (state with
            {
                FavoritesOnly = nextFavoritesOnly,
                SelectedIndex = 0,
                Status = nextFavoritesOnly ? "Favorites filter enabled." : "Favorites filter disabled."
            }, null);
        }

        if (IsCtrlF(gesture, requireShift: false))
        {
            if (selectedIndex < 0 || selectedIndex >= results.Count)
            {
                return (state with { Status = "No command selected." }, null);
            }

            var selected = results[selectedIndex].Entry;
            var favorites = FavoriteSet(state);
            var nextFavorites = favorites.Contains(selected.Id)
                ? favorites.Where(id => !string.Equals(id, selected.Id, StringComparison.Ordinal)).ToArray()
                : favorites.Append(selected.Id).Order(StringComparer.Ordinal).ToArray();

            var favorited = nextFavorites.Contains(selected.Id, StringComparer.Ordinal);
            return (state with
            {
                FavoriteEntryIds = nextFavorites,
                SelectedIndex = 0,
                Status = favorited
                    ? $"Favorited {selected.Title}."
                    : $"Unfavorited {selected.Title}."
            }, null);
        }

        if (IsCtrlNumber(gesture, out var digit))
        {
            if (digit == 0)
            {
                return (state with
                {
                    CategoryFilter = null,
                    SelectedIndex = 0,
                    Status = "Category filter cleared."
                }, null);
            }

            var categories = Enum.GetValues<CommandPaletteCategory>();
            var index = digit - 1;
            if (index < 0 || index >= categories.Length)
            {
                return (state with { Status = "No category for shortcut." }, null);
            }

            var category = categories[index];
            return (state with
            {
                CategoryFilter = category,
                SelectedIndex = 0,
                Status = $"Category filter: {category}."
            }, null);
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

    private static HashSet<string> FavoriteSet(CommandPaletteState state) =>
        new(state.FavoriteEntryIds ?? [], StringComparer.Ordinal);

    private static bool IsCtrlF(KeyGesture gesture, bool requireShift) =>
        gesture.IsCharacter &&
        gesture.Character is { } rune &&
        string.Equals(rune.ToString(), "f", StringComparison.OrdinalIgnoreCase) &&
        gesture.Modifiers.HasFlag(TerminalModifiers.Control) &&
        gesture.Modifiers.HasFlag(TerminalModifiers.Shift) == requireShift;

    private static bool IsCtrlNumber(KeyGesture gesture, out int digit)
    {
        digit = -1;
        if (!gesture.IsCharacter ||
            gesture.Character is not { } rune ||
            !gesture.Modifiers.HasFlag(TerminalModifiers.Control) ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Shift))
        {
            return false;
        }

        var value = rune.ToString();
        if (value.Length != 1 || !char.IsDigit(value[0]))
        {
            return false;
        }

        digit = value[0] - '0';
        return true;
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
            : $"{selected.Entry.Title}\n{selected.Entry.Description}\nCategory: {selected.Entry.Category}\nKey: {selected.Entry.Keybinding ?? "n/a"}\n" +
              $"Score: {selected.Score:0.000} ({selected.MatchKind})\n" +
              $"Evidence: {FormatEvidence(selected.Evidence)}" +
              (selected.Entry.ScreenNumber is { } screenNumber
                  ? $"\nScreen: {screenNumber:00} {selected.Entry.ScreenSlug}\nScreen category: {selected.Entry.ScreenCategory}"
                  : string.Empty);

        new PanelWidget
        {
            Title = "Preview",
            Child = new ParagraphWidget(body)
        }.Render(context);
    }

    private static string FormatEvidence(IReadOnlyList<CommandPaletteEvidenceEntry> evidence) =>
        evidence.Count == 0
            ? "none"
            : string.Join("; ", evidence.Take(4).Select(entry => $"{entry.Kind} {entry.Factor:0.##}"));
}
