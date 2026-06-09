// Upstream source: .external/frankentui/crates/ftui-widgets/src/table.rs (tests module)
// Full 1-1 port of all upstream table tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;
using Buffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public class TableTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    static char? CellChar(Buffer buf, ushort x, ushort y)
    {
        var cell = buf.Get(x, y);
        if (cell == null) return null;
        var raw = cell.Value.Content.Raw;
        if (raw == 0 || raw == 0x7FFF_FFFF || (raw & 0x8000_0000) != 0) return null;
        return (char)raw;
    }

    static PackedRgba? CellFg(Buffer buf, ushort x, ushort y)
    {
        var cell = buf.Get(x, y);
        return cell?.Foreground;
    }

    static string RowText(Buffer buf, ushort y)
    {
        var width = buf.Width;
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var cell = buf.Get(x, y);
            char ch = ' ';
            if (cell != null)
            {
                var raw = cell.Value.Content.Raw;
                if (raw != 0 && raw != 0x7FFF_FFFF && (raw & 0x8000_0000) == 0)
                    ch = (char)raw;
            }
            sb.Append(ch);
        }
        return sb.ToString().TrimEnd();
    }

    static string RawRowText(Buffer buf, ushort y)
    {
        var width = buf.Width;
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var cell = buf.Get(x, y);
            char ch = ' ';
            if (cell != null)
            {
                var raw = cell.Value.Content.Raw;
                if (raw != 0 && raw != 0x7FFF_FFFF && (raw & 0x8000_0000) == 0)
                    ch = (char)raw;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    // Helper to create a MouseGesture for a left-button down event.
    static MouseGesture MouseDown(ushort x, ushort y)
        => new(x, y, TerminalMouseButton.Left, TerminalMouseKind.Down);

    // Helper to create a MouseGesture for a move event.
    static MouseGesture MouseMoved(ushort x, ushort y)
        => new(x, y, TerminalMouseButton.None, TerminalMouseKind.Move);

    // Helper to create a MouseGesture for a scroll-up event.
    static MouseGesture MouseScrollUp()
        => new(0, 0, TerminalMouseButton.WheelUp, TerminalMouseKind.Scroll);

    // Helper to create a MouseGesture for a scroll-down event.
    static MouseGesture MouseScrollDown()
        => new(0, 0, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll);

    // ── Row builder tests ────────────────────────────────────────────────────

    [Fact]
    public void RowNewFromStrings()
    {
        var row = Row.New(["A", "B", "C"]);
        Assert.Equal(3, row.Cells.Length);
        Assert.Equal(1, row.RowHeight);
        Assert.Equal(0, row.Margin);
    }

    [Fact]
    public void RowBuilderMethods()
    {
        var row = Row.New(["X"])
            .Height(3)
            .BottomMargin(1)
            .Style(new WidgetStyle(null, null, CellStyleFlags.Bold));
        Assert.Equal(3, row.RowHeight);
        Assert.Equal(1, row.Margin);
        Assert.Equal(CellStyleFlags.Bold, row.RowStyle.Attrs);
    }

    [Fact]
    public void RowHeightZeroClamsToOne()
    {
        var row = Row.New(["X"]).Height(0);
        Assert.Equal(1, row.RowHeight);
    }

    // ── TableState tests ─────────────────────────────────────────────────────

    [Fact]
    public void TableStateDefault()
    {
        var state = new TableState();
        Assert.Null(state.Selected);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void TableStateSelect()
    {
        var state = new TableState();
        state.Select(5);
        Assert.Equal(5, state.Selected);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void TableStateDeselectPreservesOffset()
    {
        var state = new TableState { Offset = 10 };
        state.Select(3);
        Assert.Equal(3, state.Selected);
        state.Select(null);
        Assert.Null(state.Selected);
        Assert.Equal(10, state.Offset);
    }

    [Fact]
    public void TableStateScrollDownIsOverflowSafe()
    {
        // Ensure ScrollDown cannot wrap on invalid persisted offsets.
        var state = new TableState { Offset = int.MaxValue - 1 };
        state.ScrollDown(10, 100);
        Assert.Equal(99, state.Offset);
    }

    // ── Table rendering tests ────────────────────────────────────────────────

    [Fact]
    public void RenderZeroArea()
    {
        var table = new Table([Row.New(["A"])], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 0, 0);
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        ((IStatefulWidget<TableState>)table).Render(area, frame, new TableState());
        // Should not panic
    }

    [Fact]
    public void RenderEmptyRows()
    {
        var table = new Table([], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 10, 5);
        var pool = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        ((IWidget)table).Render(area, frame);
        // Should not panic; no content rendered
    }

    [Fact]
    public void RenderEmptyRowsClearsStaleViewport()
    {
        var table = new Table([], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        frame.Buffer.Fill(area, Cell.FromChar('X'));

        ((IWidget)table).Render(area, frame);

        Assert.Equal("          ", RawRowText(frame.Buffer, 0));
        Assert.Equal("          ", RawRowText(frame.Buffer, 1));
        Assert.Equal("          ", RawRowText(frame.Buffer, 2));
    }

    [Fact]
    public void RenderSingleRowSingleColumn()
    {
        var table = new Table([Row.New(["Hello"])], [TableConstraint.Fixed(10)]);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('H', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('e', CellChar(frame.Buffer, 1, 0));
        Assert.Equal('o', CellChar(frame.Buffer, 4, 0));
    }

    [Fact]
    public void RenderShorterCellClearsStalesSuffix()
    {
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);

        var longTable = new Table([Row.New(["Hello"])], [TableConstraint.Fixed(10)]);
        ((IWidget)longTable).Render(area, frame);

        var shortTable = new Table([Row.New(["Hi"])], [TableConstraint.Fixed(10)]);
        ((IWidget)shortTable).Render(area, frame);

        Assert.Equal("Hi        ", RawRowText(frame.Buffer, 0));
    }

    [Fact]
    public void RenderMultipleRows()
    {
        var table = new Table(
            [Row.New(["AA", "BB"]), Row.New(["CC", "DD"])],
            [TableConstraint.Fixed(4), TableConstraint.Fixed(4)]);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('C', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void RenderWithHeader()
    {
        var header = Row.New(["Name", "Val"]);
        var table = new Table(
            [Row.New(["foo", "42"])],
            [TableConstraint.Fixed(5), TableConstraint.Fixed(4)])
            .Header(header);

        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        ((IWidget)table).Render(area, frame);

        // Header on row 0
        Assert.Equal('N', CellChar(frame.Buffer, 0, 0));
        // Data on row 1
        Assert.Equal('f', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void RenderShorterHeaderClearsStalesSuffix()
    {
        var area = new Rect(0, 0, 10, 2);
        var pool = new GraphemePool();
        var frame = new Frame(10, 2, pool);

        var longTable = new Table([Row.New(["row"])], [TableConstraint.Fixed(10)])
            .Header(Row.New(["Header"]));
        ((IWidget)longTable).Render(area, frame);

        var shortTable = new Table([Row.New(["row"])], [TableConstraint.Fixed(10)])
            .Header(Row.New(["H"]));
        ((IWidget)shortTable).Render(area, frame);

        Assert.Equal("H         ", RawRowText(frame.Buffer, 0));
    }

    [Fact]
    public void ZeroHeightRowClampsAndPreservesVerticalFlow()
    {
        var table = new Table(
            [Row.New(["A"]).Height(0), Row.New(["B"])],
            [TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('B', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void ZeroHeightHeaderClampsToOneAndOffsetRows()
    {
        var header = Row.New(["H"]).Height(0);
        var table = new Table([Row.New(["D"])], [TableConstraint.Fixed(3)]).Header(header);

        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('H', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('D', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void RenderWithBlock()
    {
        var table = new Table([Row.New(["X"])], [TableConstraint.Fixed(5)])
            .Block(Block.Bordered());

        var area = new Rect(0, 0, 10, 5);
        var pool = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        ((IWidget)table).Render(area, frame);

        // Content should be inside the block border + padding
        Assert.Equal('X', CellChar(frame.Buffer, 2, 2));
    }

    [Fact]
    public void StatefulRenderWithSelection()
    {
        var table = new Table(
            [Row.New(["A"]), Row.New(["B"]), Row.New(["C"])],
            [TableConstraint.Fixed(5)])
            .HighlightStyle(new WidgetStyle(null, null, CellStyleFlags.Bold));

        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        var state = new TableState();
        state.Select(1);

        table.Render(area, frame, state);
        // Row 1 (index 1) should render "B"
        Assert.Equal('B', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void RowStyleMergePrecedenceAndSpanOverride()
    {
        var baseFg = PackedRgba.Rgb(10, 0, 0);
        var selectedFg = PackedRgba.Rgb(20, 0, 0);
        var hoveredFg = PackedRgba.Rgb(30, 0, 0);
        var tableFg = PackedRgba.Rgb(40, 0, 0);
        var rowFg = PackedRgba.Rgb(50, 0, 0);
        var highlightFg = PackedRgba.Rgb(60, 0, 0);
        var spanFg = PackedRgba.Rgb(70, 0, 0);

        var baseRow = new WidgetStyle(baseFg, null, null);
        var theme = new TableWidgetTheme
        {
            Row = baseRow,
            RowAlt = baseRow,
            RowSelected = new WidgetStyle(selectedFg, null, null),
            RowHover = new WidgetStyle(hoveredFg, null, null),
        };

        var text = TextContent.FromLines([
            TextLine.FromSpans([
                TextSpan.Raw("A"),
                TextSpan.Styled("B", new WidgetStyle(spanFg, null, null)),
            ])
        ]);

        var table = new Table(
            [Row.New([text]).Style(new WidgetStyle(rowFg, null, null))],
            [TableConstraint.Fixed(2)])
            .Style(new WidgetStyle(tableFg, null, null))
            .HighlightStyle(new WidgetStyle(highlightFg, null, null))
            .Theme(theme);

        var area = new Rect(0, 0, 2, 1);
        var pool = new GraphemePool();
        var frame = new Frame(2, 1, pool);
        var state = new TableState { Selected = 0, Hovered = 0 };

        table.Render(area, frame, state);

        Assert.Equal(highlightFg, CellFg(frame.Buffer, 0, 0));
        Assert.Equal(spanFg, CellFg(frame.Buffer, 1, 0));
    }

    [Fact]
    public void SelectionBelowOffsetAdjustsOffset()
    {
        var state = new TableState { Offset = 5, Selected = 2 };

        var table = new Table(
            Enumerable.Range(0, 10).Select(i => Row.New([$"Row {i}"])),
            [TableConstraint.Fixed(10)]);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        table.Render(area, frame, state);

        // Offset should have been adjusted down to selected
        Assert.Equal(2, state.Offset);
    }

    [Fact]
    public void TableClampsOffsetToFillViewportOnResize()
    {
        var rows = Enumerable.Range(0, 10).Select(i => Row.New([$"Row {i}"])).ToArray();
        var table = new Table(rows, [TableConstraint.Min(10)]);

        var pool = new GraphemePool();
        var state = new TableState { Offset = 7 };

        // Small viewport: show 7, 8, 9.
        var areaSmall = new Rect(0, 0, 10, 3);
        var frameSmall = new Frame(10, 3, pool);
        table.Render(areaSmall, frameSmall, state);
        Assert.Equal(7, state.Offset);
        Assert.Equal("Row 7", RowText(frameSmall.Buffer, 0));
        Assert.Equal("Row 9", RowText(frameSmall.Buffer, 2));

        // Larger viewport: offset should pull back to fill (5..9).
        var areaLarge = new Rect(0, 0, 10, 5);
        var frameLarge = new Frame(10, 5, pool);
        table.Render(areaLarge, frameLarge, state);
        Assert.Equal(5, state.Offset);
        Assert.Equal("Row 5", RowText(frameLarge.Buffer, 0));
        Assert.Equal("Row 9", RowText(frameLarge.Buffer, 4));
    }

    [Fact]
    public void TableClampsOffsetToFillViewportWithVariableRowHeights()
    {
        // Rows 0..8: height 1
        // Row 9: height 5
        // View height 10 should show rows 4..9 (with row 9 taking 5 lines).
        var rows = Enumerable.Range(0, 9).Select(i => Row.New([$"Row {i}"]))
            .Append(Row.New(["Row 9"]).Height(5))
            .ToArray();
        var table = new Table(rows, [TableConstraint.Min(10)]);

        var pool = new GraphemePool();
        var state = new TableState { Offset = 9 };

        var area = new Rect(0, 0, 10, 10);
        var frame = new Frame(10, 10, pool);
        table.Render(area, frame, state);

        Assert.Equal(4, state.Offset);
        Assert.Equal("Row 4", RowText(frame.Buffer, 0));
    }

    [Fact]
    public void SelectionInvalidIndexFallsBackToFirstRow()
    {
        var table = new Table([Row.New(["A"]), Row.New(["B"])], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 2);
        var pool = new GraphemePool();
        var frame = new Frame(5, 2, pool);
        var state = new TableState { Offset = 0, Selected = 99 };

        table.Render(area, frame, state);
        Assert.Equal(0, state.Selected);
    }

    [Fact]
    public void SelectionWithHeaderAccountsForHeaderHeight()
    {
        var header = Row.New(["H"]);
        var table = new Table([Row.New(["A"]), Row.New(["B"])], [TableConstraint.Fixed(5)])
            .Header(header);

        var area = new Rect(0, 0, 5, 2);
        var pool = new GraphemePool();
        var frame = new Frame(5, 2, pool);
        var state = new TableState { Offset = 0, Selected = 1 };

        table.Render(area, frame, state);
        Assert.Equal(1, state.Offset);
    }

    [Fact]
    public void RowsOverflowAreaTruncated()
    {
        var table = new Table(
            Enumerable.Range(0, 20).Select(i => Row.New([$"R{i}"])),
            [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        ((IWidget)table).Render(area, frame);

        // Only first 3 rows fit
        Assert.Equal('R', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('0', CellChar(frame.Buffer, 1, 0));
        Assert.Equal('2', CellChar(frame.Buffer, 1, 2));
    }

    [Fact]
    public void ColumnSpacingApplied()
    {
        var table = new Table(
            [Row.New(["A", "B"])],
            [TableConstraint.Fixed(3), TableConstraint.Fixed(3)])
            .ColumnSpacing(2);

        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        ((IWidget)table).Render(area, frame);

        // "A" starts at x=0
        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
    }

    [Fact]
    public void DividerStyleOverridesRowStyle()
    {
        var rowFg = PackedRgba.Rgb(120, 10, 10);
        var dividerFg = PackedRgba.Rgb(0, 200, 0);
        var rowStyle = new WidgetStyle(rowFg, null, null);
        var theme = new TableWidgetTheme
        {
            Row = rowStyle,
            RowAlt = rowStyle,
            Divider = new WidgetStyle(dividerFg, null, null),
        };

        var table = new Table(
            [Row.New(["AA", "BB"])],
            [TableConstraint.Fixed(2), TableConstraint.Fixed(2)])
            .Theme(theme);

        var area = new Rect(0, 0, 5, 1);
        var pool = new GraphemePool();
        var frame = new Frame(5, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal(dividerFg, CellFg(frame.Buffer, 2, 0));
    }

    [Fact]
    public void BlockBorderUsesThemeBorderStyle()
    {
        var borderFg = PackedRgba.Rgb(1, 2, 3);
        var theme = new TableWidgetTheme
        {
            Border = new WidgetStyle(borderFg, null, null),
        };

        var table = new Table([Row.New(["X"])], [TableConstraint.Fixed(1)])
            .Block(Block.Bordered())
            .Theme(theme);

        var area = new Rect(0, 0, 3, 3);
        var pool = new GraphemePool();
        var frame = new Frame(3, 3, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal(borderFg, CellFg(frame.Buffer, 0, 0));
    }

    [Fact]
    public void RenderClipsLongCellToColumnWidth()
    {
        var table = new Table([Row.New(["ABCDE"])], [TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 3, 1);
        var pool = new GraphemePool();
        var frame = new Frame(4, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('B', CellChar(frame.Buffer, 1, 0));
        Assert.Equal('C', CellChar(frame.Buffer, 2, 0));
        Assert.NotEqual('D', CellChar(frame.Buffer, 3, 0));
    }

    [Fact]
    public void RenderMultilineCellRespectsRowHeight()
    {
        var table = new Table([Row.New(["A\nB"]).Height(1)], [TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.NotEqual('B', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void RenderMultilineCellDrawsSecondLineWhenHeightAllows()
    {
        var table = new Table([Row.New(["A\nB"]).Height(2)], [TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('B', CellChar(frame.Buffer, 0, 1));
    }

    [Fact]
    public void MoreCellsThanColumnsTruncated()
    {
        var table = new Table(
            [Row.New(["A", "B", "C", "D"])],
            [TableConstraint.Fixed(3), TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 8, 1);
        var pool = new GraphemePool();
        var frame = new Frame(8, 1, pool);
        ((IWidget)table).Render(area, frame);
        // Should not panic; extra cells beyond column count are skipped
    }

    [Fact]
    public void HeaderTooTallForArea()
    {
        var header = Row.New(["H"]).Height(10);
        var table = new Table([Row.New(["X"])], [TableConstraint.Fixed(5)]).Header(header);

        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        ((IWidget)table).Render(area, frame);
        // Header doesn't fit; should return early without rendering data
    }

    [Fact]
    public void RowWithBottomMargin()
    {
        var table = new Table(
            [Row.New(["A"]).BottomMargin(1), Row.New(["B"])],
            [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 4);
        var pool = new GraphemePool();
        var frame = new Frame(5, 4, pool);
        ((IWidget)table).Render(area, frame);

        // Row "A" at y=0, margin leaves y=1 empty, row "B" at y=2
        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('B', CellChar(frame.Buffer, 0, 2));
    }

    [Fact]
    public void TableRegistersHitRegions()
    {
        var table = new Table(
            [Row.New(["A"]), Row.New(["B"]), Row.New(["C"])],
            [TableConstraint.Fixed(5)])
            .HitId(HitId.New(99));

        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(5, 3, pool);
        var state = new TableState();
        table.Render(area, frame, state);

        // Each row should have a hit region with the row index as data
        var hit0 = frame.HitTest(2, 0);
        var hit1 = frame.HitTest(2, 1);
        var hit2 = frame.HitTest(2, 2);

        Assert.Equal((HitId.New(99), HitRegionKind.Content, 0UL), hit0);
        Assert.Equal((HitId.New(99), HitRegionKind.Content, 1UL), hit1);
        Assert.Equal((HitId.New(99), HitRegionKind.Content, 2UL), hit2);
    }

    [Fact]
    public void TableNoHitWithoutHitId()
    {
        var table = new Table([Row.New(["A"])], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 1);
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(5, 1, pool);
        var state = new TableState();
        table.Render(area, frame, state);

        // No hit region should be registered
        Assert.Null(frame.HitTest(2, 0));
    }

    [Fact]
    public void TableNoHitWithoutHitGrid()
    {
        var table = new Table([Row.New(["A"])], [TableConstraint.Fixed(5)])
            .HitId(HitId.New(1));
        var area = new Rect(0, 0, 5, 1);
        var pool = new GraphemePool();
        var frame = new Frame(5, 1, pool); // No hit grid
        var state = new TableState();
        table.Render(area, frame, state);

        // HitTest returns null when no hit grid
        Assert.Null(frame.HitTest(2, 0));
    }

    // ── MeasurableWidget tests ────────────────────────────────────────────────

    [Fact]
    public void MeasureEmptyTable()
    {
        var table = new Table([], [TableConstraint.Fixed(5)]);
        var c = ((IMeasurableWidget)table).Measure(Size.Max);
        Assert.Equal(SizeConstraints.Zero, c);
    }

    [Fact]
    public void MeasureEmptyColumns()
    {
        var table = new Table([Row.New(["A"])], Array.Empty<TableConstraint>());
        var c = ((IMeasurableWidget)table).Measure(Size.Max);
        Assert.Equal(SizeConstraints.Zero, c);
    }

    [Fact]
    public void MeasureSingleRow()
    {
        var table = new Table([Row.New(["Hello"])], [TableConstraint.Fixed(10)]);
        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        Assert.Equal(5, c.Preferred.Width); // "Hello" is 5 chars
        Assert.Equal(1, c.Preferred.Height); // 1 row
        Assert.True(((IMeasurableWidget)table).HasIntrinsicSize());
    }

    [Fact]
    public void MeasureMultipleColumns()
    {
        var table = new Table(
            [Row.New(["A", "BB", "CCC"])],
            [TableConstraint.Fixed(5), TableConstraint.Fixed(5), TableConstraint.Fixed(5)])
            .ColumnSpacing(2);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        // Widths: 1 + 2 + 3 = 6, plus 2 gaps of 2 = 4 → total 10
        Assert.Equal(10, c.Preferred.Width);
        Assert.Equal(1, c.Preferred.Height);
    }

    [Fact]
    public void MeasureRespectsRowHeightAndColumnSpacing()
    {
        var table = new Table(
            [Row.New(["A", "BB"]).Height(2)],
            [TableConstraint.FitContent, TableConstraint.FitContent])
            .ColumnSpacing(2);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        Assert.Equal(5, c.Preferred.Width);
        Assert.Equal(2, c.Preferred.Height);
    }

    [Fact]
    public void MeasureAccountsForWideGlyphs()
    {
        var table = new Table(
            [Row.New(["界", "A"])],
            [TableConstraint.FitContent, TableConstraint.FitContent])
            .ColumnSpacing(1);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        Assert.Equal(4, c.Preferred.Width);
        Assert.Equal(1, c.Preferred.Height);
    }

    [Fact]
    public void MeasureWithHeader()
    {
        var header = Row.New(["Name", "Value"]);
        var table = new Table(
            [Row.New(["foo", "42"])],
            [TableConstraint.Fixed(5), TableConstraint.Fixed(5)])
            .Header(header);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        // Header "Name" and "Value" are wider than "foo" and "42"
        // Widths: max(4, 3) = 4, max(5, 2) = 5, plus 1 gap = 10
        Assert.Equal(10, c.Preferred.Width);
        // Height: 1 header + 1 data row = 2
        Assert.Equal(2, c.Preferred.Height);
    }

    [Fact]
    public void MeasureWithRowMargins()
    {
        var table = new Table(
            [Row.New(["A"]).BottomMargin(2), Row.New(["B"]).BottomMargin(1)],
            [TableConstraint.Fixed(5)]);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        // Heights: (1 + 2) + (1 + 1) = 5
        Assert.Equal(5, c.Preferred.Height);
    }

    [Fact]
    public void MeasureColumnWidthsFromMaxCell()
    {
        var table = new Table(
            [Row.New(["A", "BB"]), Row.New(["CCC", "D"])],
            [TableConstraint.Fixed(5), TableConstraint.Fixed(5)])
            .ColumnSpacing(1);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        // Column 0: max(1, 3) = 3
        // Column 1: max(2, 1) = 2
        // Total: 3 + 2 + 1 gap = 6
        Assert.Equal(6, c.Preferred.Width);
        Assert.Equal(2, c.Preferred.Height);
    }

    [Fact]
    public void MeasureMinIsColumnCount()
    {
        var table = new Table(
            [Row.New(["A", "B", "C"])],
            [TableConstraint.Fixed(5), TableConstraint.Fixed(5), TableConstraint.Fixed(5)]);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        // Minimum width should be at least the number of columns
        Assert.Equal(3, c.Min.Width);
        Assert.Equal(1, c.Min.Height);
    }

    [Fact]
    public void MeasureHasIntrinsicSize()
    {
        var empty = new Table([], [TableConstraint.Fixed(5)]);
        Assert.False(((IMeasurableWidget)empty).HasIntrinsicSize());

        var withRows = new Table([Row.New(["X"])], [TableConstraint.Fixed(5)]);
        Assert.True(((IMeasurableWidget)withRows).HasIntrinsicSize());

        var headerOnly = new Table([], [TableConstraint.Fixed(5)]).Header(Row.New(["Header"]));
        Assert.True(((IMeasurableWidget)headerOnly).HasIntrinsicSize());
    }

    // ── Stateful Persistence tests ────────────────────────────────────────────

    [Fact]
    public void TableStateWithPersistenceId()
    {
        var state = new TableState().WithPersistenceId("my-table");
        Assert.Equal("my-table", state.PersistenceId());
    }

    [Fact]
    public void TableStateDefaultNoPersistenceId()
    {
        var state = new TableState();
        Assert.Null(state.PersistenceId());
    }

    [Fact]
    public void TableStateSaveRestoreRoundTrip()
    {
        var state = new TableState().WithPersistenceId("test");
        state.Select(5);
        state.Offset = 3;
        state.SetSort(2, true);
        state.SetFilter("search term");

        var saved = ((IStateful<TablePersistState>)state).SaveState();
        Assert.Equal(5, saved.Selected);
        Assert.Equal(3, saved.Offset);
        Assert.Equal(2, saved.SortColumn);
        Assert.True(saved.SortAscending);
        Assert.Equal("search term", saved.Filter);

        // Reset state
        state.Select(null);
        state.Offset = 0;
        state.SetSort(null, false);
        state.SetFilter("");
        Assert.Null(state.Selected);
        Assert.Equal(0, state.Offset);
        Assert.Null(state.GetSortColumn());
        Assert.False(state.IsSortAscending());
        Assert.Empty(state.GetFilter());

        // Restore
        ((IStateful<TablePersistState>)state).RestoreState(saved);
        Assert.Equal(5, state.Selected);
        Assert.Equal(3, state.Offset);
        Assert.Equal(2, state.GetSortColumn());
        Assert.True(state.IsSortAscending());
        Assert.Equal("search term", state.GetFilter());
    }

    [Fact]
    public void TableStateKeyUsesPersistenceId()
    {
        var state = new TableState().WithPersistenceId("main-data-table");
        var key = state.StateKey;
        Assert.Equal("Table", key.WidgetType);
        Assert.Equal("main-data-table", key.InstanceId);
    }

    [Fact]
    public void TableStateKeyDefaultWhenNoId()
    {
        var state = new TableState();
        var key = state.StateKey;
        Assert.Equal("Table", key.WidgetType);
        Assert.Equal("default", key.InstanceId);
    }

    [Fact]
    public void TablePersistStateDefault()
    {
        var persist = new TablePersistState();
        Assert.Null(persist.Selected);
        Assert.Equal(0, persist.Offset);
        Assert.Null(persist.SortColumn);
        Assert.False(persist.SortAscending);
        Assert.Empty(persist.Filter);
    }

    // ── Undo Support Tests ────────────────────────────────────────────────────

    [Fact]
    public void TableStateUndoWidgetIdUnique()
    {
        var state1 = new TableState();
        var state2 = new TableState();
        Assert.NotEqual(state1.UndoId(), state2.UndoId());
    }

    [Fact]
    public void TableStateUndoSnapshotAndRestore()
    {
        var state = new TableState();
        state.Select(5);
        state.Offset = 2;
        state.SetSort(1, false);
        state.SetFilter("test filter");

        // Create snapshot
        var snapshot = ((IUndoSupport)state).CreateSnapshot();

        // Modify state
        state.Select(10);
        state.Offset = 7;
        state.SetSort(3, true);
        state.SetFilter("new filter");

        Assert.Equal(10, state.Selected);
        Assert.Equal(7, state.Offset);
        Assert.Equal(3, state.GetSortColumn());
        Assert.True(state.IsSortAscending());
        Assert.Equal("new filter", state.GetFilter());

        // Restore snapshot
        Assert.True(((IUndoSupport)state).RestoreSnapshot(snapshot));

        // Verify restored state
        Assert.Equal(5, state.Selected);
        Assert.Equal(2, state.Offset);
        Assert.Equal(1, state.GetSortColumn());
        Assert.False(state.IsSortAscending());
        Assert.Equal("test filter", state.GetFilter());
    }

    [Fact]
    public void TableStateUndoExtSort()
    {
        var state = new TableState();

        // Initial state
        Assert.Equal((null, false), ((ITableUndoExt)state).SortState());

        // Set sort
        ((ITableUndoExt)state).SetSortState(2, true);
        Assert.Equal((2, true), ((ITableUndoExt)state).SortState());

        // Change sort
        ((ITableUndoExt)state).SetSortState(0, false);
        Assert.Equal((0, false), ((ITableUndoExt)state).SortState());
    }

    [Fact]
    public void TableStateUndoExtFilter()
    {
        var state = new TableState();

        // Initial state
        Assert.Equal("", ((ITableUndoExt)state).FilterText());

        // Set filter
        ((ITableUndoExt)state).SetFilterText("search term");
        Assert.Equal("search term", ((ITableUndoExt)state).FilterText());

        // Clear filter
        ((ITableUndoExt)state).SetFilterText("");
        Assert.Equal("", ((ITableUndoExt)state).FilterText());
    }

    [Fact]
    public void TableStateRestoreWrongSnapshotTypeFails()
    {
        var state = new TableState();
        object wrongSnapshot = 42;
        Assert.False(((IUndoSupport)state).RestoreSnapshot(wrongSnapshot));
    }

    // ── Mouse handling tests ──────────────────────────────────────────────────

    [Fact]
    public void TableStateClickSelects()
    {
        var state = new TableState();
        var evt = MouseDown(5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 4UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 10);
        Assert.IsType<MouseResult.SelectedCase>(result);
        Assert.Equal(4, state.Selected);
    }

    [Fact]
    public void TableStateSecondClickActivates()
    {
        var state = new TableState();
        state.Select(4);

        var evt = MouseDown(5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 4UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 10);
        Assert.IsType<MouseResult.ActivatedCase>(result);
        Assert.Equal(4, state.Selected);
    }

    [Fact]
    public void TableStateClickWrongIdIgnored()
    {
        var state = new TableState();
        var evt = MouseDown(5, 2);
        var hit = ((HitId.New(99), HitRegionKind.Content, 4UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void TableStateHoverUpdates()
    {
        var state = new TableState();
        var evt = MouseMoved(5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.HoverChanged, result);
        Assert.Equal(3, state.Hovered);
    }

    [Fact]
    public void TableStateHoverSameIndexIgnored()
    {
        var state = new TableState { Hovered = 3 };
        var evt = MouseMoved(5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
        Assert.Equal(3, state.Hovered);
    }

    [Fact]
    public void TableStateHoverClears()
    {
        var state = new TableState { Hovered = 5 };
        var evt = MouseMoved(5, 2);
        // No hit (mouse moved off the table)
        var result = state.HandleMouse(evt, null, HitId.New(1), 10);
        Assert.Equal(MouseResult.HoverChanged, result);
        Assert.Null(state.Hovered);
    }

    [Fact]
    public void TableStateHoverClearWhenAlreadyNone()
    {
        var state = new TableState();
        var evt = MouseMoved(5, 2);
        var result = state.HandleMouse(evt, null, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void TableStateScrollWheelUp()
    {
        var state = new TableState { Offset = 10 };
        var evt = MouseScrollUp();
        var result = state.HandleMouse(evt, null, HitId.New(1), 20);
        Assert.Equal(MouseResult.Scrolled, result);
        Assert.Equal(7, state.Offset);
    }

    [Fact]
    public void TableStateScrollWheelDown()
    {
        var state = new TableState();
        var evt = MouseScrollDown();
        var result = state.HandleMouse(evt, null, HitId.New(1), 20);
        Assert.Equal(MouseResult.Scrolled, result);
        Assert.Equal(3, state.Offset);
    }

    [Fact]
    public void TableStateScrollDownClamps()
    {
        var state = new TableState { Offset = 18 };
        state.ScrollDown(5, 20);
        Assert.Equal(19, state.Offset);
    }

    [Fact]
    public void TableStateScrollUpClamps()
    {
        var state = new TableState { Offset = 1 };
        state.ScrollUp(5);
        Assert.Equal(0, state.Offset);
    }

    // ── Edge-Case Tests (bd-2rvwb) ────────────────────────────────────────────

    [Fact]
    public void RowWithFewerCellsThanColumns()
    {
        // Row has 1 cell but table declares 3 columns — extra columns should be empty
        var table = new Table(
            [Row.New(["A"])],
            [TableConstraint.Fixed(3), TableConstraint.Fixed(3), TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 12, 1);
        var pool = new GraphemePool();
        var frame = new Frame(12, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        // Columns 2 and 3 should not contain data characters at the offset position
        Assert.NotEqual('A', CellChar(frame.Buffer, 4, 0));
    }

    [Fact]
    public void ColumnSpacingZero()
    {
        // No gap between columns — cells should be adjacent
        var table = new Table(
            [Row.New(["AB", "CD"])],
            [TableConstraint.Fixed(2), TableConstraint.Fixed(2)])
            .ColumnSpacing(0);

        var area = new Rect(0, 0, 4, 1);
        var pool = new GraphemePool();
        var frame = new Frame(4, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('B', CellChar(frame.Buffer, 1, 0));
        Assert.Equal('C', CellChar(frame.Buffer, 2, 0));
        Assert.Equal('D', CellChar(frame.Buffer, 3, 0));
    }

    [Fact]
    public void RenderWithNonzeroOrigin()
    {
        // Table rendered at offset position, not (0,0)
        var table = new Table([Row.New(["X"])], [TableConstraint.Fixed(3)]);
        var area = new Rect(5, 3, 3, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 6, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('X', CellChar(frame.Buffer, 5, 3));
        // Nothing at (0,0)
        Assert.NotEqual('X', CellChar(frame.Buffer, 0, 0));
    }

    [Fact]
    public void SingleRowHeightExceedsArea()
    {
        // Row is taller than the viewport — should be clipped via scissor
        var table = new Table([Row.New(["T"]).Height(10)], [TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 3, 2);
        var pool = new GraphemePool();
        var frame = new Frame(3, 2, pool);
        ((IWidget)table).Render(area, frame);

        // First line of the row should still render
        Assert.Equal('T', CellChar(frame.Buffer, 0, 0));
    }

    [Fact]
    public void SelectionAndHoverOnSameRow()
    {
        // Both selected and hovered on same row — both styles should merge
        var selectedFg = PackedRgba.Rgb(100, 0, 0);
        var hoveredFg = PackedRgba.Rgb(0, 100, 0);
        var highlightFg = PackedRgba.Rgb(0, 0, 100);

        var theme = new TableWidgetTheme
        {
            RowSelected = new WidgetStyle(selectedFg, null, null),
            RowHover = new WidgetStyle(hoveredFg, null, null),
        };

        var table = new Table([Row.New(["X"])], [TableConstraint.Fixed(3)])
            .HighlightStyle(new WidgetStyle(highlightFg, null, null))
            .Theme(theme);

        var area = new Rect(0, 0, 3, 1);
        var pool = new GraphemePool();
        var frame = new Frame(3, 1, pool);
        var state = new TableState { Selected = 0, Hovered = 0 };

        table.Render(area, frame, state);
        // Highlight style wins (applied last in merge chain)
        Assert.Equal(highlightFg, CellFg(frame.Buffer, 0, 0));
    }

    [Fact]
    public void AlternatingRowStyles()
    {
        // Even/odd rows should get different theme styles
        var evenFg = PackedRgba.Rgb(10, 10, 10);
        var oddFg = PackedRgba.Rgb(20, 20, 20);
        var theme = new TableWidgetTheme
        {
            Row = new WidgetStyle(evenFg, null, null),
            RowAlt = new WidgetStyle(oddFg, null, null),
        };

        var table = new Table(
            [Row.New(["E"]), Row.New(["O"]), Row.New(["E2"])],
            [TableConstraint.Fixed(3)])
            .Theme(theme);

        var area = new Rect(0, 0, 3, 3);
        var pool = new GraphemePool();
        var frame = new Frame(3, 3, pool);
        ((IWidget)table).Render(area, frame);

        // Row 0 is even, row 1 is odd, row 2 is even
        Assert.Equal(evenFg, CellFg(frame.Buffer, 0, 0));
        Assert.Equal(oddFg, CellFg(frame.Buffer, 0, 1));
        Assert.Equal(evenFg, CellFg(frame.Buffer, 0, 2));
    }

    [Fact]
    public void ScrollUpFromZeroStaysZero()
    {
        var state = new TableState();
        state.ScrollUp(10);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void ScrollDownWithZeroRows()
    {
        var state = new TableState();
        state.ScrollDown(5, 0);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void ScrollDownWithSingleRow()
    {
        var state = new TableState();
        state.ScrollDown(5, 1);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void MouseClickOnRowExceedingRowCount()
    {
        // Hit data row index >= row_count should be ignored
        var state = new TableState();
        var evt = MouseDown(0, 0);
        var hit = ((HitId.New(1), HitRegionKind.Content, 100UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Ignored, result);
        Assert.Null(state.Selected);
    }

    [Fact]
    public void MouseRightClickIgnored()
    {
        var state = new TableState();
        var evt = new MouseGesture(0, 0, TerminalMouseButton.Right, TerminalMouseKind.Down);
        var hit = ((HitId.New(1), HitRegionKind.Content, 2UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void MouseHoverOnRowExceedingRowCount()
    {
        var state = new TableState();
        var evt = MouseMoved(0, 0);
        var hit = ((HitId.New(1), HitRegionKind.Content, 100UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        // Moves off widget, hover cleared (was None, stays None)
        Assert.Equal(MouseResult.Ignored, result);
        Assert.Null(state.Hovered);
    }

    [Fact]
    public void SelectDeselectPreservesOffsetThenReselect()
    {
        var state = new TableState { Offset = 15 };
        state.Select(20);
        Assert.Equal(20, state.Selected);
        Assert.Equal(15, state.Offset); // offset not reset on select

        state.Select(null);
        Assert.Equal(15, state.Offset); // preserve viewport on deselect

        state.Select(3);
        Assert.Equal(3, state.Selected);
        Assert.Equal(15, state.Offset); // still preserved after reselect
    }

    [Fact]
    public void OffsetClampedWhenRowsEmpty()
    {
        var table = new Table([], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        var state = new TableState { Offset = 999 };
        table.Render(area, frame, state);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void SelectionClampsWhenRowsEmpty()
    {
        var table = new Table([], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        var state = new TableState { Selected = 5 };
        table.Render(area, frame, state);
        Assert.Null(state.Selected);
    }

    [Fact]
    public void HeaderWithBottomMarginOffsetsRows()
    {
        var header = Row.New(["H"]).BottomMargin(2);
        var table = new Table([Row.New(["D"])], [TableConstraint.Fixed(3)]).Header(header);

        var area = new Rect(0, 0, 3, 5);
        var pool = new GraphemePool();
        var frame = new Frame(3, 5, pool);
        ((IWidget)table).Render(area, frame);

        // Header at y=0, margin of 2, data at y=3
        Assert.Equal('H', CellChar(frame.Buffer, 0, 0));
        Assert.Equal('D', CellChar(frame.Buffer, 0, 3));
    }

    [Fact]
    public void BlockPlusHeaderFillEntireArea()
    {
        // Block chrome is 4 rows (borders + padding), header takes 1 row — 5 rows total.
        // With area height=5, no data rows should render.
        var header = Row.New(["H"]);
        var table = new Table([Row.New(["X"])], [TableConstraint.Fixed(3)])
            .Block(Block.Bordered())
            .Header(header);

        var area = new Rect(0, 0, 5, 5);
        var pool = new GraphemePool();
        var frame = new Frame(5, 5, pool);
        ((IWidget)table).Render(area, frame);

        // Header should render inside the border + padding
        Assert.Equal('H', CellChar(frame.Buffer, 2, 2));
        // Data row "X" should NOT appear (no room)
        var dataRendered = Enumerable.Range(0, 5).Any(x =>
            Enumerable.Range(0, 5).Any(y =>
                CellChar(frame.Buffer, (ushort)x, (ushort)y) == 'X'));
        Assert.False(dataRendered);
    }

    [Fact]
    public void MinConstraintMeasure()
    {
        var table = new Table([Row.New(["AB"])], [TableConstraint.Min(10)]);
        var c = ((IMeasurableWidget)table).Measure(Size.Max);
        // Preferred width based on content, not the constraint minimum
        Assert.Equal(2, c.Preferred.Width);
        Assert.Equal(1, c.Preferred.Height);
    }

    [Fact]
    public void PercentageConstraintRender()
    {
        // Percentage constraints should not panic and produce reasonable layout
        var table = new Table(
            [Row.New(["A", "B"])],
            [TableConstraint.Percentage(50f), TableConstraint.Percentage(50f)]);
        var area = new Rect(0, 0, 20, 1);
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
    }

    [Fact]
    public void FitContentConstraintMeasure()
    {
        var table = new Table(
            [Row.New(["Hello", "World"])],
            [TableConstraint.FitContent, TableConstraint.FitContent])
            .ColumnSpacing(1);

        var c = ((IMeasurableWidget)table).Measure(Size.Max);
        // "Hello" = 5, "World" = 5, spacing = 1 → 11
        Assert.Equal(11, c.Preferred.Width);
    }

    [Fact]
    public void MeasureWithBlockAddsOverhead()
    {
        var tableNoBlock = new Table([Row.New(["X"])], [TableConstraint.Fixed(3)]);
        var tableWithBlock = new Table([Row.New(["X"])], [TableConstraint.Fixed(3)])
            .Block(Block.Bordered());

        var cNo = ((IMeasurableWidget)tableNoBlock).Measure(Size.Max);
        var cWith = ((IMeasurableWidget)tableWithBlock).Measure(Size.Max);

        // Block chrome (borders + padding) adds 4 to width and 4 to height.
        Assert.Equal(cNo.Preferred.Width + 4, cWith.Preferred.Width);
        Assert.Equal(cNo.Preferred.Height + 4, cWith.Preferred.Height);
    }

    [Fact]
    public void VariableHeightRowsSelectionScrollsDown()
    {
        // Rows: height 1, 1, 5, 1, 1. Viewport=4 rows.
        // Select row 4 (past the tall row) should adjust offset.
        var rows = new[]
        {
            Row.New(["A"]),
            Row.New(["B"]),
            Row.New(["C"]).Height(5),
            Row.New(["D"]),
            Row.New(["E"]),
        };
        var table = new Table(rows, [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 4);
        var pool = new GraphemePool();
        var frame = new Frame(5, 4, pool);
        var state = new TableState { Selected = 4 };
        table.Render(area, frame, state);

        // Selection should be visible; offset adjusted
        Assert.True(state.Offset > 0);
        Assert.Equal(4, state.Selected);
    }

    [Fact]
    public void ManyRowsWithMarginsViewportClamping()
    {
        // 20 rows each with bottom_margin=1, viewport=5 lines.
        // Each row occupies 2 lines (1 content + 1 margin). Max 2 rows visible.
        var rows = Enumerable.Range(0, 20).Select(i => Row.New([$"R{i}"]).BottomMargin(1)).ToArray();
        var table = new Table(rows, [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 5, 5);
        var pool = new GraphemePool();
        var frame = new Frame(5, 5, pool);
        var state = new TableState { Offset = 19 };
        table.Render(area, frame, state);

        // Offset should be clamped back to fill viewport
        Assert.True(state.Offset < 19);
    }

    [Fact]
    public void RenderAreaWidthOne()
    {
        // Extremely narrow area — should not panic
        var table = new Table([Row.New(["Hello"])], [TableConstraint.Fixed(5)]);
        var area = new Rect(0, 0, 1, 1);
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('H', CellChar(frame.Buffer, 0, 0));
    }

    [Fact]
    public void RenderAreaHeightOne()
    {
        // Minimal height — should show first row
        var table = new Table([Row.New(["A"]), Row.New(["B"])], [TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 3, 1);
        var pool = new GraphemePool();
        var frame = new Frame(3, 1, pool);
        ((IWidget)table).Render(area, frame);

        Assert.Equal('A', CellChar(frame.Buffer, 0, 0));
    }

    [Fact]
    public void HitRegionsWithOffset()
    {
        // When scrolled, hit data should still encode logical row index
        var table = new Table(
            Enumerable.Range(0, 10).Select(i => Row.New([$"R{i}"])),
            [TableConstraint.Fixed(5)])
            .HitId(HitId.New(42));

        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(5, 3, pool);
        var state = new TableState { Offset = 5 };
        table.Render(area, frame, state);

        // Row at y=0 should be logical row 5
        var hit0 = frame.HitTest(2, 0);
        Assert.Equal((HitId.New(42), HitRegionKind.Content, 5UL), hit0);

        var hit1 = frame.HitTest(2, 1);
        Assert.Equal((HitId.New(42), HitRegionKind.Content, 6UL), hit1);
    }

    [Fact]
    public void TableStateSortDefaults()
    {
        var state = new TableState();
        Assert.Null(state.GetSortColumn());
        Assert.False(state.IsSortAscending());
        Assert.Empty(state.GetFilter());
    }

    [Fact]
    public void TableStateSetSortToggle()
    {
        var state = new TableState();
        state.SetSort(0, true);
        Assert.Equal(0, state.GetSortColumn());
        Assert.True(state.IsSortAscending());

        // Toggle direction
        state.SetSort(0, false);
        Assert.False(state.IsSortAscending());

        // Change column
        state.SetSort(3, true);
        Assert.Equal(3, state.GetSortColumn());

        // Clear sort
        state.SetSort(null, false);
        Assert.Null(state.GetSortColumn());
    }

    [Fact]
    public void TablePersistRoundTripPreservesHoveredNone()
    {
        var state = new TableState().WithPersistenceId("t");
        state.Select(3);
        state.Hovered = 7;
        state.Offset = 2;

        var saved = ((IStateful<TablePersistState>)state).SaveState();
        ((IStateful<TablePersistState>)state).RestoreState(saved);

        // hovered is deliberately NOT persisted (transient state)
        Assert.Null(state.Hovered);
        Assert.Equal(3, state.Selected);
        Assert.Equal(2, state.Offset);
    }

    [Fact]
    public void UndoSnapshotClearsHovered()
    {
        var state = new TableState();
        state.Select(2);
        state.Hovered = 5;

        var snap = ((IUndoSupport)state).CreateSnapshot();

        // Modify
        state.Select(9);
        state.Hovered = 8;

        // Restore
        Assert.True(((IUndoSupport)state).RestoreSnapshot(snap));
        Assert.Equal(2, state.Selected);
        // hovered is cleared on restore (not preserved in snapshot)
        Assert.Null(state.Hovered);
    }

    [Fact]
    public void WideCharsInRender()
    {
        // CJK characters are 2 cells wide — should clip correctly.
        var table = new Table([Row.New(["界界界"])], [TableConstraint.Fixed(4)]);
        var area = new Rect(0, 0, 4, 1);
        var pool = new GraphemePool();
        var frame = new Frame(4, 1, pool);
        ((IWidget)table).Render(area, frame);

        // The cell at (0,0) should have content (not empty).
        var cell = frame.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        Assert.False(cell.Value.Content.IsEmpty, "first cell should contain CJK content, not be empty");
        // Cell at (1,0) should be a continuation marker for the wide char
        var cell1 = frame.Buffer.Get(1, 0);
        Assert.NotNull(cell1);
        Assert.True(cell1.Value.Content.IsContinuation, "second cell should be continuation of wide char");
    }

    [Fact]
    public void EmptyRowCells()
    {
        // Row with empty strings — should render without panic
        var table = new Table(
            [Row.New(["", "", ""])],
            [TableConstraint.Fixed(3), TableConstraint.Fixed(3), TableConstraint.Fixed(3)]);
        var area = new Rect(0, 0, 11, 1);
        var pool = new GraphemePool();
        var frame = new Frame(11, 1, pool);
        ((IWidget)table).Render(area, frame);
        // Should not panic; cells empty
    }

    [Fact]
    public void MeasureWithManyRowsSaturates()
    {
        // Height computation should use saturating arithmetic
        var rows = Enumerable.Range(0, 10000).Select(_ => Row.New(["X"]).Height(100)).ToArray();
        var table = new Table(rows, [TableConstraint.Fixed(3)]);
        var c = ((IMeasurableWidget)table).Measure(Size.Max);

        // Should not overflow — saturates at ushort.MaxValue
        Assert.True(c.Preferred.Height > 0);
    }

    [Fact]
    public void VariableHeightRowsRespectViewportVisibleRange()
    {
        var rows = new[]
        {
            Row.New(["R0"]),
            Row.New(["R1"]).Height(2),
            Row.New(["R2"]),
            Row.New(["R3"]),
        };
        var table = new Table(rows, [TableConstraint.Fixed(4)]);
        var area = new Rect(0, 0, 4, 3);
        var pool = new GraphemePool();
        var frame = new Frame(4, 3, pool);
        var state = new TableState { Offset = 1 };

        table.Render(area, frame, state);

        Assert.Equal(1, state.Offset);
        Assert.Equal("R1", RowText(frame.Buffer, 0));
        Assert.Equal("", RowText(frame.Buffer, 1));
        Assert.Equal("R2", RowText(frame.Buffer, 2));
    }

    [Fact]
    public void Render100kRowsStaysWithin8msFrameBudget()
    {
        var rows = Enumerable.Range(0, 100_000).Select(_ => Row.New(["row"])).ToArray();
        var table = new Table(rows, [TableConstraint.Fixed(12)]);
        var area = new Rect(0, 0, 12, 24);
        var state = new TableState { Offset = 50_000 };
        var pool = new GraphemePool();

        // Warm up branch prediction and caches.
        var warmup = new Frame(12, 24, pool);
        table.Render(area, warmup, state);

        var iterations = 20;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var frame = new Frame(12, 24, pool);
            table.Render(area, frame, state);
        }
        sw.Stop();
        var perFrame = sw.Elapsed / iterations;

        Assert.True(
            perFrame <= System.TimeSpan.FromMilliseconds(8),
            $"100k-row table render exceeded 8ms budget: {perFrame}");
    }

    // DIVERGENCE: tracing_table_render_span_reports_row_counts is not ported.
    // The upstream test requires the tracing subscriber infrastructure which is
    // Rust-specific. No .NET equivalent is available. The #[cfg(feature="tracing")]
    // guard means this test only runs with the feature enabled upstream.
}
