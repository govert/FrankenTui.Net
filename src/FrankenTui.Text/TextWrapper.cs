using FrankenTui.Core;

namespace FrankenTui.Text;

public static class TextWrapper
{
    public static ITextWrapAccelerator? Accelerator { get; set; }

    public static IReadOnlyList<string> Wrap(TextDocument document, ushort width, TextWrapMode mode)
    {
        ArgumentNullException.ThrowIfNull(document);

        var maxWidth = Math.Max(width, (ushort)1);
        var result = new List<string>();
        foreach (var line in document.Lines)
        {
            result.AddRange(WrapLine(line.PlainText, maxWidth, mode));
        }

        return result;
    }

    public static IReadOnlyList<string> WrapLine(string text, ushort width, TextWrapMode mode)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (mode == TextWrapMode.None || TerminalTextWidth.DisplayWidth(text) <= width)
        {
            return [text];
        }

        var result = new List<string>();
        if (Accelerator?.TryWrapLine(text, width, mode, result) == true)
        {
            return result;
        }

        if (mode == TextWrapMode.Character)
        {
            var current = string.Empty;
            var currentWidth = 0;
            foreach (var textElement in TerminalTextWidth.EnumerateTextElements(text))
            {
                var textElementWidth = Math.Max(TerminalTextWidth.TextElementWidth(textElement), 1);
                if (currentWidth + textElementWidth > width && current.Length > 0)
                {
                    result.Add(current);
                    current = string.Empty;
                    currentWidth = 0;
                }

                current += textElement;
                currentWidth += textElementWidth;
            }

            if (current.Length > 0)
            {
                result.Add(current);
            }

            return result;
        }

        var words = text.Split(' ', StringSplitOptions.None);
        var lineBuilder = string.Empty;
        foreach (var word in words)
        {
            var candidate = lineBuilder.Length == 0 ? word : $"{lineBuilder} {word}";
            if (TerminalTextWidth.DisplayWidth(candidate) <= width)
            {
                lineBuilder = candidate;
                continue;
            }

            if (lineBuilder.Length > 0)
            {
                result.Add(lineBuilder);
            }

            if (TerminalTextWidth.DisplayWidth(word) <= width)
            {
                lineBuilder = word;
                continue;
            }

            result.AddRange(WrapLine(word, width, TextWrapMode.Character));
            lineBuilder = string.Empty;
        }

        if (lineBuilder.Length > 0)
        {
            result.Add(lineBuilder);
        }

        return result;
    }
}
