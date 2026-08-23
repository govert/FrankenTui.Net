// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/grid.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust HashMap/SmallVec storage maps to Dictionary/arrays; exposed
// layout results remain read-only and retain the same ordering and geometry.

using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>Definition of a named grid area.</summary>
public readonly record struct GridArea(int Row, int Column, int Rowspan, int Colspan)
{
    public static GridArea Cell(int row, int column) => new(row, column, 1, 1);

    public static GridArea Span(int row, int column, int rowspan, int colspan) =>
        new(row, column, Math.Max(rowspan, 1), Math.Max(colspan, 1));
}

/// <summary>A constraint-based two-dimensional layout container.</summary>
public sealed class Grid
{
    private readonly Constraint[] _rowConstraints;
    private readonly Constraint[] _columnConstraints;
    private readonly ushort _rowGap;
    private readonly ushort _columnGap;
    private readonly Dictionary<string, GridArea> _namedAreas;
    private readonly OverflowBehavior _overflow;

    private Grid(
        Constraint[] rowConstraints,
        Constraint[] columnConstraints,
        ushort rowGap,
        ushort columnGap,
        Dictionary<string, GridArea> namedAreas,
        OverflowBehavior overflow)
    {
        _rowConstraints = rowConstraints;
        _columnConstraints = columnConstraints;
        _rowGap = rowGap;
        _columnGap = columnGap;
        _namedAreas = namedAreas;
        _overflow = overflow;
    }

    public static Grid New() => new(
        [],
        [],
        0,
        0,
        new Dictionary<string, GridArea>(StringComparer.Ordinal),
        FrankenTui.Layout.OverflowBehavior.Clip);

    public static Grid Default => New();

    public Grid Rows(IEnumerable<Constraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        return Copy(rowConstraints: constraints.ToArray());
    }

    public Grid Columns(IEnumerable<Constraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        return Copy(columnConstraints: constraints.ToArray());
    }

    public Grid RowGap(ushort gap) => Copy(rowGap: gap);

    public Grid ColumnGap(ushort gap) => Copy(columnGap: gap);

    /// <summary>Alias matching the upstream <c>col_gap</c> vocabulary.</summary>
    public Grid ColGap(ushort gap) => ColumnGap(gap);

    public Grid Gap(ushort gap) => Copy(rowGap: gap, columnGap: gap);

    public Grid Area(string name, GridArea area)
    {
        ArgumentNullException.ThrowIfNull(name);
        Dictionary<string, GridArea> namedAreas = CloneAreas();
        namedAreas[name] = area;
        return Copy(namedAreas: namedAreas);
    }

    public Grid Overflow(OverflowBehavior overflow) =>
        Copy(overflow: overflow ?? throw new ArgumentNullException(nameof(overflow)));

    public OverflowBehavior OverflowBehavior() => _overflow;

    public int NumRows() => _rowConstraints.Length;

    public int NumColumns() => _columnConstraints.Length;

    /// <summary>Alias matching the upstream <c>num_cols</c> vocabulary.</summary>
    public int NumCols() => NumColumns();

    public GridLayout Split(Rect area)
    {
        int rowCount = _rowConstraints.Length;
        int columnCount = _columnConstraints.Length;
        Dictionary<string, GridArea> areas = CloneAreas();

        if (rowCount == 0 || columnCount == 0 || area.IsEmpty)
        {
            return new GridLayout(
                new ushort[rowCount],
                new ushort[columnCount],
                Enumerable.Repeat(area.Y, rowCount).ToArray(),
                Enumerable.Repeat(area.X, columnCount).ToArray(),
                areas,
                0,
                0,
                area);
        }

        ushort totalRowGap = SaturatingProduct(rowCount - 1, _rowGap);
        ushort totalColumnGap = SaturatingProduct(columnCount - 1, _columnGap);
        ushort availableHeight = SaturatingSubtract(area.Height, totalRowGap);
        ushort availableWidth = SaturatingSubtract(area.Width, totalColumnGap);

        ushort[] rowHeights = ConstraintSolver.Solve(_rowConstraints, availableHeight);
        ushort[] columnWidths = ConstraintSolver.Solve(_columnConstraints, availableWidth);
        ushort[] rowPositions = CalculatePositions(rowHeights, area.Y, _rowGap);
        ushort[] columnPositions = CalculatePositions(columnWidths, area.X, _columnGap);

        return new GridLayout(
            rowHeights,
            columnWidths,
            rowPositions,
            columnPositions,
            areas,
            _rowGap,
            _columnGap,
            area);
    }

    private Grid Copy(
        Constraint[]? rowConstraints = null,
        Constraint[]? columnConstraints = null,
        ushort? rowGap = null,
        ushort? columnGap = null,
        Dictionary<string, GridArea>? namedAreas = null,
        OverflowBehavior? overflow = null) =>
        new(
            rowConstraints ?? _rowConstraints,
            columnConstraints ?? _columnConstraints,
            rowGap ?? _rowGap,
            columnGap ?? _columnGap,
            namedAreas ?? CloneAreas(),
            overflow ?? _overflow);

    private Dictionary<string, GridArea> CloneAreas() =>
        new(_namedAreas, StringComparer.Ordinal);

    private static ushort[] CalculatePositions(
        IReadOnlyList<ushort> sizes,
        ushort start,
        ushort gap)
    {
        var positions = new ushort[sizes.Count];
        ushort position = start;
        for (int index = 0; index < sizes.Count; index++)
        {
            positions[index] = position;
            position = SaturatingAdd(position, sizes[index]);
            if (index < sizes.Count - 1)
                position = SaturatingAdd(position, gap);
        }
        return positions;
    }

    private static ushort SaturatingAdd(ushort left, ushort right) =>
        (ushort)Math.Min((uint)left + right, ushort.MaxValue);

    private static ushort SaturatingSubtract(ushort left, ushort right) =>
        left > right ? (ushort)(left - right) : (ushort)0;

    private static ushort SaturatingProduct(int count, ushort value) =>
        (ushort)Math.Min((ulong)Math.Max(count, 0) * value, ushort.MaxValue);
}

/// <summary>The solved geometry of a <see cref="Grid"/>.</summary>
public sealed class GridLayout
{
    private readonly ushort[] _rowHeights;
    private readonly ushort[] _columnWidths;
    private readonly ushort[] _rowPositions;
    private readonly ushort[] _columnPositions;
    private readonly IReadOnlyDictionary<string, GridArea> _namedAreas;
    private readonly ushort _rowGap;
    private readonly ushort _columnGap;
    private readonly Rect _bounds;

    internal GridLayout(
        ushort[] rowHeights,
        ushort[] columnWidths,
        ushort[] rowPositions,
        ushort[] columnPositions,
        Dictionary<string, GridArea> namedAreas,
        ushort rowGap,
        ushort columnGap,
        Rect bounds)
    {
        _rowHeights = rowHeights;
        _columnWidths = columnWidths;
        _rowPositions = rowPositions;
        _columnPositions = columnPositions;
        _namedAreas = new System.Collections.ObjectModel.ReadOnlyDictionary<string, GridArea>(namedAreas);
        _rowGap = rowGap;
        _columnGap = columnGap;
        _bounds = bounds;
    }

    public Rect Cell(int row, int column) => Span(row, column, 1, 1);

    public Rect Span(int row, int column, int rowspan, int colspan)
    {
        rowspan = Math.Max(rowspan, 1);
        colspan = Math.Max(colspan, 1);
        if (row < 0 || column < 0 || row >= _rowHeights.Length || column >= _columnWidths.Length)
            return default;

        int endRow = (int)Math.Min((long)row + rowspan, _rowHeights.Length);
        int endColumn = (int)Math.Min((long)column + colspan, _columnWidths.Length);
        ushort width = 0;
        for (int index = column; index < endColumn; index++)
            width = SaturatingAdd(width, _columnWidths[index]);
        if (endColumn > column + 1)
            width = SaturatingAdd(width, SaturatingProduct(endColumn - column - 1, _columnGap));

        ushort height = 0;
        for (int index = row; index < endRow; index++)
            height = SaturatingAdd(height, _rowHeights[index]);
        if (endRow > row + 1)
            height = SaturatingAdd(height, SaturatingProduct(endRow - row - 1, _rowGap));

        return new Rect(_columnPositions[column], _rowPositions[row], width, height)
            .Intersection(_bounds);
    }

    public Rect? Area(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _namedAreas.TryGetValue(name, out GridArea area)
            ? Span(area.Row, area.Column, area.Rowspan, area.Colspan)
            : null;
    }

    public int NumRows() => _rowHeights.Length;

    public int NumColumns() => _columnWidths.Length;

    public int NumCols() => NumColumns();

    public ushort RowHeight(int row) =>
        row >= 0 && row < _rowHeights.Length ? _rowHeights[row] : (ushort)0;

    public ushort ColumnWidth(int column) =>
        column >= 0 && column < _columnWidths.Length ? _columnWidths[column] : (ushort)0;

    public ushort ColWidth(int column) => ColumnWidth(column);

    public IEnumerable<(int Row, int Column, Rect Rect)> Cells()
    {
        for (int row = 0; row < NumRows(); row++)
        {
            for (int column = 0; column < NumColumns(); column++)
                yield return (row, column, Cell(row, column));
        }
    }

    public IEnumerable<(int Row, int Column, Rect Rect)> IterCells() => Cells();

    private static ushort SaturatingAdd(ushort left, ushort right) =>
        (ushort)Math.Min((uint)left + right, ushort.MaxValue);

    private static ushort SaturatingProduct(int count, ushort value) =>
        (ushort)Math.Min((ulong)Math.Max(count, 0) * value, ushort.MaxValue);
}
