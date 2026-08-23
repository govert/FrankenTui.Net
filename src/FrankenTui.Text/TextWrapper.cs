// SPDX-License-Identifier: Apache-2.0
// Managed entry points corresponding to .external/frankentui/crates/ftui-text/src/wrap.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: managed widths are signed Int32 values at the public options
// boundary; negative values are rejected because Rust usize cannot represent them.

using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Text;

/// <summary>Options controlling width wrapping and line finalization.</summary>
public sealed record WrapOptions
{
    private int _width;

    public WrapOptions(int width)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width cannot be negative.");
        }

        Width = width;
    }

    public int Width
    {
        get => _width;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Width cannot be negative.");
            }

            _width = value;
        }
    }

    public TextWrapMode Mode { get; init; } = TextWrapMode.Word;

    public bool PreserveIndent { get; init; }

    public bool TrimTrailing { get; init; } = true;

    public static WrapOptions Default { get; } = new(80);

    public WrapOptions WithMode(TextWrapMode mode) => this with { Mode = mode };

    public WrapOptions WithPreserveIndent(bool preserve) => this with { PreserveIndent = preserve };

    public WrapOptions WithTrimTrailing(bool trim) => this with { TrimTrailing = trim };
}

public static partial class TextWrapper
{
    /// <summary>
    /// Optional legacy acceleration hook. Results are used only by callers that
    /// invoke the accelerator directly until an implementation is certified for
    /// the richer upstream whitespace/options contract.
    /// </summary>
    public static ITextWrapAccelerator? Accelerator { get; set; }

    public static IReadOnlyList<string> Wrap(TextDocument document, ushort width, TextWrapMode mode) =>
        Wrap(document, (int)width, mode);

    public static IReadOnlyList<string> Wrap(TextDocument document, int width, TextWrapMode mode)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new List<string>();
        foreach (var line in document.Lines)
        {
            result.AddRange(WrapText(line.PlainText, width, mode));
        }

        return result;
    }

    /// <summary>Compatibility entry point retained from the original managed API.</summary>
    public static IReadOnlyList<string> WrapLine(string text, ushort width, TextWrapMode mode) =>
        WrapText(text, width, mode);

    /// <summary>Compatibility entry point accepting the upstream-sized managed width.</summary>
    public static IReadOnlyList<string> WrapLine(string text, int width, TextWrapMode mode) =>
        WrapText(text, width, mode);

    /// <summary>Wrap text using the mode's upstream default options.</summary>
    public static IReadOnlyList<string> WrapText(string text, int width, TextWrapMode mode)
    {
        ArgumentNullException.ThrowIfNull(text);
        var options = new WrapOptions(width)
            .WithMode(mode)
            .WithPreserveIndent(mode == TextWrapMode.Char);
        return WrapWithOptions(text, options);
    }

    /// <summary>Wrap text with explicit indentation and trimming behavior.</summary>
    public static IReadOnlyList<string> WrapWithOptions(string text, WrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Width == 0 || options.Mode == TextWrapMode.None)
        {
            return SplitLinesUnwrapped(text, options);
        }

        if (options.Mode == TextWrapMode.Optimal)
        {
            // Optimal treats whitespace as paragraph glue by definition; the
            // indentation/trailing-space options intentionally do not apply.
            return WrapTextOptimal(text, options.Width);
        }

        return options.Mode switch
        {
            TextWrapMode.Char => WrapCharacters(text, options),
            TextWrapMode.Word => WrapWords(text, options, charFallback: false),
            TextWrapMode.WordChar => WrapWords(text, options, charFallback: true),
            _ => throw new ArgumentOutOfRangeException(
                nameof(options), options.Mode, "Unknown text wrapping mode."),
        };
    }

    private static IReadOnlyList<string> SplitLinesUnwrapped(string text, WrapOptions options)
    {
        var result = new List<string>();
        foreach (var rawLine in text.Split('\n', StringSplitOptions.None))
        {
            var line = rawLine.EndsWith('\r') ? rawLine[..^1] : rawLine;
            result.Add(FinalizeLine(line, options));
        }

        return result;
    }

    private static IReadOnlyList<string> WrapCharacters(string text, WrapOptions options)
    {
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var currentWidth = 0;

        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(text))
        {
            if (grapheme is "\n" or "\r\n")
            {
                lines.Add(FinalizeLine(currentLine.ToString(), options));
                currentLine.Clear();
                currentWidth = 0;
                continue;
            }

            var graphemeWidth = TerminalTextWidth.TextElementWidth(grapheme);
            if (currentWidth + graphemeWidth > options.Width && currentLine.Length > 0)
            {
                lines.Add(FinalizeLine(currentLine.ToString(), options));
                currentLine.Clear();
                currentWidth = 0;
            }

            currentLine.Append(grapheme);
            currentWidth += graphemeWidth;
        }

        // Preserve the final segment, including the empty segment after a newline.
        lines.Add(FinalizeLine(currentLine.ToString(), options));
        return lines;
    }

    private static IReadOnlyList<string> WrapWords(
        string text,
        WrapOptions options,
        bool charFallback)
    {
        var lines = new List<string>();
        foreach (var rawParagraph in text.Split('\n', StringSplitOptions.None))
        {
            var paragraph = rawParagraph.EndsWith('\r') ? rawParagraph[..^1] : rawParagraph;
            var currentLine = new StringBuilder();
            var currentWidth = 0;
            var countBefore = lines.Count;

            WrapParagraph(
                paragraph,
                options,
                charFallback,
                lines,
                currentLine,
                ref currentWidth);

            if (currentLine.Length > 0 || lines.Count == countBefore)
            {
                lines.Add(FinalizeLine(currentLine.ToString(), options));
            }
        }

        return lines;
    }

    private static void WrapParagraph(
        string text,
        WrapOptions options,
        bool charFallback,
        List<string> lines,
        StringBuilder currentLine,
        ref int currentWidth)
    {
        foreach (var word in SplitWords(text))
        {
            var whitespaceOnly = IsBreakingWhitespaceSegment(word);
            if (currentWidth == 0 && whitespaceOnly && !options.PreserveIndent)
            {
                continue;
            }

            var wordWidth = TerminalTextWidth.DisplayWidth(word);
            if (currentWidth + wordWidth <= options.Width)
            {
                currentLine.Append(word);
                currentWidth += wordWidth;
                continue;
            }

            if (currentLine.Length > 0)
            {
                if (IsBreakingWhitespaceSegment(currentLine.ToString()))
                {
                    currentLine.Clear();
                    currentWidth = 0;
                }
                else
                {
                    lines.Add(FinalizeLine(currentLine.ToString(), options));
                    currentLine.Clear();
                    currentWidth = 0;
                }

                if (whitespaceOnly && !options.PreserveIndent)
                {
                    continue;
                }
            }

            if (wordWidth > options.Width)
            {
                if (charFallback)
                {
                    WrapLongWord(word, options, lines, currentLine, ref currentWidth);
                }
                else
                {
                    lines.Add(FinalizeLine(word, options));
                }
            }
            else
            {
                if (word.Length > 0)
                {
                    currentLine.Append(word);
                }

                currentWidth = wordWidth;
            }
        }
    }

    private static void WrapLongWord(
        string word,
        WrapOptions options,
        List<string> lines,
        StringBuilder currentLine,
        ref int currentWidth)
    {
        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(word))
        {
            var graphemeWidth = TerminalTextWidth.TextElementWidth(grapheme);
            var whitespace = IsBreakingWhitespaceSegment(grapheme);
            if (currentWidth == 0 && whitespace && !options.PreserveIndent)
            {
                continue;
            }

            if (currentWidth + graphemeWidth > options.Width && currentLine.Length > 0)
            {
                lines.Add(FinalizeLine(currentLine.ToString(), options));
                currentLine.Clear();
                currentWidth = 0;
                if (whitespace && !options.PreserveIndent)
                {
                    continue;
                }
            }

            currentLine.Append(grapheme);
            currentWidth += graphemeWidth;
        }
    }

    internal static IReadOnlyList<string> SplitWords(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        bool? inWhitespace = null;

        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(text))
        {
            var whitespace = IsBreakingWhitespaceSegment(grapheme);
            if (inWhitespace is not null && whitespace != inWhitespace && current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }

            current.Append(grapheme);
            inWhitespace = whitespace;
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }

    private static string FinalizeLine(string line, WrapOptions options) =>
        options.TrimTrailing ? TrimBreakingWhitespace(line) : line;

    internal static bool IsBreakingWhitespaceSegment(string value) =>
        value.Length > 0 && value.All(IsBreakingWhitespace);
}
