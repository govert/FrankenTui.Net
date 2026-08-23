// SPDX-License-Identifier: MIT
// Behavioral parity cases for ftui-render::terminal_model at upstream 15cc6543.

using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class TerminalModelParityTests
{
    [Fact]
    public void ConstructionClampsDimensionsAndExposesInitializedState()
    {
        var model = new TerminalModel(0, 0);

        Assert.Equal((ushort)1, model.Width);
        Assert.Equal((ushort)1, model.Height);
        Assert.Equal(((ushort)0, (ushort)0), model.Cursor);
        Assert.Equal(1, model.Cells.Length);
        Assert.Equal(TerminalModel.ModelCell.Default, model.Cell(0, 0));
        Assert.True(model.CursorVisible);
        Assert.False(model.Modes.AlternateScreen);
        Assert.True(model.SyncOutputBalanced);
        Assert.False(model.HasDanglingLink);
    }

    [Fact]
    public void SourceDefaultModeDiffersFromInitializedMode()
    {
        var sgr = new SgrState(PackedRgba.Red, PackedRgba.Blue, CellStyleFlags.Bold);
        sgr.Reset();

        Assert.False(ModeFlags.Default.CursorVisible);
        Assert.True(ModeFlags.New().CursorVisible);
        Assert.Equal(SgrState.Default, sgr);
    }

    [Fact]
    public void ModelCellFactoriesCarrySourceDefaults()
    {
        var cell = TerminalModel.ModelCell.WithChar('X');

        Assert.Equal("X", cell.Text);
        Assert.Equal(PackedRgba.White, cell.Foreground);
        Assert.Equal(PackedRgba.Transparent, cell.Background);
        Assert.Equal(CellAttributes.None, cell.Attributes);
        Assert.Equal(0u, cell.LinkId);
        Assert.NotEqual(cell, TerminalModel.ModelCell.Default);
    }

    [Fact]
    public void PrintableTextWrapsImmediatelyAtRightEdge()
    {
        var model = new TerminalModel(5, 3);

        model.Process("ABCDEFGH");

        Assert.Equal("ABCDE", model.RowText(0));
        Assert.Equal("FGH", model.RowText(1));
        Assert.Equal(((ushort)3, (ushort)1), model.Cursor);
    }

    [Fact]
    public void BottomEdgeWrapReturnsToStartOfSameLineWithoutScrolling()
    {
        var model = new TerminalModel(3, 1);

        model.Process("ABCD");

        Assert.Equal("DBC", model.RowText(0));
        Assert.Equal(((ushort)1, (ushort)0), model.Cursor);
    }

    [Fact]
    public void Utf8SequenceCanSpanProcessCalls()
    {
        var model = new TerminalModel(10, 1);
        var bytes = Encoding.UTF8.GetBytes("é");

        model.Process(bytes.AsSpan(0, 1));
        Assert.Equal(string.Empty, model.RowText(0));

        model.Process(bytes.AsSpan(1));
        Assert.Equal("é", model.RowText(0));
        Assert.Equal(2ul, model.BytesProcessed);
    }

    [Fact]
    public void MultibyteRunesUseTerminalColumnWidths()
    {
        var model = new TerminalModel(10, 1);

        model.Process("aé中😀");

        Assert.Equal("aé中😀", model.RowText(0));
        Assert.Equal(((ushort)6, (ushort)0), model.Cursor);
        Assert.Equal(string.Empty, model.Cell(3, 0)!.Value.Text);
        Assert.Equal(string.Empty, model.Cell(5, 0)!.Value.Text);
    }

    [Fact]
    public void InvalidUtf8IsReplacedAndInterruptedPendingSequenceIsFlushed()
    {
        var model = new TerminalModel(10, 1);

        model.Process([0xff, 0xe2]);
        model.Process([0x1b, (byte)'Q', (byte)'A']);

        Assert.Equal("��A", model.RowText(0));
    }

    [Fact]
    public void CombiningMarkAttachesToPreviousCell()
    {
        var model = new TerminalModel(10, 1);

        model.Process("e\u0301");

        Assert.Equal("e\u0301", model.Cell(0, 0)!.Value.Text);
        Assert.Equal(((ushort)1, (ushort)0), model.Cursor);
    }

    [Fact]
    public void CombiningMarkAtLineStartRetainsPaddingSpace()
    {
        var model = new TerminalModel(10, 1);

        model.Process("\u0301");

        Assert.Equal(" \u0301", model.Cell(0, 0)!.Value.Text);
        Assert.Equal(((ushort)0, (ushort)0), model.Cursor);
    }

    [Fact]
    public void WideRuneClearsTailCellWithoutPropagatingLinkOrStyle()
    {
        var model = new TerminalModel(10, 1);

        model.Process("\u001b[1m\u001b]8;;https://example.test\a中");

        var lead = model.Cell(0, 0)!.Value;
        var tail = model.Cell(1, 0)!.Value;
        Assert.Equal("中", lead.Text);
        Assert.True(lead.Attributes.HasFlag(CellStyleFlags.Bold));
        Assert.NotEqual(0u, lead.LinkId);
        Assert.Equal(string.Empty, tail.Text);
        Assert.Equal(CellAttributes.None, tail.Attributes);
        Assert.Equal(0u, tail.LinkId);
        Assert.Equal(lead.Foreground, tail.Foreground);
        Assert.Equal(lead.Background, tail.Background);
    }

    [Fact]
    public void CursorPositioningAndRelativeMovesMatchAnsiDefaultsAndClamps()
    {
        var model = new TerminalModel(10, 5);

        model.Process("\u001b[999;999H");
        Assert.Equal(((ushort)9, (ushort)4), model.Cursor);
        model.Process("\u001b[50A\u001b[50D");
        Assert.Equal(((ushort)0, (ushort)0), model.Cursor);
        model.Process("\u001b[2B\u001b[3C");
        Assert.Equal(((ushort)3, (ushort)2), model.Cursor);
        model.Process("\u001b[H");
        Assert.Equal(((ushort)0, (ushort)0), model.Cursor);
    }

    [Theory]
    [InlineData("\u001b[20G", 9, 0)]
    [InlineData("\u001b[10d", 0, 4)]
    [InlineData("\u001b[3;7f", 6, 2)]
    [InlineData("\u001b[;H", 0, 0)]
    public void AbsoluteCursorCommandsAreOneBasedAndClamped(string sequence, ushort x, ushort y)
    {
        var model = new TerminalModel(10, 5);

        model.Process(sequence);

        Assert.Equal((x, y), model.Cursor);
    }

    [Fact]
    public void C0ControlsMoveCursorWithinBounds()
    {
        var model = new TerminalModel(10, 3);

        model.Process("ABC\b\t\t\r\n");

        Assert.Equal(((ushort)0, (ushort)1), model.Cursor);
        model.Process("\u001b[3;1H\n");
        Assert.Equal(((ushort)0, (ushort)2), model.Cursor);
    }

    [Theory]
    [InlineData(1, CellStyleFlags.Bold)]
    [InlineData(2, CellStyleFlags.Dim)]
    [InlineData(3, CellStyleFlags.Italic)]
    [InlineData(4, CellStyleFlags.Underline)]
    [InlineData(5, CellStyleFlags.Blink)]
    [InlineData(7, CellStyleFlags.Reverse)]
    [InlineData(8, CellStyleFlags.Hidden)]
    [InlineData(9, CellStyleFlags.Strikethrough)]
    public void SgrSetsEverySourceStyleFlag(int code, CellStyleFlags flag)
    {
        var model = new TerminalModel(4, 1);

        model.Process($"\u001b[{code}mX");

        Assert.True(model.Cell(0, 0)!.Value.Attributes.HasFlag(flag));
    }

    [Theory]
    [InlineData("1;22", CellStyleFlags.Bold)]
    [InlineData("2;21", CellStyleFlags.Dim)]
    [InlineData("3;23", CellStyleFlags.Italic)]
    [InlineData("4;24", CellStyleFlags.Underline)]
    [InlineData("5;25", CellStyleFlags.Blink)]
    [InlineData("7;27", CellStyleFlags.Reverse)]
    [InlineData("8;28", CellStyleFlags.Hidden)]
    [InlineData("9;29", CellStyleFlags.Strikethrough)]
    public void SgrSelectiveOffCodesRemoveTheirFlags(string codes, CellStyleFlags flag)
    {
        var model = new TerminalModel(4, 1);

        model.Process($"\u001b[{codes}mX");

        Assert.False(model.Cell(0, 0)!.Value.Attributes.HasFlag(flag));
    }

    [Theory]
    [InlineData(30, 0, 0, 0)]
    [InlineData(31, 128, 0, 0)]
    [InlineData(32, 0, 128, 0)]
    [InlineData(33, 128, 128, 0)]
    [InlineData(34, 0, 0, 128)]
    [InlineData(35, 128, 0, 128)]
    [InlineData(36, 0, 128, 128)]
    [InlineData(37, 192, 192, 192)]
    [InlineData(90, 128, 128, 128)]
    [InlineData(91, 255, 0, 0)]
    [InlineData(97, 255, 255, 255)]
    public void BasicAndBrightForegroundPalettesMatchSource(int code, byte red, byte green, byte blue)
    {
        var model = new TerminalModel(4, 1);

        model.Process($"\u001b[{code}mX");

        Assert.Equal(PackedRgba.Rgb(red, green, blue), model.Cell(0, 0)!.Value.Foreground);
    }

    [Theory]
    [InlineData(1, 128, 0, 0)]
    [InlineData(9, 255, 0, 0)]
    [InlineData(16, 0, 0, 0)]
    [InlineData(196, 255, 0, 0)]
    [InlineData(232, 8, 8, 8)]
    [InlineData(255, 238, 238, 238)]
    public void Color256PaletteMatchesSource(int index, byte red, byte green, byte blue)
    {
        var model = new TerminalModel(4, 1);

        model.Process($"\u001b[38;5;{index}mX");

        Assert.Equal(PackedRgba.Rgb(red, green, blue), model.Cell(0, 0)!.Value.Foreground);
    }

    [Theory]
    [InlineData("38;2;100;150;200")]
    [InlineData("38:2:100:150:200")]
    public void RgbColorAcceptsSemicolonAndColonParameterForms(string parameters)
    {
        var model = new TerminalModel(4, 1);

        model.Process($"\u001b[{parameters}mX");

        Assert.Equal(PackedRgba.Rgb(100, 150, 200), model.Cell(0, 0)!.Value.Foreground);
    }

    [Fact]
    public void ForegroundAndBackgroundDefaultsAndEmptySgrReset()
    {
        var model = new TerminalModel(8, 1);

        model.Process("\u001b[31;42mA\u001b[39;49mB\u001b[1m\u001b[mC");

        Assert.Equal(PackedRgba.Rgb(128, 0, 0), model.Cell(0, 0)!.Value.Foreground);
        Assert.Equal(PackedRgba.Rgb(0, 128, 0), model.Cell(0, 0)!.Value.Background);
        Assert.Equal(PackedRgba.White, model.Cell(1, 0)!.Value.Foreground);
        Assert.Equal(PackedRgba.Transparent, model.Cell(1, 0)!.Value.Background);
        Assert.Equal(CellStyleFlags.None, model.Cell(2, 0)!.Value.Attributes.Flags);
    }

    [Theory]
    [InlineData(0, "ABCD")]
    [InlineData(1, "     FGHIJ")]
    [InlineData(2, "")]
    public void EraseLineModesUseInclusiveSourceRanges(int mode, string expected)
    {
        var model = new TerminalModel(10, 1);
        model.Process("ABCDEFGHIJ\u001b[1;5H");

        model.Process($"\u001b[{mode}K");

        Assert.Equal(expected, model.RowText(0));
    }

    [Theory]
    [InlineData(0, "Line1", "Li", "")]
    [InlineData(1, "", "   e2", "Line3")]
    [InlineData(2, "", "", "")]
    [InlineData(3, "", "", "")]
    public void EraseDisplayModesMatchSource(int mode, string row0, string row1, string row2)
    {
        var model = new TerminalModel(10, 3);
        model.Process("Line1\r\nLine2\r\nLine3\u001b[2;3H");

        model.Process($"\u001b[{mode}J");

        Assert.Equal(row0, model.RowText(0));
        Assert.Equal(row1, model.RowText(1));
        Assert.Equal(row2, model.RowText(2));
    }

    [Fact]
    public void EraseUsesCurrentBackgroundAndClearsAttributesAndLinks()
    {
        var model = new TerminalModel(5, 1);
        model.Process("\u001b[1m\u001b]8;;https://example.test\aHello\u001b[1;1H\u001b[41m\u001b[K");

        var cell = model.Cell(0, 0)!.Value;
        Assert.Equal(" ", cell.Text);
        Assert.Equal(PackedRgba.Rgb(128, 0, 0), cell.Background);
        Assert.Equal(PackedRgba.White, cell.Foreground);
        Assert.Equal(CellAttributes.None, cell.Attributes);
        Assert.Equal(0u, cell.LinkId);
    }

    [Fact]
    public void HyperlinksSupportBelAndStAndAllocateDistinctIds()
    {
        var model = new TerminalModel(30, 1);

        model.Process("\u001b]8;;https://a.test\aA\u001b]8;;\a");
        model.Process("\u001b]8;;https://b.test\u001b\\B\u001b]8;;\u001b\\");

        var a = model.Cell(0, 0)!.Value;
        var b = model.Cell(1, 0)!.Value;
        Assert.NotEqual(a.LinkId, b.LinkId);
        Assert.Equal(a.LinkId, a.Attributes.LinkId);
        Assert.Equal("https://a.test", model.LinkUrl(a.LinkId));
        Assert.Equal("https://b.test", model.LinkUrl(b.LinkId));
        Assert.Equal(string.Empty, model.LinkUrl(0));
        Assert.Null(model.LinkUrl(999));
        Assert.False(model.HasDanglingLink);
    }

    [Fact]
    public void DecModesTrackCursorAlternateScreenAndNestedSyncOutput()
    {
        var model = new TerminalModel(10, 1);

        model.Process("\u001b[?25l\u001b[?1049h\u001b[?2026h\u001b[?2026h");
        Assert.False(model.CursorVisible);
        Assert.True(model.Modes.AlternateScreen);
        Assert.Equal(2u, model.Modes.SynchronizedOutputLevel);
        model.Process("\u001b[?25h\u001b[?1049l\u001b[?2026l\u001b[?2026l\u001b[?2026l");
        Assert.True(model.CursorVisible);
        Assert.False(model.Modes.AlternateScreen);
        Assert.True(model.SyncOutputBalanced);
    }

    [Fact]
    public void UnknownAndIgnoredSequencesDoNotCorruptState()
    {
        var model = new TerminalModel(12, 3);

        model.Process("AB\u001b[1;2r\u001b[99X\u001b=\u001b>CD");

        Assert.Equal("ABCD", model.RowText(0));
        Assert.Equal(((ushort)4, (ushort)0), model.Cursor);
    }

    [Fact]
    public void DoubleEscapeConsumesFirstFollowingByteAsSourceDoes()
    {
        var model = new TerminalModel(10, 1);

        model.Process("\u001b\u001bAB");

        Assert.Equal("B", model.RowText(0));
    }

    [Fact]
    public void CsiEntryIntermediateReturnsToGroundAndPrintsFinalByte()
    {
        var model = new TerminalModel(10, 1);

        model.Process("\u001b[ qOK");

        Assert.Equal("qOK", model.RowText(0));
    }

    [Fact]
    public void ResetClearsVisualAndParserStateButPreservesDimensionsAndByteCounter()
    {
        var model = new TerminalModel(20, 5);
        model.Process("\u001b[2;3H\u001b[1m\u001b]8;;https://example.test\aX\u001b[?2026h");
        var processed = model.BytesProcessed;

        model.Reset();

        Assert.Equal((ushort)20, model.Width);
        Assert.Equal((ushort)5, model.Height);
        Assert.Equal(((ushort)0, (ushort)0), model.Cursor);
        Assert.Equal(SgrState.Default, model.SgrState);
        Assert.Equal(ModeFlags.New(), model.Modes);
        Assert.Equal(processed, model.BytesProcessed);
        Assert.All(model.Cells.ToArray(), cell => Assert.Equal(TerminalModel.ModelCell.Default, cell));
    }

    [Fact]
    public void RowAndCellExposeTypedOutOfBoundsAbsence()
    {
        var model = new TerminalModel(5, 3);

        Assert.Null(model.Cell(5, 0));
        Assert.Null(model.Cell(0, 3));
        Assert.Null(model.Row(3));
        Assert.False(model.TryRowText(3, out var missing));
        Assert.Equal(string.Empty, missing);
        Assert.Equal(string.Empty, model.RowText(3));
    }

    [Fact]
    public void RowTextTrimsOnlyPaddingSpaces()
    {
        var model = new TerminalModel(10, 1);

        model.Process("Hi\u00a0");

        Assert.Equal("Hi\u00a0", model.RowText(0));
    }

    [Fact]
    public void CurrentCellAndReadOnlyRowsExposeGridProjection()
    {
        var model = new TerminalModel(10, 2);
        model.Process("AB");

        Assert.Equal(" ", model.CurrentCell!.Value.Text);
        Assert.Equal(10, model.Row(0)!.Value.Length);
        Assert.Equal("A", model.Row(0)!.Value.Span[0].Text);
    }

    [Fact]
    public void DiffGridClassifiesIdentityContentAndSizeMismatch()
    {
        var model = new TerminalModel(3, 2);
        var expected = Enumerable.Repeat(TerminalModel.ModelCell.Default, 6).ToArray();

        Assert.Null(model.DiffGrid(expected));
        model.Process("A");
        Assert.Contains("Grid differences", model.DiffGrid(expected));
        Assert.Contains("(0, 0)", model.DiffGrid(expected));
        Assert.Contains("Grid size mismatch", model.DiffGrid(expected.AsSpan(0, 5)));
    }

    [Fact]
    public void DumpSequencesMatchesSourceReadableProjection()
    {
        var bytes = Encoding.UTF8.GetBytes("\u001b[1;1H\u001b[1mHi\u001b[0m\u001b]8;;u\a\b\t\n\u001b");

        var dump = TerminalModel.DumpSequences(bytes);

        Assert.Equal("\\e[1;1H\\e[1mHi\\e[0m\\e]8;;u\\a\\x08\\x09\\x0a\\e", dump);
    }

    [Fact]
    public void EscSaveRestoreRemainsManagedCompatibilityExtension()
    {
        var model = new TerminalModel(6, 2);

        model.Process("A\u001b7\u001b[2;1HZ\u001b8B");

        Assert.Equal("AB", model.RowText(0));
        Assert.Equal("Z", model.RowText(1));
    }
}
