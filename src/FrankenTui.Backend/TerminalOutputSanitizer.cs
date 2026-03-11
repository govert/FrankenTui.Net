using System.Text;

namespace FrankenTui.Backend;

public static class TerminalOutputSanitizer
{
    public static string Sanitize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0 || !NeedsSanitization(input))
        {
            return input;
        }

        var builder = new StringBuilder(input.Length);
        for (var index = 0; index < input.Length;)
        {
            var ch = input[index];
            if (ch == '\u001b')
            {
                index = SkipEscapeSequence(input, index);
                continue;
            }

            if (IsAllowedControl(ch))
            {
                builder.Append(ch);
                index++;
                continue;
            }

            if (IsForbiddenControl(ch))
            {
                index++;
                continue;
            }

            if (char.IsHighSurrogate(ch) &&
                index + 1 < input.Length &&
                char.IsLowSurrogate(input[index + 1]))
            {
                builder.Append(ch);
                builder.Append(input[index + 1]);
                index += 2;
                continue;
            }

            builder.Append(ch);
            index++;
        }

        return builder.ToString();
    }

    private static bool NeedsSanitization(string input)
    {
        foreach (var ch in input)
        {
            if (ch == '\u001b' || IsForbiddenControl(ch))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAllowedControl(char ch) =>
        ch is '\t' or '\n' or '\r';

    private static bool IsForbiddenControl(char ch) =>
        (char.IsControl(ch) && !IsAllowedControl(ch)) ||
        ch == '\u007f' ||
        ch is >= '\u0080' and <= '\u009f';

    private static int SkipEscapeSequence(string input, int start)
    {
        var index = start + 1;
        if (index >= input.Length)
        {
            return index;
        }

        return input[index] switch
        {
            '[' => SkipCsi(input, index + 1),
            ']' => SkipOsc(input, index + 1),
            'P' or '^' or '_' => SkipStringTerminated(input, index + 1),
            >= ' ' and <= '~' => index + 1,
            _ => index
        };
    }

    private static int SkipCsi(string input, int index)
    {
        while (index < input.Length)
        {
            var ch = input[index];
            if (ch is >= '@' and <= '~')
            {
                return index + 1;
            }

            if (ch is < ' ' or > '?')
            {
                return index;
            }

            index++;
        }

        return index;
    }

    private static int SkipOsc(string input, int index)
    {
        while (index < input.Length)
        {
            var ch = input[index];
            if (ch == '\a')
            {
                return index + 1;
            }

            if (ch == '\u001b')
            {
                if (index + 1 < input.Length && input[index + 1] == '\\')
                {
                    return index + 2;
                }

                return index;
            }

            if (char.IsControl(ch))
            {
                return index;
            }

            index++;
        }

        return index;
    }

    private static int SkipStringTerminated(string input, int index)
    {
        while (index < input.Length)
        {
            var ch = input[index];
            if (ch == '\u001b')
            {
                if (index + 1 < input.Length && input[index + 1] == '\\')
                {
                    return index + 2;
                }

                return index;
            }

            if (char.IsControl(ch))
            {
                return index;
            }

            index++;
        }

        return index;
    }
}
