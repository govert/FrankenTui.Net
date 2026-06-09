// SPDX-License-Identifier: Apache-2.0
// Port of focus/indicator.rs (221L) + focus/manager.rs (1969L) + focus/mod.rs (13L)
// Focus indicator, focus manager with groups, traps, event handling.

using FrankenTui.Core;

namespace FrankenTui.Widgets.Focus;

// ── FocusIndicator ────────────────────────────────────────────────────────

public enum FocusIndicatorKind { StyleOverlay, Underline, Border, None }

public sealed class FocusIndicator
{
    public FocusIndicatorKind Kind { get; set; } = FocusIndicatorKind.StyleOverlay;
    public WidgetStyle Style { get; set; } = WidgetStyle.Default;

    public static FocusIndicator Default => new() { Kind = FocusIndicatorKind.StyleOverlay };
    public static FocusIndicator StyleOverlay(WidgetStyle s) => new() { Kind = FocusIndicatorKind.StyleOverlay, Style = s };
    public static FocusIndicator Underline() => new() { Kind = FocusIndicatorKind.Underline };
    public static FocusIndicator Border() => new() { Kind = FocusIndicatorKind.Border };
    public static FocusIndicator None() => new() { Kind = FocusIndicatorKind.None };

    public bool IsVisible => Kind != FocusIndicatorKind.None;

    public FocusIndicator WithStyle(WidgetStyle s) { Style = s; return this; }
    public FocusIndicator WithKind(FocusIndicatorKind k) { Kind = k; return this; }

    public WidgetStyle ApplyTo(WidgetStyle baseStyle) =>
        Kind == FocusIndicatorKind.None ? baseStyle : new WidgetStyle(Style.Fg ?? baseStyle.Fg, Style.Bg ?? baseStyle.Bg, Style.Attrs ?? baseStyle.Attrs);
}

// ── FocusEvent ────────────────────────────────────────────────────────────

public enum FocusEventKind { Directional, Tab, ShiftTab, MouseClick }

public sealed class FocusEvent
{
    public FocusEventKind Kind { get; set; }
    public NavDirection? Direction { get; set; }
    public ulong? TargetId { get; set; }

    public static FocusEvent Directional(NavDirection d) => new() { Kind = FocusEventKind.Directional, Direction = d };
    public static FocusEvent Tab() => new() { Kind = FocusEventKind.Tab };
    public static FocusEvent ShiftTab() => new() { Kind = FocusEventKind.ShiftTab };
    public static FocusEvent MouseClick(ulong id) => new() { Kind = FocusEventKind.MouseClick, TargetId = id };
}

// ── FocusGroup / FocusTrap ────────────────────────────────────────────────

public sealed class FocusGroup
{
    public uint Id { get; set; }
    public List<ulong> Members { get; set; }
    public FocusGroup(uint id, List<ulong> members) => (Id, Members) = (id, members);
}

public sealed class FocusTrap
{
    public uint GroupId { get; set; }
    public bool Wrap { get; set; } = true;
    public FocusTrap(uint groupId, bool wrap = true) => (GroupId, Wrap) = (groupId, wrap);
}

// ── FocusManager ──────────────────────────────────────────────────────────

public sealed class FocusManager
{
    FocusGraph _graph = new(); Dictionary<uint, FocusTrap> _traps = new();
    ulong? _current; List<ulong> _history = new(); FocusIndicator _indicator = FocusIndicator.Default;

    public FocusManager() { }

    public FocusGraph Graph => _graph;
    public ulong? CurrentFocus => _current;
    public FocusIndicator Indicator { get => _indicator; set => _indicator = value; }

    public void Register(FocusNode node) => _graph.Insert(node);

    public void Unregister(ulong id) { _graph.Remove(id); if (_current == id) _current = null; }

    public void SetFocus(ulong? id) { if (id != null && _graph.Get(id.Value)?.IsFocusable == true) { _current = id; _history.Add(id.Value); if (_history.Count > 32) _history.RemoveAt(0); } }

    public ulong? HandleEvent(FocusEvent evt)
    {
        if (_current == null) return null;

        if (evt.Kind == FocusEventKind.Directional && evt.Direction is { } dir)
        {
            var target = SpatialNav.SpatialNavigate(_graph, _current.Value, dir);
            if (target is { } t) SetFocus(t);
            return target;
        }

        if (evt.Kind is FocusEventKind.Tab or FocusEventKind.ShiftTab)
        {
            var trap = _current is { } cur
                ? _graph.Get(cur)?.GroupId is { } gid ? (_traps.TryGetValue(gid, out var ft) ? ft : null) : null
                : null;

            if (trap != null)
            {
                var order = _graph.GroupTabOrder(trap.GroupId);
                if (order.Count > 0)
                {
                    int idx = order.IndexOf(_current.Value);
                    if (evt.Kind == FocusEventKind.Tab) idx = idx < order.Count - 1 ? idx + 1 : (trap.Wrap ? 0 : order.Count - 1);
                    else idx = idx > 0 ? idx - 1 : (trap.Wrap ? order.Count - 1 : 0);
                    if (idx >= 0 && idx < order.Count) SetFocus(order[idx]);
                    return order[idx];
                }
            }
            else
            {
                var order = _graph.TabOrder();
                if (order.Count > 0)
                {
                    int idx = order.IndexOf(_current.Value);
                    if (evt.Kind == FocusEventKind.Tab) idx = (idx + 1) % order.Count;
                    else idx = idx > 0 ? idx - 1 : order.Count - 1;
                    if (idx >= 0 && idx < order.Count) SetFocus(order[idx]);
                    return order[idx];
                }
            }
        }

        if (evt.Kind == FocusEventKind.MouseClick && evt.TargetId is { } tid)
        {
            SetFocus(tid);
            return tid;
        }

        return null;
    }

    public void SetFocusTrap(uint groupId, bool wrap = true) => _traps[groupId] = new FocusTrap(groupId, wrap);
    public void RemoveFocusTrap(uint groupId) => _traps.Remove(groupId);
    public FocusNode? GetFocusedNode() => _current is { } c ? _graph.Get(c) : null;
    public void Clear() { _graph.Clear(); _current = null; _history.Clear(); }
}
