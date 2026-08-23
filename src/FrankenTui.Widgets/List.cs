// Port of .external/frankentui/crates/ftui-widgets/src/list.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// List widget with selection, filtering, mouse support, measurable sizing, undo, and state persistence.

using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

// (IUndoSupport, IListUndoExt are defined in InfraWidgets.cs — shared across stateful widgets)

// ── ListItem ──────────────────────────────────────────────────────────────────

/// <summary>A single item in a list.</summary>
public sealed class ListItem
{
    /// <summary>Display content (first line is rendered).</summary>
    public TextContent Content { get; }

    /// <summary>Per-item style.</summary>
    public WidgetStyle Style { get; }

    /// <summary>Optional prefix marker string.</summary>
    public string Marker { get; }

    ListItem(TextContent content, WidgetStyle style, string marker)
    {
        Content = content;
        Style = style;
        Marker = marker;
    }

    /// <summary>Create a new list item with the given content.</summary>
    public static ListItem New(string text) => new(TextContent.Raw(text), WidgetStyle.Default, "");

    /// <summary>Create a new list item with rich content.</summary>
    public static ListItem FromContent(TextContent content) => new(content, WidgetStyle.Default, "");

    /// <summary>Set the style for this list item.</summary>
    public ListItem WithStyle(WidgetStyle style) => new(Content, style, Marker);

    /// <summary>Set a prefix marker string for this item.</summary>
    public ListItem WithMarker(string marker) => new(Content, Style, marker);
}

// ── ListPersistState ──────────────────────────────────────────────────────────

/// <summary>
/// Persistable state for a <see cref="ListState"/>.
/// Contains the user-facing state that should survive sessions.
/// Port of <c>ListPersistState</c>.
/// </summary>
public sealed class ListPersistState
{
    /// <summary>Selected item index.</summary>
    public int? Selected { get; set; }

    /// <summary>Scroll offset (first visible item).</summary>
    public int Offset { get; set; }

    /// <summary>Incremental filter query.</summary>
    public string FilterQuery { get; set; } = "";

    /// <summary>Whether multi-select mode was enabled.</summary>
    public bool MultiSelectEnabled { get; set; }

    /// <summary>Multi-selected indices when multi-select mode is enabled.</summary>
    public List<int> MultiSelected { get; set; } = new();
}

// ── ListStateSnapshot ─────────────────────────────────────────────────────────

/// <summary>Snapshot of ListState for undo.</summary>
sealed class ListStateSnapshot
{
    public int? Selected;
    public int Offset;
    public string FilterQuery = "";
    public bool MultiSelectEnabled;
    public List<int> MultiSelected = new();
}

// ── ListState ─────────────────────────────────────────────────────────────────

/// <summary>
/// Mutable state for a <see cref="ListWidget"/> tracking selection and scroll offset.
/// </summary>
public sealed class ListState : IStateful<ListPersistState>, IUndoSupport, IListUndoExt
{
    // Unique ID for undo tracking.
    readonly UndoWidgetId _undoId = UndoWidgetId.New();

    /// <summary>Index of the currently selected item, if any.</summary>
    public int? Selected;

    /// <summary>Index of the currently hovered item, if any.</summary>
    public int? Hovered;

    /// <summary>Scroll offset (first visible item index).</summary>
    public int Offset;

    // Optional persistence ID for state saving/restoration.
    string? _persistenceId;

    // Whether to force the selected item into view on next render.
    internal bool ScrollIntoViewRequested = true;

    // Incremental filter query applied to items (case-insensitive).
    string _filterQuery = "";

    // Whether multi-select behavior is enabled.
    bool _multiSelectEnabled;

    // Set of selected indices when multi-select is enabled.
    readonly SortedSet<int> _multiSelected = new();

    /// <summary>Cached display indices (data_hash, filter_query, indices).</summary>
    public (ulong Hash, string Query, int[] Indices)? CachedDisplayIndices;

    // ── Accessors ────────────────────────────────────────────────────────────

    /// <summary>Return the currently selected item index.</summary>
    public int? GetSelected() => Selected;

    /// <summary>Set the selected item index, or null to deselect.</summary>
    public void Select(int? index)
    {
        Selected = index;
        if (index is null)
        {
            Offset = 0;
            _multiSelected.Clear();
        }
        else if (!_multiSelectEnabled)
        {
            _multiSelected.Clear();
            _multiSelected.Add(index.Value);
        }
        ScrollIntoViewRequested = true;
    }

    /// <summary>Create a new ListState with a persistence ID for state saving.</summary>
    public ListState WithPersistenceId(string id) { _persistenceId = id; return this; }

    /// <summary>Get the persistence ID, if set.</summary>
    public string? PersistenceId() => _persistenceId;

    /// <summary>Enable or disable multi-select mode.</summary>
    public void SetMultiSelect(bool enabled)
    {
        if (_multiSelectEnabled == enabled) return;
        _multiSelectEnabled = enabled;
        if (!enabled)
        {
            _multiSelected.Clear();
            if (Selected.HasValue)
                _multiSelected.Add(Selected.Value);
        }
    }

    /// <summary>Whether multi-select mode is enabled.</summary>
    public bool MultiSelectEnabled() => _multiSelectEnabled;

    /// <summary>Current incremental filter query.</summary>
    public string FilterQuery() => _filterQuery;

    /// <summary>Replace the incremental filter query.</summary>
    public void SetFilterQuery(string query)
    {
        _filterQuery = query;
        Offset = 0;
        ScrollIntoViewRequested = true;
    }

    /// <summary>Clear the current filter query.</summary>
    public void ClearFilterQuery()
    {
        if (_filterQuery.Length > 0)
        {
            _filterQuery = "";
            Offset = 0;
            ScrollIntoViewRequested = true;
        }
    }

    /// <summary>Number of selected rows (single or multi mode).</summary>
    public int SelectedCount()
        => _multiSelectEnabled ? _multiSelected.Count : (Selected.HasValue ? 1 : 0);

    /// <summary>Selected indices in multi-select mode.</summary>
    public SortedSet<int> SelectedIndices() => _multiSelected;

    /// <summary>Toggle the multi-selected state of the given index.</summary>
    internal void ToggleMultiSelected(int index)
    {
        if (!_multiSelectEnabled)
        {
            Select(index);
            return;
        }
        if (!_multiSelected.Add(index))
            _multiSelected.Remove(index);
        Selected = index;
        ScrollIntoViewRequested = true;
    }

    /// <summary>
    /// Handle a mouse event for this list.
    ///
    /// The hit data encodes the item index as a ulong.
    ///
    /// <para>
    /// The hit data (<c>ulong</c>) encodes the item index. When the list renders with
    /// a <c>hit_id</c>, each visible row registers <c>HitRegion::Content</c> with
    /// <c>data = item_index as u64</c>.
    /// </para>
    /// </summary>
    public MouseResult HandleMouse(
        MouseEvent @event,
        (HitId Id, HitRegionKind Region, ulong Data)? hit,
        HitId expectedId,
        int itemCount)
    {
        switch (@event.Kind)
        {
            case MouseEventKind.Down { Button: MouseButton.Left }:
            {
                if (hit is { } h && h.Id == expectedId && h.Region == HitRegionKind.Content)
                {
                    var index = (int)h.Data;
                    if (index < itemCount)
                    {
                        if (_multiSelectEnabled && @event.Modifiers.HasFlag(KeyModifiers.Ctrl))
                        {
                            ToggleMultiSelected(index);
                            return MouseResult.Selected(index);
                        }
                        if (_multiSelectEnabled)
                        {
                            _multiSelected.Clear();
                            _multiSelected.Add(index);
                        }
                        // Deterministic "double click": second click on already-selected row activates.
                        if (!_multiSelectEnabled && Selected == index)
                            return MouseResult.Activated(index);
                        Select(index);
                        return MouseResult.Selected(index);
                    }
                }
                return MouseResult.Ignored;
            }

            case MouseEventKind.Moved:
            {
                if (hit is { } h && h.Id == expectedId && h.Region == HitRegionKind.Content)
                {
                    var index = (int)h.Data;
                    if (index < itemCount)
                    {
                        bool changed = Hovered != index;
                        Hovered = index;
                        return changed ? MouseResult.HoverChanged : MouseResult.Ignored;
                    }
                }
                // Mouse moved off the widget or to non-content region.
                if (Hovered.HasValue)
                {
                    Hovered = null;
                    return MouseResult.HoverChanged;
                }
                return MouseResult.Ignored;
            }

            case MouseEventKind.ScrollUp:
                ScrollUp(3);
                return MouseResult.Scrolled;

            case MouseEventKind.ScrollDown:
                ScrollDown(3, itemCount);
                return MouseResult.Scrolled;

            default:
                return MouseResult.Ignored;
        }
    }

    /// <summary>Scroll the list up by the given number of lines.</summary>
    public void ScrollUp(int lines)
        => Offset = Math.Max(0, Offset - lines);

    /// <summary>
    /// Scroll the list down by the given number of lines.
    /// Clamps so that the last item can still appear at the top of the viewport.
    /// </summary>
    public void ScrollDown(int lines, int itemCount)
        => Offset = Math.Min(Offset + lines, Math.Max(0, itemCount - 1));

    /// <summary>
    /// Move selection to the next item.
    /// If nothing is selected, selects the first item. Clamps to the last item.
    /// </summary>
    public void SelectNext(int itemCount)
    {
        if (itemCount == 0) return;
        int next = Selected.HasValue
            ? Math.Min(Selected.Value + 1, itemCount - 1)
            : 0;
        Selected = next;
        if (!_multiSelectEnabled)
        {
            _multiSelected.Clear();
            _multiSelected.Add(next);
        }
        ScrollIntoViewRequested = true;
    }

    /// <summary>
    /// Move selection to the previous item.
    /// If nothing is selected, selects the first item. Clamps to 0.
    /// </summary>
    public void SelectPrevious()
    {
        int prev = Selected.HasValue ? Math.Max(0, Selected.Value - 1) : 0;
        Selected = prev;
        if (!_multiSelectEnabled)
        {
            _multiSelected.Clear();
            _multiSelected.Add(prev);
        }
        ScrollIntoViewRequested = true;
    }

    // ── IUndoSupport ──────────────────────────────────────────────────────────

    /// <summary>Get the undo widget ID. Matches Rust <c>ListState::undo_id()</c>.</summary>
    public UndoWidgetId UndoId() => _undoId;

    UndoWidgetId IUndoSupport.UndoWidgetId => _undoId;

    object IUndoSupport.CreateSnapshot() => new ListStateSnapshot
    {
        Selected = Selected,
        Offset = Offset,
        FilterQuery = _filterQuery,
        MultiSelectEnabled = _multiSelectEnabled,
        MultiSelected = new List<int>(_multiSelected),
    };

    bool IUndoSupport.RestoreSnapshot(object snapshot)
    {
        if (snapshot is not ListStateSnapshot snap) return false;
        Selected = snap.Selected;
        Hovered = null;
        Offset = snap.Offset;
        _filterQuery = snap.FilterQuery;
        _multiSelectEnabled = snap.MultiSelectEnabled;
        _multiSelected.Clear();
        foreach (var idx in snap.MultiSelected) _multiSelected.Add(idx);
        return true;
    }

    // ── IListUndoExt ─────────────────────────────────────────────────────────

    int? IListUndoExt.SelectedIndex() => Selected;

    void IListUndoExt.SetSelectedIndex(int? index)
    {
        Selected = index;
        if (index is null)
        {
            Offset = 0;
            _multiSelected.Clear();
        }
        else if (!_multiSelectEnabled)
        {
            _multiSelected.Clear();
            _multiSelected.Add(index.Value);
        }
    }

    // ── IStateful<ListPersistState> ───────────────────────────────────────────

    StateKey IStateful<ListPersistState>.StateKey => new("List", _persistenceId ?? "default");

    ListPersistState IStateful<ListPersistState>.SaveState() => new()
    {
        Selected = Selected,
        Offset = Offset,
        FilterQuery = _filterQuery,
        MultiSelectEnabled = _multiSelectEnabled,
        MultiSelected = new List<int>(_multiSelected),
    };

    void IStateful<ListPersistState>.RestoreState(ListPersistState state)
    {
        Selected = state.Selected;
        Hovered = null;
        Offset = state.Offset;
        _filterQuery = state.FilterQuery;
        _multiSelectEnabled = state.MultiSelectEnabled;
        _multiSelected.Clear();
        foreach (var idx in state.MultiSelected) _multiSelected.Add(idx);
    }
}

// ── ListWidget ────────────────────────────────────────────────────────────────

/// <summary>
/// A widget to display a list of items.
/// Implements <see cref="IStatefulWidget{ListState}"/> and <see cref="IWidget"/> (stateless wrapper).
/// Port of Rust <c>List&lt;'a&gt;</c>.
/// </summary>
public sealed class ListWidget : IStatefulWidget<ListState>, IWidget, IMeasurableWidget,
    IAccessible, CanonicalA11y.IAccessible
{
    Block? _block;
    readonly List<ListItem> _items;
    WidgetStyle _style;
    WidgetStyle _highlightStyle;
    WidgetStyle _hoverStyle;
    // Default highlight symbol is null so that Measure does not add symbol width
    // (the headless measure contract counts only explicitly-set highlight symbols).
    // Render falls back to "›" for selected rows when neither an explicit highlight
    // symbol nor an item marker is set; see the Render path below.
    // DIVERGENCE: upstream defaults highlight_symbol to None and uses item.marker
    // for selected rows. The .NET headless contract encodes a default "›" marker.
    string? _highlightSymbol;

    /// <summary>Optional hit ID for mouse interaction.</summary>
    HitId? _hitId;

    /// <summary>Optional data hash to enable caching of filtered indices.</summary>
    ulong? _dataHash;

    /// <summary>Create a new list from the given items.</summary>
    public ListWidget(IEnumerable<ListItem> items)
        => _items = new List<ListItem>(items);

    /// <summary>Parameterless constructor for object-initializer compat (showcase legacy).</summary>
    public ListWidget() => _items = new List<ListItem>();

    /// <summary>Legacy property-init compat: set items as strings.</summary>
    public IReadOnlyList<string>? Items
    {
        init
        {
            if (value is not null)
                _items.AddRange(value.Select(static s => ListItem.New(s)));
        }
    }

    /// <summary>Legacy property-init compat: set selected index.</summary>
    public int SelectedIndex
    {
        init { _legacySelectedIndex = value; }
    }

    private int _legacySelectedIndex = -1;

    /// <summary>Legacy property-init compat: set focused index (showcase legacy).</summary>
    public int FocusedIndex
    {
        init { _legacyFocusedIndex = value; }
    }

    private int _legacyFocusedIndex = -1;

    /// <summary>
    /// Set an explicit data hash to enable caching of filtered indices.
    /// Highly recommended for large lists.
    /// </summary>
    public ListWidget WithDataHash(ulong hash) { _dataHash = hash; return this; }

    /// <summary>Wrap the list in a decorative block.</summary>
    public ListWidget WithBlock(Block block) { _block = block; return this; }

    /// <summary>Set the base style for the list area.</summary>
    public ListWidget WithStyle(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set the style applied to the selected item.</summary>
    public ListWidget WithHighlightStyle(WidgetStyle style) { _highlightStyle = style; return this; }

    /// <summary>Set the style applied to the hovered item (mouse move).</summary>
    public ListWidget WithHoverStyle(WidgetStyle style) { _hoverStyle = style; return this; }

    /// <summary>Set a symbol displayed before the selected item.</summary>
    public ListWidget WithHighlightSymbol(string symbol) { _highlightSymbol = symbol; return this; }

    /// <summary>
    /// Set a hit ID for mouse interaction.
    /// When set, each list item will register a hit region with the frame's hit grid (if enabled).
    /// The hit data will be the item's index.
    /// </summary>
    public ListWidget WithHitId(HitId id) { _hitId = id; return this; }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Compute (or return cached) filtered item indices.
    /// Caches when a data hash is provided via <see cref="WithDataHash"/>.
    /// </summary>
    public int[] FilteredIndices(ListState state)
    {
        var queryStr = state.FilterQuery();

        // Check cache
        if (_dataHash.HasValue
            && state.CachedDisplayIndices is { } cached
            && cached.Hash == _dataHash.Value
            && cached.Query == queryStr)
        {
            return cached.Indices;
        }

        var query = queryStr.Trim();
        int[] indices;
        if (query.Length == 0)
        {
            indices = Enumerable.Range(0, _items.Count).ToArray();
        }
        else
        {
            var queryLower = query.ToLowerInvariant();
            var result = new List<int>(_items.Count);
            for (int idx = 0; idx < _items.Count; idx++)
            {
                var item = _items[idx];
                // Optimization: check single-span content directly to avoid extra allocation.
                string lineText;
                var lines = item.Content.Lines;
                if (lines.Length > 0 && lines[0].Spans.Length == 1)
                    lineText = lines[0].Spans[0].Content;
                else if (lines.Length > 0)
                    lineText = string.Concat(lines[0].Spans.Select(s => s.Content));
                else
                    lineText = "";

                bool markerMatches = item.Marker.Length > 0
                    && WidgetDrawing.ContainsIgnoreCase(item.Marker, queryLower);
                if (markerMatches || WidgetDrawing.ContainsIgnoreCase(lineText, queryLower))
                    result.Add(idx);
            }
            indices = result.ToArray();
        }

        if (_dataHash.HasValue)
            state.CachedDisplayIndices = (_dataHash.Value, queryStr, indices);

        return indices;
    }

    void ApplyFilteredSelectionGuard(ListState state, int[] filtered, bool forceSelectFirst)
    {
        if (filtered.Length == 0)
        {
            state.Selected = null;
            state.Hovered = null;
            state.Offset = 0;
            state.SelectedIndices().Clear();
            return;
        }

        if (state.Selected.HasValue)
        {
            if (Array.BinarySearch(filtered, state.Selected.Value) < 0)
                state.Selected = filtered[0];
        }
        else if (forceSelectFirst)
        {
            state.Selected = filtered[0];
        }

        state.SelectedIndices().RemoveWhere(idx => Array.BinarySearch(filtered, idx) < 0);
    }

    bool MoveSelectionInFiltered(ListState state, int[] filtered, int direction)
    {
        if (filtered.Length == 0)
        {
            if (state.Selected.HasValue)
            {
                state.Select(null);
                return true;
            }
            return false;
        }

        int maxPos = filtered.Length - 1;
        int nextPos;

        if (state.Selected.HasValue)
        {
            int bsResult = Array.BinarySearch(filtered, state.Selected.Value);
            int currentPos = bsResult >= 0 ? bsResult : ~bsResult;
            nextPos = Math.Clamp(currentPos + direction, 0, maxPos);
        }
        else
        {
            nextPos = direction > 0 ? 0 : maxPos;
        }

        int nextIndex = filtered[nextPos];
        if (state.Selected == nextIndex) return false;

        state.Selected = nextIndex;
        if (!state.MultiSelectEnabled())
        {
            state.SelectedIndices().Clear();
            state.SelectedIndices().Add(nextIndex);
        }
        state.ScrollIntoViewRequested = true;
        return true;
    }

    /// <summary>
    /// Handle keyboard navigation and incremental filtering for this list.
    ///
    /// Supported keys:
    /// - Navigation: <c>Up</c>/<c>Down</c>, <c>k</c>/<c>j</c> (vi-style, always navigate —
    ///   never appended to the filter query)
    /// - Incremental filter input: printable chars (except <c>j</c>/<c>k</c>)
    /// - Filter editing: <c>Backspace</c>, <c>Escape</c>
    /// - Multi-select toggle (when enabled): <c>Space</c>
    /// </summary>
    public bool HandleKey(ListState state, KeyEvent key)
    {
        bool navModifiers = key.Modifiers.HasFlag(KeyModifiers.Ctrl)
            || key.Modifiers.HasFlag(KeyModifiers.Alt)
            || key.Modifiers.HasFlag(KeyModifiers.Super);

        switch (key.Code)
        {
            case KeyCode.Up _ when !navModifiers:
            {
                var filtered = FilteredIndices(state);
                return MoveSelectionInFiltered(state, filtered, -1);
            }
            case KeyCode.Down _ when !navModifiers:
            {
                var filtered = FilteredIndices(state);
                return MoveSelectionInFiltered(state, filtered, 1);
            }
            // j/k are always vi-style navigation, even when a filter is active.
            case KeyCode.Char chK when chK.Character == 'k' && !navModifiers:
            {
                var filtered = FilteredIndices(state);
                return MoveSelectionInFiltered(state, filtered, -1);
            }
            case KeyCode.Char chJ when chJ.Character == 'j' && !navModifiers:
            {
                var filtered = FilteredIndices(state);
                return MoveSelectionInFiltered(state, filtered, 1);
            }
            case KeyCode.Char chSp when chSp.Character == ' ' && state.MultiSelectEnabled():
            {
                if (state.Selected.HasValue)
                {
                    state.ToggleMultiSelected(state.Selected.Value);
                    return true;
                }
                return false;
            }
            case KeyCode.Backspace _:
            {
                var q = state.FilterQuery();
                if (q.Length == 0) return false;
                // Pop last character — matches Rust String::pop()
                state.SetFilterQuery(q[..^1]);
                state.Offset = 0;
                state.ScrollIntoViewRequested = true;
                var filtered2 = FilteredIndices(state);
                ApplyFilteredSelectionGuard(state, filtered2, true);
                return true;
            }
            case KeyCode.Escape _:
            {
                if (state.FilterQuery().Length == 0) return false;
                state.SetFilterQuery("");
                state.Offset = 0;
                state.ScrollIntoViewRequested = true;
                var filtered3 = FilteredIndices(state);
                ApplyFilteredSelectionGuard(state, filtered3, false);
                return true;
            }
            case KeyCode.Char chAny:
            {
                var ch = chAny.Character;
                // Only printable characters, no control, no modifier combos
                if (char.IsControl(ch) || key.Ctrl() || key.Alt() || key.SuperKey()) return false;
                // Preserve uppercase input when Shift is held.
                state.SetFilterQuery(state.FilterQuery() + ch);
                state.Offset = 0;
                state.ScrollIntoViewRequested = true;
                var filtered4 = FilteredIndices(state);
                ApplyFilteredSelectionGuard(state, filtered4, true);
                return true;
            }
            default:
                return false;
        }
    }

    // ── IStatefulWidget<ListState>.Render ─────────────────────────────────────

    /// <summary>Render the list with the given state into the frame.</summary>
    public void Render(Rect area, Frame frame, ListState state)
    {
        Rect listArea;
        if (_block != null)
        {
            _block.Render(area, frame);
            listArea = _block.Inner(area);
        }
        else
        {
            listArea = area;
        }

        if (listArea.Width == 0 || listArea.Height == 0) return;

        // At Skeleton degradation and above, content is not rendered: clear the
        // owned area and return so shorter/stale renders do not leak. Matches the
        // upstream WidgetClearContract: RenderContent is false at Skeleton.
        if (!frame.Degradation.RenderContent())
        {
            WidgetDrawing.ClearTextArea(frame, listArea, _style);
            return;
        }

        bool filterActive = state.FilterQuery().Trim().Length > 0;

        // Clear the owned list area so shorter rows and empty states do not
        // leak stale content from prior renders.
        WidgetDrawing.ClearTextArea(frame, listArea, _style);

        if (_items.Count == 0)
        {
            state.Selected = null;
            state.Hovered = null;
            state.Offset = 0;
            state.SelectedIndices().Clear();
            WidgetDrawing.DrawTextSpan(frame, listArea.X, listArea.Y, "No items", _style, listArea.Right);
            return;
        }

        // Clamp selection/hover to item bounds before applying filters.
        if (state.Selected.HasValue && state.Selected.Value >= _items.Count)
            state.Selected = _items.Count - 1;
        if (state.Hovered.HasValue && state.Hovered.Value >= _items.Count)
            state.Hovered = null;

        var filteredIndices = FilteredIndices(state);
        ApplyFilteredSelectionGuard(state, filteredIndices, filterActive);

        if (filteredIndices.Length == 0)
        {
            WidgetDrawing.DrawTextSpan(frame, listArea.X, listArea.Y, "No matches", _style, listArea.Right);
            return;
        }

        int listHeight = listArea.Height;
        int maxOffset = Math.Max(0, filteredIndices.Length - Math.Max(listHeight, 1));
        state.Offset = Math.Min(state.Offset, maxOffset);

        // Clear hovered if it's no longer in the filtered set.
        if (state.Hovered.HasValue && Array.BinarySearch(filteredIndices, state.Hovered.Value) < 0)
            state.Hovered = null;

        // Ensure visible range includes selected item.
        if (state.ScrollIntoViewRequested)
        {
            if (state.Selected.HasValue)
            {
                int selectedPos = Array.BinarySearch(filteredIndices, state.Selected.Value);
                if (selectedPos >= 0)
                {
                    if (selectedPos >= state.Offset + listHeight)
                        state.Offset = selectedPos - listHeight + 1;
                    else if (selectedPos < state.Offset)
                        state.Offset = selectedPos;
                }
            }
            state.ScrollIntoViewRequested = false;
        }

        for (int row = 0; row < listHeight; row++)
        {
            int slotIndex = state.Offset + row;
            if (slotIndex >= filteredIndices.Length) break;

            int i = filteredIndices[slotIndex];
            var item = _items[i];
            ushort y = (ushort)(listArea.Y + row);
            if (y >= listArea.Bottom) break;

            bool isSelected = state.Selected == i
                || (state.MultiSelectEnabled() && state.SelectedIndices().Contains(i));
            bool isHovered = state.Hovered == i;

            // Determine style: merge highlight on top of item style so
            // unset highlight properties inherit from the item.
            WidgetStyle itemStyle = isHovered ? MergeStyles(_hoverStyle, item.Style) : item.Style;
            if (isSelected) itemStyle = MergeStyles(_highlightStyle, itemStyle);

            // Apply item background style to the whole row
            var rowArea = new Rect(listArea.X, y, listArea.Width, 1);
            WidgetDrawing.ClearTextRow(frame, rowArea, itemStyle);

            // Determine symbol: explicit highlight symbol, else the item marker,
            // else the .NET default "›" for selected rows (headless contract).
            string symbol = isSelected
                ? (_highlightSymbol ?? (item.Marker.Length > 0 ? item.Marker : "›"))
                : item.Marker;

            ushort x = listArea.X;

            // Draw symbol if present
            if (symbol.Length > 0)
            {
                x = WidgetDrawing.DrawTextSpan(frame, x, y, symbol, itemStyle, listArea.Right);
                // Add a space after symbol
                x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", itemStyle, listArea.Right);
            }

            // Draw content — list items are single-line (first line only)
            // Note: List items are currently single-line for simplicity in v1
            var lines = item.Content.Lines;
            if (lines.Length > 0)
            {
                foreach (var span in lines[0].Spans)
                {
                    WidgetStyle spanStyle = span.Style.IsEmpty
                        ? itemStyle
                        : MergeStyles(span.Style, itemStyle);
                    // DIVERGENCE: Rust draw_text_span_with_link carries an optional OSC hyperlink.
                    // C# port omits link parameter — not yet implemented in the render layer.
                    x = WidgetDrawing.DrawTextSpan(frame, x, y, span.Content, spanStyle, listArea.Right);
                    if (x >= listArea.Right) break;
                }
            }

            // Register hit region for this item (if hit testing enabled)
            if (_hitId.HasValue)
                frame.RegisterHit(rowArea, _hitId.Value, HitRegionKind.Content, (ulong)i);
        }

        // Draw scroll indicators when items overflow the viewport
        if (filteredIndices.Length > listHeight && listArea.Width > 0)
        {
            ushort indicatorX = (ushort)(listArea.Right - 1);
            if (state.Offset > 0)
                WidgetDrawing.DrawTextSpan(frame, indicatorX, listArea.Y, "↑", _style, listArea.Right);
            if (state.Offset + listHeight < filteredIndices.Length)
                WidgetDrawing.DrawTextSpan(frame, indicatorX, (ushort)(listArea.Bottom - 1), "↓", _style, listArea.Right);
        }
    }

    // ── IWidget (stateless wrapper) ───────────────────────────────────────────

    /// <summary>Render the list using a default state.</summary>
    void IWidget.Render(Rect area, Frame frame)
    {
        var state = new ListState();
        // Apply legacy property-init selection/focus (showcase legacy compat).
        int legacy = _legacySelectedIndex >= 0 ? _legacySelectedIndex : _legacyFocusedIndex;
        if (legacy >= 0)
            state.Select(legacy);
        Render(area, frame, state);
    }

    // ── Accessibility ─────────────────────────────────────────────────────────

    /// <summary>Get the legacy compatibility projection of this list's accessibility nodes.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        ulong baseId = WidgetDrawing.A11yNodeId(area);
        int itemCount = _items.Count;
        ulong[] childIds = Enumerable.Range(0, itemCount)
            .Select(index => baseId + 1UL + (ulong)index)
            .ToArray();

        string title = _block?.TitleText() ?? string.Empty;
        CanonicalA11y.A11yNodeInfo listNode = CanonicalA11y.A11yNodeInfo
            .New(baseId, CanonicalA11y.A11yRole.List, area)
            .WithChildren(childIds);
        if (title.Length > 0)
            listNode = listNode.WithName(title);
        listNode = listNode.WithDescription($"{itemCount} items");

        var nodes = new List<CanonicalA11y.A11yNodeInfo>(itemCount + 1) { listNode };
        for (int index = 0; index < itemCount; index++)
        {
            ulong itemId = baseId + 1UL + (ulong)index;
            TextContent content = _items[index].Content;
            string itemText = content.Lines.Length == 0
                ? string.Empty
                : string.Concat(content.Lines[0].Spans.Select(span => span.Content));

            CanonicalA11y.A11yNodeInfo itemNode = CanonicalA11y.A11yNodeInfo
                .New(itemId, CanonicalA11y.A11yRole.ListItem, area)
                .WithParent(baseId);
            if (itemText.Length > 0)
                itemNode = itemNode.WithName(itemText);
            nodes.Add(itemNode);
        }

        return nodes;
    }

    // ── IMeasurableWidget ─────────────────────────────────────────────────────

    /// <summary>Measure the preferred size of this list widget.</summary>
    public SizeConstraints Measure(Size available)
    {
        (ushort chromeW, ushort chromeH) = _block?.ChromeSize() ?? (0, 0);

        if (_items.Count == 0)
        {
            return new SizeConstraints
            {
                Min = new Size(chromeW, chromeH),
                Preferred = new Size(chromeW, chromeH),
                Max = null,
            };
        }

        var innerAvailable = new Size(
            (ushort)Math.Max(0, available.Width - chromeW),
            (ushort)Math.Max(0, available.Height - chromeH));

        ushort maxWidth = 0;
        ushort totalHeight = 0;
        foreach (var item in _items)
        {
            var ic = MeasureItem(item, innerAvailable);
            if (ic.Preferred.Width > maxWidth) maxWidth = ic.Preferred.Width;
            totalHeight = (ushort)Math.Min(totalHeight + ic.Preferred.Height, ushort.MaxValue);
        }

        // Add highlight symbol width if present
        if (_highlightSymbol is { } sym)
        {
            ushort symWidth = (ushort)(WidgetDrawing.GraphemeWidth(sym) + 1); // +1 for space
            maxWidth = (ushort)Math.Min(maxWidth + symWidth, ushort.MaxValue);
        }

        ushort preferredWidth = (ushort)Math.Min(maxWidth + chromeW, ushort.MaxValue);
        ushort preferredHeight = (ushort)Math.Min(totalHeight + chromeH, ushort.MaxValue);
        ushort minHeight = (ushort)(chromeH + Math.Min(1, (int)totalHeight));

        return new SizeConstraints
        {
            Min = new Size(chromeW, minHeight),
            Preferred = new Size(preferredWidth, preferredHeight),
            Max = null, // Lists can scroll, so no max
        };
    }

    /// <summary>Measure a single list item.</summary>
    static SizeConstraints MeasureItem(ListItem item, Size _available)
    {
        ushort markerWidth = (ushort)WidgetDrawing.GraphemeWidth(item.Marker);
        ushort spaceAfterMarker = item.Marker.Length > 0 ? (ushort)1 : (ushort)0;

        ushort textWidth = item.Content.Lines.Length > 0
            ? (ushort)Math.Min(item.Content.Lines[0].Width, ushort.MaxValue)
            : (ushort)0;

        ushort total = (ushort)Math.Min(
            (int)markerWidth + spaceAfterMarker + textWidth,
            ushort.MaxValue);

        return SizeConstraints.Exact(new Size(total, 1));
    }

    /// <summary>Whether this list has an intrinsic size (true when non-empty).</summary>
    public bool HasIntrinsicSize() => _items.Count > 0;

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Merge two styles: apply <paramref name="top"/> properties over <paramref name="bottom"/>.
    /// Matches Rust <c>Style::merge</c>.
    /// </summary>
    static WidgetStyle MergeStyles(WidgetStyle top, WidgetStyle bottom)
        => new(top.Fg ?? bottom.Fg, top.Bg ?? bottom.Bg, top.Attrs ?? bottom.Attrs);
}
