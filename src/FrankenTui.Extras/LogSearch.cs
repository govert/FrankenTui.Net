using FrankenTui.Core;
using System.Text.RegularExpressions;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public sealed record LogSearchState(
    string Query,
    bool RegexMode = false,
    bool CaseSensitive = false,
    int ContextLines = 0,
    bool SearchOpen = true,
    string? Error = null);

public sealed record LogSearchResult(
    IReadOnlyList<string> Lines,
    int MatchCount,
    string? Error = null);

public static class LogSearchEngine
{
    public static LogSearchResult Apply(IReadOnlyList<string> lines, LogSearchState state)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.Query))
        {
            return new LogSearchResult(lines, lines.Count);
        }

        try
        {
            var matches = state.RegexMode
                ? ApplyRegex(lines, state)
                : ApplyLiteral(lines, state);
            return new LogSearchResult(matches, matches.Count);
        }
        catch (ArgumentException error)
        {
            return new LogSearchResult([], 0, error.Message);
        }
    }

    private static IReadOnlyList<string> ApplyLiteral(IReadOnlyList<string> lines, LogSearchState state)
    {
        var comparison = state.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var matchingIndexes = lines
            .Select((line, index) => (line, index))
            .Where(candidate => candidate.line.Contains(state.Query, comparison))
            .Select(candidate => candidate.index)
            .ToArray();
        return ExpandContext(lines, matchingIndexes, state.ContextLines)
            .Select(index => HighlightLiteral(lines[index], state.Query, comparison))
            .ToArray();
    }

    private static IReadOnlyList<string> ApplyRegex(IReadOnlyList<string> lines, LogSearchState state)
    {
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
        var index = line.IndexOf(query, comparison);
        if (index < 0)
        {
            return line;
        }

        return $"{line[..index]}«{line.Substring(index, query.Length)}»{line[(index + query.Length)..]}";
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
