using FrankenTui.Core;
using System.Text.RegularExpressions;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum LogSearchTier
{
    Off,
    Lite,
    Full
}

public sealed record LogSearchState(
    string Query,
    bool RegexMode = false,
    bool CaseSensitive = false,
    int ContextLines = 0,
    bool SearchOpen = true,
    string? Error = null,
    LogSearchTier Tier = LogSearchTier.Full);

public sealed record LogSearchResult(
    IReadOnlyList<string> Lines,
    int MatchCount,
    string? Error = null,
    LogSearchTier Tier = LogSearchTier.Full);

public static class LogSearchEngine
{
    public static LogSearchResult Apply(IReadOnlyList<string> lines, LogSearchState state)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.Query))
        {
            return new LogSearchResult(lines, lines.Count, Tier: ResolveTier(lines.Count, state));
        }

        try
        {
            var tier = ResolveTier(lines.Count, state);
            var matches = state.RegexMode
                ? ApplyRegex(lines, state, tier)
                : ApplyLiteral(lines, state, tier);
            return new LogSearchResult(matches, matches.Count, Tier: tier);
        }
        catch (ArgumentException error)
        {
            return new LogSearchResult([], 0, error.Message, ResolveTier(lines.Count, state));
        }
    }

    private static IReadOnlyList<string> ApplyLiteral(IReadOnlyList<string> lines, LogSearchState state, LogSearchTier tier)
    {
        var comparison = state.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var matchingIndexes = lines
            .Select((line, index) => (line, index))
            .Where(candidate => candidate.line.Contains(state.Query, comparison))
            .Select(candidate => candidate.index)
            .ToArray();
        var context = tier == LogSearchTier.Full ? state.ContextLines : 0;
        return ExpandContext(lines, matchingIndexes, context)
            .Select(index => tier == LogSearchTier.Full ? HighlightLiteral(lines[index], state.Query, comparison) : lines[index])
            .ToArray();
    }

    private static IReadOnlyList<string> ApplyRegex(IReadOnlyList<string> lines, LogSearchState state, LogSearchTier tier)
    {
        if (tier == LogSearchTier.Off)
        {
            return ["Search disabled for current budget."];
        }

        if (tier == LogSearchTier.Lite)
        {
            return ApplyLiteral(lines, state with { RegexMode = false }, tier);
        }

        var options = state.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regex = new Regex(state.Query, options);
        var matchingIndexes = lines
            .Select((line, index) => (line, index))
            .Where(candidate => regex.IsMatch(candidate.line))
            .Select(candidate => candidate.index)
            .ToArray();
        return ExpandContext(lines, matchingIndexes, state.ContextLines)
            .Select(index => regex.Replace(lines[index], static match => $"«{match.Value}»"))
            .ToArray();
    }

    private static IReadOnlyList<int> ExpandContext(IReadOnlyList<string> lines, IReadOnlyList<int> matches, int contextLines)
    {
        var indexes = new SortedSet<int>();
        foreach (var match in matches)
        {
            for (var index = Math.Max(match - contextLines, 0); index <= Math.Min(match + contextLines, lines.Count - 1); index++)
            {
                indexes.Add(index);
            }
        }

        return indexes.ToArray();
    }

    private static string HighlightLiteral(string line, string query, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(query))
        {
            return line;
        }

        var builder = new System.Text.StringBuilder(line.Length + query.Length * 2);
        var cursor = 0;
        while (cursor < line.Length)
        {
            var index = line.IndexOf(query, cursor, comparison);
            if (index < 0)
            {
                builder.Append(line.AsSpan(cursor));
                break;
            }

            builder.Append(line.AsSpan(cursor, index - cursor));
            builder.Append('«');
            builder.Append(line.AsSpan(index, query.Length));
            builder.Append('»');
            cursor = index + query.Length;
        }

        return builder.ToString();
    }

    private static LogSearchTier ResolveTier(int lineCount, LogSearchState state)
    {
        if (state.Tier != LogSearchTier.Full)
        {
            return state.Tier;
        }

        if (lineCount > 500)
        {
            return LogSearchTier.Lite;
        }

        return LogSearchTier.Full;
    }
}

public static class LogSearchController
{
    private static readonly int[] ContextCycle = [0, 1, 2, 5];

    public static LogSearchState Apply(LogSearchState state, KeyTerminalEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(keyEvent);

        var gesture = keyEvent.Gesture;
        if (gesture.Key == TerminalKey.Escape && gesture.Modifiers == TerminalModifiers.None)
        {
            return state with { SearchOpen = false, Error = null };
        }

        if (gesture.Key == TerminalKey.Backspace && gesture.Modifiers == TerminalModifiers.None)
        {
            var query = state.Query.Length == 0 ? string.Empty : state.Query[..^1];
            return state with { Query = query, Error = null };
        }

        if (gesture.IsCharacter && gesture.Character is { } rune && gesture.Modifiers == TerminalModifiers.None)
        {
            var lower = rune.ToString().ToLowerInvariant();
            return lower switch
            {
                "r" => state with { RegexMode = !state.RegexMode, Error = null },
                "c" => state with { CaseSensitive = !state.CaseSensitive, Error = null },
                "n" => state with { ContextLines = NextContext(state.ContextLines), Error = null },
                _ => state with { Query = state.Query + rune, Error = null }
            };
        }

        return state;
    }

    public static IReadOnlyList<string> MergeLiveLines(
        IReadOnlyList<string> baseline,
        IReadOnlyList<string> appended,
        int limit = 32)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(appended);

        return baseline.Concat(appended).TakeLast(limit).ToArray();
    }

    private static int NextContext(int current)
    {
        var index = Array.IndexOf(ContextCycle, current);
        index = index < 0 ? 0 : index;
        return ContextCycle[(index + 1) % ContextCycle.Length];
    }
}

public sealed class LogSearchWidget : IWidget
{
    public LogSearchState State { get; init; } = new(string.Empty);

    public IReadOnlyList<string> SourceLines { get; init; } = [];

    public void Render(RuntimeRenderContext context)
    {
        var result = LogSearchEngine.Apply(SourceLines, State);
        var rows = new List<string>
        {
            $"/ {State.Query}",
            $"mode={(State.RegexMode ? "regex" : "literal")} case={(State.CaseSensitive ? "sensitive" : "insensitive")} context={State.ContextLines} matches={result.MatchCount}"
        };

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            rows.Add($"error: {result.Error}");
        }
        else if (result.Lines.Count == 0)
        {
            rows.Add("No matches - Esc to close, / to edit.");
        }
        else
        {
            rows.AddRange(result.Lines);
        }

        new BlockWidget
        {
            Title = "Log Search",
            Child = new PaddingWidget(
                new ParagraphWidget(string.Join(Environment.NewLine, rows)),
                Sides.All(1))
        }.Render(context);
    }
}
