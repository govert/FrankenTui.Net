using System.Text;

namespace FrankenTui.Render;

public static class AnsiBuilder
{
    private const string Escape = "\u001b";
    private const string StringTerminator = "\u001b\\";
    private const int MaxOsc8FieldBytes = 4096;

    public static string CursorPosition(ushort row, ushort column) => $"{Escape}[{row + 1};{column + 1}H";

    public static string ColumnPosition(ushort column) => $"{Escape}[{column + 1}G";

    public static string CursorForward(ushort count) =>
        count switch
        {
            0 => string.Empty,
            1 => $"{Escape}[C",
            _ => $"{Escape}[{count}C"
        };

    public static string CursorBackward(ushort count) =>
        count switch
        {
            0 => string.Empty,
            1 => $"{Escape}[D",
            _ => $"{Escape}[{count}D"
        };

    public static string SgrReset() => $"{Escape}[0m";

    public static string HideCursor() => $"{Escape}[?25l";

    public static string ShowCursor() => $"{Escape}[?25h";

    public static string CursorSave() => $"{Escape}7";

    public static string CursorRestore() => $"{Escape}8";

    public static string SyncOutputBegin() => $"{Escape}[?2026h";

    public static string SyncOutputEnd() => $"{Escape}[?2026l";

    public static string KittyKeyboardEnable() => $"{Escape}[>15u";

    public static string KittyKeyboardDisable() => $"{Escape}[<u";

    public static string EraseLine(EraseLineMode mode) => $"{Escape}[{(byte)mode}K";

    public static string EraseDisplay(EraseDisplayMode mode) => $"{Escape}[{(byte)mode}J";

    public static string HyperlinkEnd() => $"{Escape}]8;;{StringTerminator}";

    public static void AppendBestCursorMove(
        StringBuilder builder,
        ushort? currentColumn,
        ushort? currentRow,
        ushort targetColumn,
        ushort targetRow)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (currentColumn == targetColumn && currentRow == targetRow)
        {
            return;
        }

        if (currentRow != targetRow || currentColumn is null)
        {
            builder.Append(CursorPosition(targetRow, targetColumn));
            return;
        }

        var cup = CursorPosition(targetRow, targetColumn);
        var cha = ColumnPosition(targetColumn);
        var best = cup;
        if (cha.Length < best.Length)
        {
            best = cha;
        }

        if (targetColumn > currentColumn.Value)
        {
            var cuf = CursorForward((ushort)(targetColumn - currentColumn.Value));
            if (cuf.Length < best.Length)
            {
                best = cuf;
            }
        }
        else if (targetColumn < currentColumn.Value)
        {
            var cub = CursorBackward((ushort)(currentColumn.Value - targetColumn));
            if (cub.Length < best.Length)
            {
                best = cub;
            }
        }

        builder.Append(best);
    }

    public static void AppendSgrReset(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(SgrReset());
    }

    public static void AppendSgrFlags(StringBuilder builder, CellStyleFlags flags)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (flags == CellStyleFlags.None)
        {
            return;
        }

        Span<int> codes = stackalloc int[8];
        var count = 0;
        AddFlag(flags, CellStyleFlags.Bold, 1, codes, ref count);
        AddFlag(flags, CellStyleFlags.Dim, 2, codes, ref count);
        AddFlag(flags, CellStyleFlags.Italic, 3, codes, ref count);
        AddFlag(flags, CellStyleFlags.Underline, 4, codes, ref count);
        AddFlag(flags, CellStyleFlags.Blink, 5, codes, ref count);
        AddFlag(flags, CellStyleFlags.Reverse, 7, codes, ref count);
        AddFlag(flags, CellStyleFlags.Strikethrough, 9, codes, ref count);
        AddFlag(flags, CellStyleFlags.Hidden, 8, codes, ref count);

        builder.Append(Escape).Append('[');
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                builder.Append(';');
            }

            builder.Append(codes[i]);
        }

        builder.Append('m');
    }

    public static void AppendForeground(StringBuilder builder, PackedRgba color)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(Escape).Append("[38;2;")
            .Append(color.R).Append(';')
            .Append(color.G).Append(';')
            .Append(color.B).Append('m');
    }

    public static void AppendBackground(StringBuilder builder, PackedRgba color)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(Escape).Append("[48;2;")
            .Append(color.R).Append(';')
            .Append(color.G).Append(';')
            .Append(color.B).Append('m');
    }

    public static bool TryAppendHyperlinkStart(StringBuilder builder, string? url)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!IsSafeOsc8Field(url))
        {
            return false;
        }

        builder.Append(Escape).Append("]8;;").Append(url).Append(StringTerminator);
        return true;
    }

    public static string SanitizeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return " ";
        }

        var builder = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsControl(rune))
            {
                builder.Append(' ');
                continue;
            }

            builder.Append(rune.ToString());
        }

        return builder.Length == 0 ? " " : builder.ToString();
    }

    private static void AddFlag(
        CellStyleFlags flags,
        CellStyleFlags flag,
        int code,
        Span<int> codes,
        ref int count)
    {
        if (flags.HasFlag(flag))
        {
            codes[count++] = code;
        }
    }

    private static bool IsSafeOsc8Field(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Encoding.UTF8.GetByteCount(value) <= MaxOsc8FieldBytes &&
        !value.EnumerateRunes().Any(Rune.IsControl);
}
