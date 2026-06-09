// Upstream source: .external/frankentui/crates/ftui-widgets/src/block.rs (tests module)
// Full 1-1 port of all upstream block.rs tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class BlockTests
{
    [Fact]
    public void InnerWithAllBorders()
    {
        var block = Block.New().Borders(Borders.All);
        var area = new Rect(0, 0, 10, 10);
        var inner = block.Inner(area);
        Assert.Equal(new Rect(1, 1, 8, 8), inner);
    }

    [Fact]
    public void InnerWithNoBorders()
    {
        var block = Block.New();
        var area = new Rect(0, 0, 10, 10);
        var inner = block.Inner(area);
        Assert.Equal(area, inner);
    }

    [Fact]
    public void InnerWithPartialBorders()
    {
        var block = Block.New().Borders(Borders.Top | Borders.Left);
        var area = new Rect(0, 0, 10, 10);
        var inner = block.Inner(area);
        Assert.Equal(new Rect(1, 1, 9, 9), inner);
    }

    [Fact]
    public void RenderEmptyArea()
    {
        var block = Block.New().Borders(Borders.All);
        var area = new Rect(0, 0, 0, 0);
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        block.Render(area, frame);
        // Should not throw
    }

    [Fact]
    public void RenderBlockWithSquareBorders()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Square);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('┌', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('┐', buf.Get(4, 0)!.Value.Content.AsChar());
        Assert.Equal('└', buf.Get(0, 2)!.Value.Content.AsChar());
        Assert.Equal('┘', buf.Get(4, 2)!.Value.Content.AsChar());
        Assert.Equal('─', buf.Get(2, 0)!.Value.Content.AsChar());
        Assert.Equal('│', buf.Get(0, 1)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderBlockWithTitle()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Square)
            .Title("Hi");
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('H', buf.Get(1, 0)!.Value.Content.AsChar());
        Assert.Equal('i', buf.Get(2, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderTitleOverridesOnMultipleCalls()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Square)
            .Title("First")
            .Title("Second");
        var area = new Rect(0, 0, 12, 3);
        var pool = new GraphemePool();
        var frame = new Frame(12, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('S', buf.Get(1, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderBlockWithBackground()
    {
        var block = Block.New().Style(new WidgetStyle(null, PackedRgba.Rgb(10, 20, 30), null));
        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal(PackedRgba.Rgb(10, 20, 30), buf.Get(0, 0)!.Value.Background);
        Assert.Equal(PackedRgba.Rgb(10, 20, 30), buf.Get(2, 1)!.Value.Background);
    }

    [Fact]
    public void InnerWithOnlyBottom()
    {
        var block = Block.New().Borders(Borders.Bottom);
        var area = new Rect(0, 0, 10, 10);
        var inner = block.Inner(area);
        Assert.Equal(new Rect(0, 0, 10, 9), inner);
    }

    [Fact]
    public void InnerWithOnlyRight()
    {
        var block = Block.New().Borders(Borders.Right);
        var area = new Rect(0, 0, 10, 10);
        var inner = block.Inner(area);
        Assert.Equal(new Rect(0, 0, 9, 10), inner);
    }

    [Fact]
    public void InnerSaturatesOnTinyArea()
    {
        var block = Block.New().Borders(Borders.All);
        var area = new Rect(0, 0, 1, 1);
        var inner = block.Inner(area);
        // 1x1 with all borders: x+1=1, w-2=0, y+1=1, h-2=0
        Assert.Equal(0, inner.Width);
    }

    [Fact]
    public void BorderedConstructor()
    {
        var block = Block.Bordered();
        Assert.Equal(Borders.All, block.BordersValue);
    }

    [Fact]
    public void DefaultHasNoBorders()
    {
        var block = Block.New();
        Assert.Equal(Borders.None, block.BordersValue);
        Assert.Null(block.TitleValue);
    }

    [Fact]
    public void RenderRoundedBorders()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Rounded);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('╭', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('╮', buf.Get(4, 0)!.Value.Content.AsChar());
        Assert.Equal('╰', buf.Get(0, 2)!.Value.Content.AsChar());
        Assert.Equal('╯', buf.Get(4, 2)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderDoubleBorders()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Double);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('╔', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('╗', buf.Get(4, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderPartialBordersCornersOnlyWhenEdgesEnabled()
    {
        var block = Block.New()
            .Borders(Borders.Top | Borders.Left | Borders.Bottom)
            .BorderType(Widgets.BorderType.Square);
        var area = new Rect(0, 0, 4, 3);
        var pool = new GraphemePool();
        var frame = new Frame(4, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('┌', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('└', buf.Get(0, 2)!.Value.Content.AsChar());
        Assert.Equal('─', buf.Get(3, 0)!.Value.Content.AsChar());
        Assert.Equal('─', buf.Get(3, 2)!.Value.Content.AsChar());
        var midCell = buf.Get(3, 1)!.Value;
        Assert.True(midCell.IsEmpty || midCell.Content.AsChar() == ' ');
    }

    [Fact]
    public void RenderVerticalOnlyBordersUseVerticalGlyphs()
    {
        var block = Block.New()
            .Borders(Borders.Left | Borders.Right)
            .BorderType(Widgets.BorderType.Double);
        var area = new Rect(0, 0, 4, 3);
        var pool = new GraphemePool();
        var frame = new Frame(4, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('║', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('║', buf.Get(3, 0)!.Value.Content.AsChar());
        var innerCell = buf.Get(1, 0)!.Value;
        Assert.True(innerCell.IsEmpty || innerCell.Content.AsChar() == ' ');
    }

    [Fact]
    public void RenderMissingLeftKeepsHorizontalCornerLogic()
    {
        var block = Block.New()
            .Borders(Borders.Top | Borders.Right | Borders.Bottom)
            .BorderType(Widgets.BorderType.Square);
        var area = new Rect(0, 0, 4, 3);
        var pool = new GraphemePool();
        var frame = new Frame(4, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('─', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('┐', buf.Get(3, 0)!.Value.Content.AsChar());
        Assert.Equal('─', buf.Get(0, 2)!.Value.Content.AsChar());
        Assert.Equal('┘', buf.Get(3, 2)!.Value.Content.AsChar());
        Assert.Equal('│', buf.Get(3, 1)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderTitleLeftAligned()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .Title("Test")
            .TitleAlignment(Alignment.Left);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('T', buf.Get(1, 0)!.Value.Content.AsChar());
        Assert.Equal('e', buf.Get(2, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderTitleCenterAligned()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .Title("Hi")
            .TitleAlignment(Alignment.Center);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        block.Render(area, frame);

        // Title "Hi" (2 chars) in 8 available (10-2 borders), centered at offset 3
        var buf = frame.Buffer;
        Assert.Equal('H', buf.Get(4, 0)!.Value.Content.AsChar());
        Assert.Equal('i', buf.Get(5, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderTitleCenterAlignedWithWideGrapheme()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .Title("界")
            .TitleAlignment(Alignment.Center);
        var area = new Rect(0, 0, 8, 3);
        var pool = new GraphemePool();
        var frame = new Frame(8, 3, pool);
        block.Render(area, frame);

        // Available width = 6, title width = 2 => center offset 2 => x = 3
        var buf = frame.Buffer;
        var cell = buf.Get(3, 0)!.Value;
        Assert.True(
            cell.Content.AsChar() == '界' || cell.Content.IsGrapheme,
            "expected title grapheme at x=3");
        Assert.True(buf.Get(4, 0)!.Value.IsContinuation);
    }

    [Fact]
    public void RenderTitleRightAligned()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .Title("Hi")
            .TitleAlignment(Alignment.Right);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        // "Hi" right-aligned: right()-1 - 2 = col 7
        Assert.Equal('H', buf.Get(7, 0)!.Value.Content.AsChar());
        Assert.Equal('i', buf.Get(8, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderTitleRightAlignedTruncatedWideGraphemeUsesFittedWidth()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .Title("界界")
            .TitleAlignment(Alignment.Right);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        var cell = buf.Get(2, 0)!.Value;
        Assert.True(
            cell.Content.AsChar() == '界' || cell.Content.IsGrapheme,
            "expected fitted wide title to be right aligned");
        Assert.True(buf.Get(3, 0)!.Value.IsContinuation);
        Assert.Equal('─', buf.Get(1, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderMultiTitleAlignmentUsesLastTitleAndAlignment()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .Title("Left")
            .TitleAlignment(Alignment.Left)
            .Title("Right")
            .TitleAlignment(Alignment.Right);
        var area = new Rect(0, 0, 12, 3);
        var pool = new GraphemePool();
        var frame = new Frame(12, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('R', buf.Get(6, 0)!.Value.Content.AsChar());
        Assert.NotEqual('L', buf.Get(1, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void TitleNotRenderedWithoutTopBorder()
    {
        var block = Block.New()
            .Borders(Borders.Left | Borders.Right | Borders.Bottom)
            .Title("Hi");
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        // No title should appear on row 0
        Assert.NotEqual('H', buf.Get(1, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void BorderStyleApplied()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderStyle(new WidgetStyle(PackedRgba.Rgb(255, 0, 0), null, null));
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal(PackedRgba.Rgb(255, 0, 0), buf.Get(0, 0)!.Value.Foreground);
    }

    [Fact]
    public void OnlyHorizontalBorders()
    {
        var block = Block.New()
            .Borders(Borders.Top | Borders.Bottom)
            .BorderType(Widgets.BorderType.Square);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        // Top and bottom should have horizontal lines
        Assert.Equal('─', buf.Get(2, 0)!.Value.Content.AsChar());
        Assert.Equal('─', buf.Get(2, 2)!.Value.Content.AsChar());
        // Left edge should be empty (no vertical border)
        var leftCell = buf.Get(0, 1)!.Value;
        Assert.True(leftCell.IsEmpty || leftCell.Content.AsChar() == ' ');
    }

    [Fact]
    public void DegradationSimpleBordersForcesAscii()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Rounded);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        frame.SetDegradation(DegradationLevel.SimpleBorders);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('+', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('+', buf.Get(4, 0)!.Value.Content.AsChar());
        Assert.Equal('-', buf.Get(2, 0)!.Value.Content.AsChar());
        Assert.Equal('|', buf.Get(0, 1)!.Value.Content.AsChar());
    }

    [Fact]
    public void DegradationSimpleBordersPartialEdgesUseAsciiCorners()
    {
        var block = Block.New()
            .Borders(Borders.Top | Borders.Right | Borders.Bottom)
            .BorderType(Widgets.BorderType.Double);
        var area = new Rect(0, 0, 4, 3);
        var pool = new GraphemePool();
        var frame = new Frame(4, 3, pool);
        frame.SetDegradation(DegradationLevel.SimpleBorders);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('-', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('+', buf.Get(3, 0)!.Value.Content.AsChar());
        Assert.Equal('-', buf.Get(0, 2)!.Value.Content.AsChar());
        Assert.Equal('+', buf.Get(3, 2)!.Value.Content.AsChar());
        Assert.Equal('|', buf.Get(3, 1)!.Value.Content.AsChar());
    }

    [Fact]
    public void DegradationNoStylingRendersTitleWithoutStyles()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderStyle(new WidgetStyle(PackedRgba.Rgb(200, 0, 0), null, null))
            .Title("Hi");
        var area = new Rect(0, 0, 6, 3);
        var pool = new GraphemePool();
        var frame = new Frame(6, 3, pool);
        frame.SetDegradation(DegradationLevel.NoStyling);
        block.Render(area, frame);

        var buf = frame.Buffer;
        var defaultFg = Cell.Empty.Foreground;
        Assert.Equal('H', buf.Get(1, 0)!.Value.Content.AsChar());
        Assert.Equal(defaultFg, buf.Get(1, 0)!.Value.Foreground);
    }

    [Fact]
    public void DegradationNoStylingKeepsBorderWhenTitleDoesNotFit()
    {
        var titled = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Square)
            .Title("界");
        var plain = Block.New()
            .Borders(Borders.All)
            .BorderType(Widgets.BorderType.Square);
        var area = new Rect(0, 0, 3, 3);

        var titledPool = new GraphemePool();
        var titledFrame = new Frame(3, 3, titledPool);
        titledFrame.SetDegradation(DegradationLevel.NoStyling);
        titled.Render(area, titledFrame);

        var plainPool = new GraphemePool();
        var plainFrame = new Frame(3, 3, plainPool);
        plainFrame.SetDegradation(DegradationLevel.NoStyling);
        plain.Render(area, plainFrame);

        Assert.Equal(titledFrame.Buffer.Get(1, 0), plainFrame.Buffer.Get(1, 0));
    }

    [Fact]
    public void DegradationNoStylingDropsBorderStyleEverywhere()
    {
        var block = Block.New()
            .Borders(Borders.All)
            .BorderStyle(new WidgetStyle(PackedRgba.Rgb(200, 0, 0), null, CellStyleFlags.Bold));
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        frame.SetDegradation(DegradationLevel.NoStyling);
        block.Render(area, frame);

        var border = frame.Buffer.Get(0, 0)!.Value;
        var defaultCell = Cell.FromChar(border.Content.AsChar()!.Value);
        Assert.Equal(border.Foreground, defaultCell.Foreground);
        Assert.Equal(border.Background, defaultCell.Background);
        Assert.Equal(border.Attributes, defaultCell.Attributes);
    }

    [Fact]
    public void DegradationEssentialOnlyClearsStaleAndTitle()
    {
        var block = Block.Bordered()
            .BorderType(Widgets.BorderType.Square)
            .Title("Hi");
        var area = new Rect(0, 0, 6, 3);
        var pool = new GraphemePool();
        var frame = new Frame(6, 3, pool);
        block.Render(area, frame);

        frame.SetDegradation(DegradationLevel.EssentialOnly);
        block.Render(area, frame);

        var buf = frame.Buffer;
        for (ushort y = 0; y < area.Height; y++)
        {
            for (ushort x = 0; x < area.Width; x++)
            {
                Assert.True(
                    buf.Get(x, y)!.Value.IsEmpty,
                    $"expected cleared cell at ({x}, {y}), got {buf.Get(x, y)}");
            }
        }
    }

    [Fact]
    public void DegradationSkeletonClearsArea()
    {
        var block = Block.Bordered();
        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        frame.Buffer.Fill(area, Cell.FromChar('X'));
        frame.SetDegradation(DegradationLevel.Skeleton);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.True(buf.Get(0, 0)!.Value.IsEmpty);
    }

    [Fact]
    public void BlockEquality()
    {
        var a = Block.New().Borders(Borders.All).Title("Test");
        var b = Block.New().Borders(Borders.All).Title("Test");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Render1X1NoPanic()
    {
        var block = Block.Bordered();
        var area = new Rect(0, 0, 1, 1);
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        block.Render(area, frame);
        // Should not panic/throw
    }

    [Fact]
    public void Render2X2WithBorders()
    {
        var block = Block.Bordered().BorderType(Widgets.BorderType.Square);
        var area = new Rect(0, 0, 2, 2);
        var pool = new GraphemePool();
        var frame = new Frame(2, 2, pool);
        block.Render(area, frame);

        var buf = frame.Buffer;
        Assert.Equal('┌', buf.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('┐', buf.Get(1, 0)!.Value.Content.AsChar());
        Assert.Equal('└', buf.Get(0, 1)!.Value.Content.AsChar());
        Assert.Equal('┘', buf.Get(1, 1)!.Value.Content.AsChar());
    }

    [Fact]
    public void TitleTooNarrow()
    {
        // Width 3 with all borders = 1 char available for title
        var block = Block.Bordered().Title("LongTitle");
        var area = new Rect(0, 0, 4, 3);
        var pool = new GraphemePool();
        var frame = new Frame(4, 3, pool);
        block.Render(area, frame);
        // Should not panic, title gets truncated
    }

    [Fact]
    public void AlignmentDefaultIsLeft()
    {
        Assert.Equal(Alignment.Left, default(Alignment));
    }

    // --- IMeasurableWidget tests ---

    [Fact]
    public void ChromeSizeNoBorders()
    {
        var block = Block.New();
        Assert.Equal((0, 0), block.ChromeSize());
    }

    [Fact]
    public void ChromeSizeAllBorders()
    {
        var block = Block.Bordered();
        // Block::bordered() includes 1 cell padding on each side.
        // Chrome = borders (2) + padding (2) = 4 on each axis.
        Assert.Equal(((ushort)4, (ushort)4), block.ChromeSize());
    }

    [Fact]
    public void ChromeSizePartialBorders()
    {
        var block = Block.New().Borders(Borders.Top | Borders.Left);
        Assert.Equal(((ushort)1, (ushort)1), block.ChromeSize());
    }

    [Fact]
    public void ChromeSizeHorizontalOnly()
    {
        var block = Block.New().Borders(Borders.Left | Borders.Right);
        Assert.Equal(((ushort)2, (ushort)0), block.ChromeSize());
    }

    [Fact]
    public void ChromeSizeVerticalOnly()
    {
        var block = Block.New().Borders(Borders.Top | Borders.Bottom);
        Assert.Equal(((ushort)0, (ushort)2), block.ChromeSize());
    }

    [Fact]
    public void MeasureNoBorders()
    {
        var block = Block.New();
        var constraints = block.Measure(Size.Max);
        Assert.Equal(Size.Zero, constraints.Min);
        Assert.Equal(Size.Zero, constraints.Preferred);
    }

    [Fact]
    public void MeasureAllBorders()
    {
        var block = Block.Bordered();
        var constraints = block.Measure(Size.Max);
        Assert.Equal(new Size(4, 4), constraints.Min);
        Assert.Equal(new Size(4, 4), constraints.Preferred);
        Assert.Null(constraints.Max); // Unbounded
    }

    [Fact]
    public void MeasurePartialBorders()
    {
        var block = Block.New().Borders(Borders.Top | Borders.Right);
        var constraints = block.Measure(Size.Max);
        Assert.Equal(new Size(1, 1), constraints.Min);
        Assert.Equal(new Size(1, 1), constraints.Preferred);
    }

    [Fact]
    public void HasIntrinsicSizeWithBorders()
    {
        var block = Block.Bordered();
        Assert.True(block.HasIntrinsicSize());
    }

    [Fact]
    public void HasNoIntrinsicSizeWithoutBorders()
    {
        var block = Block.New();
        Assert.False(block.HasIntrinsicSize());
    }

    [Fact]
    public void MeasureIsPure()
    {
        var block = Block.Bordered();
        var a = block.Measure(new Size(100, 50));
        var b = block.Measure(new Size(100, 50));
        Assert.Equal(a, b);
    }
}
