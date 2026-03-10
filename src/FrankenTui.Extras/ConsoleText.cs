using System.Text;

namespace FrankenTui.Extras;

public static class ConsoleText
{
    public static string StripAnsi(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var value = text[index];
            if (value == '\u001b')
            {
                index = SkipEscapeSequence(text, index);
                continue;
            }

            if (char.IsControl(value) && value is not ('\r' or '\n' or '\t'))
            {
                continue;
            }

            builder.Append(value);
        }

        return builder.ToString();
    }

    public static IReadOnlyList<string> VisibleLines(string text) =>
        StripAnsi(text)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

    public static string NormalizeVisibleWhitespace(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var stripped = StripAnsi(text).Replace("\r", string.Empty, StringComparison.Ordinal);
        var lines = stripped.Split('\n')
            .Select(static line => line.TrimEnd())
            .ToArray();
        return string.Join(Environment.NewLine, lines);
    }

    private static int SkipEscapeSequence(string text, int index)
    {
        if (index >= text.Length - 1)
        {
            return index;
        }

        var next = text[index + 1];
        if (next == '[')
        {
            var cursor = index + 2;
            while (cursor < text.Length)
            {
                var value = text[cursor];
                if (value >= '@' && value <= '~')
                {
                    return cursor;
                }

                cursor++;
            }

            return text.Length - 1;
        }

        if (next == ']')
        {
            var cursor = index + 2;
            while (cursor < text.Length)
            {
                if (text[cursor] == '\a')
                {
                    return cursor;
                }

                if (text[cursor] == '\u001b' &&
                    cursor + 1 < text.Length &&
                    text[cursor + 1] == '\\')
                {
                    return cursor + 1;
                }

                cursor++;
            }

            return text.Length - 1;
        }

        return index + 1;
    }
}
