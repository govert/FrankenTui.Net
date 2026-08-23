// SPDX-License-Identifier: Apache-2.0
// Ported from crates/ftui-render/src/ansi.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 3582765BAC7D49C0508F9258E1C39B641A12EA29A2FC9B986C0EB88BE57E12D0.

using System.Text;

namespace FrankenTui.Render;

public readonly record struct SgrCodes(byte On, byte Off);

/// <summary>Pure ANSI/VT sequence generation helpers.</summary>
public static class AnsiBuilder
{
    private const string Escape = "\u001b";
    private const char Bell = '\u0007';
    private const int MaxOsc8FieldBytes = 4096;

    public const string SgrResetSequence = "\u001b[0m";
    public const string CursorSaveSequence = "\u001b7";
    public const string CursorRestoreSequence = "\u001b8";
    public const string CursorHideSequence = "\u001b[?25l";
    public const string CursorShowSequence = "\u001b[?25h";
    public const string ResetScrollRegionSequence = "\u001b[r";
    public const string SyncBeginSequence = "\u001b[?2026h";
    public const string SyncEndSequence = "\u001b[?2026l";
    public const string AltScreenEnterSequence = "\u001b[?1049h";
    public const string AltScreenLeaveSequence = "\u001b[?1049l";
    public const string BracketedPasteEnableSequence = "\u001b[?2004h";
    public const string BracketedPasteDisableSequence = "\u001b[?2004l";
    public const string MouseEnableSequence =
        "\u001b[?1001l\u001b[?1003l\u001b[?1005l\u001b[?1015l\u001b[?1016l" +
        "\u001b[?1006;1000;1002h\u001b[?1006h\u001b[?1000h\u001b[?1002h";
    public const string MouseDisableSequence =
        "\u001b[?1000;1002;1006l\u001b[?1000l\u001b[?1002l\u001b[?1006l" +
        "\u001b[?1001l\u001b[?1003l\u001b[?1005l\u001b[?1015l\u001b[?1016l";
    public const string FocusEnableSequence = "\u001b[?1004h";
    public const string FocusDisableSequence = "\u001b[?1004l";

    public static readonly SgrCodes SgrBold = new(1, 22);
    public static readonly SgrCodes SgrDim = new(2, 22);
    public static readonly SgrCodes SgrItalic = new(3, 23);
    public static readonly SgrCodes SgrUnderline = new(4, 24);
    public static readonly SgrCodes SgrBlink = new(5, 25);
    public static readonly SgrCodes SgrReverse = new(7, 27);
    public static readonly SgrCodes SgrHidden = new(8, 28);
    public static readonly SgrCodes SgrStrikethrough = new(9, 29);

    private static readonly (CellStyleFlags Flag, SgrCodes Codes)[] FlagTableStorage =
    [
        (CellStyleFlags.Bold, SgrBold),
        (CellStyleFlags.Dim, SgrDim),
        (CellStyleFlags.Italic, SgrItalic),
        (CellStyleFlags.Underline, SgrUnderline),
        (CellStyleFlags.Blink, SgrBlink),
        (CellStyleFlags.Reverse, SgrReverse),
        (CellStyleFlags.Hidden, SgrHidden),
        (CellStyleFlags.Strikethrough, SgrStrikethrough),
    ];

    public static ReadOnlySpan<(CellStyleFlags Flag, SgrCodes Codes)> FlagTable => FlagTableStorage;

    public static SgrCodes? SgrCodesForFlag(CellStyleFlags flag) => flag switch
    {
        CellStyleFlags.Bold => SgrBold,
        CellStyleFlags.Dim => SgrDim,
        CellStyleFlags.Italic => SgrItalic,
        CellStyleFlags.Underline => SgrUnderline,
        CellStyleFlags.Blink => SgrBlink,
        CellStyleFlags.Reverse => SgrReverse,
        CellStyleFlags.Hidden => SgrHidden,
        CellStyleFlags.Strikethrough => SgrStrikethrough,
        _ => null,
    };

    public static string SgrReset() => SgrResetSequence;

    public static void AppendSgrReset(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(SgrResetSequence);
    }

    public static string SgrFlags(CellStyleFlags flags)
    {
        var builder = new StringBuilder(32);
        AppendSgrFlags(builder, flags);
        return builder.ToString();
    }

    public static void AppendSgrFlags(StringBuilder builder, CellStyleFlags flags)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (flags == CellStyleFlags.None)
        {
            return;
        }

        builder.Append(Escape).Append('[');
        var first = true;
        foreach (var (flag, codes) in FlagTableStorage)
        {
            if ((flags & flag) == 0)
            {
                continue;
            }

            if (!first)
            {
                builder.Append(';');
            }

            builder.Append(codes.On);
            first = false;
        }

        builder.Append('m');
    }

    public static string SgrFlagsOff(
        CellStyleFlags flagsToDisable,
        CellStyleFlags flagsToKeep,
        out CellStyleFlags collateral)
    {
        var builder = new StringBuilder(32);
        collateral = AppendSgrFlagsOff(builder, flagsToDisable, flagsToKeep);
        return builder.ToString();
    }

    public static CellStyleFlags AppendSgrFlagsOff(
        StringBuilder builder,
        CellStyleFlags flagsToDisable,
        CellStyleFlags flagsToKeep)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var collateral = CellStyleFlags.None;
        foreach (var (flag, codes) in FlagTableStorage)
        {
            if ((flagsToDisable & flag) == 0)
            {
                continue;
            }

            builder.Append(Escape).Append('[').Append(codes.Off).Append('m');
            if (codes.Off != 22)
            {
                continue;
            }

            var other = flag == CellStyleFlags.Bold ? CellStyleFlags.Dim : CellStyleFlags.Bold;
            if ((flagsToKeep & other) != 0 && (flagsToDisable & other) == 0)
            {
                collateral |= other;
            }
        }

        return collateral;
    }

    public static string ForegroundRgb(byte red, byte green, byte blue) =>
        $"{Escape}[38;2;{red};{green};{blue}m";

    public static string BackgroundRgb(byte red, byte green, byte blue) =>
        $"{Escape}[48;2;{red};{green};{blue}m";

    public static string Foreground256(byte index) => $"{Escape}[38;5;{index}m";

    public static string Background256(byte index) => $"{Escape}[48;5;{index}m";

    public static string Foreground16(byte index)
    {
        var code = index < 8 ? 30 + index : 90 + index - 8;
        return $"{Escape}[{code}m";
    }

    public static string Background16(byte index)
    {
        var code = index < 8 ? 40 + index : 100 + index - 8;
        return $"{Escape}[{code}m";
    }

    public static string ForegroundDefault() => $"{Escape}[39m";

    public static string BackgroundDefault() => $"{Escape}[49m";

    public static string Foreground(PackedRgba color) =>
        color.A == 0 ? ForegroundDefault() : ForegroundRgb(color.R, color.G, color.B);

    public static string Background(PackedRgba color) =>
        color.A == 0 ? BackgroundDefault() : BackgroundRgb(color.R, color.G, color.B);

    public static void AppendForeground(StringBuilder builder, PackedRgba color)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(Foreground(color));
    }

    public static void AppendBackground(StringBuilder builder, PackedRgba color)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(Background(color));
    }

    public static string CursorPosition(ushort row, ushort column) =>
        $"{Escape}[{(uint)row + 1};{(uint)column + 1}H";

    public static string ColumnPosition(ushort column) => $"{Escape}[{(uint)column + 1}G";

    public static string CursorUp(ushort count) => RelativeCursor(count, 'A');

    public static string CursorDown(ushort count) => RelativeCursor(count, 'B');

    public static string CursorForward(ushort count) => RelativeCursor(count, 'C');

    public static string CursorBackward(ushort count) => RelativeCursor(count, 'D');

    public static string CarriageReturn() => "\r";

    public static string LineFeed() => "\n";

    public static string CursorSave() => CursorSaveSequence;

    public static string CursorRestore() => CursorRestoreSequence;

    public static string HideCursor() => CursorHideSequence;

    public static string ShowCursor() => CursorShowSequence;

    public static string EraseLine(EraseLineMode mode) => mode switch
    {
        EraseLineMode.ToEnd => $"{Escape}[K",
        EraseLineMode.ToStart => $"{Escape}[1K",
        EraseLineMode.All => $"{Escape}[2K",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static string EraseDisplay(EraseDisplayMode mode) => mode switch
    {
        EraseDisplayMode.ToEnd => $"{Escape}[J",
        EraseDisplayMode.ToStart => $"{Escape}[1J",
        EraseDisplayMode.All => $"{Escape}[2J",
        EraseDisplayMode.Scrollback => $"{Escape}[3J",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static string SetScrollRegion(ushort top, ushort bottom) =>
        $"{Escape}[{(uint)top + 1};{(uint)bottom + 1}r";

    public static string ResetScrollRegion() => ResetScrollRegionSequence;

    public static string SyncOutputBegin() => SyncBeginSequence;

    public static string SyncOutputEnd() => SyncEndSequence;

    public static string HyperlinkStart(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        return IsSafeOsc8Field(url) ? $"{Escape}]8;;{url}{Bell}" : string.Empty;
    }

    public static string HyperlinkEnd() => $"{Escape}]8;;{Bell}";

    public static string HyperlinkStartWithId(string id, string url)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(url);
        if (!IsSafeOsc8Field(url)
            || !IsSafeOsc8Field(id)
            || id.Contains(';')
            || id.Contains(':')
            || id.Contains('='))
        {
            return string.Empty;
        }

        return $"{Escape}]8;id={id};{url}{Bell}";
    }

    public static bool TryAppendHyperlinkStart(StringBuilder builder, string? url)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (url is null || !IsSafeOsc8Field(url))
        {
            return false;
        }

        builder.Append(Escape).Append("]8;;").Append(url).Append(Bell);
        return true;
    }

    public static bool TryAppendHyperlinkStartWithId(StringBuilder builder, string? id, string? url)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (id is null || url is null)
        {
            return false;
        }

        var sequence = HyperlinkStartWithId(id, url);
        if (sequence.Length == 0)
        {
            return false;
        }

        builder.Append(sequence);
        return true;
    }

    public static string AltScreenEnter() => AltScreenEnterSequence;

    public static string AltScreenLeave() => AltScreenLeaveSequence;

    public static string BracketedPasteEnable() => BracketedPasteEnableSequence;

    public static string BracketedPasteDisable() => BracketedPasteDisableSequence;

    public static string MouseEnable() => MouseEnableSequence;

    public static string MouseDisable() => MouseDisableSequence;

    public static string FocusEnable() => FocusEnableSequence;

    public static string FocusDisable() => FocusDisableSequence;

    /// <summary>Target compatibility helpers for a mode not defined by upstream ansi.rs.</summary>
    public static string KittyKeyboardEnable() => $"{Escape}[>15u";

    public static string KittyKeyboardDisable() => $"{Escape}[<u";

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

        var best = ColumnPosition(targetColumn);
        if (targetColumn > currentColumn.Value)
        {
            var relative = CursorForward((ushort)(targetColumn - currentColumn.Value));
            if (relative.Length < best.Length)
            {
                best = relative;
            }
        }
        else if (targetColumn < currentColumn.Value)
        {
            var relative = CursorBackward((ushort)(currentColumn.Value - targetColumn));
            if (relative.Length < best.Length)
            {
                best = relative;
            }
        }

        builder.Append(best);
    }

    /// <summary>Target compatibility sanitizer used before ordinary cell emission.</summary>
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
            builder.Append(Rune.IsControl(rune) ? " " : rune.ToString());
        }

        return builder.Length == 0 ? " " : builder.ToString();
    }

    private static string RelativeCursor(ushort count, char suffix) => count switch
    {
        0 => string.Empty,
        1 => $"{Escape}[{suffix}",
        _ => $"{Escape}[{count}{suffix}",
    };

    private static bool IsSafeOsc8Field(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index++;
            }
            else if (char.IsLowSurrogate(current))
            {
                return false;
            }
        }

        return Encoding.UTF8.GetByteCount(value) <= MaxOsc8FieldBytes
            && !value.EnumerateRunes().Any(Rune.IsControl);
    }
}
