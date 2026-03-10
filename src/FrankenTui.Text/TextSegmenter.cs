using System.Text;

namespace FrankenTui.Text;

public static class TextSegmenter
{
    public static IReadOnlyList<TextSegment> Segment(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = new List<TextSegment>();
        var builder = new StringBuilder();
        TextDirection? currentDirection = null;
        TextScript? currentScript = null;

        foreach (var rune in text.EnumerateRunes())
        {
            var direction = GetDirection(rune);
            var script = GetScript(rune);
            if (builder.Length > 0 &&
                currentDirection == direction &&
                currentScript == script)
            {
                builder.Append(rune.ToString());
                continue;
            }

            Flush();
            builder.Append(rune.ToString());
            currentDirection = direction;
            currentScript = script;
        }

        Flush();
        return result;

        void Flush()
        {
            if (builder.Length == 0 || currentDirection is null || currentScript is null)
            {
                return;
            }

            result.Add(new TextSegment(builder.ToString(), currentDirection.Value, currentScript.Value));
            builder.Clear();
        }
    }

    public static TextDirection GetDirection(Rune rune)
    {
        if (IsArabicHebrew(rune))
        {
            return TextDirection.RightToLeft;
        }

        if (Rune.IsWhiteSpace(rune) || Rune.IsPunctuation(rune))
        {
            return TextDirection.Neutral;
        }

        return TextDirection.LeftToRight;
    }

    public static TextScript GetScript(Rune rune)
    {
        var value = rune.Value;
        if (IsEmoji(value))
        {
            return TextScript.Emoji;
        }

        if (IsArabicHebrew(rune))
        {
            return TextScript.ArabicHebrew;
        }

        if (IsCjk(value))
        {
            return TextScript.Cjk;
        }

        if ((value is >= 0x0041 and <= 0x024F) || (value is >= 0x1E00 and <= 0x1EFF))
        {
            return TextScript.Latin;
        }

        return TextScript.Common;
    }

    private static bool IsArabicHebrew(Rune rune)
    {
        var value = rune.Value;
        return (value is >= 0x0590 and <= 0x08FF) || (value is >= 0xFB1D and <= 0xFEFC);
    }

    private static bool IsCjk(int value) =>
        (value is >= 0x3040 and <= 0x30FF) ||
        (value is >= 0x3400 and <= 0x9FFF) ||
        (value is >= 0xAC00 and <= 0xD7AF);

    private static bool IsEmoji(int value) =>
        (value is >= 0x1F300 and <= 0x1FAFF) ||
        (value is >= 0x2600 and <= 0x27BF);
}

public sealed record TextSegment(string Text, TextDirection Direction, TextScript Script);
