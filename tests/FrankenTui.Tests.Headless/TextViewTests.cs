// SPDX-License-Identifier: Apache-2.0
// Source-equivalent tests for .external/frankentui/crates/ftui-text/src/view.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Text;
using FrankenTui.Widgets.TextAreaInternals;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Closed source denominator: the first 48 facts correspond one-for-one with
/// the 48 upstream view.rs tests. Remaining facts cover managed coordinate,
/// ownership, and compatibility adaptations.
/// </summary>
public sealed class TextViewTests
{
    [Fact]
    public void ViewBasicCounts()
    {
        var view = new TextView("a\nbb", 10, TextWrapMode.None);
        Assert.Equal(2, view.SourceLineCount);
        Assert.Equal(2, view.VirtualLineCount);
        Assert.Equal(2, view.MaxWidth);
    }

    [Fact]
    public void ViewWrapsWord()
    {
        var view = new TextView("hello world", 5, TextWrapMode.Word);
        Assert.Equal(new[] { "hello", "world" }, view.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void ViewWrapsCjkByCells()
    {
        var view = new TextView("你好世界", 4, TextWrapMode.Char);
        Assert.Equal(new[] { "你好", "世界" }, view.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void ViewStripsCrlf()
    {
        var view = new TextView("a\r\nb", 10, TextWrapMode.None);
        Assert.Equal(new[] { "a", "b" }, view.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void VisibleRangeClampsScroll()
    {
        var view = new TextView("a\nb\nc", 10, TextWrapMode.None);
        AssertRange(view.VisibleRange(5, 2), 1, 3);
    }

    [Fact]
    public void ScrollToLineClamps()
    {
        var view = new TextView("a\nb\nc\nd", 10, TextWrapMode.None);
        Assert.Equal(2, view.ScrollToLine(3, 2));
    }

    [Fact]
    public void ScrollByPagesMovesInViewportSteps()
    {
        var view = new TextView("1\n2\n3\n4\n5", 10, TextWrapMode.None);
        int scroll = view.ScrollByPages(0, 1, 2);
        Assert.Equal(2, scroll);
        Assert.Equal(0, view.ScrollByPages(scroll, -1, 2));
    }

    [Fact]
    public void ScrollToBottomRespectsViewport()
    {
        var view = new TextView("a\nb\nc\nd", 10, TextWrapMode.None);
        Assert.Equal(2, view.ScrollToBottom(2));
        Assert.Equal(0, view.ScrollToTop());
    }

    [Fact]
    public void VisibleLinesReturnsSlice()
    {
        var view = new TextView("a\nb\nc\nd", 10, TextWrapMode.None);
        Assert.Equal(new[] { "b", "c" }, view.VisibleLines(1, 2).Select(static line => line.Text));
    }

    [Fact]
    public void ViewportStructIsCopyable()
    {
        var viewport = new Viewport(80, 24);
        Viewport copy = viewport;
        Assert.Equal(80, copy.Width);
        Assert.Equal(24, copy.Height);
    }

    [Fact]
    public void EmptyTextView()
    {
        var view = new TextView(string.Empty, 10, TextWrapMode.None);
        Assert.Equal(1, view.SourceLineCount);
        Assert.Equal(1, view.VirtualLineCount);
        Assert.Equal(0, view.MaxWidth);
    }

    [Fact]
    public void EmptyTextScroll()
    {
        var view = new TextView(string.Empty, 10, TextWrapMode.None);
        Assert.Equal(0, view.MaxScroll(5));
        Assert.Equal(0, view.ClampScroll(100, 5));
        AssertRange(view.VisibleRange(0, 5), 0, 1);
    }

    [Fact]
    public void SourceToVirtualNoWrap()
    {
        var view = new TextView("a\nb\nc", 10, TextWrapMode.None);
        Assert.Equal(0, view.SourceToVirtual(0));
        Assert.Equal(1, view.SourceToVirtual(1));
        Assert.Equal(2, view.SourceToVirtual(2));
        Assert.Null(view.SourceToVirtual(3));
    }

    [Fact]
    public void VirtualToSourceNoWrap()
    {
        var view = new TextView("a\nb\nc", 10, TextWrapMode.None);
        Assert.Equal(0, view.VirtualToSource(0));
        Assert.Equal(1, view.VirtualToSource(1));
        Assert.Equal(2, view.VirtualToSource(2));
        Assert.Null(view.VirtualToSource(3));
    }

    [Fact]
    public void SourceToVirtualWithWrap()
    {
        var view = new TextView("abcde\nxy", 3, TextWrapMode.Char);
        Assert.Equal(0, view.SourceToVirtual(0));
        Assert.Equal(2, view.SourceToVirtual(1));
    }

    [Fact]
    public void VirtualToSourceWithWrap()
    {
        var view = new TextView("abcde\nxy", 3, TextWrapMode.Char);
        Assert.Equal(0, view.VirtualToSource(0));
        Assert.Equal(0, view.VirtualToSource(1));
        Assert.Equal(1, view.VirtualToSource(2));
    }

    [Fact]
    public void IsWrapFlagSetCorrectly()
    {
        var view = new TextView("abcde", 3, TextWrapMode.Char);
        Assert.False(view.Lines[0].IsWrap);
        Assert.True(view.Lines[1].IsWrap);
    }

    [Fact]
    public void SetTextRecomputes()
    {
        var view = new TextView("abc", 10, TextWrapMode.None);
        Assert.Equal(1, view.SourceLineCount);
        view.SetText("a\nb\nc");
        Assert.Equal(3, view.SourceLineCount);
        Assert.Equal(3, view.VirtualLineCount);
    }

    [Fact]
    public void SetWrapRecomputes()
    {
        var view = new TextView("hello world", 5, TextWrapMode.None);
        int before = view.VirtualLineCount;
        view.SetWrap(TextWrapMode.Word);
        Assert.True(view.VirtualLineCount >= before);
    }

    [Fact]
    public void SetWrapSameModeIsNoop()
    {
        var view = new TextView("hello", 10, TextWrapMode.None);
        int count = view.VirtualLineCount;
        view.SetWrap(TextWrapMode.None);
        Assert.Equal(count, view.VirtualLineCount);
    }

    [Fact]
    public void SetWidthRecomputes()
    {
        var view = new TextView("abcdef", 3, TextWrapMode.Char);
        int narrow = view.VirtualLineCount;
        view.SetWidth(100);
        Assert.True(narrow > view.VirtualLineCount);
    }

    [Fact]
    public void SetWidthSameIsNoop()
    {
        var view = new TextView("abc", 10, TextWrapMode.None);
        int count = view.VirtualLineCount;
        view.SetWidth(10);
        Assert.Equal(count, view.VirtualLineCount);
    }

    [Fact]
    public void WrapModeAccessor()
        => Assert.Equal(TextWrapMode.Word, new TextView("abc", 10, TextWrapMode.Word).WrapMode);

    [Fact]
    public void WidthAccessor()
        => Assert.Equal(42, new TextView("abc", 42, TextWrapMode.None).Width);

    [Fact]
    public void MaxWidthAcrossLines()
        => Assert.Equal(5, new TextView("ab\nabcde\nxy", 100, TextWrapMode.None).MaxWidth);

    [Fact]
    public void MaxWidthWithWideChars()
        => Assert.Equal(4, new TextView("世界", 100, TextWrapMode.None).MaxWidth);

    [Fact]
    public void ClampScrollWithinBounds()
    {
        var view = new TextView("a\nb\nc\nd", 10, TextWrapMode.None);
        Assert.Equal(0, view.ClampScroll(0, 2));
        Assert.Equal(1, view.ClampScroll(1, 2));
        Assert.Equal(2, view.ClampScroll(2, 2));
        Assert.Equal(2, view.ClampScroll(3, 2));
        Assert.Equal(2, view.ClampScroll(100, 2));
    }

    [Fact]
    public void ClampScrollViewportLargerThanContent()
    {
        var view = new TextView("a\nb", 10, TextWrapMode.None);
        Assert.Equal(0, view.ClampScroll(0, 10));
        Assert.Equal(0, view.ClampScroll(5, 10));
    }

    [Fact]
    public void ClampScrollZeroViewport()
    {
        var view = new TextView("a\nb\nc", 10, TextWrapMode.None);
        Assert.Equal(1, view.ClampScroll(1, 0));
    }

    [Fact]
    public void MaxScrollBasic()
    {
        var view = new TextView("a\nb\nc\nd\ne", 10, TextWrapMode.None);
        Assert.Equal(2, view.MaxScroll(3));
        Assert.Equal(0, view.MaxScroll(5));
        Assert.Equal(0, view.MaxScroll(10));
    }

    [Fact]
    public void MaxScrollZeroViewport()
        => Assert.Equal(3, new TextView("a\nb\nc", 10, TextWrapMode.None).MaxScroll(0));

    [Fact]
    public void VisibleRangeBasic()
    {
        var view = new TextView("a\nb\nc\nd\ne", 10, TextWrapMode.None);
        AssertRange(view.VisibleRange(0, 3), 0, 3);
        AssertRange(view.VisibleRange(1, 3), 1, 4);
        AssertRange(view.VisibleRange(2, 3), 2, 5);
    }

    [Fact]
    public void VisibleRangeZeroViewport()
        => AssertRange(new TextView("a\nb\nc", 10, TextWrapMode.None).VisibleRange(0, 0), 0, 0);

    [Fact]
    public void VisibleLinesContent()
    {
        var view = new TextView("alpha\nbeta\ngamma\ndelta", 10, TextWrapMode.None);
        IReadOnlyList<ViewLine> visible = view.VisibleLines(1, 2);
        Assert.Equal(2, visible.Count);
        Assert.Equal("beta", visible[0].Text);
        Assert.Equal("gamma", visible[1].Text);
    }

    [Fact]
    public void ScrollToLineBasic()
    {
        var view = new TextView("a\nb\nc\nd\ne", 10, TextWrapMode.None);
        Assert.Equal(0, view.ScrollToLine(0, 3));
        Assert.Equal(2, view.ScrollToLine(2, 3));
        Assert.Equal(2, view.ScrollToLine(4, 3));
    }

    [Fact]
    public void ScrollToLineNonexistent()
        => Assert.Null(new TextView("a\nb", 10, TextWrapMode.None).ScrollToLine(5, 2));

    [Fact]
    public void ScrollByLinesPositive()
    {
        var view = new TextView("a\nb\nc\nd\ne", 10, TextWrapMode.None);
        Assert.Equal(2, view.ScrollByLines(0, 2, 3));
        Assert.Equal(2, view.ScrollByLines(0, 100, 3));
    }

    [Fact]
    public void ScrollByLinesNegative()
    {
        var view = new TextView("a\nb\nc\nd\ne", 10, TextWrapMode.None);
        Assert.Equal(1, view.ScrollByLines(2, -1, 3));
        Assert.Equal(0, view.ScrollByLines(2, -100, 3));
    }

    [Fact]
    public void ScrollByPagesForward()
    {
        var view = TenLineView();
        Assert.Equal(3, view.ScrollByPages(0, 1, 3));
        Assert.Equal(6, view.ScrollByPages(0, 2, 3));
    }

    [Fact]
    public void ScrollByPagesBackward()
    {
        var view = TenLineView();
        Assert.Equal(3, view.ScrollByPages(6, -1, 3));
        Assert.Equal(0, view.ScrollByPages(6, -3, 3));
    }

    [Fact]
    public void ScrollByPagesZeroViewport()
        => Assert.Equal(0, new TextView("a\nb\nc", 10, TextWrapMode.None).ScrollByPages(0, 1, 0));

    [Fact]
    public void ScrollToTopAndBottom()
    {
        var view = new TextView("a\nb\nc\nd\ne", 10, TextWrapMode.None);
        Assert.Equal(0, view.ScrollToTop());
        Assert.Equal(2, view.ScrollToBottom(3));
        Assert.Equal(0, view.ScrollToBottom(5));
        Assert.Equal(4, view.ScrollToBottom(1));
    }

    [Fact]
    public void TrailingNewlineText()
    {
        var view = new TextView("a\nb\n", 10, TextWrapMode.None);
        Assert.Equal(3, view.SourceLineCount);
        Assert.Equal(string.Empty, view.Lines[^1].Text);
    }

    [Fact]
    public void OnlyNewlines()
    {
        var view = new TextView("\n\n\n", 10, TextWrapMode.None);
        Assert.Equal(4, view.SourceLineCount);
        Assert.Equal(4, view.VirtualLineCount);
        Assert.All(view.Lines, static line => Assert.Equal(string.Empty, line.Text));
    }

    [Fact]
    public void ViewLineSourceLineTracking()
    {
        var view = new TextView("ab\ncd\nef", 10, TextWrapMode.None);
        for (int index = 0; index < view.Lines.Count; index++)
        {
            Assert.Equal(index, view.Lines[index].SourceLine);
            Assert.False(view.Lines[index].IsWrap);
        }
    }

    [Fact]
    public void ViewLineWidthTracking()
    {
        var view = new TextView("ab\nabcde\n世", 10, TextWrapMode.None);
        Assert.Equal(2, view.Lines[0].Width);
        Assert.Equal(5, view.Lines[1].Width);
        Assert.Equal(2, view.Lines[2].Width);
    }

    [Fact]
    public void ViewportDefault()
    {
        Viewport viewport = default;
        Assert.Equal(0, viewport.Width);
        Assert.Equal(0, viewport.Height);
    }

    [Fact]
    public void ViewportEquality()
    {
        Assert.Equal(new Viewport(80, 24), new Viewport(80, 24));
        Assert.NotEqual(new Viewport(80, 24), new Viewport(120, 24));
    }

    [Fact]
    public void RopeCompositionUsesAnExplicitImmutableSnapshot()
    {
        var rope = new Rope("alpha\nbeta");
        var view = new TextView(rope.ToString(), 20, TextWrapMode.None);
        rope.Append("\ngamma");

        Assert.Equal(2, view.SourceLineCount);
        view.SetText(rope.ToString());
        Assert.Equal(3, view.SourceLineCount);
    }

    [Fact]
    public void UnicodeCoordinatesKeepUtf16ScalarsGraphemesAndCellsDistinct()
    {
        const string text = "😀e\u0301界";
        var view = new TextView(text, 2, TextWrapMode.Char);

        Assert.Equal(5, text.Length); // UTF-16 code units.
        Assert.Equal(4, text.EnumerateRunes().Count()); // Unicode scalars.
        Assert.Equal(3, view.VirtualLineCount); // Grapheme-safe wrapped lines.
        Assert.Equal(new[] { 2, 1, 2 }, view.Lines.Select(static line => line.Width)); // Display cells.
        Assert.Equal(new[] { "😀", "e\u0301", "界" }, view.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void SourceUnsignedCoordinatesRejectNegativeManagedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Viewport(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ViewLine("x", -1, false, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextView("x", -1, TextWrapMode.None));
        var view = new TextView("x", 1, TextWrapMode.None);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.SetWidth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => view.ClampScroll(-1, 1));
    }

    [Fact]
    public void MalformedUtf16IsRejectedInsteadOfSilentlyReplaced()
    {
        Assert.Throws<ArgumentException>(() => new TextView("\ud800", 10, TextWrapMode.None));
        var view = new TextView("valid", 10, TextWrapMode.None);
        Assert.Throws<ArgumentException>(() => view.SetText("\udc00"));
    }

    [Fact]
    public void CloneOwnsAnIndependentSnapshotAndLayout()
    {
        var original = new TextView("one\ntwo", 10, TextWrapMode.None);
        TextView clone = original.Clone();
        original.SetText("changed");
        Assert.Equal(2, clone.SourceLineCount);
        Assert.Equal(new[] { "one", "two" }, clone.Lines.Select(static line => line.Text));
    }

    [Fact]
    public void ExtendedEstablishedWrapModesRemainComposable()
    {
        var view = new TextView("abcdefgh", 3, TextWrapMode.WordChar);
        Assert.True(view.VirtualLineCount > 1);
        view.SetText("one two three");
        view.SetWidth(7);
        view.SetWrap(TextWrapMode.Optimal);
        Assert.NotEmpty(view.Lines);
        Assert.Equal("one two three", string.Join(' ', view.Lines.Select(static line => line.Text)));
    }

    private static TextView TenLineView()
        => new(string.Join('\n', Enumerable.Range(0, 10).Select(static index => $"line{index}")), 100, TextWrapMode.None);

    private static void AssertRange(Range actual, int expectedStart, int expectedEnd)
    {
        Assert.False(actual.Start.IsFromEnd);
        Assert.False(actual.End.IsFromEnd);
        Assert.Equal(expectedStart, actual.Start.Value);
        Assert.Equal(expectedEnd, actual.End.Value);
    }
}