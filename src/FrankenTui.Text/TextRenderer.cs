using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Text;

public static class TextRenderer
{
    public static IReadOnlyList<RenderedTextLine> Layout(
        TextDocument document,
        ushort width,
        TextRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var effectiveOptions = options ?? TextRenderOptions.Default;
        var result = new List<RenderedTextLine>();
        foreach (var line in document.Lines)
        {
            result.AddRange(LayoutLine(line, Math.Max(width, (ushort)1), effectiveOptions));
        }

        return result;
    }

    public static IReadOnlyList<RenderedTextLine> LayoutViewport(
        TextDocument document,
        ushort width,
        int firstVisualLine,
        int maxVisualLines,
        TextRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (maxVisualLines <= 0)
        {
            return [];
        }

        var effectiveOptions = options ?? TextRenderOptions.Default;
        var normalizedOptions = effectiveOptions with { FirstVisualLine = 0, MaxVisualLines = null };
        var maxWidth = Math.Max(width, (ushort)1);
        var start = Math.Max(firstVisualLine, 0);
        var end = start + maxVisualLines;
        var visualLine = 0;
        var result = new List<RenderedTextLine>(maxVisualLines);

        foreach (var line in document.Lines)
        {
            if (visualLine >= end)
            {
                break;
            }

            var laidOut = LayoutLine(line, maxWidth, normalizedOptions);
            if (visualLine + laidOut.Count <= start)
            {
                visualLine += laidOut.Count;
                continue;
            }

            foreach (var rendered in laidOut)
            {
                if (visualLine >= end)
                {
                    break;
                }

                if (visualLine >= start)
                {
                    result.Add(rendered);
                }

                visualLine++;
            }
        }

        return result;
    }

    public static void Write(
        RenderBuffer buffer,
        ushort x,
        ushort y,
        RenderedTextLine line,
        UiStyle fallbackStyle)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(line);

        var column = x;
        foreach (var run in line.Runs)
        {
            var style = run.Style ?? fallbackStyle;
            foreach (var textElement in TerminalTextWidth.EnumerateTextElements(run.Text))
            {
                var width = Math.Max(TerminalTextWidth.TextElementWidth(textElement), 1);
                if (column >= buffer.Width)
                {
                    return;
                }

                buffer.SetText(column, y, textElement, style.ToCell());
                if (column > ushort.MaxValue - width)
                {
                    return;
                }

                column += (ushort)width;
            }
        }
    }

    private static IReadOnlyList<RenderedTextLine> LayoutLine(TextLine line, ushort width, TextRenderOptions options)
    {
        var runs = BuildRuns(line);
        if (options.WrapMode == TextWrapMode.None || TotalWidth(runs) <= width)
        {
            return [new RenderedTextLine(runs)];
        }

        return options.WrapMode switch
        {
            TextWrapMode.Character => WrapByCharacter(runs, width),
            _ => WrapByWord(runs, width)
        };
    }

    private static IReadOnlyList<RenderedTextRun> BuildRuns(TextLine line) =>
        line.Spans
            .Where(static span => span.Text.Length > 0)
            .Select(static span => new RenderedTextRun(span.Text, span.Style))
            .ToArray();

    private static IReadOnlyList<RenderedTextLine> WrapByCharacter(IReadOnlyList<RenderedTextRun> runs, ushort width)
    {
        var lines = new List<RenderedTextLine>();
        var currentText = new List<(string TextElement, UiStyle? Style)>();
        var currentWidth = 0;

        foreach (var run in runs)
        {
            foreach (var textElement in TerminalTextWidth.EnumerateTextElements(run.Text))
            {
                var textElementWidth = Math.Max(TerminalTextWidth.TextElementWidth(textElement), 1);
                if (currentWidth + textElementWidth > width && currentText.Count > 0)
                {
                    lines.Add(new RenderedTextLine(MergeTextElements(currentText)));
                    currentText.Clear();
                    currentWidth = 0;
                }

                currentText.Add((textElement, run.Style));
                currentWidth += textElementWidth;
            }
        }

        if (currentText.Count > 0 || lines.Count == 0)
        {
            lines.Add(new RenderedTextLine(MergeTextElements(currentText)));
        }

        return lines;
    }

    private static IReadOnlyList<RenderedTextLine> WrapByWord(IReadOnlyList<RenderedTextRun> runs, ushort width)
    {
        var tokens = Tokenize(runs);
        var lines = new List<RenderedTextLine>();
        var current = new List<RenderedTextRun>();
        var currentWidth = 0;

        foreach (var token in tokens)
        {
            var tokenWidth = TotalWidth(token.Runs);
            if (currentWidth == 0 && token.IsWhitespace)
            {
                continue;
            }

            if (currentWidth > 0 && currentWidth + tokenWidth > width)
            {
                lines.Add(new RenderedTextLine(current.ToArray()));
                current.Clear();
                currentWidth = 0;

                if (token.IsWhitespace)
                {
                    continue;
                }
            }

            if (tokenWidth > width && !token.IsWhitespace)
            {
                foreach (var broken in WrapByCharacter(token.Runs, width))
                {
                    if (current.Count == 0)
                    {
                        lines.Add(broken);
                    }
                    else
                    {
                        current.AddRange(broken.Runs);
                        lines.Add(new RenderedTextLine(current.ToArray()));
                        current.Clear();
                    }
                }

                currentWidth = 0;
                continue;
            }

            current.AddRange(token.Runs);
            currentWidth += tokenWidth;
        }

        if (current.Count > 0 || lines.Count == 0)
        {
            lines.Add(new RenderedTextLine(current.ToArray()));
        }

        return lines;
    }

    private static IReadOnlyList<Token> Tokenize(IReadOnlyList<RenderedTextRun> runs)
    {
        var tokens = new List<Token>();
        var currentTextElements = new List<(string TextElement, UiStyle? Style)>();
        bool? whitespace = null;

        foreach (var run in runs)
        {
            foreach (var textElement in TerminalTextWidth.EnumerateTextElements(run.Text))
            {
                var firstRune = textElement.EnumerateRunes().FirstOrDefault();
                var isWhitespace = firstRune.Value != 0 && Rune.IsWhiteSpace(firstRune);
                if (whitespace is not null && whitespace != isWhitespace)
                {
                    tokens.Add(new Token(MergeTextElements(currentTextElements), whitespace.Value));
                    currentTextElements.Clear();
                }

                currentTextElements.Add((textElement, run.Style));
                whitespace = isWhitespace;
            }
        }

        if (currentTextElements.Count > 0 && whitespace is not null)
        {
            tokens.Add(new Token(MergeTextElements(currentTextElements), whitespace.Value));
        }

        return tokens;
    }

    private static IReadOnlyList<RenderedTextRun> MergeTextElements(IReadOnlyList<(string TextElement, UiStyle? Style)> textElements)
    {
        var result = new List<RenderedTextRun>();
        UiStyle? currentStyle = null;
        var builder = new StringBuilder();
        var hasStyle = false;

        foreach (var (textElement, style) in textElements)
        {
            if (builder.Length > 0 && (!Equals(currentStyle, style) || hasStyle != (style is not null)))
            {
                result.Add(new RenderedTextRun(builder.ToString(), currentStyle));
                builder.Clear();
            }

            builder.Append(textElement);
            currentStyle = style;
            hasStyle = style is not null;
        }

        if (builder.Length > 0)
        {
            result.Add(new RenderedTextRun(builder.ToString(), currentStyle));
        }

        return result;
    }

    private static int TotalWidth(IReadOnlyList<RenderedTextRun> runs) =>
        runs.Sum(static run => TerminalTextWidth.DisplayWidth(run.Text));

    private sealed record Token(IReadOnlyList<RenderedTextRun> Runs, bool IsWhitespace);
}

public sealed record RenderedTextLine(IReadOnlyList<RenderedTextRun> Runs)
{
    public string PlainText => string.Concat(Runs.Select(static run => run.Text));
}

public sealed record RenderedTextRun(string Text, UiStyle? Style);
