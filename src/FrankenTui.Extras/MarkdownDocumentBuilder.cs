using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Text;

namespace FrankenTui.Extras;

public static class MarkdownDocumentBuilder
{
    private const int DocumentCacheCapacity = 32;
    private const int MathCacheCapacity = 128;
    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, TextDocument> DocumentCache = new(StringComparer.Ordinal);
    private static readonly Queue<string> DocumentCacheOrder = new();
    private static readonly Dictionary<string, string> MathCache = new(StringComparer.Ordinal);
    private static readonly Queue<string> MathCacheOrder = new();

    public static int CachedDocumentCount
    {
        get
        {
            lock (CacheLock)
            {
                return DocumentCache.Count;
            }
        }
    }

    public static int CachedMathCount
    {
        get
        {
            lock (CacheLock)
            {
                return MathCache.Count;
            }
        }
    }

    public static void ClearCaches()
    {
        lock (CacheLock)
        {
            DocumentCache.Clear();
            DocumentCacheOrder.Clear();
            MathCache.Clear();
            MathCacheOrder.Clear();
        }
    }

    public static TextDocument ParseCached(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        lock (CacheLock)
        {
            if (DocumentCache.TryGetValue(markdown, out var cached))
            {
                return cached;
            }
        }

        var parsed = Parse(markdown);
        lock (CacheLock)
        {
            if (DocumentCache.TryGetValue(markdown, out var cached))
            {
                return cached;
            }

            while (DocumentCache.Count >= DocumentCacheCapacity && DocumentCacheOrder.TryDequeue(out var oldest))
            {
                DocumentCache.Remove(oldest);
            }

            DocumentCache[markdown] = parsed;
            DocumentCacheOrder.Enqueue(markdown);
        }

        return parsed;
    }

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
                result.Add(ParseTableLine(rawLine));
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

    private static TextLine ParseTableLine(string rawLine)
    {
        if (IsTableSeparator(rawLine))
        {
            return new TextLine([new TextSpan(rawLine, UiStyle.Muted)]);
        }

        var spans = new List<TextSpan>();
        var cells = rawLine.Split('|');
        var startsWithPipe = rawLine.TrimStart().StartsWith('|');
        var endsWithPipe = rawLine.TrimEnd().EndsWith('|');
        var first = startsWithPipe ? 1 : 0;
        var lastExclusive = endsWithPipe ? cells.Length - 1 : cells.Length;

        if (startsWithPipe)
        {
            spans.Add(new TextSpan("| ", UiStyle.Muted));
        }

        for (var cell = first; cell < lastExclusive; cell++)
        {
            if (cell > first)
            {
                spans.Add(new TextSpan(" | ", UiStyle.Muted));
            }

            spans.AddRange(ParseInline(cells[cell].Trim()));
        }

        if (endsWithPipe)
        {
            spans.Add(new TextSpan(" |", UiStyle.Muted));
        }

        return new TextLine(spans);
    }

    private static bool IsTableSeparator(string rawLine)
    {
        var trimmed = rawLine.Trim();
        if (!trimmed.Contains('|', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var ch in trimmed.Trim('|').Trim())
        {
            if (ch is not ('-' or ':' or '|' or ' '))
            {
                return false;
            }
        }

        return trimmed.Contains('-', StringComparison.Ordinal);
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

            if (TryParseMath(text, ref index, spans))
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

    private static bool TryParseMath(string text, ref int index, List<TextSpan> spans)
    {
        if (text[index] != '$')
        {
            return false;
        }

        var display = index + 1 < text.Length && text[index + 1] == '$';
        var start = display ? index + 2 : index + 1;
        var delimiter = display ? "$$" : "$";
        var end = text.IndexOf(delimiter, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return false;
        }

        var expression = text[start..end].Trim();
        var style = display
            ? UiStyle.Accent.WithFlags(CellStyleFlags.Bold)
            : UiStyle.Warning.WithFlags(CellStyleFlags.Italic);
        spans.Add(new TextSpan(ConvertMathCached(expression), style));
        index = end + delimiter.Length;
        return true;
    }

    private static string ConvertMathCached(string expression)
    {
        lock (CacheLock)
        {
            if (MathCache.TryGetValue(expression, out var cached))
            {
                return cached;
            }
        }

        var converted = ConvertMath(expression);
        lock (CacheLock)
        {
            if (MathCache.TryGetValue(expression, out var cached))
            {
                return cached;
            }

            while (MathCache.Count >= MathCacheCapacity && MathCacheOrder.TryDequeue(out var oldest))
            {
                MathCache.Remove(oldest);
            }

            MathCache[expression] = converted;
            MathCacheOrder.Enqueue(expression);
        }

        return converted;
    }

    private static string ConvertMath(string expression)
    {
        var result = expression;
        var replacements = new (string Latex, string Unicode)[]
        {
            (@"\alpha", "α"),
            (@"\beta", "β"),
            (@"\gamma", "γ"),
            (@"\delta", "δ"),
            (@"\Delta", "Δ"),
            (@"\pi", "π"),
            (@"\sum", "Σ"),
            (@"\int", "∫"),
            (@"\infty", "∞"),
            (@"\approx", "≈"),
            (@"\leq", "≤"),
            (@"\geq", "≥"),
            (@"\neq", "≠"),
            (@"\times", "×"),
            (@"\div", "÷"),
            (@"\pm", "±"),
            (@"\mid", "|"),
            (@"\text", string.Empty)
        };

        foreach (var (latex, unicode) in replacements)
        {
            result = result.Replace(latex, unicode, StringComparison.Ordinal);
        }

        result = ReplaceFractions(result);
        result = ReplaceSqrt(result);
        return result.Replace("{", string.Empty, StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal);
    }

    private static string ReplaceFractions(string text)
    {
        var result = text;
        var start = result.IndexOf(@"\frac{", StringComparison.Ordinal);
        while (start >= 0)
        {
            var numeratorStart = start + 6;
            var numeratorEnd = result.IndexOf("}{", numeratorStart, StringComparison.Ordinal);
            if (numeratorEnd < 0)
            {
                break;
            }

            var denominatorStart = numeratorEnd + 2;
            var denominatorEnd = result.IndexOf('}', denominatorStart);
            if (denominatorEnd < 0)
            {
                break;
            }

            var numerator = result[numeratorStart..numeratorEnd];
            var denominator = result[denominatorStart..denominatorEnd];
            result = result[..start] + numerator + "/" + denominator + result[(denominatorEnd + 1)..];
            start = result.IndexOf(@"\frac{", start + 1, StringComparison.Ordinal);
        }

        return result;
    }

    private static string ReplaceSqrt(string text)
    {
        var result = text;
        var start = result.IndexOf(@"\sqrt{", StringComparison.Ordinal);
        while (start >= 0)
        {
            var valueStart = start + 6;
            var valueEnd = result.IndexOf('}', valueStart);
            if (valueEnd < 0)
            {
                break;
            }

            var value = result[valueStart..valueEnd];
            result = string.Concat(result.AsSpan(0, start), "√", value, result.AsSpan(valueEnd + 1));
            start = result.IndexOf(@"\sqrt{", start + 1, StringComparison.Ordinal);
        }

        return result;
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
