using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class CanvasPrimitiveTests
{
    [Theory]
    [InlineData(CanvasMode.Braille, 2, 4)]
    [InlineData(CanvasMode.Block, 2, 2)]
    [InlineData(CanvasMode.HalfBlock, 1, 2)]
    public void CanvasModeReportsCellPixelDensity(CanvasMode mode, int expectedColumns, int expectedRows)
    {
        Assert.Equal(expectedColumns, CanvasPixelRect.ColsPerCell(mode));
        Assert.Equal(expectedRows, CanvasPixelRect.RowsPerCell(mode));
    }

    [Fact]
    public void CanvasPixelRectMapsAbsoluteCellIntersectionToLocalBraillePixels()
    {
        var canvasArea = new Rect(10, 5, 20, 8);
        var overlay = new Rect(12, 7, 4, 3);

        var rect = CanvasPixelRect.FromCellIntersection(overlay, canvasArea, CanvasMode.Braille);

        Assert.Equal(new CanvasPixelRect(4, 8, 8, 12), rect);
        Assert.True(rect!.Value.Contains(4, 8));
        Assert.False(rect.Value.Contains(12, 20));
    }

    [Fact]
    public void CanvasPixelRectClipsToCanvasAreaBeforeScaling()
    {
        var canvasArea = new Rect(10, 5, 4, 3);
        var overlay = new Rect(8, 6, 4, 4);

        var rect = CanvasPixelRect.FromCellIntersection(overlay, canvasArea, CanvasMode.HalfBlock);

        Assert.Equal(new CanvasPixelRect(0, 2, 2, 4), rect);
    }

    [Fact]
    public void CanvasPixelRectReturnsNullForDisjointCells()
    {
        var canvasArea = new Rect(10, 5, 4, 3);
        var overlay = new Rect(20, 20, 2, 2);

        Assert.Null(CanvasPixelRect.FromCellIntersection(overlay, canvasArea, CanvasMode.Block));
    }

    [Fact]
    public void CanvasPainterRendersBrailleCells()
    {
        var painter = new CanvasPainter(4, 8);
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                painter.Point(x, y);
            }
        }

        var buffer = new RenderBuffer(2, 2);
        painter.Render(new Rect(0, 0, 2, 2), buffer, Cell.FromChar(' '));

        Assert.Equal("\u28ff\u28ff", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("\u28ff\u28ff", HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void CanvasPainterRenderExcludingSkipsAbsoluteCells()
    {
        var painter = new CanvasPainter(4, 8);
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                painter.Point(x, y);
            }
        }

        var buffer = new RenderBuffer(2, 2);
        painter.RenderExcluding(
            new Rect(0, 0, 2, 2),
            buffer,
            Cell.FromChar(' '),
            new Rect(1, 0, 1, 2));

        Assert.Equal("\u28ff", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(Cell.Empty, buffer.Get(1, 0));
        Assert.Equal(Cell.Empty, buffer.Get(1, 1));
    }

    [Fact]
    public void CanvasWidgetRendersPainterIntoRuntimeBounds()
    {
        var painter = new CanvasPainter(2, 4);
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                painter.Point(x, y);
            }
        }

        var buffer = new RenderBuffer(4, 3);
        new CanvasWidget { Painter = painter }
            .Render(new RuntimeRenderContext(buffer, new Rect(2, 1, 1, 1), Theme.DefaultTheme));

        Assert.Contains("\u28ff", HeadlessBufferView.RowText(buffer, 1));
        Assert.Equal(Cell.Empty, buffer.Get(0, 0));
    }
}
