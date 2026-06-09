// SPDX-License-Identifier: Apache-2.0
// Port of command_palette/mod.rs (3271L) + scorer.rs (3709L)
// Command palette with fuzzy search, incremental scoring, match highlighting.

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ── Scorer types ──────────────────────────────────────────────────────────

public enum MatchType { Exact, Prefix, WordStart, Substring, Fuzzy }
public enum EvidenceKind { TitleExact, TitlePrefix, TitleWordStart, TitleSubstring, TitleFuzzy, TagExact, TagPrefix, DescExact, DescPrefix }

public sealed record MatchResult
{
    public double Score { get; init; }
    public MatchType Type { get; init; }
    public List<(EvidenceKind Kind, double Weight, double Evidence)> Evidence { get; init; } = new();
    public List<(int Start, int End)> MatchPositions { get; init; } = new();
    public int ActionIndex { get; init; }
}

public sealed class IncrementalStats
{
    public ulong Queries { get; set; } public ulong FullRescans { get; set; } public ulong IncrementalUpdates { get; set; }
}

public sealed class IncrementalScorer
{
    List<string> _titles = new(), _titlesLower = new(); List<List<int>> _wordStarts = new();
    List<(MatchResult, double)>? _cache; string? _prevQuery; IncrementalStats _stats = new();

    public void SetCorpus(List<string> titles, List<string> titlesLower, List<List<int>> wordStarts) { _titles = titles; _titlesLower = titlesLower; _wordStarts = wordStarts; _cache = null; _prevQuery = null; }
    public IncrementalStats Stats => _stats;

    public List<MatchResult> Score(string query, int maxResults = 20)
    {
        _stats.Queries++;
        if (_prevQuery != null && query.StartsWith(_prevQuery)) { _stats.IncrementalUpdates++; return FilterIncremental(query, maxResults); }
        _stats.FullRescans++;
        return FullScore(query, maxResults);
    }

    List<MatchResult> FullScore(string query, int max)
    {
        var results = new List<MatchResult>();
        for (int i = 0; i < _titles.Count; i++)
        {
            var mr = ScoreSingle(query, _titles[i], _titlesLower[i], _wordStarts[i], i);
            if (mr != null) results.Add(mr);
        }
        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (results.Count > max) results = results.Take(max).ToList();
        _cache = results.Select(r => (r, r.Score)).ToList();
        _prevQuery = query;
        return results;
    }

    List<MatchResult> FilterIncremental(string query, int max)
    {
        if (_cache == null) return FullScore(query, max);
        var results = new List<MatchResult>();
        foreach (var (cached, _) in _cache)
        {
            var mr = ScoreSingle(query, _titles[cached.ActionIndex], _titlesLower[cached.ActionIndex], _wordStarts[cached.ActionIndex], cached.ActionIndex);
            if (mr != null) results.Add(mr);
        }
        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (results.Count > max) results = results.Take(max).ToList();
        _prevQuery = query;
        return results;
    }

    static MatchResult? ScoreSingle(string query, string title, string titleLower, List<int> wordStarts, int idx)
    {
        if (string.IsNullOrEmpty(query)) return null;
        var ql = query.ToLowerInvariant();
        // Exact match
        if (titleLower == ql) return new() { Score = 100, Type = MatchType.Exact, ActionIndex = idx, MatchPositions = new() { (0, title.Length) } };
        // Prefix
        if (titleLower.StartsWith(ql)) return new() { Score = 80, Type = MatchType.Prefix, ActionIndex = idx, MatchPositions = new() { (0, ql.Length) } };
        // Word start
        foreach (var ws in wordStarts)
            if (ws + ql.Length <= titleLower.Length && titleLower.Substring(ws, ql.Length) == ql)
                return new() { Score = 60, Type = MatchType.WordStart, ActionIndex = idx, MatchPositions = new() { (ws, ws + ql.Length) } };
        // Substring
        int si = titleLower.IndexOf(ql, StringComparison.Ordinal);
        if (si >= 0) return new() { Score = 40, Type = MatchType.Substring, ActionIndex = idx, MatchPositions = new() { (si, si + ql.Length) } };
        // Fuzzy (simple char-by-char matching)
        if (IsFuzzyMatch(titleLower, ql)) return new() { Score = 20, Type = MatchType.Fuzzy, ActionIndex = idx };
        return null;
    }

    static bool IsFuzzyMatch(string text, string query)
    {
        int qi = 0;
        for (int i = 0; i < text.Length && qi < query.Length; i++)
            if (text[i] == query[qi]) qi++;
        return qi == query.Length;
    }
}

// ── ActionItem ────────────────────────────────────────────────────────────

public sealed class ActionItem
{
    public string Id { get; set; } = ""; public string Title { get; set; } = "";
    public string? Description { get; set; } public List<string> Tags { get; set; } = new();
    public string? Category { get; set; }

    public static ActionItem New(string id, string title) => new() { Id = id, Title = title };
    public ActionItem WithDescription(string d) { Description = d; return this; }
    public ActionItem WithTags(params string[] t) { Tags = t.ToList(); return this; }
    public ActionItem WithCategory(string c) { Category = c; return this; }
}

// ── PaletteAction / PaletteStyle ──────────────────────────────────────────

public abstract record PaletteAction { public sealed record Execute(string Id) : PaletteAction; public sealed record Dismiss : PaletteAction; }

public sealed class PaletteStyle
{
    public WidgetStyle Border { get; set; } = new(PackedRgba.Rgb(100, 100, 120), null, null);
    public WidgetStyle Input { get; set; } = new(PackedRgba.Rgb(220, 220, 230), null, null);
    public WidgetStyle Item { get; set; } = new(PackedRgba.Rgb(190, 190, 200), null, null);
    public WidgetStyle ItemSelected { get; set; } = new(PackedRgba.Rgb(255, 255, 255), PackedRgba.Rgb(50, 50, 75), null);
    public WidgetStyle MatchHighlight { get; set; } = new(PackedRgba.Rgb(255, 210, 60), null, null);
    public WidgetStyle Description { get; set; } = new(PackedRgba.Rgb(140, 140, 160), null, null);
    public WidgetStyle Category { get; set; } = new(PackedRgba.Rgb(100, 180, 255), null, null);
    public WidgetStyle Hint { get; set; } = new(PackedRgba.Rgb(100, 100, 120), null, null);

    public static PaletteStyle Default => new();
}

public enum MatchFilter { All, Exact, Prefix, WordStart, Substring, Fuzzy }

// ── CommandPalette ────────────────────────────────────────────────────────

public sealed class CommandPalette : IWidget
{
    List<ActionItem> _actions = new(); List<string> _titles = new(), _titlesLower = new();
    List<List<int>> _wordStarts = new(); IncrementalScorer _scorer = new();
    string _query = ""; int _cursor, _selected, _scrollOffset; bool _visible;
    PaletteStyle _style = PaletteStyle.Default; MatchFilter _matchFilter = MatchFilter.All;
    int _maxVisible = 10; string _title = " Command Palette "; bool _fillArea;

    public CommandPalette Style(PaletteStyle s) { _style = s; return this; }
    public CommandPalette MaxVisible(int m) { _maxVisible = m; return this; }
    public CommandPalette FillArea(bool f) { _fillArea = f; return this; }
    public CommandPalette Title(string t) { _title = t; return this; }
    public bool Visible => _visible;

    public void Register(string id, string title, string? desc = null, string[]? tags = null)
    {
        var action = ActionItem.New(id, title);
        if (desc != null) action.WithDescription(desc);
        if (tags != null) action.WithTags(tags);
        RegisterAction(action);
    }

    public void RegisterAction(ActionItem action)
    {
        _actions.Add(action); _titles.Add(action.Title); _titlesLower.Add(action.Title.ToLowerInvariant());
        var ws = new List<int>(); ws.Add(0);
        for (int i = 0; i < action.Title.Length; i++) if (i > 0 && action.Title[i - 1] == ' ') ws.Add(i);
        _wordStarts.Add(ws);
        _scorer.SetCorpus(_titles, _titlesLower, _wordStarts);
    }

    public void Open() { _visible = true; _query = ""; _cursor = 0; _selected = 0; _scrollOffset = 0; }
    public void Close() { _visible = false; }

    public PaletteAction? HandleEvent(char c, KeyCode code, bool ctrl)
    {
        if (!_visible) return null;
        if (code is KeyCode.Escape) { Close(); return new PaletteAction.Dismiss(); }
        if (code is KeyCode.Enter) { Close(); return _scorer.Score(_query, _maxVisible).Count > _selected ? new PaletteAction.Execute(_actions[_selected].Id) : null; }
        if (code is KeyCode.Up && _selected > 0) { _selected--; return null; }
        if (code is KeyCode.Down && _selected < Math.Min(_maxVisible, _scorer.Score(_query, _maxVisible).Count) - 1) { _selected++; return null; }
        if (code is KeyCode.Backspace && _query.Length > 0) { _query = _query.Remove(--_cursor, 1); return null; }
        if (code is KeyCode.Left && _cursor > 0) { _cursor--; return null; }
        if (code is KeyCode.Right && _cursor < _query.Length) { _cursor++; return null; }
        if (code is KeyCode.Home) { _cursor = 0; return null; }
        if (code is KeyCode.End) { _cursor = _query.Length; return null; }
        if (!char.IsControl(c)) { _query = _query.Insert(_cursor++, c.ToString()); return null; }
        return null;
    }

    public void Render(Rect area, Frame frame)
    {
        if (!_visible || area.Width < 10 || area.Height < 3) return;
        var deg = frame.Degradation;
        var results = _scorer.Score(_query, _maxVisible);
        ushort pw = (ushort)Math.Min(50, area.Width - 4);
        ushort px = (ushort)(area.X + (area.Width - pw) / 2);
        ushort py = (ushort)(area.Y + Math.Min(3, (area.Height - Math.Min(results.Count + 3, area.Height)) / 2));
        ushort ph = (ushort)Math.Min((ushort)(results.Count + 3), area.Height);

        // Border and background
        var block = Block.New().Borders_(Borders.All).BorderType(BorderType.Rounded).Title(_title);
        block.Render(new Rect(px, py, pw, ph), frame);

        // Query input
        ushort qx = (ushort)(px + 1); ushort qy = (ushort)(py + 1);
        string displayQuery = _query.Length == 0 ? "Type to search..." : _query;
        WidgetDrawing.DrawTextSpan(frame, qx, qy, displayQuery, _query.Length == 0 ? _style.Hint : _style.Input, (ushort)(px + pw - 1));

        // Results
        _selected = Math.Min(_selected, Math.Max(0, results.Count - 1));
        for (int i = 0; i < results.Count && (i + 2) < ph - 1; i++)
        {
            var r = results[i];
            var action = r.ActionIndex < _actions.Count ? _actions[r.ActionIndex] : null;
            if (action == null) continue;
            ushort ry = (ushort)(qy + 1 + i);
            var s = i == _selected ? _style.ItemSelected : _style.Item;
            string line = action.Title;
            WidgetDrawing.DrawTextSpan(frame, (ushort)(qx + 2), ry, line, s, (ushort)(px + pw - 1));
            // Match highlights
            foreach (var (ms, me) in r.MatchPositions)
                if (ms < line.Length)
                    WidgetDrawing.DrawTextSpan(frame, (ushort)(qx + 2 + ms), ry,
                        line.Substring(ms, Math.Min(me, line.Length) - ms), _style.MatchHighlight, (ushort)(px + pw - 1));
            if (action.Category is { } cat)
                WidgetDrawing.DrawTextSpan(frame, qx, ry, $"[{cat}]", _style.Category, (ushort)(px + pw - 1));
        }
    }

    public bool IsEssential() => false;
}
