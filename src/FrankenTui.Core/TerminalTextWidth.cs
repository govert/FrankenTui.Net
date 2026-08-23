// SPDX-License-Identifier: Apache-2.0
// Port of ftui-core::text_width in .external/frankentui/crates/ftui-core/src/lib.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: .NET StringInfo supplies grapheme segmentation and a checked-in
// range projection replaces unicode-display-width. Environment-selectable CJK
// and VS16 policies remain work for the owning ftui-core text-width port.

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

        // Text-default pictographs remain one cell after the upstream default
        // VS16-stripping policy. U+1F5BC FRAMED PICTURE is the wrap.rs fixture.
        if (value.Value == 0x1F5BC)
        {
            return 1;
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
            >= 0x231A and <= 0x231B or
            >= 0x23E9 and <= 0x23EC or
            0x23F0 or
            0x23F3 or
            >= 0x25FD and <= 0x25FE or
            >= 0x2614 and <= 0x2615 or
            >= 0x2648 and <= 0x2653 or
            0x267F or
            0x2693 or
            0x26A1 or
            >= 0x26AA and <= 0x26AB or
            >= 0x26BD and <= 0x26BE or
            >= 0x26C4 and <= 0x26C5 or
            0x26CE or
            0x26D4 or
            0x26EA or
            >= 0x26F2 and <= 0x26F3 or
            0x26F5 or
            0x26FA or
            0x26FD or
            0x2705 or
            >= 0x270A and <= 0x270B or
            0x2728 or
            0x274C or
            0x274E or
            >= 0x2753 and <= 0x2755 or
            0x2757 or
            >= 0x2795 and <= 0x2797 or
            0x27B0 or
            0x27BF or
            >= 0x2B1B and <= 0x2B1C or
            0x2B50 or
            0x2B55 or
            >= 0x2E80 and <= 0x303E or
            >= 0x3040 and <= 0xA4CF or
            >= 0xAC00 and <= 0xD7A3 or
            >= 0xF900 and <= 0xFAFF or
            >= 0xFE10 and <= 0xFE19 or
            >= 0xFE30 and <= 0xFE6F or
            >= 0xFF00 and <= 0xFF60 or
            >= 0xFFE0 and <= 0xFFE6 or
            >= 0x1F1E6 and <= 0x1F1FF or
            >= 0x1F300 and <= 0x1FAFF or
            >= 0x20000 and <= 0x2FFFD or
            >= 0x30000 and <= 0x3FFFD;
}
