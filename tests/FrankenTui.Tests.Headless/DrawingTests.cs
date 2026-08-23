// Differential contract for crates/ftui-render/src/drawing.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;
using FrankenTui.Render;
using Buffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class DrawingTests
{
    [Fact]
    public void HorizontalAndVerticalLinesMatchSource()
    {
        var buffer = new Buffer(10, 10);
        buffer.DrawHorizontalLine(2, 0, 5, Cell.FromChar('─'));
        buffer.DrawVerticalLine(0, 1, 4, Cell.FromChar('│'));

        Assert.Null(TextAt(buffer, 1, 0));
        Assert.Equal("─", TextAt(buffer, 2, 0));
        Assert.Equal("─", TextAt(buffer, 6, 0));
        Assert.Null(TextAt(buffer, 7, 0));
        Assert.Equal("│", TextAt(buffer, 0, 1));
        Assert.Equal("│", TextAt(buffer, 0, 4));
        Assert.Null(TextAt(buffer, 0, 5));
    }

    [Fact]
    public void ZeroLengthLinesWriteNothing()
    {
        var buffer = new Buffer(2, 2);
        buffer.DrawHorizontalLine(0, 0, 0, Cell.FromChar('x'));
        buffer.DrawVerticalLine(0, 0, 0, Cell.FromChar('x'));

        Assert.True(buffer.Get(0, 0)!.Value.IsEmpty);
    }

    [Fact]
    public void LinesRespectScissorThroughSetFastFallback()
    {
        var buffer = new Buffer(10, 10);
        buffer.PushScissor(new Rect(0, 2, 3, 3));
        buffer.DrawHorizontalLine(0, 2, 10, Cell.FromChar('x'));
        buffer.DrawVerticalLine(2, 0, 10, Cell.FromChar('|'));

        Assert.Equal("x", TextAt(buffer, 0, 2));
        Assert.Equal("|", TextAt(buffer, 2, 4));
        Assert.Null(TextAt(buffer, 3, 2));
        Assert.Null(TextAt(buffer, 2, 5));
    }

    [Fact]
    public void SetFastPreservesTransparentBackgroundLikeSet()
    {
        var red = PackedRgba.Red;
        var fast = new Buffer(1, 1);
        fast.Set(0, 0, Cell.Empty.WithBackground(red));
        fast.SetFast(0, 0, Cell.FromChar('X'));

        var slow = new Buffer(1, 1);
        slow.Set(0, 0, Cell.Empty.WithBackground(red));
        slow.Set(0, 0, Cell.FromChar('X'));

        Assert.Equal(slow.Get(0, 0), fast.Get(0, 0));
        Assert.Equal(red, fast.Get(0, 0)!.Value.Background);
    }

    [Fact]
    public void SetFastUsesFullPathForOpacityStack()
    {
        var buffer = new Buffer(1, 1);
        buffer.PushOpacity(0.5f);
        buffer.SetFast(0, 0, Cell.FromChar('X').WithForeground(PackedRgba.Red));

        Assert.Equal(128, buffer.Get(0, 0)!.Value.Foreground.A);
    }

    [Fact]
    public void FilledRectangleRespectsBoundsAndScissor()
    {
        var buffer = new Buffer(5, 5);
        buffer.PushScissor(new Rect(2, 2, 2, 2));
        buffer.DrawRectFilled(new Rect(1, 1, 3, 3), Cell.FromChar('█'));

        Assert.Equal("█", TextAt(buffer, 2, 2));
        Assert.Equal("█", TextAt(buffer, 3, 3));
        Assert.Null(TextAt(buffer, 1, 1));
        Assert.Null(TextAt(buffer, 4, 4));
    }

    [Fact]
    public void EmptyFilledRectangleWritesNothing()
    {
        var buffer = new Buffer(2, 2);
        buffer.DrawRectFilled(new Rect(0, 0, 0, 0), Cell.FromChar('x'));

        Assert.True(buffer.Get(0, 0)!.Value.IsEmpty);
    }

    [Theory]
    [InlineData(1, 1, "#")]
    [InlineData(2, 2, "####")]
    [InlineData(1, 4, "####")]
    [InlineData(4, 1, "####")]
    public void RectangleOutlineHandlesDegenerateDimensions(
        ushort width,
        ushort height,
        string expectedCells)
    {
        var buffer = new Buffer(5, 5);
        buffer.DrawRectOutline(new Rect(0, 0, width, height), Cell.FromChar('#'));

        var count = buffer.Cells.ToArray().Count(static cell => !cell.IsEmpty);
        Assert.Equal(expectedCells.Length, count);
    }

    [Fact]
    public void RectangleOutlineLeavesInteriorEmpty()
    {
        var buffer = new Buffer(5, 5);
        buffer.DrawRectOutline(new Rect(0, 0, 5, 5), Cell.FromChar('#'));

        Assert.Equal("#", TextAt(buffer, 0, 0));
        Assert.Equal("#", TextAt(buffer, 4, 4));
        Assert.Equal("#", TextAt(buffer, 0, 2));
        Assert.Null(TextAt(buffer, 2, 2));
    }

    [Fact]
    public void PrintTextReturnsEndAndPreservesStyle()
    {
        var foreground = PackedRgba.Red;
        var background = PackedRgba.Blue;
        var buffer = new Buffer(20, 1);
        var template = Cell.FromChar(' ')
            .WithForeground(foreground)
            .WithBackground(background);

        var end = buffer.PrintText(2, 0, "Hello", template);

        Assert.Equal(7, end);
        Assert.Equal("H", TextAt(buffer, 2, 0));
        Assert.Equal("o", TextAt(buffer, 6, 0));
        Assert.Equal(foreground, buffer.Get(2, 0)!.Value.Foreground);
        Assert.Equal(background, buffer.Get(2, 0)!.Value.Background);
    }

    [Fact]
    public void PrintTextClipsAtExplicitAndBufferEdges()
    {
        var buffer = new Buffer(5, 1);
        var end = buffer.PrintTextClipped(0, 0, "Hello World", Cell.FromChar(' '), 5);

        Assert.Equal(5, end);
        Assert.Equal("o", TextAt(buffer, 4, 0));
        Assert.Equal(5, buffer.PrintText(0, 0, "Hello World", Cell.FromChar(' ')));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 3, 5)]
    public void PrintTextStopsWhenStartIsAtOrPastLimit(ushort x, ushort maxX, ushort end)
    {
        var buffer = new Buffer(10, 1);

        Assert.Equal(end, buffer.PrintTextClipped(x, 0, "Hello", Cell.FromChar(' '), maxX));
        Assert.True(buffer.Get(0, 0)!.Value.IsEmpty);
    }

    [Fact]
    public void WideCharacterDoesNotStartWhenItCannotFit()
    {
        var buffer = new Buffer(10, 1);

        Assert.Equal(4, buffer.PrintTextClipped(4, 0, "中", Cell.FromChar(' '), 5));
        Assert.Null(TextAt(buffer, 4, 0));
    }

    [Fact]
    public void MultiScalarGraphemeUsesFirstScalarAndClearsItsTail()
    {
        var buffer = new Buffer(4, 1);
        buffer.SetRaw(1, 0, Cell.FromChar('|'));
        var template = Cell.FromChar(' ')
            .WithForeground(PackedRgba.Red)
            .WithBackground(PackedRgba.Blue);

        var end = buffer.PrintTextClipped(0, 0, "👍🏽", template, 4);

        Assert.Equal(2, end);
        Assert.Equal("👍", TextAt(buffer, 0, 0));
        Assert.True(buffer.Get(1, 0)!.Value.IsContinuation);
    }

    [Fact]
    public void VariationSelectorSequenceUsesPinnedBaseCharacterWidth()
    {
        var buffer = new Buffer(4, 1);
        buffer.SetRaw(1, 0, Cell.FromChar('|'));

        var end = buffer.PrintTextClipped(0, 0, "⚙️", Cell.FromChar(' '), 4);

        Assert.Equal(1, end);
        Assert.Equal("⚙", TextAt(buffer, 0, 0));
        Assert.Equal("|", TextAt(buffer, 1, 0));
    }

    [Fact]
    public void LegacyWriteTextStillPreservesFullManagedGrapheme()
    {
        var buffer = new Buffer(4, 1);

        BufferPainter.WriteText(buffer, 0, 0, "e\u0301", Cell.FromChar(' '));

        Assert.Equal("e\u0301", TextAt(buffer, 0, 0));
    }

    [Theory]
    [MemberData(nameof(BorderCases))]
    public void BorderPresetsDrawTheirDeclaredCorners(BorderChars border)
    {
        var buffer = new Buffer(5, 3);
        buffer.DrawBorder(new Rect(0, 0, 5, 3), border, Cell.FromChar(' '));

        Assert.Equal(border.TopLeft.ToString(), TextAt(buffer, 0, 0));
        Assert.Equal(border.TopRight.ToString(), TextAt(buffer, 4, 0));
        Assert.Equal(border.BottomLeft.ToString(), TextAt(buffer, 0, 2));
        Assert.Equal(border.BottomRight.ToString(), TextAt(buffer, 4, 2));
        Assert.Equal(border.Horizontal.ToString(), TextAt(buffer, 2, 0));
        Assert.Equal(border.Vertical.ToString(), TextAt(buffer, 0, 1));
        Assert.Null(TextAt(buffer, 2, 1));
    }

    [Theory]
    [InlineData(1, 1, "┌")]
    [InlineData(1, 2, "┌└")]
    [InlineData(2, 1, "┌┐")]
    [InlineData(1, 3, "┌│└")]
    [InlineData(3, 1, "┌─┐")]
    public void BorderHandlesNarrowAndFlatRectangles(ushort width, ushort height, string expected)
    {
        var buffer = new Buffer(5, 5);
        buffer.DrawBorder(new Rect(0, 0, width, height), BorderChars.Square, Cell.FromChar(' '));

        var actual = new string(buffer.Cells.ToArray()
            .Where(static cell => !cell.IsEmpty)
            .Select(cell => TextForCell(buffer, cell)![0])
            .ToArray());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BorderPreservesStyleAndRespectsScissor()
    {
        var buffer = new Buffer(10, 5);
        var template = Cell.FromChar(' ')
            .WithForeground(PackedRgba.Green)
            .WithBackground(PackedRgba.Blue);
        buffer.PushScissor(new Rect(0, 0, 3, 3));
        buffer.DrawBorder(new Rect(0, 0, 6, 4), BorderChars.Square, template);

        Assert.Equal(PackedRgba.Green, buffer.Get(0, 0)!.Value.Foreground);
        Assert.Equal(PackedRgba.Blue, buffer.Get(0, 0)!.Value.Background);
        Assert.Equal("─", TextAt(buffer, 2, 0));
        Assert.Null(TextAt(buffer, 5, 0));
        Assert.Null(TextAt(buffer, 0, 3));
    }

    [Fact]
    public void CompatibilityBorderOverloadRetainsOriginalArgumentOrder()
    {
        var buffer = new Buffer(3, 2);

        BufferPainter.DrawBorder(buffer, new Rect(0, 0, 3, 2), Cell.FromChar(' '), BorderChars.Ascii);

        Assert.Equal("+", TextAt(buffer, 0, 0));
        Assert.Equal("-", TextAt(buffer, 1, 0));
    }

    [Fact]
    public void BoxFillsOnlyInteriorThenDrawsBorder()
    {
        var buffer = new Buffer(5, 4);
        var border = Cell.FromChar(' ').WithForeground(PackedRgba.Red);
        var fill = Cell.FromChar('.').WithForeground(PackedRgba.Green);

        buffer.DrawBox(new Rect(0, 0, 5, 4), BorderChars.Square, border, fill);

        Assert.Equal("┌", TextAt(buffer, 0, 0));
        Assert.Equal(PackedRgba.Red, buffer.Get(0, 0)!.Value.Foreground);
        Assert.Equal(".", TextAt(buffer, 1, 1));
        Assert.Equal(PackedRgba.Green, buffer.Get(1, 1)!.Value.Foreground);
        Assert.Equal("┘", TextAt(buffer, 4, 3));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void SmallBoxesDoNotInventInterior(ushort width, ushort height)
    {
        var buffer = new Buffer(3, 3);
        buffer.DrawBox(
            new Rect(0, 0, width, height),
            BorderChars.Square,
            Cell.FromChar(' '),
            Cell.FromChar('X'));

        Assert.DoesNotContain(buffer.Cells.ToArray(), cell => TextForCell(buffer, cell) == "X");
    }

    [Fact]
    public void PaintAreaChangesColorsWithoutChangingContent()
    {
        var buffer = new Buffer(3, 3);
        buffer.Set(1, 1, Cell.FromChar('X'));

        buffer.PaintArea(
            new Rect(0, 0, 3, 3),
            PackedRgba.Green,
            PackedRgba.Blue);

        var cell = buffer.Get(1, 1)!.Value;
        Assert.Equal("X", TextAt(buffer, 1, 1));
        Assert.Equal(PackedRgba.Green, cell.Foreground);
        Assert.Equal(PackedRgba.Blue, cell.Background);
    }

    [Fact]
    public void PaintAreaClipsToBufferAndScissor()
    {
        var buffer = new Buffer(3, 3);
        buffer.PushScissor(new Rect(1, 1, 2, 2));

        buffer.PaintArea(new Rect(0, 0, 100, 100), PackedRgba.Red, null);

        Assert.NotEqual(PackedRgba.Red, buffer.Get(0, 0)!.Value.Foreground);
        Assert.Equal(PackedRgba.Red, buffer.Get(1, 1)!.Value.Foreground);
        Assert.Equal(PackedRgba.Red, buffer.Get(2, 2)!.Value.Foreground);
    }

    [Fact]
    public void PaintAreaAppliesOpacityOnceAndCompositesBackground()
    {
        var buffer = new Buffer(1, 1);
        buffer.Set(0, 0, Cell.FromChar('X').WithBackground(PackedRgba.Blue));
        buffer.PushOpacity(0.5f);

        buffer.PaintArea(new Rect(0, 0, 1, 1), PackedRgba.Red, PackedRgba.Green);

        var cell = buffer.Get(0, 0)!.Value;
        Assert.Equal(128, cell.Foreground.A);
        Assert.Equal(byte.MaxValue, cell.Background.A);
        Assert.NotEqual(PackedRgba.Green, cell.Background);
    }

    [Fact]
    public void PaintAreaWithNoColorsPreservesCell()
    {
        var buffer = new Buffer(2, 1);
        var original = Cell.FromChar('A').WithForeground(PackedRgba.Rgb(10, 20, 30));
        buffer.Set(0, 0, original);

        buffer.PaintArea(new Rect(0, 0, 2, 1), null, null);

        Assert.Equal(original, buffer.Get(0, 0));
    }

    [Fact]
    public void NestedBordersAndTitleComposeDeterministically()
    {
        var buffer = new Buffer(12, 6);
        var template = Cell.FromChar(' ');
        buffer.DrawBorder(new Rect(0, 0, 12, 6), BorderChars.Double, template);
        buffer.DrawBorder(new Rect(1, 1, 10, 4), BorderChars.Square, template);
        buffer.PrintText(1, 0, "Title", template);

        Assert.Equal("╔", TextAt(buffer, 0, 0));
        Assert.Equal("T", TextAt(buffer, 1, 0));
        Assert.Equal("e", TextAt(buffer, 5, 0));
        Assert.Equal("═", TextAt(buffer, 6, 0));
        Assert.Equal("╗", TextAt(buffer, 11, 0));
        Assert.Equal("┌", TextAt(buffer, 1, 1));
        Assert.Equal("┘", TextAt(buffer, 10, 4));
    }

    public static TheoryData<BorderChars> BorderCases => new()
    {
        BorderChars.Square,
        BorderChars.Rounded,
        BorderChars.Double,
        BorderChars.Heavy,
        BorderChars.Ascii,
    };

    private static string? TextAt(Buffer buffer, ushort x, ushort y)
    {
        var cell = buffer.Get(x, y);
        return cell is { } value ? TextForCell(buffer, value) : null;
    }

    private static string? TextForCell(Buffer buffer, Cell cell) =>
        cell.IsEmpty || cell.IsContinuation ? null : buffer.ResolveText(cell);
}
