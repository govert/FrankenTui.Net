using System.Globalization;
using System.Text;

namespace FrankenTui.Core;

public static class TerminalTextWidth
{
    public static int CharWidth(char value) => RuneWidth(new Rune(value));

    public static IEnumerable<string> EnumerateTextElements(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            yield return (string)enumerator.Current!;
        }
    }

    public static int RuneWidth(Rune value)
    {
        if (value.Value is '\t' or '\n' or '\r')
        {
            return 1;
        }

        var category = Rune.GetUnicodeCategory(value);
        if (category == UnicodeCategory.Control)
        {
            return 0;
        }

        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
        {
            return 0;
        }

        return IsWide(value.Value) ? 2 : 1;
    }

    public static int DisplayWidth(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var width = 0;
        foreach (var textElement in EnumerateTextElements(text))
        {
            width += TextElementWidth(textElement);
        }

        return width;
    }

    public static int TextElementWidth(string textElement)
    {
        ArgumentNullException.ThrowIfNull(textElement);

        var width = 0;
        foreach (var rune in textElement.EnumerateRunes())
        {
            width = Math.Max(width, RuneWidth(rune));
        }

        return width;
    }

    private static bool IsWide(int value) =>
        value is
            >= 0x1100 and <= 0x115F or
            0x2329 or
            0x232A or
            >= 0x2E80 and <= 0x303E or
            >= 0x3040 and <= 0xA4CF or
            >= 0xAC00 and <= 0xD7A3 or
            >= 0xF900 and <= 0xFAFF or
            >= 0xFE10 and <= 0xFE19 or
            >= 0xFE30 and <= 0xFE6F or
            >= 0xFF00 and <= 0xFF60 or
            >= 0xFFE0 and <= 0xFFE6 or
            >= 0x1F300 and <= 0x1FAFF or
            >= 0x20000 and <= 0x2FFFD or
            >= 0x30000 and <= 0x3FFFD;
}
