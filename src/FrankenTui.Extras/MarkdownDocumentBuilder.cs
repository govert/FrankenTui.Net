using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Text;

namespace FrankenTui.Extras;

public static class MarkdownDocumentBuilder
{
    public static TextDocument Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var result = new List<TextLine>();
        var codeLines = new List<string>();
        var inCodeBlock = false;
        var codeLanguage = string.Empty;

        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    FlushCodeBlock(result, codeLines, codeLanguage);
                    codeLines.Clear();
                    codeLanguage = string.Empty;
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                    codeLanguage = rawLine[3..].Trim();
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(rawLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                result.Add(TextLine.FromText(string.Empty));
                continue;
            }

            if (TryParseHeading(rawLine, out var heading))
            {
                result.Add(heading);
                continue;
            }

            if (TryParseList(rawLine, out var listLine))
            {
                result.Add(listLine);
                continue;
            }

            if (rawLine.StartsWith("> ", StringComparison.Ordinal))
            {
                var spans = new List<TextSpan> { new("│ ", UiStyle.Muted.WithFlags(CellStyleFlags.Italic)) };
                spans.AddRange(ParseInline(rawLine[2..]));
                result.Add(new TextLine(spans));
                continue;
            }

            if (rawLine.Contains('|', StringComparison.Ordinal))
            {
                result.Add(new TextLine([new TextSpan(rawLine, UiStyle.Muted)]));
                continue;
            }

            result.Add(new TextLine(ParseInline(rawLine)));
        }

        if (inCodeBlock)
        {
            FlushCodeBlock(result, codeLines, codeLanguage);
        }

        return new TextDocument(result);
    }

    private static bool TryParseHeading(string rawLine, out TextLine heading)
    {
        heading = TextLine.FromText(string.Empty);
        var level = 0;
        while (level < rawLine.Length && rawLine[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= rawLine.Length || rawLine[level] != ' ')
        {
            return false;
        }

        var style = level switch
        {
            1 => UiStyle.Accent,
            2 => UiStyle.Warning,
            _ => UiStyle.Default.WithFlags(CellStyleFlags.Bold)
        };
        heading = new TextLine(
        [
            new TextSpan($"{new string('▌', Math.Min(level, 3))} {rawLine[(level + 1)..]}", style)
        ]);
        return true;
    }

    private static bool TryParseList(string rawLine, out TextLine listLine)
    {
        listLine = TextLine.FromText(string.Empty);
        if (!(rawLine.StartsWith("- ", StringComparison.Ordinal) ||
              rawLine.StartsWith("* ", StringComparison.Ordinal)))
        {
            return false;
        }

        var spans = new List<TextSpan> { new("• ", UiStyle.Accent) };
        spans.AddRange(ParseInline(rawLine[2..]));
        listLine = new TextLine(spans);
        return true;
    }

    private static IReadOnlyList<TextSpan> ParseInline(string text)
    {
        var spans = new List<TextSpan>();
        var index = 0;
        while (index < text.Length)
        {
            if (TryParseLink(text, ref index, spans))
            {
                continue;
            }

            if (text.AsSpan(index).StartsWith("**", StringComparison.Ordinal))
            {
                var end = text.IndexOf("**", index + 2, StringComparison.Ordinal);
                if (end > index)
                {
                    spans.Add(new TextSpan(text[(index + 2)..end], UiStyle.Accent));
                    index = end + 2;
                    continue;
                }
            }

            if (text[index] == '_')
            {
                var end = text.IndexOf('_', index + 1);
                if (end > index)
                {
                    spans.Add(new TextSpan(text[(index + 1)..end], UiStyle.Default.WithFlags(CellStyleFlags.Italic)));
                    index = end + 1;
                    continue;
                }
            }

            if (text[index] == '`')
            {
                var end = text.IndexOf('`', index + 1);
                if (end > index)
                {
                    spans.Add(new TextSpan(text[(index + 1)..end], UiStyle.Muted.WithBackground(PackedRgba.Rgb(24, 30, 40))));
                    index = end + 1;
                    continue;
                }
            }

            spans.Add(new TextSpan(text[index].ToString(), UiStyle.Default));
            index++;
        }

        return spans;
    }

    private static bool TryParseLink(string text, ref int index, List<TextSpan> spans)
    {
        if (text[index] != '[')
        {
            return false;
        }

        var closeLabel = text.IndexOf(']', index + 1);
        if (closeLabel < 0 || closeLabel + 1 >= text.Length || text[closeLabel + 1] != '(')
        {
            return false;
        }

        var closeUrl = text.IndexOf(')', closeLabel + 2);
        if (closeUrl < 0)
        {
            return false;
        }

        var label = text[(index + 1)..closeLabel];
        var url = text[(closeLabel + 2)..closeUrl];
        spans.Add(new TextSpan(label, UiStyle.Accent.WithFlags(CellStyleFlags.Underline)));
        spans.Add(new TextSpan($" <{url}>", UiStyle.Muted));
        index = closeUrl + 1;
        return true;
    }

    private static void FlushCodeBlock(List<TextLine> result, List<string> codeLines, string codeLanguage)
    {
        if (codeLines.Count == 0)
        {
            return;
        }

        if (codeLanguage.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
            codeLanguage.Equals("cs", StringComparison.OrdinalIgnoreCase))
        {
            result.AddRange(SyntaxHighlighter.HighlightCSharpDocument(string.Join('\n', codeLines)).Lines);
            return;
        }

        foreach (var line in codeLines)
        {
            result.Add(new TextLine([new TextSpan(line, UiStyle.Muted.WithBackground(PackedRgba.Rgb(24, 30, 40)))]));
        }
    }
}
