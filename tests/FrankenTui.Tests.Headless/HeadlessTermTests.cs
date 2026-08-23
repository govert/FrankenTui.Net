using System.Reflection;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Behavioral projection of all 42 tests in upstream ftui-render/headless.rs at
/// commit 15cc6543f76b814394c590f9e7719dedd6684e4c.
/// </summary>
public sealed class HeadlessTermTests
{
    [Fact]
    public void ConstructorCreatesBlankScreenAndRejectsZeroDimensions()
    {
        var term = new HeadlessTerm(80, 24);

        Assert.Equal((ushort)80, term.Width);
        Assert.Equal((ushort)24, term.Height);
        Assert.Equal(((ushort)0, (ushort)0), term.Cursor);
        Assert.Equal(24, term.ScreenText().Count);
        Assert.All(term.ScreenText(), line => Assert.Empty(line));
        Assert.Empty(term.RowText(24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeadlessTerm(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeadlessTerm(1, 0));
    }

    [Fact]
    public void ProcessSupportsTextCupAndCapturedBytes()
    {
        var term = new HeadlessTerm(20, 5);
        term.Process("Hello, world!");
        term.Process(Bytes("\u001b[2;3HTest"));

        Assert.Equal("Hello, world!", term.RowText(0));
        Assert.Equal("  Test", term.RowText(1));
        Assert.Equal(((ushort)6, (ushort)1), term.Cursor);
        Assert.Equal(Bytes("Hello, world!\u001b[2;3HTest"), term.CapturedOutput.ToArray());
    }

    [Fact]
    public void ScreenTextAndStringReturnEveryTrimmedRowWithLfSeparators()
    {
        var term = new HeadlessTerm(10, 3);
        term.Process("\u001b[1;1HLine 1");
        term.Process("\u001b[2;1HLine 2");
        term.Process("\u001b[3;1HLine 3");

        Assert.Equal(["Line 1", "Line 2", "Line 3"], term.ScreenText());
        Assert.Equal("Line 1\nLine 2\nLine 3", term.ScreenString());

        var sparse = new HeadlessTerm(10, 3);
        sparse.Process("\u001b[1;1HAB\u001b[2;1HCD");
        Assert.Equal("AB\nCD\n", sparse.ScreenString());
    }

    [Fact]
    public void AssertionHelpersAcceptMatchesAndTrimExpectedTrailingWhitespace()
    {
        var term = new HeadlessTerm(10, 3);
        term.Process("\u001b[1;1HHello\u001b[3;1HWorld");

        term.AssertMatches(["Hello   ", "", "World"]);
        term.AssertRow(0, "Hello  ");
        term.AssertCursor(5, 2);
    }

    [Fact]
    public void AssertionHelpersExposeSourceShapedFailureDiagnostics()
    {
        var term = new HeadlessTerm(10, 3);
        term.Process("Hello");

        var content = Assert.Throws<InvalidOperationException>(
            () => term.AssertMatches(["Wrong", "", ""]));
        Assert.Contains("screen content mismatch", content.Message);
        Assert.Contains("first difference at column 0", content.Message);

        var count = Assert.Throws<InvalidOperationException>(
            () => term.AssertMatches(["", ""]));
        Assert.Contains("line count mismatch", count.Message);

        var row = Assert.Throws<InvalidOperationException>(() => term.AssertRow(0, "World"));
        Assert.Contains("row 0 mismatch", row.Message);

        var cursor = Assert.Throws<InvalidOperationException>(() => term.AssertCursor(5, 5));
        Assert.Contains("cursor position mismatch", cursor.Message);
    }

    [Fact]
    public void DiffReturnsNullForMatchAndDetailsForContentOrLineCountMismatch()
    {
        var term = new HeadlessTerm(10, 3);
        term.Process("\u001b[1;1HHello\u001b[3;1HWorld");

        Assert.Null(term.Diff(["Hello", "", "World"]));

        var content = Assert.IsType<ScreenDiff>(term.Diff(["Hello", "X", "World"]));
        var line = Assert.Single(content.Mismatches);
        Assert.Equal(1, line.Line);
        Assert.Equal(string.Empty, line.Got);
        Assert.Equal("X", line.Want);

        var count = Assert.IsType<ScreenDiff>(term.Diff(["Hello", ""]));
        Assert.Equal(3, count.ActualLines);
        Assert.Equal(2, count.ExpectedLines);
        Assert.Contains("Line count: got 3, expected 2", count.ToString());
    }

    [Fact]
    public void ScreenDiffFormatsCharacterAndLengthHintsAndClonesDeeply()
    {
        var character = new ScreenDiff(1, 1, [new LineDiff(0, "ABCXEF", "ABCDEF")]);
        Assert.Contains("line 0", character.ToString());
        Assert.Contains("first difference at column 3", character.ToString());

        var length = new ScreenDiff(1, 1, [new LineDiff(0, "ABC", "ABCDEF")]);
        Assert.Contains("diverges at column 3", length.ToString());

        var clone = character.Clone();
        clone.Mismatches[0].Got = "changed";
        Assert.Equal("ABCXEF", character.Mismatches[0].Got);
        Assert.Equal("ABCXEF", character.Mismatches[0].Clone().Got);
    }

    [Fact]
    public void ResetClearsGridCursorParserStyleLinksAndCapturedOutputInPlace()
    {
        var term = new HeadlessTerm(10, 3);
        var model = term.Model;
        term.Process("\u001b[1m\u001b]8;;https://example.com\aHello");
        term.Reset();

        Assert.Same(model, term.Model);
        Assert.Equal(((ushort)0, (ushort)0), term.Cursor);
        Assert.Empty(term.CapturedOutput.ToArray());
        Assert.All(term.ScreenText(), line => Assert.Empty(line));
        Assert.Equal(0u, term.Model.ActiveLinkId);
        Assert.Equal(CellStyleFlags.None, term.Model.Cell(0, 0)!.Value.Attributes.Flags);
    }

    [Fact]
    public void ExportStringAndFileContainDimensionsContentCursorAndAnsiDump()
    {
        var term = new HeadlessTerm(20, 5);
        term.Process("\u001b[1;1HExported content");

        var inline = term.ExportString();
        Assert.Contains("20x5 cursor=(16,0)", inline);
        Assert.Contains("  0| Exported content", inline);

        var path = Path.Combine(Path.GetTempPath(), $"ftui_headless_{Guid.NewGuid():N}.txt");
        try
        {
            term.Export(path);
            var contents = File.ReadAllText(path);
            Assert.Contains("HeadlessTerm Export", contents);
            Assert.Contains("Size: 20x5", contents);
            Assert.Contains("Captured output: 22 bytes", contents);
            Assert.Contains("Exported content", contents);
            Assert.Contains("ANSI Dump", contents);
            Assert.Contains("\\e[1;1HExported content", contents);
            Assert.DoesNotContain("\r\n", contents);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TerminalModelDumpSequencesCoversCsiOscControlsAndEdgeCases()
    {
        Assert.Equal("\\e[1;1H\\e[1mHello\\e[0m",
            TerminalModel.DumpSequences(Bytes("\u001b[1;1H\u001b[1mHello\u001b[0m")));
        Assert.Equal("\\e]8;;https://example.com\\aLink\\e]8;;\\a",
            TerminalModel.DumpSequences(Bytes("\u001b]8;;https://example.com\aLink\u001b]8;;\a")));
        Assert.Equal("\\e]0;title\\e\\\\",
            TerminalModel.DumpSequences(Bytes("\u001b]0;title\u001b\\")));
        Assert.Equal("\\x08\\x09\\x0a", TerminalModel.DumpSequences([0x08, 0x09, 0x0a]));
        Assert.Equal("text\\e", TerminalModel.DumpSequences(Bytes("text\u001b")));
        Assert.Equal("\\eQ", TerminalModel.DumpSequences(Bytes("\u001bQ")));
    }

    [Fact]
    public void SgrAndHyperlinksAreTrackedThroughExposedModel()
    {
        var styled = new HeadlessTerm(20, 5);
        styled.Process("\u001b[1;31mBold Red\u001b[0m");
        Assert.Equal("Bold Red", styled.RowText(0));
        Assert.True(styled.Model.Cell(0, 0)!.Value.Attributes.Flags.HasFlag(CellStyleFlags.Bold));

        var linked = new HeadlessTerm(20, 5);
        linked.Process("\u001b]8;;https://example.com\aLink\u001b]8;;\a");
        var cell = linked.Model.Cell(0, 0)!.Value;
        Assert.Equal("Link", linked.RowText(0));
        Assert.NotEqual(0u, cell.Attributes.LinkId);
        Assert.Equal("https://example.com", linked.Model.LinkUrl(cell.Attributes.LinkId));
        Assert.Equal(0u, linked.Model.ActiveLinkId);
    }

    [Fact]
    public void MultilineEraseAndPendingWrapMatchTerminalBehavior()
    {
        var multiline = new HeadlessTerm(20, 5);
        multiline.Process("Line 1\r\nLine 2\r\nLine 3");
        multiline.AssertMatches(["Line 1", "Line 2", "Line 3", "", ""]);

        var erased = new HeadlessTerm(10, 3);
        erased.Process("XXXXXXXXXX\u001b[1;1H\u001b[2J");
        erased.AssertMatches(["", "", ""]);

        var wrapped = new HeadlessTerm(5, 3);
        wrapped.Process("ABCDEFGH");
        Assert.Equal("ABCDE", wrapped.RowText(0));
        Assert.Equal("FGH", wrapped.RowText(1));
    }

    [Fact]
    public void PresenterOutputRoundTripsThroughHeadlessTerm()
    {
        var previous = new RenderBuffer(10, 3);
        var next = new RenderBuffer(10, 3);
        Write(next, "Hello");

        var result = new Presenter(TerminalCapabilities.Modern())
            .Present(next, BufferDiff.Compute(previous, next));
        var term = new HeadlessTerm(10, 3);
        term.Process(result.Output);

        term.AssertRow(0, "Hello");
    }

    [Fact]
    public void PresenterIncrementalUpdateRoundTripsThroughSameTerm()
    {
        var empty = new RenderBuffer(10, 3);
        var hello = new RenderBuffer(10, 3);
        Write(hello, "Hello");
        var presenter = new Presenter(TerminalCapabilities.Modern());
        var term = new HeadlessTerm(10, 3);

        term.Process(presenter.Present(hello, BufferDiff.Compute(empty, hello)).Output);
        term.AssertRow(0, "Hello");

        var world = new RenderBuffer(10, 3);
        Write(world, "World");
        term.Process(presenter.Present(world, BufferDiff.Compute(hello, world)).Output);
        term.AssertRow(0, "World");
    }

    [Fact]
    public void CursorRelativeMovementSupportsEveryDirectionDefaultsAndComposition()
    {
        var term = new HeadlessTerm(20, 10);
        term.Process("\u001b[5;5H");
        term.AssertCursor(4, 4);
        term.Process("\u001b[2A");
        term.AssertCursor(4, 2);
        term.Process("\u001b[3B");
        term.AssertCursor(4, 5);
        term.Process("\u001b[5C");
        term.AssertCursor(9, 5);
        term.Process("\u001b[4D");
        term.AssertCursor(5, 5);

        term.Process("\u001b[A\u001b[C\u001b[B\u001b[D");
        term.AssertCursor(5, 5);

        term.Process("\u001b[1;1H\u001b[3C\u001b[2B\u001b[1D\u001b[1A");
        term.AssertCursor(2, 1);
    }

    [Fact]
    public void CursorMovementClampsAndAbsoluteColumnAndRowAreOneBased()
    {
        var term = new HeadlessTerm(20, 10);

        term.Process("\u001b[1;1H\u001b[99A\u001b[99D");
        term.AssertCursor(0, 0);
        term.Process("\u001b[10;20H\u001b[99B\u001b[99C");
        term.AssertCursor(19, 9);
        term.Process("\u001b[3;1H\u001b[8G");
        term.AssertCursor(7, 2);
        term.Process("\u001b[1;5H\u001b[6d");
        term.AssertCursor(4, 5);
    }

    [Fact]
    public void CursorMoveThenWritePreservesExistingRows()
    {
        var term = new HeadlessTerm(20, 5);
        term.Process("\u001b[3;1HABC\u001b[2AXY");

        term.AssertRow(0, "   XY");
        term.AssertRow(2, "ABC");
    }

    [Fact]
    public void PublicDenominatorContainsAllSourceTypesFieldsAndOperations()
    {
        var assembly = typeof(HeadlessTerm).Assembly;
        Assert.NotNull(assembly.GetType("FrankenTui.Render.HeadlessTerm"));
        Assert.NotNull(assembly.GetType("FrankenTui.Render.LineDiff"));
        Assert.NotNull(assembly.GetType("FrankenTui.Render.ScreenDiff"));

        var headlessProperties = typeof(HeadlessTerm).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Subset(headlessProperties, new HashSet<string>(
            [nameof(HeadlessTerm.Width), nameof(HeadlessTerm.Height), nameof(HeadlessTerm.Cursor),
             nameof(HeadlessTerm.Model), nameof(HeadlessTerm.CapturedOutput)], StringComparer.Ordinal));

        var headlessMethods = typeof(HeadlessTerm).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Subset(headlessMethods, new HashSet<string>(
            [nameof(HeadlessTerm.Process), nameof(HeadlessTerm.RowText), nameof(HeadlessTerm.ScreenText),
             nameof(HeadlessTerm.ScreenString), nameof(HeadlessTerm.Reset), nameof(HeadlessTerm.AssertMatches),
             nameof(HeadlessTerm.AssertRow), nameof(HeadlessTerm.AssertCursor), nameof(HeadlessTerm.Diff),
             nameof(HeadlessTerm.Export), nameof(HeadlessTerm.ExportString)], StringComparer.Ordinal));

        Assert.Equal([nameof(LineDiff.Got), nameof(LineDiff.Line), nameof(LineDiff.Want)],
            typeof(LineDiff).GetProperties().Select(property => property.Name).Order());
        Assert.Equal([nameof(ScreenDiff.ActualLines), nameof(ScreenDiff.ExpectedLines), nameof(ScreenDiff.Mismatches)],
            typeof(ScreenDiff).GetProperties().Select(property => property.Name).Order());
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static void Write(RenderBuffer buffer, string text)
    {
        for (ushort column = 0; column < text.Length; column++)
        {
            buffer.Set(column, 0, Cell.FromChar(text[column]));
        }
    }
}
