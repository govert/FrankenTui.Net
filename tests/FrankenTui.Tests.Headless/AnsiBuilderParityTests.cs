// Differential contract for crates/ftui-render/src/ansi.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class AnsiBuilderParityTests
{
    [Fact]
    public void SgrResetMatchesSourceBytes()
    {
        Assert.Equal("\u001b[0m", AnsiBuilder.SgrReset());
        var builder = new StringBuilder();
        AnsiBuilder.AppendSgrReset(builder);
        Assert.Equal(AnsiBuilder.SgrReset(), builder.ToString());
    }

    [Theory]
    [InlineData(CellStyleFlags.Bold, 1, 22)]
    [InlineData(CellStyleFlags.Dim, 2, 22)]
    [InlineData(CellStyleFlags.Italic, 3, 23)]
    [InlineData(CellStyleFlags.Underline, 4, 24)]
    [InlineData(CellStyleFlags.Blink, 5, 25)]
    [InlineData(CellStyleFlags.Reverse, 7, 27)]
    [InlineData(CellStyleFlags.Hidden, 8, 28)]
    [InlineData(CellStyleFlags.Strikethrough, 9, 29)]
    public void SingleFlagCodesMatchSource(CellStyleFlags flag, byte on, byte off)
    {
        Assert.Equal(new SgrCodes(on, off), AnsiBuilder.SgrCodesForFlag(flag));
    }

    [Fact]
    public void CompositeAndEmptyFlagsHaveNoSingleCode()
    {
        Assert.Null(AnsiBuilder.SgrCodesForFlag(CellStyleFlags.None));
        Assert.Null(AnsiBuilder.SgrCodesForFlag(CellStyleFlags.Bold | CellStyleFlags.Dim));
        Assert.Equal(8, AnsiBuilder.FlagTable.Length);
    }

    [Theory]
    [InlineData(CellStyleFlags.None, "")]
    [InlineData(CellStyleFlags.Bold, "\u001b[1m")]
    [InlineData(CellStyleFlags.Hidden, "\u001b[8m")]
    [InlineData(CellStyleFlags.Strikethrough, "\u001b[9m")]
    [InlineData(CellStyleFlags.Bold | CellStyleFlags.Italic, "\u001b[1;3m")]
    [InlineData((CellStyleFlags)255, "\u001b[1;2;3;4;5;7;8;9m")]
    public void SgrFlagsUseSourceOrdering(CellStyleFlags flags, string expected)
    {
        Assert.Equal(expected, AnsiBuilder.SgrFlags(flags));
    }

    [Theory]
    [InlineData(CellStyleFlags.None, CellStyleFlags.None, "", CellStyleFlags.None)]
    [InlineData(CellStyleFlags.Bold, CellStyleFlags.None, "\u001b[22m", CellStyleFlags.None)]
    [InlineData(CellStyleFlags.Dim, CellStyleFlags.Bold, "\u001b[22m", CellStyleFlags.Bold)]
    [InlineData(CellStyleFlags.Bold, CellStyleFlags.Dim, "\u001b[22m", CellStyleFlags.Dim)]
    [InlineData(CellStyleFlags.Bold | CellStyleFlags.Dim, CellStyleFlags.None, "\u001b[22m\u001b[22m", CellStyleFlags.None)]
    [InlineData(CellStyleFlags.Italic | CellStyleFlags.Underline, CellStyleFlags.None, "\u001b[23m\u001b[24m", CellStyleFlags.None)]
    [InlineData(CellStyleFlags.Hidden | CellStyleFlags.Strikethrough, CellStyleFlags.None, "\u001b[28m\u001b[29m", CellStyleFlags.None)]
    public void SgrFlagsOffMatchesEmissionAndCollateralRules(
        CellStyleFlags disable,
        CellStyleFlags keep,
        string expected,
        CellStyleFlags expectedCollateral)
    {
        var sequence = AnsiBuilder.SgrFlagsOff(disable, keep, out var collateral);

        Assert.Equal(expected, sequence);
        Assert.Equal(expectedCollateral, collateral);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 2, 3)]
    [InlineData(9, 10, 99)]
    [InlineData(100, 200, 255)]
    [InlineData(255, 255, 255)]
    public void TrueColorSequencesMatchReferenceFormatting(byte red, byte green, byte blue)
    {
        Assert.Equal($"\u001b[38;2;{red};{green};{blue}m", AnsiBuilder.ForegroundRgb(red, green, blue));
        Assert.Equal($"\u001b[48;2;{red};{green};{blue}m", AnsiBuilder.BackgroundRgb(red, green, blue));
    }

    [Fact]
    public void PackedTransparentUsesDefaultAndOpaqueUsesRgb()
    {
        Assert.Equal("\u001b[39m", AnsiBuilder.Foreground(PackedRgba.Transparent));
        Assert.Equal("\u001b[49m", AnsiBuilder.Background(PackedRgba.Transparent));
        Assert.Equal("\u001b[38;2;10;20;30m", AnsiBuilder.Foreground(PackedRgba.Rgb(10, 20, 30)));
        Assert.Equal("\u001b[48;2;10;20;30m", AnsiBuilder.Background(PackedRgba.Rgb(10, 20, 30)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(255)]
    public void Palette256SequencesCoverByteDomain(byte index)
    {
        Assert.Equal($"\u001b[38;5;{index}m", AnsiBuilder.Foreground256(index));
        Assert.Equal($"\u001b[48;5;{index}m", AnsiBuilder.Background256(index));
    }

    [Theory]
    [InlineData(0, 30, 40)]
    [InlineData(7, 37, 47)]
    [InlineData(8, 90, 100)]
    [InlineData(15, 97, 107)]
    public void Palette16BoundariesMatchSource(byte index, int foreground, int background)
    {
        Assert.Equal($"\u001b[{foreground}m", AnsiBuilder.Foreground16(index));
        Assert.Equal($"\u001b[{background}m", AnsiBuilder.Background16(index));
    }

    [Theory]
    [InlineData(0, 0, "\u001b[1;1H")]
    [InlineData(23, 79, "\u001b[24;80H")]
    [InlineData(65535, 65535, "\u001b[65536;65536H")]
    public void CursorPositionIsOneIndexed(ushort row, ushort column, string expected)
    {
        Assert.Equal(expected, AnsiBuilder.CursorPosition(row, column));
    }

    [Theory]
    [InlineData(0, "\u001b[1G")]
    [InlineData(79, "\u001b[80G")]
    [InlineData(65535, "\u001b[65536G")]
    public void ColumnPositionIsOneIndexed(ushort column, string expected)
    {
        Assert.Equal(expected, AnsiBuilder.ColumnPosition(column));
    }

    [Theory]
    [InlineData(0, "", "", "", "")]
    [InlineData(1, "\u001b[A", "\u001b[B", "\u001b[C", "\u001b[D")]
    [InlineData(42, "\u001b[42A", "\u001b[42B", "\u001b[42C", "\u001b[42D")]
    [InlineData(65535, "\u001b[65535A", "\u001b[65535B", "\u001b[65535C", "\u001b[65535D")]
    public void RelativeCursorSequencesMatchSource(
        ushort count,
        string up,
        string down,
        string forward,
        string backward)
    {
        Assert.Equal(up, AnsiBuilder.CursorUp(count));
        Assert.Equal(down, AnsiBuilder.CursorDown(count));
        Assert.Equal(forward, AnsiBuilder.CursorForward(count));
        Assert.Equal(backward, AnsiBuilder.CursorBackward(count));
    }

    [Fact]
    public void CursorAndRawLineMovementConstantsMatchSource()
    {
        Assert.Equal("\r", AnsiBuilder.CarriageReturn());
        Assert.Equal("\n", AnsiBuilder.LineFeed());
        Assert.Equal("\u001b7", AnsiBuilder.CursorSave());
        Assert.Equal("\u001b8", AnsiBuilder.CursorRestore());
        Assert.Equal("\u001b[?25l", AnsiBuilder.HideCursor());
        Assert.Equal("\u001b[?25h", AnsiBuilder.ShowCursor());
    }

    [Fact]
    public void EraseModesMatchSourceIncludingScrollback()
    {
        Assert.Equal("\u001b[K", AnsiBuilder.EraseLine(EraseLineMode.ToEnd));
        Assert.Equal("\u001b[1K", AnsiBuilder.EraseLine(EraseLineMode.ToStart));
        Assert.Equal("\u001b[2K", AnsiBuilder.EraseLine(EraseLineMode.All));
        Assert.Equal("\u001b[J", AnsiBuilder.EraseDisplay(EraseDisplayMode.ToEnd));
        Assert.Equal("\u001b[1J", AnsiBuilder.EraseDisplay(EraseDisplayMode.ToStart));
        Assert.Equal("\u001b[2J", AnsiBuilder.EraseDisplay(EraseDisplayMode.All));
        Assert.Equal("\u001b[3J", AnsiBuilder.EraseDisplay(EraseDisplayMode.Scrollback));
    }

    [Fact]
    public void InvalidManagedEraseValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiBuilder.EraseLine((EraseLineMode)255));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiBuilder.EraseDisplay((EraseDisplayMode)255));
    }

    [Fact]
    public void ScrollAndSynchronizedOutputSequencesMatchSource()
    {
        Assert.Equal("\u001b[1;24r", AnsiBuilder.SetScrollRegion(0, 23));
        Assert.Equal("\u001b[6;6r", AnsiBuilder.SetScrollRegion(5, 5));
        Assert.Equal("\u001b[r", AnsiBuilder.ResetScrollRegion());
        Assert.Equal("\u001b[?2026h", AnsiBuilder.SyncOutputBegin());
        Assert.Equal("\u001b[?2026l", AnsiBuilder.SyncOutputEnd());
    }

    [Fact]
    public void HyperlinksUseBelTerminationAndAllowSourceEmptyFields()
    {
        Assert.Equal("\u001b]8;;https://example.com\u0007", AnsiBuilder.HyperlinkStart("https://example.com"));
        Assert.Equal("\u001b]8;;\u0007", AnsiBuilder.HyperlinkStart(string.Empty));
        Assert.Equal("\u001b]8;;\u0007", AnsiBuilder.HyperlinkEnd());
        Assert.Equal("\u001b]8;id=link-1;https://example.com\u0007",
            AnsiBuilder.HyperlinkStartWithId("link-1", "https://example.com"));
        Assert.Equal("\u001b]8;id=;https://example.com\u0007",
            AnsiBuilder.HyperlinkStartWithId(string.Empty, "https://example.com"));
    }

    [Fact]
    public void HyperlinksRejectControlsParameterBreakoutAndMalformedUtf16()
    {
        foreach (var unsafeUrl in new[] { "a\u001bb", "a\nb", "\uD800", "a\uDC00b" })
        {
            Assert.Equal(string.Empty, AnsiBuilder.HyperlinkStart(unsafeUrl));
        }

        foreach (var unsafeId in new[] { "a;b", "a:b", "a=b", "a\u001bb", "\uD800" })
        {
            Assert.Equal(string.Empty, AnsiBuilder.HyperlinkStartWithId(unsafeId, "https://example.com"));
        }
    }

    [Fact]
    public void Osc8LimitIsMeasuredInUtf8Bytes()
    {
        Assert.NotEmpty(AnsiBuilder.HyperlinkStart(new string('a', 4096)));
        Assert.Empty(AnsiBuilder.HyperlinkStart(new string('a', 4097)));
        Assert.NotEmpty(AnsiBuilder.HyperlinkStart(string.Concat(Enumerable.Repeat("é", 2048))));
        Assert.Empty(AnsiBuilder.HyperlinkStart(string.Concat(Enumerable.Repeat("é", 2049))));
    }

    [Fact]
    public void ModeControlMatchesExactCurrentSourceConstants()
    {
        Assert.Equal("\u001b[?1049h", AnsiBuilder.AltScreenEnter());
        Assert.Equal("\u001b[?1049l", AnsiBuilder.AltScreenLeave());
        Assert.Equal("\u001b[?2004h", AnsiBuilder.BracketedPasteEnable());
        Assert.Equal("\u001b[?2004l", AnsiBuilder.BracketedPasteDisable());
        Assert.Equal("\u001b[?1004h", AnsiBuilder.FocusEnable());
        Assert.Equal("\u001b[?1004l", AnsiBuilder.FocusDisable());
        Assert.Contains("?1016l", AnsiBuilder.MouseEnable(), StringComparison.Ordinal);
        Assert.EndsWith("\u001b[?1002h", AnsiBuilder.MouseEnable(), StringComparison.Ordinal);
        Assert.EndsWith("\u001b[?1016l", AnsiBuilder.MouseDisable(), StringComparison.Ordinal);
    }

    [Fact]
    public void EveryControlSequenceIsAsciiAndOscSequencesUseBel()
    {
        var sequences = new[]
        {
            AnsiBuilder.SgrReset(),
            AnsiBuilder.SgrFlags((CellStyleFlags)255),
            AnsiBuilder.ForegroundRgb(255, 128, 0),
            AnsiBuilder.Background256(255),
            AnsiBuilder.CursorPosition(23, 79),
            AnsiBuilder.SetScrollRegion(0, 23),
            AnsiBuilder.HyperlinkStart("https://example.com"),
            AnsiBuilder.HyperlinkEnd(),
            AnsiBuilder.MouseEnable(),
            AnsiBuilder.MouseDisable(),
        };

        Assert.All(sequences, sequence => Assert.All(sequence, character => Assert.InRange((int)character, 0, 127)));
        Assert.EndsWith("\u0007", sequences[6], StringComparison.Ordinal);
        Assert.EndsWith("\u0007", sequences[7], StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingTargetCompatibilityHelpersRemainAvailable()
    {
        Assert.Equal("\u001b[>15u", AnsiBuilder.KittyKeyboardEnable());
        Assert.Equal("\u001b[<u", AnsiBuilder.KittyKeyboardDisable());
        Assert.Equal("A B", AnsiBuilder.SanitizeText("A\u001bB"));

        var builder = new StringBuilder();
        AnsiBuilder.AppendBestCursorMove(builder, 5, 3, 50, 3);
        Assert.DoesNotContain('H', builder.ToString());
    }
}
