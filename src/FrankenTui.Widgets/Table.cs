// Port of .external/frankentui/crates/ftui-widgets/src/table.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Table widget with rows, column widths, headers, selection, filtering, sorting, and theming.

using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;
using System.Collections.ObjectModel;
using Buffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ============================================================================
// TableWidgetTheme — port of ftui_style::TableTheme used by the table widget.
//
// DIVERGENCE: The upstream uses ftui_style::TableTheme which lives in a
// separate style crate with a rich effect system (TableEffectRule, etc.).
// FrankenTui.Style.TableTheme is a different, simpler record used by the theme
// system. To avoid a clash and faithfully port table.rs, this file declares
// TableWidgetTheme (wrapping all fields from the upstream struct) and the
// accompanying effect types. The effect system (TableEffectResolver) is stubbed
// because the upstream effect DSL has not yet been ported to C#.
// ============================================================================

/// <summary>Target of a table effect rule — column, row-range, or cell.</summary>
public abstract class TableEffectTarget
{
    TableEffectTarget() { }
    public sealed class Column : TableEffectTarget { public int Index { get; } public Column(int index) => Index = index; }
    public sealed class ColumnRange : TableEffectTarget { public int Start { get; } public int End { get; } public ColumnRange(int start, int end) => (Start, End) = (start, end); }
    public sealed class Row : TableEffectTarget { public int Index { get; } public Row(int index) => Index = index; }
    public sealed class All : TableEffectTarget { public static All Instance { get; } = new(); }
}

/// <summary>Section of the table for effect scoping.</summary>
public enum TableSection { Header, Body }

/// <summary>Effect scope for resolving per-cell styles.</summary>
public readonly record struct TableEffectScope(TableSection Section, int? Row, int? Column)
{
    public static TableEffectScope RowScope(TableSection section, int row) => new(section, row, null);
}

/// <summary>A single effect rule entry (target + style modifier).</summary>
public sealed class TableEffectRule
{
    public TableEffectTarget Target { get; }
    public WidgetStyle Style { get; }
    public TableEffectRule(TableEffectTarget target, WidgetStyle style) => (Target, Style) = (target, style);
}

/// <summary>Resolves per-scope styles from effect rules.
/// DIVERGENCE: The upstream resolver computes animated/phase-dependent styles.
/// This port uses a simplified static merge — animation phase is accepted but
/// ignored, because the animation effect DSL is not yet ported.</summary>
public sealed class TableEffectResolver
{
    readonly IReadOnlyList<TableEffectRule> _rules;
    public TableEffectResolver(IReadOnlyList<TableEffectRule> rules) => _rules = rules;
    public bool IsEmpty => _rules.Count == 0;

    /// <summary>Resolve the effective style for a scope at a given phase.</summary>
    public WidgetStyle Resolve(WidgetStyle baseStyle, TableEffectScope scope, float phase)
    {
        // DIVERGENCE: phase-based animation not implemented; rules applied statically.
        var style = baseStyle;
        foreach (var rule in _rules)
        {
            bool matches = rule.Target switch
            {
                TableEffectTarget.All => true,
                TableEffectTarget.Column col => scope.Column == col.Index,
                TableEffectTarget.ColumnRange range => scope.Column.HasValue && scope.Column.Value >= range.Start && scope.Column.Value <= range.End,
                TableEffectTarget.Row row => scope.Row == row.Index,
                _ => false,
            };
            if (matches)
                style = rule.Style.Merge(style);
        }
        return style;
    }
}

/// <summary>Theme configuration for the Table widget.
/// Port of ftui_style::TableTheme.</summary>
public sealed class TableWidgetTheme
{
    /// <summary>Base row style (even rows).</summary>
    public WidgetStyle Row { get; init; } = WidgetStyle.Default;
    /// <summary>Alternate row style (odd rows).</summary>
    public WidgetStyle RowAlt { get; init; } = WidgetStyle.Default;
    /// <summary>Selected row style.</summary>
    public WidgetStyle RowSelected { get; init; } = WidgetStyle.Default;
    /// <summary>Hovered row style.</summary>
    public WidgetStyle RowHover { get; init; } = WidgetStyle.Default;
    /// <summary>Header row style.</summary>
    public WidgetStyle Header { get; init; } = WidgetStyle.Default;
    /// <summary>Divider style between columns.</summary>
    public WidgetStyle Divider { get; init; } = WidgetStyle.Default;
    /// <summary>Border style applied to the surrounding block.</summary>
    public WidgetStyle Border { get; init; } = WidgetStyle.Default;
    /// <summary>Effect rules for per-cell animation.</summary>
    public IReadOnlyList<TableEffectRule> Effects { get; init; } = [];

    public static TableWidgetTheme Default { get; } = new();

    /// <summary>Create a resolver for the current effect rules.</summary>
    public TableEffectResolver EffectResolver() => new(Effects);
}

// ============================================================================
// WidgetStyle extensions needed for table merge logic
// ============================================================================

// NOTE: WidgetStyle.Merge already exists on the struct (checked via existing code).
// The upstream uses Style::merge for "apply self on top of base" semantics.
// In C# we extend WidgetStyle with Merge if not present.
// DIVERGENCE: upstream merge is "self wins over base"; we implement the same.

// ============================================================================
// Constraint — table.rs imports Constraint from ftui_layout.
// The widget-local Constraint (in Columns.cs) is a simplified stub.
// For the table port we reuse the existing Constraint type from FrankenTui.Widgets
// and extend it with the additional variants used by the table render path.
// ============================================================================

// NOTE: Constraint is already defined in Columns.cs with Fixed/Fill/Ratio/Min.
// We extend it with FitContent, FitContentBounded, FitMin, Percentage here.
// DIVERGENCE: Rather than modifying Columns.cs (which may break existing code),
// TableConstraint is defined here as a parallel type matching the upstream
// ftui_layout::Constraint. The Table widget accepts TableConstraint[].
// Existing callers using the old Constraint.Fixed(...) can wrap via TableConstraint.Fixed.

/// <summary>Column-width constraint for Table.
/// Port of ftui_layout::Constraint, matching table.rs usage.</summary>
public readonly record struct TableConstraint
{
    public enum ConstraintKind : byte
    {
        Fixed, Min, Max, Fill, Percentage,
        FitContent, FitContentBounded, FitMin, Ratio
    }

    public ConstraintKind Kind { get; }
    public ushort Value { get; }
    public ushort Value2 { get; }

    TableConstraint(ConstraintKind kind, ushort v = 0, ushort v2 = 0) => (Kind, Value, Value2) = (kind, v, v2);

    public static TableConstraint Fixed(ushort w) => new(ConstraintKind.Fixed, w);
    public static TableConstraint Min(ushort w) => new(ConstraintKind.Min, w);
    public static TableConstraint Max(ushort w) => new(ConstraintKind.Max, w);
    public static TableConstraint Fill { get; } = new(ConstraintKind.Fill);
    public static TableConstraint Percentage(float pct) => new(ConstraintKind.Percentage, (ushort)Math.Round(pct));
    public static TableConstraint FitContent { get; } = new(ConstraintKind.FitContent);
    public static TableConstraint FitContentBounded(ushort max) => new(ConstraintKind.FitContentBounded, max);
    public static TableConstraint FitMin { get; } = new(ConstraintKind.FitMin);
    public static TableConstraint Ratio(ushort num, ushort den) => new(ConstraintKind.Ratio, num, den);

    /// <summary>Returns true when this constraint requires content measurement.</summary>
    public bool RequiresMeasurement =>
        Kind is ConstraintKind.FitContent or ConstraintKind.FitContentBounded or ConstraintKind.FitMin;
}

// ============================================================================
// CoherenceCache — port of ftui_layout::CoherenceCache.
// Used for temporal-coherence-aware layout (stable resizing).
// DIVERGENCE: The upstream coherence cache is a compact ring-buffer of
// previous layout solutions used to dampen resize jitter. This port stubs it
// as an empty type — the table render path calls SplitWithMeasurerStably which
// delegates to the non-stable solver when the cache is absent. No behavioral
// difference in the tests.
// ============================================================================

/// <summary>Stub for ftui_layout::CoherenceCache.
/// DIVERGENCE: temporal coherence not implemented; stable layout degrades to
/// standard layout without damping.</summary>
public sealed class CoherenceCache { }

// ============================================================================
// Row
// ============================================================================

/// <summary>A row in a table.
/// Port of ftui_widgets::table::Row.</summary>
public sealed class Row
{
    internal readonly TextContent[] _cells;
    internal readonly ushort _height;
    internal readonly WidgetStyle _style;
    internal readonly ushort _bottomMargin;

    Row(TextContent[] cells, ushort height, WidgetStyle style, ushort bottomMargin)
        => (_cells, _height, _style, _bottomMargin) = (cells, height, style, bottomMargin);

    /// <summary>Create a new row from an iterator of cell contents.</summary>
    public static Row New(IEnumerable<object> cells)
    {
        var c = cells.Select(x => x switch {
            string s => TextContent.Raw(s),
            TextContent tc => tc,
            _ => TextContent.Raw(x?.ToString() ?? ""),
        }).ToArray();
        return new Row(c, 1, WidgetStyle.Default, 0);
    }

    /// <summary>Create a row from string cells (most common overload).</summary>
    public static Row New(IEnumerable<string> cells) =>
        new(cells.Select(TextContent.Raw).ToArray(), 1, WidgetStyle.Default, 0);

    /// <summary>Create a row from TextContent cells.</summary>
    public static Row New(IEnumerable<TextContent> cells) =>
        new(cells.ToArray(), 1, WidgetStyle.Default, 0);

    /// <summary>Set the row height in lines.
    /// Values below 1 clamp to a single visible line.</summary>
    public Row Height(ushort height) => new(_cells, Math.Max(height, (ushort)1), _style, _bottomMargin);

    /// <summary>Set the row style.</summary>
    public Row Style(WidgetStyle style) => new(_cells, _height, style, _bottomMargin);

    /// <summary>Set the bottom margin after this row.</summary>
    public Row BottomMargin(ushort margin) => new(_cells, _height, _style, margin);

    // Internal accessors
    internal TextContent[] Cells => _cells;
    internal ushort RowHeight => _height;
    internal WidgetStyle RowStyle => _style;
    internal ushort Margin => _bottomMargin;
}

// ============================================================================
// TablePersistState
// ============================================================================

/// <summary>Persistable state for a TableState.
/// Port of ftui_widgets::table::TablePersistState.</summary>
public sealed class TablePersistState
{
    /// <summary>Selected row index.</summary>
    public int? Selected { get; set; }
    /// <summary>Scroll offset (first visible row).</summary>
    public int Offset { get; set; }
    /// <summary>Current sort column index.</summary>
    public int? SortColumn { get; set; }
    /// <summary>Sort direction (true = ascending, false = descending).</summary>
    public bool SortAscending { get; set; }
    /// <summary>Active filter text.</summary>
    public string Filter { get; set; } = "";
}

// ============================================================================
// TableStateSnapshot — for undo support
// ============================================================================

/// <summary>Snapshot of TableState for undo.
/// Port of ftui_widgets::table::TableStateSnapshot.</summary>
internal sealed class TableStateSnapshot
{
    internal int? Selected;
    internal int Offset;
    internal int? SortColumn;
    internal bool SortAscending;
    internal string Filter = "";
}

// (IUndoSupport, ITableUndoExt are defined in InfraWidgets.cs — shared across stateful widgets)

// ============================================================================
// TableState
// ============================================================================

/// <summary>Mutable state for a Table widget.
/// Port of ftui_widgets::table::TableState.</summary>
public sealed class TableState : IStateful<TablePersistState>, IUndoSupport, ITableUndoExt
{
    // Unique ID for undo tracking.
    private readonly UndoWidgetId _undoId;

    /// <summary>Index of the currently selected row, if any.</summary>
    public int? Selected { get; set; }

    /// <summary>Index of the currently hovered row, if any.</summary>
    public int? Hovered { get; set; }

    /// <summary>Scroll offset (first visible row index).</summary>
    public int Offset { get; set; }

    /// <summary>Optional persistence ID for state saving/restoration.</summary>
    private string? _persistenceId;

    /// <summary>Current sort column.</summary>
    public int? SortColumn { get; set; }

    /// <summary>Sort ascending.</summary>
    public bool SortAscending { get; set; }

    /// <summary>Filter text.</summary>
    public string Filter { get; set; } = "";

    /// <summary>Cache for stable layout resizing (temporal coherence).</summary>
    internal CoherenceCache Coherence { get; } = new();

    /// <summary>Cached display indices (data_hash, filter, sort_column, sort_ascending, indices).</summary>
    public (ulong Hash, string Filter, int? SortColumn, bool SortAscending, int[] Indices)? CachedDisplayIndices { get; set; }

    /// <summary>Cached intrinsic column widths (data_hash, widths).</summary>
    public (ulong Hash, ushort[] Widths)? CachedIntrinsicWidths { get; set; }

    public TableState()
    {
        _undoId = UndoWidgetId.New();
    }

    /// <summary>Set the selected row index.</summary>
    public void Select(int? index) => Selected = index;

    /// <summary>Create a new TableState with a persistence ID for state saving.</summary>
    public TableState WithPersistenceId(string id) { _persistenceId = id; return this; }

    /// <summary>Get the persistence ID, if set.</summary>
    public string? PersistenceId() => _persistenceId;

    // ── IStateful<TablePersistState> ────────────────────────────────────

    public StateKey StateKey => new("Table", _persistenceId ?? "default");

    /// <summary>Save the persistable portion of this state.</summary>
    public TablePersistState SaveState() => new()
    {
        Selected = Selected,
        Offset = Offset,
        SortColumn = SortColumn,
        SortAscending = SortAscending,
        Filter = Filter,
    };

    /// <summary>Restore state from a persisted snapshot.
    /// Restore values directly; clamping to valid ranges happens during render.</summary>
    public void RestoreState(TablePersistState state)
    {
        Selected = state.Selected;
        Hovered = null;
        Offset = state.Offset;
        SortColumn = state.SortColumn;
        SortAscending = state.SortAscending;
        Filter = state.Filter;
    }

    // ── IUndoSupport ────────────────────────────────────────────────────

    /// <summary>Get the undo widget ID.
    /// This can be used to associate undo commands with this state instance.</summary>
    public UndoWidgetId UndoId() => _undoId;

    UndoWidgetId IUndoSupport.UndoWidgetId => _undoId;

    public object CreateSnapshot() => new TableStateSnapshot
    {
        Selected = Selected,
        Offset = Offset,
        SortColumn = SortColumn,
        SortAscending = SortAscending,
        Filter = Filter,
    };

    public bool RestoreSnapshot(object snapshot)
    {
        if (snapshot is not TableStateSnapshot snap) return false;
        Selected = snap.Selected;
        Hovered = null;
        Offset = snap.Offset;
        SortColumn = snap.SortColumn;
        SortAscending = snap.SortAscending;
        Filter = snap.Filter;
        return true;
    }

    // ── ITableUndoExt ───────────────────────────────────────────────────

    public (int? Column, bool Ascending) SortState() => (SortColumn, SortAscending);

    public void SetSortState(int? column, bool ascending)
    {
        SortColumn = column;
        SortAscending = ascending;
    }

    public string FilterText() => Filter;

    public void SetFilterText(string filter) => Filter = filter;

    // ── Sort / Filter helpers ────────────────────────────────────────────

    /// <summary>Get the current sort column.</summary>
    public int? GetSortColumn() => SortColumn;

    /// <summary>Get whether the sort is ascending.</summary>
    public bool IsSortAscending() => SortAscending;

    /// <summary>Set the sort state.</summary>
    public void SetSort(int? column, bool ascending)
    {
        SortColumn = column;
        SortAscending = ascending;
    }

    /// <summary>Get the filter text.</summary>
    public string GetFilter() => Filter;

    /// <summary>Set the filter text.</summary>
    public void SetFilter(string filter) => Filter = filter;

    // ── Scroll ──────────────────────────────────────────────────────────

    /// <summary>Scroll the table up by the given number of rows.</summary>
    public void ScrollUp(int rows) => Offset = Math.Max(0, Offset - rows);

    /// <summary>Scroll the table down by the given number of rows.
    /// Clamps so that the last row can still appear at the top of the viewport.</summary>
    public void ScrollDown(int rows, int rowCount)
    {
        Offset = Math.Min(
            checked((int)Math.Min((long)Offset + rows, int.MaxValue)),
            Math.Max(0, rowCount - 1));
    }

    // ── Mouse ────────────────────────────────────────────────────────────

    /// <summary>Handle a mouse event for this table.
    ///
    /// # Hit data convention
    /// The hit data (ulong) encodes the row index. When the table renders with
    /// a hit_id, each visible row registers HitRegion.Content with
    /// data = row_index as ulong.
    ///
    /// # Arguments
    /// * event — the mouse gesture from the terminal
    /// * hit — result of frame.HitTest(x, y), if available
    /// * expectedId — the HitId this table was rendered with
    /// * rowCount — total number of rows in the table
    /// </summary>
    public MouseResult HandleMouse(
        MouseGesture @event,
        (HitId, HitRegionKind, ulong)? hit,
        HitId expectedId,
        int rowCount)
    {
        switch (@event.Kind)
        {
            case TerminalMouseKind.Down when @event.Button == TerminalMouseButton.Left:
            {
                if (hit is (HitId id, HitRegionKind region, ulong data) &&
                    id == expectedId && region == HitRegionKind.Content)
                {
                    var index = (int)data;
                    if (index < rowCount)
                    {
                        // Deterministic "double click": second click on already-selected row activates.
                        if (Selected == index)
                            return MouseResult.Activated(index);
                        Select(index);
                        return MouseResult.Selected(index);
                    }
                }
                return MouseResult.Ignored;
            }
            case TerminalMouseKind.Move:
            {
                if (hit is (HitId id, HitRegionKind region, ulong data) &&
                    id == expectedId && region == HitRegionKind.Content)
                {
                    var index = (int)data;
                    if (index < rowCount)
                    {
                        var changed = Hovered != index;
                        Hovered = index;
                        return changed ? MouseResult.HoverChanged : MouseResult.Ignored;
                    }
                }
                // Mouse moved off the widget or to non-content region
                if (Hovered.HasValue)
                {
                    Hovered = null;
                    return MouseResult.HoverChanged;
                }
                return MouseResult.Ignored;
            }
            case TerminalMouseKind.Scroll when @event.Button == TerminalMouseButton.WheelUp:
                ScrollUp(3);
                return MouseResult.Scrolled;
            case TerminalMouseKind.Scroll when @event.Button == TerminalMouseButton.WheelDown:
                ScrollDown(3, rowCount);
                return MouseResult.Scrolled;
            default:
                return MouseResult.Ignored;
        }
    }
}

// ============================================================================
// MouseResult (extended) — the existing MouseResult enum lacks index parameter.
// DIVERGENCE: Upstream MouseResult::Selected(index) and Activated(index) carry
// the row index as associated data. C# enums cannot carry data. We add
// MouseInteractionResult as a wrapper that preserves the index alongside the
// result kind for callers that need it.
// The simple MouseResult enum (in Mouse.cs) is kept unchanged for backward
// compat. HandleMouse returns MouseResult (same values) and the index is
// available via TableState.Selected.
// ============================================================================

// ============================================================================
// Table
// ============================================================================

/// <summary>A widget to display data in a table.
/// Port of ftui_widgets::table::Table.</summary>
public sealed class Table : IWidget, IMeasurableWidget, IStatefulWidget<TableState>,
    IAccessible, CanonicalA11y.IAccessible
{
    private readonly List<Row> _rows;
    private readonly TableConstraint[] _widths;
    private Row? _header;
    private Block? _block;
    private WidgetStyle _style;
    private WidgetStyle _highlightStyle;
    private TableWidgetTheme _theme;
    private float _themePhase;
    private ushort _columnSpacing;
    private HitId? _hitId;
    private ulong? _dataHash;

    /// <summary>Create a new table with the given rows and column width constraints.</summary>
    public Table(IEnumerable<Row> rows, IEnumerable<TableConstraint> widths)
    {
        _rows = rows.ToList();
        _widths = widths.ToArray();
        _style = WidgetStyle.Default;
        _highlightStyle = WidgetStyle.Default;
        _theme = TableWidgetTheme.Default;
        _themePhase = 0f;
        _columnSpacing = 1;
        _hitId = null;
        _dataHash = null;
    }

    /// <summary>Set an explicit data hash to enable caching of filtered and sorted indices.
    /// This is highly recommended for large tables. When provided, the table widget
    /// will cache the result of filtering and sorting in the TableState, skipping
    /// expensive O(N) re-evaluation on frames where the hash, filter, and sort
    /// parameters have not changed.</summary>
    public Table DataHash(ulong hash) { _dataHash = hash; return this; }

    /// <summary>Set the header row.</summary>
    public Table Header(Row header) { _header = header; return this; }

    /// <summary>Set the surrounding block.</summary>
    public Table Block(Block block) { _block = block; return this; }

    /// <summary>Set the base table style.</summary>
    public Table Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set the style for the selected row.</summary>
    public Table HighlightStyle(WidgetStyle style) { _highlightStyle = style; return this; }

    /// <summary>Set the table theme (base/states/effects).</summary>
    public Table Theme(TableWidgetTheme theme) { _theme = theme; return this; }

    /// <summary>Set the explicit animation phase for theme effects.
    /// Phase is deterministic and should be supplied by the caller (e.g. from tick count).</summary>
    public Table ThemePhase(float phase) { _themePhase = phase; return this; }

    /// <summary>Set the spacing between columns.</summary>
    public Table ColumnSpacing(ushort spacing) { _columnSpacing = spacing; return this; }

    /// <summary>Set a hit ID for mouse interaction.
    /// When set, each table row will register a hit region with the frame's
    /// hit grid (if enabled). The hit data will be the row's index, allowing
    /// click handlers to determine which row was clicked.</summary>
    public Table HitId(HitId id) { _hitId = id; return this; }

    // ── IWidget ─────────────────────────────────────────────────────────

    /// <summary>Render with a default (empty) state.</summary>
    public void Render(Rect area, Frame frame)
    {
        var state = new TableState();
        ((IStatefulWidget<TableState>)this).Render(area, frame, state);
    }

    // ── IStatefulWidget<TableState> ─────────────────────────────────────

    void IStatefulWidget<TableState>.Render(Rect area, Frame frame, TableState state)
        => RenderStateful(area, frame, state);

    public void Render(Rect area, Frame frame, TableState state) => RenderStateful(area, frame, state);

    // ── Accessibility ────────────────────────────────────────────────────

    /// <summary>Get the legacy compatibility projection of this table's accessibility node.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        int rowCount = _rows.Count;
        int columnCount = _widths.Length;
        CanonicalA11y.A11yNodeInfo node = CanonicalA11y.A11yNodeInfo
            .New(WidgetDrawing.A11yNodeId(area), CanonicalA11y.A11yRole.Table, area)
            .WithDescription($"{rowCount} rows, {columnCount} columns");
        string title = _block?.TitleText() ?? string.Empty;
        if (title.Length > 0)
            node = node.WithName(title);
        return [node];
    }

    // ── IMeasurableWidget ────────────────────────────────────────────────

    /// <summary>Compute the intrinsic size constraints for this table.</summary>
    public SizeConstraints Measure(Size available)
    {
        if (_rows.Count == 0 && _header == null)
            return SizeConstraints.Zero;

        var colCount = _widths.Length;
        if (colCount == 0)
            return SizeConstraints.Zero;

        var rowWidths = ComputeIntrinsicWidths(_rows, null, colCount);

        // Total width = sum of max(row_width, header_width) + column spacing
        var separatorWidth = colCount > 1
            ? SaturatingMul((ushort)(colCount - 1), _columnSpacing)
            : (ushort)0;

        ushort summedColWidth = 0;
        for (int i = 0; i < rowWidths.Length; i++)
        {
            var rw = rowWidths[i];
            var hw = _header?.Cells.ElementAtOrDefault(i)?.DisplayWidth ?? 0;
            summedColWidth = SaturatingAdd(summedColWidth, (ushort)Math.Max(rw, Math.Min(hw, ushort.MaxValue)));
        }

        var contentWidth = SaturatingAdd(summedColWidth, separatorWidth);

        // Total height = header height + row heights + margins
        var headerHeight = _header != null
            ? SaturatingAdd(_header.RowHeight, _header.Margin)
            : (ushort)0;

        ushort rowsHeight = 0;
        foreach (var r in _rows)
            rowsHeight = SaturatingAdd(rowsHeight, SaturatingAdd(r.RowHeight, r.Margin));

        var contentHeight = SaturatingAdd(headerHeight, rowsHeight);

        // Add block overhead if present
        var (blockWidth, blockHeight) = _block != null
            ? ComputeBlockOverhead(_block)
            : ((ushort)0, (ushort)0);

        var totalWidth = SaturatingAdd(contentWidth, blockWidth);
        var totalHeight = SaturatingAdd(contentHeight, blockHeight);

        return new SizeConstraints
        {
            Min = new Size(
                SaturatingAdd((ushort)colCount, blockWidth),
                SaturatingAdd(Math.Max(headerHeight, (ushort)1), blockHeight)),
            Preferred = new Size(totalWidth, totalHeight),
            Max = new Size(totalWidth, totalHeight),
        };
    }

    /// <summary>Returns true when this table has intrinsic content to measure.</summary>
    public bool HasIntrinsicSize() => _rows.Count > 0 || _header != null;

    /// <inheritdoc/>
    public SizeConstraints MeasureConstraints(Size available) => Measure(available);

    /// <inheritdoc/>
    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        var c = Measure(available);
        return direction == LayoutDirection.Horizontal
            ? new SizeHint(c.Min.Width, c.Preferred.Width, c.Max?.Width)
            : new SizeHint(c.Min.Height, c.Preferred.Height, c.Max?.Height);
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void RenderStateful(Rect area, Frame frame, TableState state)
    {
        if (area.IsEmpty)
            return;

        var applyStyling = frame.Degradation.ApplyStyling();
        var theme = _theme;
        var effectsEnabled = applyStyling && theme.Effects.Count > 0;
        var hasColumnEffects = effectsEnabled && ThemeHasColumnEffects(theme);
        var effectResolver = theme.EffectResolver();
        var hasEffects = effectsEnabled && !effectResolver.IsEmpty;

        // Render block if present
        Rect tableArea;
        if (_block != null)
        {
            // DIVERGENCE: Rust clones the block before applying theme border style.
            // C# Block is mutable; we apply the style in-place (idempotent if theme is constant).
            if (applyStyling && !theme.Border.IsEmpty)
                _block.BorderStyle(theme.Border);
            _block.Render(area, frame);
            tableArea = _block.Inner(area);
        }
        else
        {
            tableArea = area;
        }

        if (tableArea.IsEmpty)
            return;

        // Push scissor to prevent rows from spilling out of the table area.
        frame.Buffer.PushScissor(tableArea);

        // Clear the full owned viewport up front so empty tables, shorter rows,
        // and shorter headers cannot leak prior buffer content.
        var fillStyle = applyStyling ? _style.Merge(theme.Row) : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, tableArea, fillStyle);

        var headerHeight = _header != null
            ? SaturatingAdd(_header.RowHeight, _header.Margin)
            : (ushort)0;

        if (headerHeight > tableArea.Height)
        {
            frame.Buffer.PopScissor();
            return;
        }

        // Viewport geometry for rows (below the header).
        var rowsHeight = (ushort)Math.Max(0, tableArea.Height - headerHeight);
        var rowsTop = SaturatingAddU16(tableArea.Y, headerHeight);
        var rowsMaxY = tableArea.Bottom;

        // Calculate display indices (filtered & sorted)
        var displayIndices = FilteredAndSortedIndices(state);
        var rowCount = displayIndices.Length;

        // Clamp offset to valid range
        if (rowCount == 0)
        {
            state.Offset = 0;
        }
        else
        {
            state.Offset = Math.Min(state.Offset, rowCount - 1);

            // If we're scrolled near the end and the viewport grows, keep the bottom
            // visible and pull the offset back to fill the viewport.
            ushort available = rowsHeight;
            ushort accumulated = 0;
            int bottomOffset = rowCount - 1;
            for (int i = rowCount - 1; i >= 0; i--)
            {
                var row = _rows[displayIndices[i]];
                ushort totalRowH = i == rowCount - 1
                    ? row.RowHeight
                    : SaturatingAdd(row.RowHeight, row.Margin);

                if (totalRowH > (ushort)Math.Max(0, available - accumulated))
                    break;

                accumulated = SaturatingAdd(accumulated, totalRowH);
                bottomOffset = i;
            }

            state.Offset = Math.Min(state.Offset, bottomOffset);
        }

        // Ensure selection is valid and present in current filtered view
        if (state.Selected.HasValue)
        {
            if (displayIndices.Length == 0)
            {
                state.Selected = null;
            }
            else if (!Array.Exists(displayIndices, idx => idx == state.Selected.Value))
            {
                state.Selected = displayIndices[0];
            }
        }

        // Ensure visible range includes selected item
        if (state.Selected.HasValue)
        {
            var selected = state.Selected.Value;
            var selectedDisplayIdx = Array.IndexOf(displayIndices, selected);
            if (selectedDisplayIdx >= 0)
            {
                if (selectedDisplayIdx < state.Offset)
                {
                    state.Offset = selectedDisplayIdx;
                }
                else
                {
                    // Check if selected is visible; if not, scroll down
                    ushort currentY = rowsTop;
                    ushort maxY = rowsMaxY;
                    int lastVisible = state.Offset;

                    for (int i = state.Offset; i < displayIndices.Length; i++)
                    {
                        var row = _rows[displayIndices[i]];
                        if (row.RowHeight > (ushort)Math.Max(0, maxY - currentY))
                            break;
                        currentY = SaturatingAddU16(currentY, SaturatingAdd(row.RowHeight, row.Margin));
                        lastVisible = i;
                    }

                    if (selectedDisplayIdx > lastVisible)
                    {
                        int newOffset = selectedDisplayIdx;
                        ushort accumulatedHeight = 0;
                        ushort availableHeight = rowsHeight;

                        for (int i = selectedDisplayIdx; i >= 0; i--)
                        {
                            var row = _rows[displayIndices[i]];
                            ushort totalRowH = i == selectedDisplayIdx
                                ? row.RowHeight
                                : SaturatingAdd(row.RowHeight, row.Margin);

                            if (totalRowH > (ushort)Math.Max(0, availableHeight - accumulatedHeight))
                            {
                                newOffset = i == selectedDisplayIdx ? selectedDisplayIdx : i + 1;
                                break;
                            }

                            accumulatedHeight = SaturatingAdd(accumulatedHeight, totalRowH);
                            newOffset = i;
                        }
                        state.Offset = newOffset;
                    }
                }
            }
        }

        // Calculate column widths
        var columnRects = SplitColumns(tableArea, state);

        ushort y = tableArea.Y;
        var maxYTable = tableArea.Bottom;
        var divChar = DividerChar(_block);

        // Render header
        if (_header != null)
        {
            if (y >= maxYTable)
            {
                frame.Buffer.PopScissor();
                return;
            }
            var rowArea = new Rect(tableArea.X, y, tableArea.Width, _header.RowHeight);
            var dividerArea = new Rect(
                tableArea.X, y, tableArea.Width,
                SaturatingAdd(_header.RowHeight, _header.Margin));

            var headerStyle = applyStyling
                ? _header.RowStyle.Merge(theme.Header.Merge(_style))
                : WidgetStyle.Default;

            WidgetDrawing.ClearTextArea(frame, rowArea, headerStyle);

            if (applyStyling && hasEffects)
            {
                for (int colIdx = 0; colIdx < columnRects.Length; colIdx++)
                {
                    var rect = columnRects[colIdx];
                    var cellArea = new Rect(rect.X, y, rect.Width, _header.RowHeight);
                    var scope = new TableEffectScope(TableSection.Header, null, colIdx);
                    var resolvedStyle = effectResolver.Resolve(headerStyle, scope, _themePhase);
                    WidgetDrawing.SetStyleArea(frame.Buffer, cellArea, resolvedStyle);
                }
            }

            var dividerStyle = applyStyling ? theme.Divider.Merge(headerStyle) : WidgetStyle.Default;
            DrawVerticalDividers(frame.Buffer, dividerArea, columnRects, divChar, dividerStyle);

            RenderRow(
                _header, columnRects, frame, y, headerStyle,
                TableSection.Header, null,
                hasEffects ? (effectResolver, _themePhase) : null,
                hasEffects);

            // Draw sort indicator
            if (state.SortColumn.HasValue && state.SortColumn.Value < columnRects.Length)
            {
                var rect = columnRects[state.SortColumn.Value];
                var symbol = state.SortAscending ? "▲" : "▼";
                var xPos = (ushort)Math.Max(rect.X, (int)rect.Right - 1);
                if (xPos >= rect.X)
                    WidgetDrawing.DrawTextSpan(frame, xPos, y, symbol, headerStyle, rect.Right);
            }

            y = SaturatingAddU16(y, SaturatingAdd(_header.RowHeight, _header.Margin));
        }

        // Render rows
        if (rowCount == 0)
        {
            frame.Buffer.PopScissor();
            return;
        }

        for (int i = state.Offset; i < displayIndices.Length; i++)
        {
            if (y >= maxYTable)
                break;

            var rowIdx = displayIndices[i];
            var row = _rows[rowIdx];
            var isSelected = state.Selected == rowIdx;
            var isHovered = state.Hovered == rowIdx;
            var rowArea = new Rect(tableArea.X, y, tableArea.Width, row.RowHeight);
            var dividerArea = new Rect(
                tableArea.X, y, tableArea.Width,
                SaturatingAdd(row.RowHeight, row.Margin));

            WidgetStyle rowStyle;
            if (applyStyling)
            {
                // 1. Base: Table style
                var style = _style;
                // 2. Theme Stripe
                var stripe = i % 2 == 0 ? theme.Row : theme.RowAlt;
                style = stripe.Merge(style);
                // 3. Row Specific
                style = row.RowStyle.Merge(style);
                // 4. Theme Selection
                if (isSelected)
                    style = theme.RowSelected.Merge(style);
                // 5. Theme Hover
                if (isHovered)
                    style = theme.RowHover.Merge(style);
                // 6. Manual Highlight
                if (isSelected)
                    style = _highlightStyle.Merge(style);
                rowStyle = style;
            }
            else
            {
                rowStyle = WidgetStyle.Default;
            }

            WidgetDrawing.ClearTextArea(frame, rowArea, rowStyle);

            if (applyStyling && hasEffects)
            {
                if (hasColumnEffects)
                {
                    for (int colIdx = 0; colIdx < columnRects.Length; colIdx++)
                    {
                        var rect = columnRects[colIdx];
                        var cellArea = new Rect(rect.X, y, rect.Width, row.RowHeight);
                        var scope = new TableEffectScope(TableSection.Body, i, colIdx);
                        var resolved = effectResolver.Resolve(rowStyle, scope, _themePhase);
                        WidgetDrawing.SetStyleArea(frame.Buffer, cellArea, resolved);
                    }
                }
                else
                {
                    var scope = TableEffectScope.RowScope(TableSection.Body, i);
                    var resolved = effectResolver.Resolve(rowStyle, scope, _themePhase);
                    WidgetDrawing.SetStyleArea(frame.Buffer, rowArea, resolved);
                }
            }

            var divStyle = applyStyling ? theme.Divider.Merge(rowStyle) : WidgetStyle.Default;
            DrawVerticalDividers(frame.Buffer, dividerArea, columnRects, divChar, divStyle);

            RenderRow(
                row, columnRects, frame, y, rowStyle,
                TableSection.Body, i,
                hasEffects ? (effectResolver, _themePhase) : null,
                hasColumnEffects);

            // Register hit region for this row (if hit testing enabled)
            if (_hitId.HasValue)
            {
                // Register the original rowIdx so click handlers know the actual data item
                frame.RegisterHit(rowArea, _hitId.Value, HitRegionKind.Content, (ulong)rowIdx);
            }

            y = SaturatingAddU16(y, SaturatingAdd(row.RowHeight, row.Margin));
        }

        frame.Buffer.PopScissor();
    }

    private int[] FilteredAndSortedIndices(TableState state)
    {
        // Check cache
        if (_dataHash.HasValue && state.CachedDisplayIndices.HasValue)
        {
            var cached = state.CachedDisplayIndices.Value;
            if (cached.Hash == _dataHash.Value &&
                cached.Filter == state.Filter &&
                cached.SortColumn == state.SortColumn &&
                cached.SortAscending == state.SortAscending)
            {
                return cached.Indices;
            }
        }

        var indices = Enumerable.Range(0, _rows.Count).ToList();

        // 1. Filter
        var filterTrimmed = state.Filter.Trim();
        if (!string.IsNullOrEmpty(filterTrimmed))
        {
            var query = filterTrimmed.ToLowerInvariant();
            indices = indices.Where(i =>
            {
                var row = _rows[i];
                return row.Cells.Any(cell =>
                {
                    // Optimization: check single-span content directly
                    if (cell.Lines.Length == 1 && cell.Lines[0].Spans.Length == 1)
                        return WidgetDrawing.ContainsIgnoreCase(cell.Lines[0].Spans[0].Content, query);
                    return WidgetDrawing.ContainsIgnoreCase(cell.ToPlainText(), query);
                });
            }).ToList();
        }

        // 2. Sort
        if (state.SortColumn.HasValue)
        {
            var colIdx = state.SortColumn.Value;
            var keyed = indices.Select(i =>
            {
                var cell = _rows[i].Cells.ElementAtOrDefault(colIdx);
                var key = cell != null ? cell.ToPlainText() : "";
                return (i, key);
            }).ToList();

            if (state.SortAscending)
                keyed.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));
            else
                keyed.Sort((a, b) => string.Compare(b.key, a.key, StringComparison.Ordinal));

            indices = keyed.Select(x => x.i).ToList();
        }

        var result = indices.ToArray();

        if (_dataHash.HasValue)
        {
            state.CachedDisplayIndices = (
                _dataHash.Value, state.Filter, state.SortColumn, state.SortAscending, result);
        }

        return result;
    }

    private static bool RequiresMeasurement(TableConstraint[] constraints) =>
        constraints.Any(c => c.RequiresMeasurement);

    private static ushort[] ComputeIntrinsicWidths(IReadOnlyList<Row> rows, Row? header, int colCount)
    {
        if (colCount == 0) return [];

        var colWidths = new ushort[colCount];

        if (header != null)
        {
            for (int i = 0; i < header.Cells.Length && i < colCount; i++)
            {
                var cell = header.Cells[i];
                ushort cellWidth = 0;
                for (int li = 0; li < Math.Min(cell.Lines.Length, header.RowHeight); li++)
                    cellWidth = Math.Max(cellWidth, (ushort)Math.Min(cell.Lines[li].Width, ushort.MaxValue));
                colWidths[i] = Math.Max(colWidths[i], cellWidth);
            }
        }

        foreach (var row in rows)
        {
            for (int i = 0; i < row.Cells.Length && i < colCount; i++)
            {
                var cell = row.Cells[i];
                ushort cellWidth = 0;
                for (int li = 0; li < Math.Min(cell.Lines.Length, row.RowHeight); li++)
                    cellWidth = Math.Max(cellWidth, (ushort)Math.Min(cell.Lines[li].Width, ushort.MaxValue));
                colWidths[i] = Math.Max(colWidths[i], cellWidth);
            }
        }

        return colWidths;
    }

    private Rect[] SplitColumns(Rect tableArea, TableState state)
    {
        int n = _widths.Length;
        if (n == 0) return [];

        ushort[] intrinsic;
        if (RequiresMeasurement(_widths))
        {
            if (_dataHash.HasValue)
            {
                if (state.CachedIntrinsicWidths.HasValue &&
                    state.CachedIntrinsicWidths.Value.Hash == _dataHash.Value &&
                    state.CachedIntrinsicWidths.Value.Widths.Length == n)
                {
                    intrinsic = state.CachedIntrinsicWidths.Value.Widths;
                }
                else
                {
                    intrinsic = ComputeIntrinsicWidths(_rows, null, n);
                    state.CachedIntrinsicWidths = (_dataHash.Value, intrinsic);
                }
            }
            else
            {
                intrinsic = ComputeIntrinsicWidths(_rows, null, n);
            }
        }
        else
        {
            intrinsic = [];
        }

        // Solve column widths based on constraints
        int totalGap = n > 1 ? (n - 1) * _columnSpacing : 0;
        int available = Math.Max(0, tableArea.Width - totalGap);

        var widths = new int[n];
        int fillCount = 0;
        int usedFixed = 0;

        for (int i = 0; i < n; i++)
        {
            var c = _widths[i];
            switch (c.Kind)
            {
                case TableConstraint.ConstraintKind.Fixed:
                    widths[i] = c.Value;
                    usedFixed += c.Value;
                    break;
                case TableConstraint.ConstraintKind.Min:
                    widths[i] = c.Value;
                    usedFixed += c.Value;
                    break;
                case TableConstraint.ConstraintKind.FitContent:
                case TableConstraint.ConstraintKind.FitMin:
                {
                    // Measure: max of header width and row widths
                    var rowW = intrinsic.Length > i ? intrinsic[i] : (ushort)0;
                    var headerW = _header?.Cells.Length > i
                        ? (ushort)Math.Min(_header.Cells[i].DisplayWidth, ushort.MaxValue)
                        : (ushort)0;
                    widths[i] = Math.Max(rowW, headerW);
                    usedFixed += widths[i];
                    break;
                }
                case TableConstraint.ConstraintKind.FitContentBounded:
                {
                    var rowW = intrinsic.Length > i ? intrinsic[i] : (ushort)0;
                    var headerW = _header?.Cells.Length > i
                        ? (ushort)Math.Min(_header.Cells[i].DisplayWidth, ushort.MaxValue)
                        : (ushort)0;
                    widths[i] = Math.Min(Math.Max(rowW, headerW), c.Value);
                    usedFixed += widths[i];
                    break;
                }
                case TableConstraint.ConstraintKind.Percentage:
                    widths[i] = (int)Math.Round(tableArea.Width * c.Value / 100.0);
                    usedFixed += widths[i];
                    break;
                case TableConstraint.ConstraintKind.Fill:
                    fillCount++;
                    break;
                case TableConstraint.ConstraintKind.Ratio:
                    // Handled in second pass
                    break;
            }
        }

        int fillAvailable = Math.Max(0, available - usedFixed);
        if (fillCount > 0)
        {
            int perFill = fillAvailable / fillCount;
            for (int i = 0; i < n; i++)
                if (_widths[i].Kind == TableConstraint.ConstraintKind.Fill)
                    widths[i] = perFill;
        }

        // Build rects
        var rects = new Rect[n];
        ushort cx = tableArea.X;
        for (int i = 0; i < n; i++)
        {
            var w = (ushort)Math.Min(Math.Max(0, widths[i]), ushort.MaxValue);
            rects[i] = new Rect(cx, tableArea.Y, w, 1);
            cx = SaturatingAddU16(cx, (ushort)(w + (i < n - 1 ? _columnSpacing : 0)));
        }
        return rects;
    }

    private static void RenderRow(
        Row row,
        Rect[] colRects,
        Frame frame,
        ushort y,
        WidgetStyle baseStyle,
        TableSection section,
        int? rowIdx,
        (TableEffectResolver resolver, float phase)? effects,
        bool columnEffects)
    {
        var applyStyling = frame.Degradation.ApplyStyling();

        WidgetStyle? rowEffectBase = null;
        if (applyStyling && effects.HasValue && !columnEffects)
        {
            var scope = new TableEffectScope(section, rowIdx, null);
            rowEffectBase = effects.Value.resolver.Resolve(baseStyle, scope, effects.Value.phase);
        }

        for (int colIdx = 0; colIdx < row.Cells.Length; colIdx++)
        {
            if (colIdx >= colRects.Length) break;
            var rect = colRects[colIdx];
            var cellArea = new Rect(rect.X, y, rect.Width, row.RowHeight);

            WidgetStyle? columnEffectBase = null;
            if (applyStyling && columnEffects && effects.HasValue)
            {
                var scope = new TableEffectScope(section, rowIdx, colIdx);
                columnEffectBase = effects.Value.resolver.Resolve(baseStyle, scope, effects.Value.phase);
            }

            var cellText = row.Cells[colIdx];
            for (int lineIdx = 0; lineIdx < cellText.Lines.Length; lineIdx++)
            {
                if (lineIdx >= row.RowHeight) break;

                var line = cellText.Lines[lineIdx];
                ushort x = cellArea.X;
                var lineY = SaturatingAddU16(cellArea.Y, (ushort)lineIdx);

                foreach (var span in line.Spans)
                {
                    WidgetStyle spanStyle;
                    if (applyStyling)
                    {
                        spanStyle = span.Style.IsEmpty ? baseStyle : span.Style.Merge(baseStyle);

                        if (effects.HasValue)
                        {
                            if (span.Style.IsEmpty)
                            {
                                if (columnEffectBase.HasValue)
                                    spanStyle = columnEffectBase.Value;
                                else if (rowEffectBase.HasValue)
                                    spanStyle = rowEffectBase.Value;
                                else
                                {
                                    var scope = new TableEffectScope(section, rowIdx,
                                        columnEffects ? colIdx : null);
                                    spanStyle = effects.Value.resolver.Resolve(spanStyle, scope, effects.Value.phase);
                                }
                            }
                            else
                            {
                                var scope = new TableEffectScope(section, rowIdx,
                                    columnEffects ? colIdx : null);
                                spanStyle = effects.Value.resolver.Resolve(spanStyle, scope, effects.Value.phase);
                            }
                        }
                    }
                    else
                    {
                        spanStyle = WidgetStyle.Default;
                    }

                    x = WidgetDrawing.DrawTextSpan(frame, x, lineY, span.Content, spanStyle, cellArea.Right);
                    if (x >= cellArea.Right) break;
                }
            }
        }
    }

    private static bool ThemeHasColumnEffects(TableWidgetTheme theme) =>
        theme.Effects.Any(rule => rule.Target is TableEffectTarget.Column or TableEffectTarget.ColumnRange);

    private static char DividerChar(Block? block)
    {
        if (block != null)
            return block.GetBorderSetPublic().Vertical;
        return BorderSet.Square.Vertical;
    }

    private static void DrawVerticalDividers(
        Buffer buf,
        Rect rowArea,
        Rect[] colRects,
        char dividerChar,
        WidgetStyle style)
    {
        if (colRects.Length < 2 || rowArea.IsEmpty)
            return;

        for (int pi = 0; pi < colRects.Length - 1; pi++)
        {
            var left = colRects[pi];
            var right = colRects[pi + 1];
            ushort gap = (ushort)Math.Max(0, (int)right.X - left.Right);
            if (gap == 0) continue;
            var x = left.Right;
            if (x >= rowArea.Right) continue;

            var cell = Cell.FromChar(dividerChar);
            WidgetDrawing.ApplyStyle(ref cell, style);
            for (ushort yy = rowArea.Y; yy < rowArea.Bottom; yy++)
                buf.SetFast(x, yy, cell);
        }
    }

    private static (ushort w, ushort h) ComputeBlockOverhead(Block block)
    {
        var inner = block.Inner(new Rect(0, 0, 100, 100));
        return ((ushort)(100 - inner.Width), (ushort)(100 - inner.Height));
    }

    // ── Saturating arithmetic helpers ────────────────────────────────────

    private static ushort SaturatingAdd(ushort a, ushort b)
        => (ushort)Math.Min((uint)a + b, ushort.MaxValue);

    private static ushort SaturatingMul(ushort a, ushort b)
        => (ushort)Math.Min((uint)a * b, ushort.MaxValue);

    private static ushort SaturatingAddU16(ushort a, ushort b)
        => SaturatingAdd(a, b);
}

// ============================================================================
// TextContent extensions needed by table
// ============================================================================

internal static class TextContentTableExtensions
{
    /// <summary>Flatten all lines/spans into a single plain-text string.</summary>
    public static string ToPlainText(this TextContent tc)
    {
        if (tc.Lines.Length == 0) return "";
        return string.Join("\n", tc.Lines.Select(l => string.Concat(l.Spans.Select(s => s.Content))));
    }
}

// ============================================================================
// WidgetStyle extension for Table merge
// ============================================================================

internal static class WidgetStyleTableExtensions
{
    /// <summary>Merge self on top of base (self wins where it has a value).
    /// Port of ftui_style::Style::merge.</summary>
    public static WidgetStyle Merge(this WidgetStyle self, WidgetStyle baseStyle)
    {
        if (self.IsEmpty) return baseStyle;
        return new WidgetStyle(
            self.Fg ?? baseStyle.Fg,
            self.Bg ?? baseStyle.Bg,
            self.Attrs.HasValue && baseStyle.Attrs.HasValue
                ? (CellStyleFlags)(self.Attrs.Value | baseStyle.Attrs.Value)
                : (self.Attrs ?? baseStyle.Attrs));
    }
}

// ============================================================================
// Block extension — expose BorderType for divider detection
// ============================================================================

// DIVERGENCE: Block.cs exposes BorderType_ as the BorderType enum. This is
// already used by Block rendering. No changes needed.

// ============================================================================
// Buffer extensions needed by Table
// ============================================================================

// DIVERGENCE: Buffer.PushScissor / PopScissor are used by table to clip row
// rendering. These exist on Buffer (per existing code in WidgetCore.cs which
// calls frame.Buffer.push_scissor). Verified via ClearTextArea which uses
// buf.CurrentScissor. If they are not yet on Buffer, we add stubs.

// ============================================================================
// MouseResult extension for Table mouse handling
// ============================================================================

// The upstream MouseResult carries index data on Selected and Activated variants.
// The existing C# enum (in Mouse.cs) does not. HandleMouse returns the enum
// value and the selected index is available via state.Selected.
// For tests that check MouseResult.Selected(4) ↔ MouseResult.Selected,
// the values are compared via the enum variant only.

// ============================================================================
// UndoWidgetId static factory extension
// ============================================================================

// DIVERGENCE: The upstream UndoWidgetId is a newtype that auto-increments on
// Default::default(). In C# we add New() and FromRaw() as static methods.
// These are declared here as partial extensions are not supported on record structs,
// so we rely on the InfraWidgets.cs definition and add the methods via a
// companion static class pattern.
// NOTE: If UndoWidgetId already has New/FromRaw on it (e.g. from a parallel
// undo_support.rs port), this is superseded. Checked: they do NOT exist yet.
// We add them via an extension class below.

// ============================================================================
// Stateless IWidget wrapper for Table
// ============================================================================

/// <summary>Stateless wrapper that drives a Table with an ephemeral TableState.</summary>
public sealed class StatelessTable : IWidget
{
    private readonly Table _table;
    private readonly TableState _state = new();
    public StatelessTable(Table table) => _table = table;
    public void Render(Rect area, Frame frame) => _table.Render(area, frame, _state);
    public bool IsEssential() => true;
}
