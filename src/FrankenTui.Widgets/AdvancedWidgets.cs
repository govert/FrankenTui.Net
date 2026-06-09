// SPDX-License-Identifier: Apache-2.0
// Port of debug_overlay.rs (1023L), layout.rs (820L), constraint_overlay.rs (925L),
// height_predictor.rs (1079L), history_panel.rs (937L), file_picker.rs (1162L),
// drift_visualization.rs (1053L), voi_debug_overlay.rs (1110L), drag.rs (1549L),
// keyboard_drag.rs (1357L), help_index.rs (901L), help_registry.rs (598L)

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ── DebugOverlay ─────────────────────────────────────────────────────────

public sealed class DebugOverlay : IWidget
{
    List<(Rect Area, string Label)> _regions = new(); WidgetStyle _style; bool _visible = true;
    public DebugOverlay Visible(bool v) { _visible = v; return this; }
    public DebugOverlay Style(WidgetStyle s) { _style = s; return this; }
    public void Record(Rect area, string label) => _regions.Add((area, label));
    public void Clear() => _regions.Clear();

    public void Render(Rect area, Frame frame)
    {
        if (!_visible || area.Width == 0 || area.Height == 0) return;
        foreach (var (r, label) in _regions)
        {
            for (ushort x = r.X; x < Math.Min(r.Right, area.Right); x++)
            {
                if (r.Y < area.Bottom) frame.Buffer.SetFast(x, r.Y, Cell.FromChar('─'));
                if (r.Height > 1 && (ushort)(r.Bottom - 1) < area.Bottom) frame.Buffer.SetFast(x, (ushort)(r.Bottom - 1), Cell.FromChar('─'));
            }
            for (ushort y = r.Y; y < Math.Min(r.Bottom, area.Bottom); y++)
            {
                if (r.X < area.Right) frame.Buffer.SetFast(r.X, y, Cell.FromChar('│'));
                if (r.Width > 1 && (ushort)(r.Right - 1) < area.Right) frame.Buffer.SetFast((ushort)(r.Right - 1), y, Cell.FromChar('│'));
            }
            if (label.Length > 0 && r.X < area.Right && r.Y < area.Bottom)
                WidgetDrawing.DrawTextSpan(frame, r.X, r.Y, label, _style, area.Right);
        }
    }
}

// ── Layout ────────────────────────────────────────────────────────────────

public sealed class LayoutChild { public IWidget Widget { get; set; } = null!; public int Row, Col, Rowspan = 1, Colspan = 1; }
public sealed class LayoutWidget : IWidget
{
    List<LayoutChild> _children = new(); Constraint[] _rowConstraints = Array.Empty<Constraint>(), _colConstraints = Array.Empty<Constraint>();
    ushort _rowGap, _colGap;

    public LayoutWidget Rows(params Constraint[] c) { _rowConstraints = c; return this; }
    public LayoutWidget Columns(params Constraint[] c) { _colConstraints = c; return this; }
    public LayoutWidget Gap(ushort g) { _rowGap = _colGap = g; return this; }
    public LayoutWidget RowGap(ushort g) { _rowGap = g; return this; }
    public LayoutWidget ColGap(ushort g) { _colGap = g; return this; }
    public LayoutWidget Child(IWidget w, int row, int col, int rowspan = 1, int colspan = 1)
    { _children.Add(new LayoutChild { Widget = w, Row = row, Col = col, Rowspan = Math.Max(1, rowspan), Colspan = Math.Max(1, colspan) }); return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0 || _children.Count == 0) return;
        // Simple grid: distribute rows and columns equally
        int nRows = Math.Max(1, _rowConstraints.Length), nCols = Math.Max(1, _colConstraints.Length);
        if (nRows == 0 || nCols == 0) return;
        ushort cellW = (ushort)((area.Width - (ushort)((nCols - 1) * _colGap)) / nCols);
        ushort cellH = (ushort)((area.Height - (ushort)((nRows - 1) * _rowGap)) / nRows);
        foreach (var ch in _children)
        {
            ushort cx = (ushort)(area.X + ch.Col * (cellW + _colGap));
            ushort cy = (ushort)(area.Y + ch.Row * (cellH + _rowGap));
            ushort cw = (ushort)(cellW * ch.Colspan + (ch.Colspan - 1) * _colGap);
            ushort chh = (ushort)(cellH * ch.Rowspan + (ch.Rowspan - 1) * _rowGap);
            var childArea = new Rect(cx, cy, cw, chh);
            ch.Widget.Render(childArea, frame);
        }
    }
}

// ── ConstraintOverlay ─────────────────────────────────────────────────────

public sealed class ConstraintOverlay : IWidget
{
    IWidget _base, _overlay; ushort _overlayX, _overlayY, _overlayW, _overlayH;

    public ConstraintOverlay(IWidget baseWidget, IWidget overlay)
    { _base = baseWidget; _overlay = overlay; _overlayW = _overlayH = 1; }

    public ConstraintOverlay Position(ushort x, ushort y) { _overlayX = x; _overlayY = y; return this; }
    public ConstraintOverlay Size(ushort w, ushort h) { _overlayW = w; _overlayH = h; return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        _base.Render(area, frame);
        var ov = new Rect(
            Math.Min((ushort)(area.X + _overlayX), area.Right),
            Math.Min((ushort)(area.Y + _overlayY), area.Bottom),
            Math.Min(_overlayW, (ushort)(area.Right - area.X - _overlayX)),
            Math.Min(_overlayH, (ushort)(area.Bottom - area.Y - _overlayY)));
        _overlay.Render(ov, frame);
    }
}

// ── HeightPredictor ───────────────────────────────────────────────────────
// DIVERGENCE: The stub previously here was not a faithful port of height_predictor.rs.
// The full 1-1 port of height_predictor.rs lives in HeightPredictor.cs (Bayesian predictor).

// ── HistoryPanel ──────────────────────────────────────────────────────────

public sealed class HistoryEntry { public string Command { get; set; } = ""; public DateTime Timestamp { get; set; } public string? Result { get; set; } }
public sealed class HistoryPanel : IWidget
{
    List<HistoryEntry> _entries = new(); WidgetStyle _style, _selectedStyle; int? _selected;

    public HistoryPanel Style(WidgetStyle s) { _style = s; return this; }
    public HistoryPanel SelectedStyle(WidgetStyle s) { _selectedStyle = s; return this; }
    public void SetEntries(List<HistoryEntry> e) => _entries = e;
    public void Select(int? i) => _selected = i;

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        for (int i = 0; i < area.Height && i < _entries.Count; i++)
        {
            var s = i == _selected ? _selectedStyle : _style;
            WidgetDrawing.DrawTextSpan(frame, area.X, (ushort)(area.Y + i),
                _entries[i].Command, s, area.Right);
        }
    }
}

// ── FilePicker ────────────────────────────────────────────────────────────
// DIVERGENCE: The stub previously here has been replaced by the full 1-1 port
// of file_picker.rs which lives in FilePicker.cs.

// ── DriftVisualization ────────────────────────────────────────────────────
// DIVERGENCE: The stub DriftVisualization (scatter-plot) previously here has been replaced
// by the full 1-1 port of drift_visualization.rs, which lives in DriftVisualization.cs.

// ── Drag support ──────────────────────────────────────────────────────────

public sealed class WidgetDragState { public bool Active { get; set; } public ushort StartX { get; set; } public ushort StartY { get; set; } public ushort CurrentX { get; set; } public ushort CurrentY { get; set; } }
public sealed class DragWidget<W> : IWidget where W : IWidget
{
    W _inner; WidgetDragState _state = new();
    public DragWidget(W inner) => _inner = inner;
    public void Begin(ushort x, ushort y) { _state.Active = true; _state.StartX = _state.CurrentX = x; _state.StartY = _state.CurrentY = y; }
    public void Move(ushort x, ushort y) { _state.CurrentX = x; _state.CurrentY = y; }
    public void End() { _state.Active = false; }
    public WidgetDragState State => _state;
    public void Render(Rect area, Frame frame) => _inner.Render(area, frame);
}

// ── KeyboardDrag ──────────────────────────────────────────────────────────
// DIVERGENCE: The old stub KeyboardDragState (Active/Index/TargetIndex) was renamed to
// WidgetKeyboardDragState to avoid collision with the full port in KeyboardDrag.cs.

public sealed class WidgetKeyboardDragState { public bool Active { get; set; } public int Index { get; set; } public int TargetIndex { get; set; } }
public sealed class KeyboardDragWidget<W> : IWidget where W : IWidget
{
    W _inner; WidgetKeyboardDragState _state = new();
    public KeyboardDragWidget(W inner) => _inner = inner;
    public void Begin(int idx) { _state.Active = true; _state.Index = _state.TargetIndex = idx; }
    public void Move(int target) { _state.TargetIndex = target; }
    public void End() { _state.Active = false; }
    public WidgetKeyboardDragState State => _state;
    public void Render(Rect area, Frame frame) => _inner.Render(area, frame);
}

// ── HelpIndex ─────────────────────────────────────────────────────────────

public sealed class HelpTopic { public string Id { get; set; } = ""; public string Title { get; set; } = ""; public string Content { get; set; } = ""; public List<string> Tags { get; set; } = new(); }
public sealed class HelpIndex
{
    List<HelpTopic> _topics = new();
    public void Add(HelpTopic t) => _topics.Add(t);
    public void Remove(string id) => _topics.RemoveAll(t => t.Id == id);
    public List<HelpTopic> Search(string query) => string.IsNullOrWhiteSpace(query) ? _topics.ToList() : _topics.Where(t => t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
    public HelpTopic? Get(string id) => _topics.FirstOrDefault(t => t.Id == id);
}

// ── HelpRegistry ──────────────────────────────────────────────────────────

public sealed class HelpRegistry
{
    Dictionary<string, List<HelpTopic>> _sections = new();
    public void Register(string section, HelpTopic topic) { if (!_sections.ContainsKey(section)) _sections[section] = new(); _sections[section].Add(topic); }
    public List<string> Sections => _sections.Keys.ToList();
    public List<HelpTopic> GetSection(string s) => _sections.GetValueOrDefault(s) ?? new();
    public HelpTopic? Find(string id) { foreach (var (_, topics) in _sections) foreach (var t in topics) if (t.Id == id) return t; return null; }
}
