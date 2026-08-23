// Port of .external/frankentui/crates/ftui-widgets/src/tabs.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Horizontal tab bar with keyboard navigation, overflow handling, closable tabs, and tab reordering helpers.

using FrankenTui.Core;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

// ── Tab ──────────────────────────────────────────────────────────────────────

/// <summary>A single tab entry.</summary>
public sealed class Tab
{
    private readonly string _title;
    private readonly WidgetStyle _style;
    private readonly bool _closable;

    private Tab(string title, WidgetStyle style, bool closable)
    {
        _title = title;
        _style = style;
        _closable = closable;
    }

    /// <summary>Create a new tab with a title.</summary>
    public static Tab New(string title) => new(title, WidgetStyle.Default, false);

    /// <summary>Set style for this tab.</summary>
    public Tab WithStyle(WidgetStyle style) => new(_title, style, _closable);

    /// <summary>Set whether this tab can be closed.</summary>
    public Tab WithClosable(bool closable) => new(_title, _style, closable);

    // Builder-pattern alias matching upstream: .closable(bool)
    /// <summary>Set whether this tab can be closed (builder alias).</summary>
    public Tab Closable(bool closable) => new(_title, _style, closable);

    // Builder-pattern alias matching upstream: .style(Style)
    /// <summary>Set style for this tab (builder alias).</summary>
    public Tab Style(WidgetStyle style) => new(_title, style, _closable);

    /// <summary>Get tab title.</summary>
    public string Title() => _title;

    /// <summary>Whether this tab can be closed.</summary>
    public bool IsClosable() => _closable;

    /// <summary>Get tab style.</summary>
    internal WidgetStyle GetStyle() => _style;
}

// ── TabsState ─────────────────────────────────────────────────────────────────

/// <summary>State for a <see cref="Tabs"/> widget.</summary>
public sealed class TabsState
{
    /// <summary>Active tab index.</summary>
    public int Active { get; set; }

    /// <summary>Left-most tab index when overflow scrolling is active.</summary>
    public int Offset { get; set; }

    /// <summary>Select a specific tab index.</summary>
    public bool Select(int index, int tabCount)
    {
        if (tabCount == 0)
        {
            Active = 0;
            Offset = 0;
            return false;
        }
        int next = Math.Min(index, Math.Max(tabCount - 1, 0));
        if (Active == next)
            return false;
        Active = next;
        if (Active < Offset)
            Offset = Active;
        return true;
    }

    /// <summary>Move active tab right by one.</summary>
    public bool Next(int tabCount)
    {
        if (tabCount == 0)
            return false;
        return Select(Math.Min(Active + 1, Math.Max(tabCount - 1, 0)), tabCount);
    }

    /// <summary>Move active tab left by one.</summary>
    public bool Previous(int tabCount)
    {
        if (tabCount == 0)
            return false;
        return Select(Math.Max(Active - 1, 0), tabCount);
    }

    /// <summary>
    /// Handle keyboard tab switching.
    /// Supported: Left / Right, number keys 1..9.
    /// </summary>
    public bool HandleKey(KeyEvent key, int tabCount)
    {
        switch (key.Code)
        {
            case KeyCode.Left:
                return Previous(tabCount);
            case KeyCode.Right:
                return Next(tabCount);
            case KeyCode.Char ch when ch.Character >= '1' && ch.Character <= '9':
            {
                int idx = ch.Character - '1';
                if (idx >= tabCount)
                    return false;
                return Select(idx, tabCount);
            }
            default:
                return false;
        }
    }

    /// <summary>
    /// Handle mouse selection for tabs.
    /// Hit data convention: each tab row registers data = tab_index as ulong.
    /// </summary>
    public MouseResult HandleMouse(
        MouseEvent evt,
        (HitId id, HitRegionKind region, ulong data)? hit,
        HitId expectedId,
        int tabCount)
    {
        if (evt.Kind is MouseEventKind.Down { Button: MouseButton.Left })
        {
            if (hit is { } h && h.id == expectedId && h.region == HitRegionKind.Content)
            {
                int idx = (int)h.data;
                if (idx < tabCount)
                {
                    if (Active == idx)
                        return MouseResult.Activated(idx);
                    Select(idx, tabCount);
                    return MouseResult.Selected(idx);
                }
            }
            return MouseResult.Ignored;
        }
        return MouseResult.Ignored;
    }
}

// ── Tabs ──────────────────────────────────────────────────────────────────────

/// <summary>Tabs widget.</summary>
public sealed class Tabs : IStatefulWidget<TabsState>, IWidget, IAccessible, CanonicalA11y.IAccessible
{
    private readonly List<Tab> _tabs;
    private WidgetStyle _style;
    private WidgetStyle _activeStyle;
    private string _separator;
    private string _closeMarker;
    private string _overflowLeftMarker;
    private string _overflowRightMarker;
    private HitId? _hitId;

    /// <summary>Create tabs from an enumerable of Tab entries.</summary>
    public Tabs(IEnumerable<Tab> tabs)
    {
        _tabs = tabs.ToList();
        _style = WidgetStyle.Default;
        _activeStyle = WidgetStyle.Default;
        _separator = " ";
        _closeMarker = " x";
        _overflowLeftMarker = "<";
        _overflowRightMarker = ">";
        _hitId = null;
    }

    /// <summary>Set base style.</summary>
    public Tabs WithStyle(WidgetStyle style) { _style = style; return this; }

    // Builder alias matching upstream: .style(Style)
    /// <summary>Set base style (builder alias).</summary>
    public Tabs Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set active tab style.</summary>
    public Tabs WithActiveStyle(WidgetStyle style) { _activeStyle = style; return this; }

    // Builder alias matching upstream: .active_style(Style)
    /// <summary>Set active tab style (builder alias).</summary>
    public Tabs ActiveStyle(WidgetStyle style) { _activeStyle = style; return this; }

    /// <summary>Set separator between tabs.</summary>
    public Tabs WithSeparator(string separator) { _separator = separator; return this; }

    // Builder alias matching upstream: .separator(str)
    /// <summary>Set separator between tabs (builder alias).</summary>
    public Tabs Separator(string separator) { _separator = separator; return this; }

    /// <summary>Set hit id for mouse interactions.</summary>
    public Tabs WithHitId(HitId id) { _hitId = id; return this; }

    // Builder alias matching upstream: .hit_id(HitId)
    /// <summary>Set hit id for mouse interactions (builder alias).</summary>
    public Tabs HitId(HitId id) { _hitId = id; return this; }

    /// <summary>Immutable tab slice.</summary>
    public IReadOnlyList<Tab> GetTabs() => _tabs;

    // ── Private helpers ────────────────────────────────────────────────────

    private string TabLabel(Tab tab, bool active)
    {
        var sb = new System.Text.StringBuilder();
        if (active)
            sb.Append('[');
        else
            sb.Append(' ');
        sb.Append(tab.Title());
        if (tab.IsClosable())
            sb.Append(_closeMarker);
        if (active)
            sb.Append(']');
        else
            sb.Append(' ');
        return sb.ToString();
    }

    private int VisibleEnd(TabsState state, int width)
    {
        if (_tabs.Count == 0 || width == 0)
            return state.Offset;

        int sepWidth = DisplayWidth(_separator);
        int used = 0;
        int end = state.Offset;

        for (int idx = state.Offset; idx < _tabs.Count; idx++)
        {
            int w = DisplayWidth(TabLabel(_tabs[idx], idx == state.Active));
            int extra = idx == state.Offset ? 0 : sepWidth;

            if (end == state.Offset)
            {
                // Always allow at least one tab; draw helper clips if too long.
                used = w;
                end = idx + 1;
                if (used > width)
                    break;
                continue;
            }

            if (SaturatingAdd(SaturatingAdd(used, extra), w) > width)
                break;

            used = SaturatingAdd(SaturatingAdd(used, extra), w);
            end = idx + 1;
        }

        return Math.Max(end, Math.Min(state.Offset + 1, _tabs.Count));
    }

    private (int start, int end, bool overflowLeft, bool overflowRight) ComputeVisibleRange(
        TabsState state, int areaWidth)
    {
        if (_tabs.Count == 0 || areaWidth == 0)
        {
            state.Active = 0;
            state.Offset = 0;
            return (0, 0, false, false);
        }

        state.Active = Math.Min(state.Active, Math.Max(_tabs.Count - 1, 0));
        state.Offset = Math.Min(state.Offset, Math.Max(_tabs.Count - 1, 0));
        if (state.Active < state.Offset)
            state.Offset = state.Active;

        int leftMarkerW = DisplayWidth(_overflowLeftMarker);
        int rightMarkerW = DisplayWidth(_overflowRightMarker);

        int availableWidth = areaWidth;
        int start = state.Offset;
        int end = VisibleEnd(state, availableWidth);

        // If active is out of view (e.g. initial render with small width), jump to it
        if (state.Active >= end)
        {
            start = state.Active;
            state.Offset = start;
            end = VisibleEnd(state, availableWidth);
        }

        // Iteratively refine width based on overflow markers
        for (int i = 0; i < 3; i++)
        {
            bool overflowLeft = start > 0;
            bool overflowRight = end < _tabs.Count;

            int nextWidth = areaWidth;
            if (overflowLeft)
                nextWidth = SaturatingSub(nextWidth, leftMarkerW);
            if (overflowRight)
                nextWidth = SaturatingSub(nextWidth, rightMarkerW);

            if (nextWidth == availableWidth)
                break;
            availableWidth = nextWidth;

            // Re-calculate with new width
            end = VisibleEnd(state, availableWidth);

            // Ensure active is still visible
            if (state.Active >= end)
            {
                start = state.Active;
                state.Offset = start;
                end = VisibleEnd(state, availableWidth);
            }
        }

        bool finalOverflowLeft = start > 0;
        bool finalOverflowRight = end < _tabs.Count;
        return (start, end, finalOverflowLeft, finalOverflowRight);
    }

    /// <summary>Close the active tab if it is closable.</summary>
    public Tab? CloseActive(TabsState state)
    {
        if (_tabs.Count == 0)
        {
            state.Active = 0;
            state.Offset = 0;
            return null;
        }
        state.Active = Math.Min(state.Active, Math.Max(_tabs.Count - 1, 0));
        if (!_tabs[state.Active].IsClosable())
            return null;

        var removed = _tabs[state.Active];
        _tabs.RemoveAt(state.Active);

        if (_tabs.Count == 0)
        {
            state.Active = 0;
            state.Offset = 0;
        }
        else if (state.Active >= _tabs.Count)
        {
            state.Active = Math.Max(_tabs.Count - 1, 0);
            state.Offset = Math.Min(state.Offset, state.Active);
        }
        return removed;
    }

    /// <summary>Move active tab one position to the left.</summary>
    public bool MoveActiveLeft(TabsState state)
    {
        if (_tabs.Count < 2 || state.Active == 0 || state.Active >= _tabs.Count)
            return false;
        // swap
        (_tabs[state.Active], _tabs[state.Active - 1]) = (_tabs[state.Active - 1], _tabs[state.Active]);
        state.Active -= 1;
        state.Offset = Math.Min(state.Offset, state.Active);
        return true;
    }

    /// <summary>Move active tab one position to the right.</summary>
    public bool MoveActiveRight(TabsState state)
    {
        if (_tabs.Count < 2 || state.Active + 1 >= _tabs.Count)
            return false;
        // swap
        (_tabs[state.Active], _tabs[state.Active + 1]) = (_tabs[state.Active + 1], _tabs[state.Active]);
        state.Active += 1;
        return true;
    }

    // ── IStatefulWidget<TabsState> ─────────────────────────────────────────

    /// <summary>Render the tabs widget with the given state.</summary>
    public void Render(Rect area, Frame frame, TabsState state)
    {
        if (area.IsEmpty || area.Height == 0)
            return;

        var deg = frame.Degradation;
        var baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;

        WidgetDrawing.ClearTextRow(frame, area, baseStyle);

        if (!deg.RenderContent() || _tabs.Count == 0)
            return;

        var (start, end, overflowLeft, overflowRight) =
            ComputeVisibleRange(state, area.Width);

        ushort left = area.X;
        ushort right = area.Right;

        if (overflowLeft)
        {
            WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, _overflowLeftMarker, baseStyle, area.Right);
            left = (ushort)Math.Min(ushort.MaxValue, left + DisplayWidth(_overflowLeftMarker));
        }
        if (overflowRight)
        {
            right = (ushort)Math.Max(0, right - DisplayWidth(_overflowRightMarker));
            WidgetDrawing.DrawTextSpan(frame, right, area.Y, _overflowRightMarker, baseStyle, area.Right);
        }

        ushort x = left;
        for (int idx = start; idx < end; idx++)
        {
            if (x >= right)
                break;
            if (idx > start && _separator.Length > 0)
            {
                x = WidgetDrawing.DrawTextSpan(frame, x, area.Y, _separator, baseStyle, right);
                if (x >= right)
                    break;
            }

            var tab = _tabs[idx];
            var label = TabLabel(tab, idx == state.Active);
            var tabStyle = baseStyle;
            if (deg.ApplyStyling())
            {
                tabStyle = _style.Merge(tab.GetStyle());
                if (idx == state.Active)
                    tabStyle = _activeStyle.Merge(tabStyle);
            }
            ushort before = x;
            x = WidgetDrawing.DrawTextSpan(frame, x, area.Y, label, tabStyle, right);

            if (_hitId is { } id)
            {
                ushort hitWidth = (ushort)Math.Max((ushort)1, (ushort)(x > before ? x - before : 0));
                frame.RegisterHit(
                    new Rect(before, area.Y, hitWidth, 1),
                    id,
                    HitRegionKind.Content,
                    (ulong)idx);
            }
        }
    }

    // ── IWidget ────────────────────────────────────────────────────────────

    /// <summary>Render the tabs widget with default state.</summary>
    void IWidget.Render(Rect area, Frame frame)
    {
        var state = new TabsState();
        Render(area, frame, state);
    }

    bool IWidget.IsEssential() => true;

    // ── IAccessible ────────────────────────────────────────────────────────

    /// <summary>
    /// Get the legacy compatibility projection of this widget's accessibility nodes.
    /// Port of ftui_a11y::Accessible::accessibility_nodes.
    /// </summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        ulong baseId = WidgetDrawing.A11yNodeId(area);
        int tabCount = _tabs.Count;
        var childIds = Enumerable.Range(0, tabCount)
            .Select(i => baseId + 1 + (ulong)i)
            .ToList();

        CanonicalA11y.A11yNodeInfo groupNode = CanonicalA11y.A11yNodeInfo
            .New(baseId, CanonicalA11y.A11yRole.Group, area)
            .WithName($"{tabCount} tabs")
            .WithChildren(childIds);

        var nodes = new List<CanonicalA11y.A11yNodeInfo> { groupNode };
        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            ulong tabId = baseId + 1 + (ulong)i;
            nodes.Add(
                CanonicalA11y.A11yNodeInfo.New(tabId, CanonicalA11y.A11yRole.Tab, area)
                    .WithName(tab.Title())
                    .WithParent(baseId));
        }
        return nodes;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static int DisplayWidth(string s)
    {
        // Port of ftui_text::display_width — sum grapheme widths.
        if (string.IsNullOrEmpty(s)) return 0;
        int w = 0;
        var en = System.Globalization.StringInfo.GetTextElementEnumerator(s);
        while (en.MoveNext())
            w += WidgetDrawing.GraphemeWidth(en.GetTextElement());
        return w;
    }

    private static int SaturatingAdd(int a, int b)
    {
        long r = (long)a + b;
        return r > int.MaxValue ? int.MaxValue : (int)r;
    }

    private static int SaturatingSub(int a, int b)
    {
        int r = a - b;
        return r < 0 ? 0 : r;
    }
}
