// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/grid.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class GridTests
{
    [Fact]
    public void EmptyGrid()
    {
        GridLayout layout = Grid.New().Split(new Rect(0, 0, 100, 50));
        Assert.Equal(0, layout.NumRows());
        Assert.Equal(0, layout.NumColumns());
    }

    [Fact]
    public void Simple2x2Grid()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Fixed(10)])
            .Columns([Constraint.Fixed(20), Constraint.Fixed(20)])
            .Split(new Rect(0, 0, 100, 50));
        Assert.Equal(2, layout.NumRows());
        Assert.Equal(2, layout.NumColumns());
        Assert.Equal(new Rect(0, 0, 20, 10), layout.Cell(0, 0));
        Assert.Equal(new Rect(20, 0, 20, 10), layout.Cell(0, 1));
        Assert.Equal(new Rect(0, 10, 20, 10), layout.Cell(1, 0));
        Assert.Equal(new Rect(20, 10, 20, 10), layout.Cell(1, 1));
    }

    [Fact]
    public void GridWithGaps()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Fixed(10)])
            .Columns([Constraint.Fixed(20), Constraint.Fixed(20)])
            .RowGap(2).ColGap(5).Split(new Rect(0, 0, 100, 50));
        Assert.Equal(new Rect(0, 0, 20, 10), layout.Cell(0, 0));
        Assert.Equal(new Rect(25, 0, 20, 10), layout.Cell(0, 1));
        Assert.Equal(new Rect(0, 12, 20, 10), layout.Cell(1, 0));
        Assert.Equal(new Rect(25, 12, 20, 10), layout.Cell(1, 1));
    }

    [Fact]
    public void PercentageConstraints()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Percentage(50f), Constraint.Percentage(50f)])
            .Columns([Constraint.Percentage(30f), Constraint.Percentage(70f)])
            .Split(new Rect(0, 0, 100, 50));
        Assert.Equal((ushort)25, layout.RowHeight(0));
        Assert.Equal((ushort)25, layout.RowHeight(1));
        Assert.Equal((ushort)30, layout.ColumnWidth(0));
        Assert.Equal((ushort)70, layout.ColumnWidth(1));
    }

    [Fact]
    public void MinConstraintsFillSpace()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Min(5)])
            .Columns([Constraint.Fixed(20), Constraint.Min(10)])
            .Split(new Rect(0, 0, 100, 50));
        Assert.Equal((ushort)10, layout.RowHeight(0));
        Assert.Equal((ushort)40, layout.RowHeight(1));
        Assert.Equal((ushort)20, layout.ColumnWidth(0));
        Assert.Equal((ushort)80, layout.ColumnWidth(1));
    }

    [Fact]
    public void GridSpanClampsOutOfBounds()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(4), Constraint.Fixed(6)])
            .Columns([Constraint.Fixed(8), Constraint.Fixed(12)])
            .Split(new Rect(0, 0, 40, 20));
        Assert.Equal(new Rect(8, 4, 12, 6), layout.Span(1, 1, 5, 5));
    }

    [Fact]
    public void GridSpanIncludesGapsBetweenTracks()
    {
        GridLayout layout = Grid.New().Rows([Constraint.Fixed(3)])
            .Columns([Constraint.Fixed(2), Constraint.Fixed(2), Constraint.Fixed(2)])
            .ColGap(1).Split(new Rect(0, 0, 20, 10));
        Assert.Equal(new Rect(0, 0, 8, 3), layout.Span(0, 0, 1, 3));
    }

    [Fact]
    public void GridTinyAreaWithGapsProducesZeroTracks()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(1), Constraint.Fixed(1)])
            .Columns([Constraint.Fixed(1), Constraint.Fixed(1)])
            .RowGap(2).ColGap(2).Split(new Rect(0, 0, 1, 1));
        Assert.Equal((ushort)0, layout.RowHeight(0));
        Assert.Equal((ushort)0, layout.RowHeight(1));
        Assert.Equal((ushort)0, layout.ColumnWidth(0));
        Assert.Equal((ushort)0, layout.ColumnWidth(1));
    }

    [Fact]
    public void CellSpanning()
    {
        GridLayout layout = ThreeByThree().Split(new Rect(0, 0, 100, 50));
        Assert.Equal(new Rect(0, 0, 20, 10), layout.Span(0, 0, 1, 1));
        Assert.Equal(new Rect(0, 0, 40, 10), layout.Span(0, 0, 1, 2));
        Assert.Equal(new Rect(0, 0, 20, 20), layout.Span(0, 0, 2, 1));
        Assert.Equal(new Rect(0, 0, 40, 20), layout.Span(0, 0, 2, 2));
    }

    [Fact]
    public void CellSpanningWithGaps()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Fixed(10)])
            .Columns([Constraint.Fixed(20), Constraint.Fixed(20)])
            .RowGap(2).ColGap(5).Split(new Rect(0, 0, 100, 50));
        Rect full = layout.Span(0, 0, 2, 2);
        Assert.Equal((ushort)45, full.Width);
        Assert.Equal((ushort)22, full.Height);
    }

    [Fact]
    public void NamedAreas()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(5), Constraint.Min(10), Constraint.Fixed(3)])
            .Columns([Constraint.Fixed(20), Constraint.Min(30)])
            .Area("header", GridArea.Span(0, 0, 1, 2))
            .Area("sidebar", GridArea.Span(1, 0, 2, 1))
            .Area("content", GridArea.Cell(1, 1))
            .Area("footer", GridArea.Cell(2, 1))
            .Split(new Rect(0, 0, 80, 30));
        Rect header = layout.Area("header")!.Value;
        Assert.Equal((ushort)0, header.Y);
        Assert.Equal((ushort)5, header.Height);
        Rect sidebar = layout.Area("sidebar")!.Value;
        Assert.Equal((ushort)0, sidebar.X);
        Assert.Equal((ushort)20, sidebar.Width);
        Rect content = layout.Area("content")!.Value;
        Assert.Equal((ushort)20, content.X);
        Assert.Equal((ushort)5, content.Y);
        Rect footer = layout.Area("footer")!.Value;
        Assert.Equal((ushort)(content.Y + content.Height), footer.Y);
    }

    [Fact]
    public void OutOfBoundsReturnsEmpty()
    {
        GridLayout layout = OneCell().Split(new Rect(0, 0, 100, 50));
        Assert.Equal(default, layout.Cell(5, 5));
        Assert.Equal(default, layout.Cell(0, 5));
        Assert.Equal(default, layout.Cell(5, 0));
    }

    [Fact]
    public void IterCells()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Fixed(10)])
            .Columns([Constraint.Fixed(20), Constraint.Fixed(20)])
            .Split(new Rect(0, 0, 100, 50));
        var cells = layout.IterCells().ToArray();
        Assert.Equal(4, cells.Length);
        Assert.Equal((0, 0, new Rect(0, 0, 20, 10)), cells[0]);
        Assert.Equal((0, 1, new Rect(20, 0, 20, 10)), cells[1]);
        Assert.Equal((1, 0, new Rect(0, 10, 20, 10)), cells[2]);
        Assert.Equal((1, 1, new Rect(20, 10, 20, 10)), cells[3]);
    }

    [Fact]
    public void UndefinedAreaReturnsNone() =>
        Assert.Null(OneCell().Split(new Rect(0, 0, 100, 50)).Area("nonexistent"));

    [Fact]
    public void EmptyAreaProducesEmptyCells()
    {
        GridLayout layout = OneCell().Split(new Rect(0, 0, 0, 0));
        Assert.Equal(new Rect(0, 0, 0, 0), layout.Cell(0, 0));
    }

    [Fact]
    public void OffsetArea()
    {
        GridLayout layout = OneCell().Split(new Rect(10, 5, 100, 50));
        Assert.Equal(new Rect(10, 5, 20, 10), layout.Cell(0, 0));
    }

    [Fact]
    public void RatioConstraints()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Ratio(1, 3), Constraint.Ratio(2, 3)])
            .Columns([Constraint.Fixed(30)]).Split(new Rect(0, 0, 30, 30));
        Assert.Equal((ushort)10, layout.RowHeight(0));
        Assert.Equal((ushort)20, layout.RowHeight(1));
    }

    [Fact]
    public void MaxConstraints()
    {
        GridLayout layout = Grid.New().Rows([Constraint.Max(5), Constraint.Fixed(20)])
            .Columns([Constraint.Fixed(30)]).Split(new Rect(0, 0, 30, 30));
        Assert.True(layout.RowHeight(0) <= 5);
        Assert.Equal((ushort)20, layout.RowHeight(1));
    }

    [Fact]
    public void FixedConstraintsExceedAvailableClamped()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Fixed(10)])
            .Columns([Constraint.Fixed(7), Constraint.Fixed(7)])
            .Split(new Rect(0, 0, 10, 15));
        Assert.Equal(new ushort[] { 10, 5 }, new[] { layout.RowHeight(0), layout.RowHeight(1) });
        Assert.Equal(new ushort[] { 7, 3 }, new[] { layout.ColumnWidth(0), layout.ColumnWidth(1) });
    }

    [Fact]
    public void RatioConstraintsCalculateStrictly()
    {
        GridLayout layout = Grid.New().Rows([Constraint.Fixed(1)])
            .Columns([Constraint.Ratio(1, 3), Constraint.Ratio(2, 3)])
            .Split(new Rect(0, 0, 5, 1));
        Assert.Equal((ushort)1, layout.ColumnWidth(0));
        Assert.Equal((ushort)3, layout.ColumnWidth(1));
    }

    [Fact]
    public void UniformGapSetsBoth()
    {
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(10), Constraint.Fixed(10)])
            .Columns([Constraint.Fixed(20), Constraint.Fixed(20)])
            .Gap(3).Split(new Rect(0, 0, 100, 50));
        Assert.Equal((ushort)23, layout.Cell(0, 1).X);
        Assert.Equal((ushort)13, layout.Cell(1, 0).Y);
    }

    [Fact]
    public void GridAreaCellIs1x1Span()
    {
        GridArea area = GridArea.Cell(2, 3);
        Assert.Equal(new GridArea(2, 3, 1, 1), area);
    }

    [Fact]
    public void GridAreaSpanClampsZero()
    {
        GridArea area = GridArea.Span(0, 0, 0, 0);
        Assert.Equal(1, area.Rowspan);
        Assert.Equal(1, area.Colspan);
    }

    [Fact]
    public void GridNumRowsCols()
    {
        Grid grid = Grid.New()
            .Rows([Constraint.Fixed(5), Constraint.Fixed(5), Constraint.Fixed(5)])
            .Columns([Constraint.Fixed(10), Constraint.Fixed(10)]);
        Assert.Equal(3, grid.NumRows());
        Assert.Equal(2, grid.NumColumns());
    }

    [Fact]
    public void GridRowHeightColWidthOutOfBounds()
    {
        GridLayout layout = OneCell().Split(new Rect(0, 0, 100, 50));
        Assert.Equal((ushort)10, layout.RowHeight(0));
        Assert.Equal((ushort)0, layout.RowHeight(99));
        Assert.Equal((ushort)20, layout.ColumnWidth(0));
        Assert.Equal((ushort)0, layout.ColumnWidth(99));
    }

    [Fact]
    public void GridSpanClampedToBounds()
    {
        GridLayout layout = OneCell().Split(new Rect(0, 0, 100, 50));
        Assert.Equal(new Rect(0, 0, 20, 10), layout.Span(0, 0, 5, 5));
    }

    [Fact]
    public void GridWithAllConstraintTypes()
    {
        GridLayout layout = Grid.New()
            .Rows([
                Constraint.Fixed(5), Constraint.Percentage(20f), Constraint.Min(3),
                Constraint.Max(10), Constraint.Ratio(1, 4),
            ])
            .Columns([Constraint.Fixed(30)]).Split(new Rect(0, 0, 30, 50));
        int total = Enumerable.Range(0, layout.NumRows()).Sum(row => layout.RowHeight(row));
        Assert.True(total <= 50);
    }

    [Fact]
    public void InvariantTotalSizeWithinBounds()
    {
        foreach ((ushort width, ushort height) in new (ushort, ushort)[] { (50, 30), (100, 50), (80, 24) })
        {
            GridLayout layout = Grid.New()
                .Rows([Constraint.Fixed(10), Constraint.Min(5), Constraint.Percentage(20f)])
                .Columns([Constraint.Fixed(15), Constraint.Min(10), Constraint.Ratio(1, 2)])
                .Split(new Rect(0, 0, width, height));
            int totalHeight = Enumerable.Range(0, layout.NumRows()).Sum(row => layout.RowHeight(row));
            int totalWidth = Enumerable.Range(0, layout.NumColumns()).Sum(column => layout.ColumnWidth(column));
            Assert.True(totalHeight <= height);
            Assert.True(totalWidth <= width);
        }
    }

    [Fact]
    public void InvariantCellsWithinArea()
    {
        var area = new Rect(10, 20, 80, 60);
        GridLayout layout = Grid.New()
            .Rows([Constraint.Fixed(15), Constraint.Min(10), Constraint.Fixed(15)])
            .Columns([Constraint.Fixed(20), Constraint.Min(20), Constraint.Fixed(20)])
            .RowGap(2).ColGap(3).Split(area);
        foreach ((_, _, Rect cell) in layout.IterCells())
        {
            Assert.True(cell.X >= area.X);
            Assert.True(cell.Y >= area.Y);
            Assert.True(cell.Right <= area.Right);
            Assert.True(cell.Bottom <= area.Bottom);
        }
    }

    [Fact]
    public void GridLayoutSpanClampsZero()
    {
        GridLayout layout = OneCell().Split(new Rect(0, 0, 100, 50));
        Rect span = layout.Span(0, 0, 0, 0);
        Assert.Equal(layout.Cell(0, 0), span);
        Assert.Equal((ushort)20, span.Width);
        Assert.Equal((ushort)10, span.Height);
    }

    private static Grid OneCell() => Grid.New()
        .Rows([Constraint.Fixed(10)]).Columns([Constraint.Fixed(20)]);

    private static Grid ThreeByThree() => Grid.New()
        .Rows([Constraint.Fixed(10), Constraint.Fixed(10), Constraint.Fixed(10)])
        .Columns([Constraint.Fixed(20), Constraint.Fixed(20), Constraint.Fixed(20)]);
}
