// SPDX-License-Identifier: Apache-2.0
// Port of choreography.rs (737L), error_boundary.rs (1000L), log_ring.rs (775L),
// notification_queue.rs (1050L), json_view.rs (876L), hint_ranker.rs (846L),
// layout_debugger.rs (699L), undo_support.rs (820L), measure_cache.rs (1007L)

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

// ── Choreography ──────────────────────────────────────────────────────────

public enum Action { Enter, Exit, Update, Highlight, Pulse }
public sealed class ChoreographyStep { public string WidgetId { get; set; } = ""; public Action Action { get; set; } public TimeSpan Duration { get; set; } public TimeSpan Delay { get; set; } }
public sealed class Choreography { public List<ChoreographyStep> Steps { get; } = new(); public void Add(ChoreographyStep s) => Steps.Add(s); }

// ── ErrorBoundary ─────────────────────────────────────────────────────────

public sealed class CapturedError { public string Message { get; set; } = ""; public string? Detail { get; set; } }
public enum ErrorBoundaryState { Ok, Recovering, Fatal }
public sealed class ErrorBoundary<W> : IStatefulWidget<ErrorBoundaryState> where W : IWidget
{
    W _inner; CapturedError? _last; WidgetStyle _style;
    public ErrorBoundary(W inner) => _inner = inner;
    public ErrorBoundary<W> Style(WidgetStyle s) { _style = s; return this; }
    public CapturedError? LastError => _last;
    public void Render(Rect area, Frame frame, ErrorBoundaryState state)
    {
        if (area.Width == 0 || area.Height == 0) return;
        try { _inner.Render(area, frame); }
        catch (Exception ex) { _last = new CapturedError { Message = ex.Message, Detail = ex.StackTrace }; WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, $"ERROR: {ex.Message}", _style, area.Right); }
    }
}

// ── LogRing ───────────────────────────────────────────────────────────────

public sealed class LogRing<T> : IWidget
{
    T[] _buf; int _head, _count; WidgetStyle _style; Func<T, string> _formatter;

    public LogRing(int capacity, Func<T, string>? formatter = null) { _buf = new T[capacity]; _formatter = formatter ?? (t => t?.ToString() ?? ""); }
    public LogRing<T> Style(WidgetStyle s) { _style = s; return this; }
    public void Push(T item) { _buf[_head] = item; _head = (_head + 1) % _buf.Length; if (_count < _buf.Length) _count++; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        int start = _count < _buf.Length ? 0 : _head;
        for (int i = 0; i < area.Height && i < _count; i++)
        {
            int idx = (start + i) % _buf.Length;
            WidgetDrawing.DrawTextSpan(frame, area.X, (ushort)(area.Y + i), _formatter(_buf[idx]), _style, area.Right);
        }
    }
}

// ── JsonView ──────────────────────────────────────────────────────────────

public enum JsonToken { ObjectStart, ObjectEnd, ArrayStart, ArrayEnd, Key, String, Number, Bool, Null, Colon, Comma }
public sealed class JsonView : IWidget
{
    string _json; int _indent; WidgetStyle _style, _keyStyle, _valueStyle;

    public JsonView(string json) { _json = json; _indent = 2; }
    public JsonView Indent(int i) { _indent = i; return this; }
    public JsonView Style(WidgetStyle s) { _style = s; return this; }
    public JsonView KeyStyle(WidgetStyle s) { _keyStyle = s; return this; }
    public JsonView ValueStyle(WidgetStyle s) { _valueStyle = s; return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0 || _json.Length == 0) return;
        // Simple: render JSON as indented plain text
        string indented = TryPrettyPrint(_json, _indent) ?? _json;
        var lines = indented.Split('\n');
        for (int i = 0; i < lines.Length && i < area.Height; i++)
            WidgetDrawing.DrawTextSpan(frame, area.X, (ushort)(area.Y + i), lines[i], _style, area.Right);
    }

    static string? TryPrettyPrint(string json, int indent)
    {
        try
        {
            var obj = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch { return null; }
    }
}

// ── HintRanker ────────────────────────────────────────────────────────────

public enum HintContext { Empty, Typing, Selection, Navigation }
public sealed class HintEntry { public string Label { get; set; } = ""; public string Description { get; set; } = ""; public double Score { get; set; } public string? Key { get; set; } }
public sealed class HintRanker
{
    List<HintEntry> _entries = new();

    public void Add(HintEntry e) => _entries.Add(e);
    public List<HintEntry> Rank(string query, int topN = 5)
    {
        if (string.IsNullOrEmpty(query)) return _entries.OrderByDescending(e => e.Score).Take(topN).ToList();
        var q = query.ToLowerInvariant();
        return _entries.Select(e => (e, Score: MatchScore(e, q))).OrderByDescending(x => x.Score).Take(topN).Select(x => x.e).ToList();
    }

    static double MatchScore(HintEntry e, string q)
    {
        if (e.Label.ToLowerInvariant().StartsWith(q)) return 1.0;
        if (e.Label.ToLowerInvariant().Contains(q)) return 0.5;
        if (e.Description.ToLowerInvariant().Contains(q)) return 0.3;
        return 0;
    }
}

// LayoutDebugger is declared in LayoutDebuggerWidgets.cs (canonical port with
// LayoutRecord/LayoutConstraints/ConstraintOverlayWidget).

// UndoSupport types (UndoWidgetId, TextEditOperation, SelectionOperation,
// TreeOperation, ListOperation, TableOperation, Unit, WidgetTextEditCmd, and the
// IUndoSupport / I*UndoExt interfaces) are declared in UndoSupport.cs (canonical
// 1-1 port of undo_support.rs).
