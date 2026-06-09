// Upstream source: crates/ftui-widgets/src/paragraph.rs (tests module)
// Full 1-1 port of all upstream paragraph tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class ParagraphTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Read all characters in row y of the frame as a string. Port of raw_row_text.
    /// </summary>
    private static string RawRowText(Frame frame, ushort y)
    {
        ushort width = frame.Width;
        var actual = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var cell = frame.Buffer.Get(x, y);
            char ch = ' ';
            if (cell != null)
            {
                if (cell.Value.IsEmpty || cell.Value.IsContinuation)
                {
                    ch = ' ';
                }
                else
                {
                    var content = cell.Value.Content;
                    if (content.IsGrapheme)
                    {
                        string? resolved = frame.Buffer.ResolveText(cell.Value);
                        ch = resolved is { Length: > 0 } ? resolved[0] : ' ';
                    }
                    else
                    {
                        char? asChar = content.AsChar();
                        ch = asChar ?? ' ';
                    }
                }
            }
            actual.Append(ch);
        }
        return actual.ToString();
    }

    /// <summary>Get the character at buffer position (x,y). Returns space for empty/null cells.</summary>
    private static char CellChar(Frame frame, ushort x, ushort y)
    {
        var cell = frame.Buffer.Get(x, y);
        if (cell == null || cell.Value.IsEmpty || cell.Value.IsContinuation) return ' ';
        var content = cell.Value.Content;
        if (content.IsGrapheme)
        {
            string? resolved = frame.Buffer.ResolveText(cell.Value);
            return resolved is { Length: > 0 } ? resolved[0] : ' ';
        }
        return content.AsChar() ?? ' ';
    }

    private static Frame MakeFrame(ushort width, ushort height)
    {
        var pool = new GraphemePool();
        return new Frame(width, height, pool);
    }

    // ── Render tests ──────────────────────────────────────────────────────

    [Fact]
    public void RenderSimpleText()
    {
        var para = new Paragraph(TextContent.Raw("Hello"));
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        para.Render(area, frame);

        Assert.Equal('H', CellChar(frame, 0, 0));
        Assert.Equal('o', CellChar(frame, 4, 0));
    }

    [Fact]
    public void RenderMultilineText()
    {
        var para = new Paragraph(TextContent.Raw("AB\nCD"));
        var area = new Rect(0, 0, 5, 3);
        var frame = MakeFrame(5, 3);
        para.Render(area, frame);

        Assert.Equal('A', CellChar(frame, 0, 0));
        Assert.Equal('B', CellChar(frame, 1, 0));
        Assert.Equal('C', CellChar(frame, 0, 1));
        Assert.Equal('D', CellChar(frame, 1, 1));
    }

    [Fact]
    public void RenderCenteredText()
    {
        // "Hi" is 2 wide, area is 10, so starts at (10-2)/2 = 4
        var para = new Paragraph(TextContent.Raw("Hi")).Alignment(Alignment.Center);
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        para.Render(area, frame);

        Assert.Equal('H', CellChar(frame, 4, 0));
        Assert.Equal('i', CellChar(frame, 5, 0));
    }

    [Fact]
    public void RenderWithScroll()
    {
        // Should skip Line1, show Line2 and Line3
        var para = new Paragraph(TextContent.Raw("Line1\nLine2\nLine3")).Scroll((1, 0));
        var area = new Rect(0, 0, 10, 2);
        var frame = MakeFrame(10, 2);
        para.Render(area, frame);

        Assert.Equal('L', CellChar(frame, 0, 0));
        Assert.Equal('2', CellChar(frame, 4, 0));
    }

    [Fact]
    public void RenderEmptyArea()
    {
        // Should not throw — area (0,0) is empty so render is a no-op
        var para = new Paragraph(TextContent.Raw("Hello"));
        var area = new Rect(0, 0, 0, 0);
        var frame = MakeFrame(1, 1);
        para.Render(area, frame);
        // Just verify no exception was thrown
    }

    [Fact]
    public void LineMinWidthTracksWordsAcrossSpans()
    {
        // "alpha beta  gamma" split across spans; longest word is 5 chars
        var line = TextLine.FromSpans(new[]
        {
            TextSpan.Raw("alpha"),
            TextSpan.Styled(" ", new WidgetStyle(null, null, CellStyleFlags.Bold)),
            TextSpan.Raw("beta"),
            TextSpan.Raw("  "),
            TextSpan.Raw("gamma"),
        });

        var text = TextContent.FromLines(new[] { line });
        var para = new Paragraph(text);
        Assert.Equal(5, para.CalculateMinWidth());
    }

    [Fact]
    public void MeasureWrapCountsCachedVisualLines()
    {
        var para = new Paragraph(TextContent.Raw("hello world from cache")).Wrap(WrapMode.Word);
        var constraints = para.Measure(new Size(8, 10));

        Assert.Equal(4, constraints.Preferred.Height);
        Assert.Equal(5, constraints.Min.Width);
    }

    [Fact]
    public void MeasureWrapNonePreservesNaturalWidth()
    {
        var para = new Paragraph(TextContent.Raw("abcdef")).Wrap(WrapMode.None);
        var constraints = para.Measure(new Size(3, 10));

        Assert.Equal(6, constraints.Preferred.Width);
        Assert.Equal(1, constraints.Preferred.Height);
    }

    [Fact]
    public void RenderEmptyTextClearsContent()
    {
        var para = new Paragraph(TextContent.Raw(""));
        var area = new Rect(0, 0, 3, 1);
        var frame = MakeFrame(3, 1);

        // Seed with non-space content; an empty Paragraph render should clear it.
        frame.Buffer.Fill(area, Cell.FromChar('X'));

        para.Render(area, frame);

        // After clear, cells should be space (not 'X')
        Assert.Equal(' ', CellChar(frame, 0, 0));
        Assert.Equal(' ', CellChar(frame, 2, 0));
    }

    [Fact]
    public void RenderRightAligned()
    {
        // "Hi" is 2 wide, area is 10, so starts at 10-2 = 8
        var para = new Paragraph(TextContent.Raw("Hi")).Alignment(Alignment.Right);
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        para.Render(area, frame);

        Assert.Equal('H', CellChar(frame, 8, 0));
        Assert.Equal('i', CellChar(frame, 9, 0));
    }

    [Fact]
    public void RenderWithWordWrap()
    {
        // "hello " fits in 6, "world" wraps to next line
        var para = new Paragraph(TextContent.Raw("hello world")).Wrap(WrapMode.Word);
        var area = new Rect(0, 0, 6, 3);
        var frame = MakeFrame(6, 3);
        para.Render(area, frame);

        Assert.Equal('h', CellChar(frame, 0, 0));
        Assert.Equal('w', CellChar(frame, 0, 1));
    }

    [Fact]
    public void RenderWithCharWrap()
    {
        // First line: abcd, second line: efgh
        var para = new Paragraph(TextContent.Raw("abcdefgh")).Wrap(WrapMode.Char);
        var area = new Rect(0, 0, 4, 3);
        var frame = MakeFrame(4, 3);
        para.Render(area, frame);

        Assert.Equal('a', CellChar(frame, 0, 0));
        Assert.Equal('d', CellChar(frame, 3, 0));
        Assert.Equal('e', CellChar(frame, 0, 1));
    }

    [Fact]
    public void ScrollPastAllLines()
    {
        // All lines skipped, but the paragraph still owns and clears its area.
        var para = new Paragraph(TextContent.Raw("AB")).Scroll((5, 0));
        var area = new Rect(0, 0, 5, 2);
        var frame = MakeFrame(5, 2);
        para.Render(area, frame);

        // Area should be cleared (space character)
        Assert.Equal(' ', CellChar(frame, 0, 0));
    }

    [Fact]
    public void RenderShorterTextClearsStaleLines()
    {
        var area = new Rect(0, 0, 8, 2);
        var frame = MakeFrame(8, 2);

        new Paragraph(TextContent.Raw("Hello\nWorld")).Render(area, frame);
        new Paragraph(TextContent.Raw("Hi")).Render(area, frame);

        Assert.Equal("Hi      ", RawRowText(frame, 0));
        Assert.Equal("        ", RawRowText(frame, 1));
    }

    [Fact]
    public void RenderClippedAtAreaHeight()
    {
        // Only first 2 lines should render
        var para = new Paragraph(TextContent.Raw("A\nB\nC\nD\nE"));
        var area = new Rect(0, 0, 5, 2);
        var frame = MakeFrame(5, 2);
        para.Render(area, frame);

        Assert.Equal('A', CellChar(frame, 0, 0));
        Assert.Equal('B', CellChar(frame, 0, 1));
    }

    [Fact]
    public void RenderClippedAtAreaWidth()
    {
        var para = new Paragraph(TextContent.Raw("ABCDEF"));
        var area = new Rect(0, 0, 3, 1);
        var frame = MakeFrame(3, 1);
        para.Render(area, frame);

        Assert.Equal('A', CellChar(frame, 0, 0));
        Assert.Equal('C', CellChar(frame, 2, 0));
    }

    [Fact]
    public void AlignXLeft()
    {
        // Port of align_x_left: with area.x=5, left-aligned text starts at 5
        var para = new Paragraph(TextContent.Raw("X")).Alignment(Alignment.Left);
        var area = new Rect(5, 0, 20, 1);
        var frame = MakeFrame(30, 1);
        para.Render(area, frame);

        Assert.Equal('X', CellChar(frame, 5, 0));
    }

    [Fact]
    public void AlignXCenter()
    {
        // line_width=6, area.x=0, area.width=20: (20-6)/2 = 7
        var para = new Paragraph(TextContent.Raw("abcdef")).Alignment(Alignment.Center);
        var area = new Rect(0, 0, 20, 1);
        var frame = MakeFrame(20, 1);
        para.Render(area, frame);

        Assert.Equal('a', CellChar(frame, 7, 0));
    }

    [Fact]
    public void AlignXRight()
    {
        // line_width=5, area.x=0, area.width=20: 20-5 = 15
        var para = new Paragraph(TextContent.Raw("abcde")).Alignment(Alignment.Right);
        var area = new Rect(0, 0, 20, 1);
        var frame = MakeFrame(20, 1);
        para.Render(area, frame);

        Assert.Equal('a', CellChar(frame, 15, 0));
    }

    [Fact]
    public void AlignXWideLineSaturates()
    {
        // line wider than area: AlignX should saturate, text starts at x=0
        var area = new Rect(0, 0, 10, 1);

        var frame1 = MakeFrame(10, 1);
        new Paragraph(TextContent.Raw("12345678901234567890")).Alignment(Alignment.Right)
            .Render(area, frame1);
        // The text is 20 wide but area is only 10 — first visible char should be at x=0
        Assert.Equal('1', CellChar(frame1, 0, 0));

        var frame2 = MakeFrame(10, 1);
        new Paragraph(TextContent.Raw("12345678901234567890")).Alignment(Alignment.Center)
            .Render(area, frame2);
        Assert.Equal('1', CellChar(frame2, 0, 0));
    }

    [Fact]
    public void BuilderMethodsChain()
    {
        var para = new Paragraph(TextContent.Raw("test"))
            .Style(WidgetStyle.Default)
            .Wrap(WrapMode.Word)
            .Alignment(Alignment.Center)
            .Scroll((1, 2));
        // Verify it builds without panic
        var area = new Rect(0, 0, 10, 5);
        var frame = MakeFrame(10, 5);
        para.Render(area, frame);
    }

    [Fact]
    public void RenderAtOffsetArea()
    {
        var para = new Paragraph(TextContent.Raw("X"));
        var area = new Rect(3, 4, 5, 2);
        var frame = MakeFrame(10, 10);
        para.Render(area, frame);

        Assert.Equal('X', CellChar(frame, 3, 4));
        // Cell at (0,0) should be empty (unmodified by paragraph)
        Assert.True(frame.Buffer.Get(0, 0)!.Value.IsEmpty);
    }

    [Fact]
    public void WrapClippedAtAreaBottom()
    {
        // Long wrapped text should stop at area height; only 2 rows of 4 chars each
        var para = new Paragraph(TextContent.Raw("abcdefghijklmnop")).Wrap(WrapMode.Char);
        var area = new Rect(0, 0, 4, 2);
        var frame = MakeFrame(4, 2);
        para.Render(area, frame);

        Assert.Equal('a', CellChar(frame, 0, 0));
        Assert.Equal('e', CellChar(frame, 0, 1));
    }

    // ── Degradation tests ─────────────────────────────────────────────────

    [Fact]
    public void DegradationSkeletonSkipsContent()
    {
        var para = new Paragraph(TextContent.Raw("Hello"));
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        new Paragraph(TextContent.Raw("Stale")).Render(area, frame);
        frame.SetDegradation(DegradationLevel.Skeleton);
        para.Render(area, frame);

        // Skeleton clears previously rendered content instead of leaving it behind.
        Assert.Equal("          ", RawRowText(frame, 0));
    }

    [Fact]
    public void DegradationFullRendersContent()
    {
        var para = new Paragraph(TextContent.Raw("Hello"));
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        frame.SetDegradation(DegradationLevel.Full);
        para.Render(area, frame);

        Assert.Equal('H', CellChar(frame, 0, 0));
    }

    [Fact]
    public void DegradationEssentialOnlyStillRendersText()
    {
        // EssentialOnly still renders content (< Skeleton)
        var para = new Paragraph(TextContent.Raw("Hello"));
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        frame.SetDegradation(DegradationLevel.EssentialOnly);
        para.Render(area, frame);

        Assert.Equal('H', CellChar(frame, 0, 0));
    }

    [Fact]
    public void DegradationNoStylingIgnoresSpanStyles()
    {
        // Create text with a styled span
        var styledSpan = TextSpan.Styled("Hello", new WidgetStyle(PackedRgba.Red, null, null));
        var line = TextLine.FromSpans(new[] { styledSpan });
        var text = TextContent.FromLines(new[] { line });
        var para = new Paragraph(text);
        var area = new Rect(0, 0, 10, 1);
        var frame = MakeFrame(10, 1);
        frame.SetDegradation(DegradationLevel.NoStyling);
        para.Render(area, frame);

        // Text should render but span style should be ignored
        Assert.Equal('H', CellChar(frame, 0, 0));
        // Foreground color should NOT be red
        Assert.NotEqual(PackedRgba.Red, frame.Buffer.Get(0, 0)!.Value.Foreground);
    }

    // ── MeasurableWidget tests ─────────────────────────────────────────────

    [Fact]
    public void MeasureSimpleText()
    {
        var para = new Paragraph(TextContent.Raw("Hello"));
        var constraints = para.Measure(Size.Max);

        // "Hello" is 5 chars wide, 1 line tall
        Assert.Equal(new Size(5, 1), constraints.Preferred);
        Assert.Equal((ushort)1, constraints.Min.Height);
        // Min width is the longest word = "Hello" = 5
        Assert.Equal((ushort)5, constraints.Min.Width);
    }

    [Fact]
    public void MeasureMultilineText()
    {
        var para = new Paragraph(TextContent.Raw("Line1\nLine22\nL3"));
        var constraints = para.Measure(Size.Max);

        // Max width is "Line22" = 6, height = 3 lines
        Assert.Equal(new Size(6, 3), constraints.Preferred);
        Assert.Equal((ushort)1, constraints.Min.Height);
        // Min width is longest word = "Line22" = 6
        Assert.Equal((ushort)6, constraints.Min.Width);
    }

    [Fact]
    public void MeasureWithBlock()
    {
        var block = Block.Bordered();
        var para = new Paragraph(TextContent.Raw("Hi")).Block(block);
        var constraints = para.Measure(Size.Max);

        // "Hi" = 2 wide, 1 tall, plus chrome (borders + padding each side) = 4 on each axis
        Assert.Equal(new Size(6, 5), constraints.Preferred);
        Assert.Equal((ushort)6, constraints.Min.Width);
        Assert.Equal((ushort)5, constraints.Min.Height);
    }

    [Fact]
    public void MeasureWithWordWrap()
    {
        var para = new Paragraph(TextContent.Raw("hello world")).Wrap(WrapMode.Word);
        // Measure with narrow available width
        var constraints = para.Measure(new Size(6, 10));

        // With 6 chars available, "hello" fits, "world" wraps
        Assert.Equal(2, constraints.Preferred.Height);
        // Min width is longest word = "hello" = 5
        Assert.Equal((ushort)5, constraints.Min.Width);
    }

    [Fact]
    public void MeasureEmptyText()
    {
        var para = new Paragraph(TextContent.Raw(""));
        var constraints = para.Measure(Size.Max);

        // Empty text: 0 width, 0 height (no lines)
        Assert.Equal((ushort)0, constraints.Preferred.Width);
        Assert.Equal((ushort)0, constraints.Preferred.Height);
        // Min height is 0 for empty text (no content to display)
        // This ensures min <= preferred invariant holds
        Assert.Equal((ushort)0, constraints.Min.Height);
    }

    [Fact]
    public void CalculateMinWidthSingleLongWord()
    {
        var para = new Paragraph(TextContent.Raw("supercalifragilistic"));
        Assert.Equal(20, para.CalculateMinWidth());
    }

    [Fact]
    public void CalculateMinWidthMultipleWords()
    {
        // Longest word is "quick" or "brown" = 5
        var para = new Paragraph(TextContent.Raw("the quick brown fox"));
        Assert.Equal(5, para.CalculateMinWidth());
    }

    [Fact]
    public void CalculateMinWidthMultiline()
    {
        // Longest word is "longword" = 8
        var para = new Paragraph(TextContent.Raw("short\nlongword\na"));
        Assert.Equal(8, para.CalculateMinWidth());
    }

    [Fact]
    public void EstimateWrappedHeightNoWrapNeeded()
    {
        var para = new Paragraph(TextContent.Raw("short")).Wrap(WrapMode.Word);
        // Width 10 is enough for "short" (5 chars)
        Assert.Equal(1, para.EstimateWrappedHeight(10));
    }

    [Fact]
    public void EstimateWrappedHeightNeedsWrap()
    {
        var para = new Paragraph(TextContent.Raw("hello world")).Wrap(WrapMode.Word);
        // Width 6: "hello" fits (5 chars), "world" (5 chars) wraps
        Assert.Equal(2, para.EstimateWrappedHeight(6));
    }

    [Fact]
    public void HasIntrinsicSize()
    {
        var para = new Paragraph(TextContent.Raw("test"));
        Assert.True(para.HasIntrinsicSize());
    }

    [Fact]
    public void MeasureIsPure()
    {
        var para = new Paragraph(TextContent.Raw("Hello World"));
        var a = para.Measure(new Size(100, 50));
        var b = para.Measure(new Size(100, 50));
        Assert.Equal(a, b);
    }

    // ── Accessibility tests ────────────────────────────────────────────────

    [Fact]
    public void AccessibilityTruncatesLongUnicodeWithoutPanicking()
    {
        // '界' is U+754C (BMP), so each char is 1 C# char
        var text = TextContent.Raw(new string('界', 210));
        var para = new Paragraph(text);
        var nodes = para.AccessibilityNodes(new Rect(0, 0, 10, 1));
        var name = nodes[0].Name;

        Assert.NotNull(name);
        Assert.EndsWith("...", name);
        Assert.Equal(200, name!.Length);
    }

    [Fact]
    public void AccessibilityTruncatesDescriptionWhenBlockTitlePresent()
    {
        var text = TextContent.Raw(new string('界', 210));
        var para = new Paragraph(text).Block(Block.Bordered().Title("Body"));
        var nodes = para.AccessibilityNodes(new Rect(0, 0, 10, 1));
        var node = nodes[0];

        Assert.Equal("Body", node.Name);
        var description = node.Description;
        Assert.NotNull(description);
        Assert.EndsWith("...", description);
        Assert.Equal(200, description!.Length);
    }

    [Fact]
    public void AccessibilityPreservesExactly200CharsWithoutEllipsis()
    {
        var text = TextContent.Raw(new string('界', 200));
        var para = new Paragraph(text);
        var nodes = para.AccessibilityNodes(new Rect(0, 0, 10, 1));
        var name = nodes[0].Name;

        Assert.NotNull(name);
        Assert.DoesNotContain("...", name);
        Assert.Equal(200, name!.Length);
    }

    [Fact]
    public void AccessibilityTruncatesOnGraphemeBoundaries()
    {
        // "é" is 'e' + combining acute accent = 1 grapheme cluster = 2 chars
        // 210 repetitions = 420 chars total
        string graphemeCluster = "é"; // e + combining accent
        var text = TextContent.Raw(string.Concat(Enumerable.Repeat(graphemeCluster, 210)));
        var para = new Paragraph(text);
        var nodes = para.AccessibilityNodes(new Rect(0, 0, 10, 1));
        var name = nodes[0].Name;

        Assert.NotNull(name);
        // Name must be truncated (420 chars > 200)
        var prefix = name!.EndsWith("...") ? name[..^3] : name;
        // Total length including "..." should fit in 200 chars
        Assert.True(name.Length <= 200);
        // Prefix must end on a grapheme boundary (not split mid-grapheme)
        // e + combining accent is 2 chars; prefix must have even length (chars come in pairs)
        if (prefix.Length > 0)
        {
            // Last two chars should be the grapheme cluster
            Assert.True(prefix.EndsWith("é") || prefix.EndsWith("é"));
        }
    }
}
