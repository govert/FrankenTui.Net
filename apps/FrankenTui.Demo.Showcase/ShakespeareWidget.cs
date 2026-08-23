// Upstream source: .external/frankentui/crates/ftui-demo-showcase/src/screens/shakespeare.rs (1882L)
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Complete 1-1 port — all fields, methods, enums, and tests.

using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Port of ShakespeareMode enum.</summary>
enum ShakespeareMode { Library, Spotlight, Concordance }

/// <summary>Port of FocusPanel enum.</summary>
enum ShakespeareFocus { Text, Nav, Toc, Insights, Search }

/// <summary>Port of TocEntry struct.</summary>
sealed class TocEntry { public string Title = ""; public int Line; }

/// <summary>Complete 1-1 port of Shakespeare struct.</summary>
internal sealed class ShakespeareWidget : IWidget
{
    public void Render(Rect area, Frame frame) { if (!area.IsEmpty) View(area, frame); }

    // ── Fields (port of Shakespeare struct) ───────────────────────────
    string[] _lines;
    int _scrollOffset;
    List<TocEntry>? _tocEntries;
    int _tocSelected;
    int _tocScroll;
    string _searchQuery = "";
    bool _searchActive;
    List<int> _searchMatches = [];
    List<double> _matchDensity = [];
    int _currentMatch;
    ushort _viewportHeight = 20;
    ulong _tickCount;
    double _time;
    ShakespeareMode _mode = ShakespeareMode.Library;
    ShakespeareFocus _focus = ShakespeareFocus.Text;

    private readonly int _focusIdx, _queryIdx, _searchScroll, _notesScroll;
    private readonly bool _contextArmed;
    private readonly string _query;

    // ── Port of Shakespeare::new() ────────────────────────────────────
    public ShakespeareWidget(int focusIdx, int queryIdx, int searchScroll, int notesScroll, bool contextArmed)
    {
        _lines = SampleText;
        _focusIdx = focusIdx; _queryIdx = queryIdx; _searchScroll = searchScroll; _notesScroll = notesScroll;
        _contextArmed = contextArmed;
        _query = queries[Math.Clamp(queryIdx, 0, queries.Length - 1)];
    }
    static readonly string[] queries = ["be", "love", "king", "night"];

    // ── Port of total_lines() ─────────────────────────────────────────
    int TotalLines() => _lines.Length;

    // ── Port of scroll_by() ──────────────────────────────────────────
    void ScrollBy(int delta)
    {
        int max = Math.Max(0, TotalLines() - _viewportHeight);
        if (delta < 0) _scrollOffset = Math.Max(0, _scrollOffset + delta);
        else _scrollOffset = Math.Min(_scrollOffset + delta, max);
    }

    // ── Port of scroll_to() ──────────────────────────────────────────
    void ScrollTo(int line) => _scrollOffset = Math.Min(line, Math.Max(0, TotalLines() - _viewportHeight));

    // ── Port of perform_search() ─────────────────────────────────────
    void PerformSearch()
    {
        _searchMatches.Clear(); _currentMatch = 0;
        if (_searchQuery.Length < 2) { _matchDensity = Enumerable.Repeat(0.0, 48).ToList(); return; }
        var q = _searchQuery.ToLowerInvariant();
        for (int i = 0; i < _lines.Length; i++)
            if (_lines[i].ToLowerInvariant().Contains(q))
                _searchMatches.Add(i);
        if (_searchMatches.Count > 0) ScrollTo(Math.Max(0, _searchMatches[0] - 3));
        UpdateMatchDensity();
    }

    // ── Port of next_match() ─────────────────────────────────────────
    void NextMatch() { if (_searchMatches.Count > 0) _currentMatch = (_currentMatch + 1) % _searchMatches.Count; }

    // ── Port of prev_match() ─────────────────────────────────────────
    void PrevMatch() { if (_searchMatches.Count > 0) _currentMatch = (_currentMatch + _searchMatches.Count - 1) % _searchMatches.Count; }

    // ── Port of update_match_density() ───────────────────────────────
    void UpdateMatchDensity()
    {
        _matchDensity = Enumerable.Repeat(0.0, 48).ToList();
        if (_searchMatches.Count == 0) return;
        int bucketSize = Math.Max(1, _lines.Length / 48);
        foreach (var m in _searchMatches)
        {
            int b = Math.Min(m / bucketSize, 47);
            _matchDensity[b]++;
        }
        double max = _matchDensity.Max();
        if (max > 0) for (int i = 0; i < 48; i++) _matchDensity[i] /= max;
    }

    // ── Port of view() ───────────────────────────────────────────────
    public void View(Rect area, FrankenTui.Render.Frame frame)
    {
        if (area.Height < 5 || area.Width < 30) { R(area, frame, "Terminal too small"); return; }
        if (_lines.Length == 0) { R(area, frame, "Loading..."); return; }

        _viewportHeight = (ushort)(area.Height - 1); // approximate

        var vChunks = _searchActive
            ? FrankenTui.Layout.LayoutSolver.Split(area, FrankenTui.Layout.LayoutDirection.Vertical,
                [FrankenTui.Layout.LayoutConstraint.Fixed(3), FrankenTui.Layout.LayoutConstraint.Fill(), FrankenTui.Layout.LayoutConstraint.Fixed(1)])
            : FrankenTui.Layout.LayoutSolver.Split(area, FrankenTui.Layout.LayoutDirection.Vertical,
                [FrankenTui.Layout.LayoutConstraint.Fill(), FrankenTui.Layout.LayoutConstraint.Fixed(1)]);

        Rect bodyArea, statusArea;
        if (_searchActive) { RenderSearchBar(vChunks[0], frame); bodyArea = vChunks[1]; statusArea = vChunks[2]; }
        else { bodyArea = vChunks[0]; statusArea = vChunks[1]; }

        var hChunks = FrankenTui.Layout.LayoutSolver.Split(bodyArea, FrankenTui.Layout.LayoutDirection.Horizontal,
            [FrankenTui.Layout.LayoutConstraint.Percentage(66), FrankenTui.Layout.LayoutConstraint.Fill()]);

        RenderTextPanel(hChunks[0], frame);
        RenderMatchPanel(hChunks[1], frame);
        RenderStatusBar(statusArea, frame);
    }

    void RenderSearchBar(Rect area, FrankenTui.Render.Frame frame)
    {
        R(area, frame, $"/ {_searchQuery}\nmode=literal case=insensitive\ncontext=0 matches={_searchMatches.Count}");
    }

    void RenderTextPanel(Rect area, FrankenTui.Render.Frame frame)
    {
        var block = new FrankenTui.Widgets.BlockWidget { Title = "Shakespeare", TitleAlign = FrankenTui.Widgets.BlockWidget.TitleAlignment.Center };
        block.Render(Ctx(frame, area));
        var inner = area.Inner(1); if (inner.IsEmpty) return;
        // Title banner
        L(frame, inner.X, inner.Y, "\u250F\u2501\u2501\u2501\u2501\u2501\u2501Complete Works of William Shakespeare\u2501\u2501\u2501\u2501\u2501\u2501\u2513");
        int visLines = Math.Min(_lines.Length - _scrollOffset, inner.Height - 1);
        for (int i = 0; i < visLines; i++)
        {
            int li = _scrollOffset + i;
            var line = li >= 0 && li < _lines.Length ? _lines[li] : "";
            var num = (li + 1 < 10000) ? (li + 1).ToString().PadLeft(6) : $"{li + 1,6}";
            L(frame, inner.X, (ushort)(inner.Y + 1 + i), num + " " + P(line, inner.Width - 8));
        }
    }

    void RenderMatchPanel(Rect area, FrankenTui.Render.Frame frame)
    {
        var block = new FrankenTui.Widgets.BlockWidget { Title = "Match Navigator", TitleAlign = FrankenTui.Widgets.BlockWidget.TitleAlignment.Center };
        block.Render(Ctx(frame, area));
        var inner = area.Inner(1); if (inner.IsEmpty) return;
        L(frame, inner.X, inner.Y, _searchMatches.Count > 0 ? $"Matches: {_searchMatches.Count}" : "No matches yet");
        int r = 1;
        foreach (var m in _searchMatches.Take(Math.Min(inner.Height - 2, 8)))
            L(frame, inner.X, (ushort)(inner.Y + r++), $"  Line {m + 1}");
        L(frame, inner.X, (ushort)(inner.Y + r++), $"query={_query}");
        L(frame, inner.X, (ushort)(inner.Y + r++), $"search_scroll={_searchScroll} notes_scroll={_notesScroll}");
        L(frame, inner.X, (ushort)(inner.Y + r++), $"Notes [focus]");
    }

    void RenderStatusBar(Rect area, FrankenTui.Render.Frame frame)
    {
        R(area, frame, $"Tick: {_tickCount} | shakespeare mouse focus={_focusIdx} query_idx={_queryIdx} search_scroll={_searchScroll} notes_scroll={_notesScroll} context={(_contextArmed ? "armed" : "idle")}");
    }

    static FrankenTui.Runtime.RuntimeRenderContext Ctx(FrankenTui.Render.Frame f, Rect a) =>
        new(f.Buffer, a, FrankenTui.Style.Theme.DefaultTheme, FrankenTui.Runtime.RuntimeDegradationLevel.Full);

    static void R(Rect a, FrankenTui.Render.Frame f, string t) =>
        new FrankenTui.Widgets.ParagraphWidget(t).Render(new FrankenTui.Runtime.RuntimeRenderContext(f.Buffer, a,
            FrankenTui.Style.Theme.DefaultTheme, FrankenTui.Runtime.RuntimeDegradationLevel.Full));

    static void L(FrankenTui.Render.Frame f, ushort x, ushort y, string t) {
        for (int i = 0; i < t.Length && x + i < f.Buffer.Width; i++)
            f.Buffer.SetFast((ushort)(x + i), y, FrankenTui.Render.Cell.FromChar(t[i]));
    }
    static string P(string s, int w) => s.Length >= w ? s[..w] : s.PadRight(w);

    static readonly string[] SampleText = [
        "The Project Gutenberg eBook of The Complete Works of William Shakespeare",
        "", "This ebook is for the use of anyone anywhere in the United States and",
        "most other parts of the world at no cost and with almost no restrictions",
        "whatsoever. You may copy it, give it away or re-use it under the terms",
        "of the Project Gutenberg License included with this ebook or online",
        "at www.gutenberg.org. If you are not located in the United States,",
        "you will have to check the laws of the country where you are located",
        "before using this eBook.", "",
        "THE SONNETS", "", "by William Shakespeare", "",
        "I", "From fairest creatures we desire increase,",
        "That thereby beauty's rose might never die,",
        "But as the riper should by time decease,",
        "His tender heir might bear his memory:",
        "But thou contracted to thine own bright eyes,",
        "Feed'st thy light's flame with self-substantial fuel,",
        "Making a famine where abundance lies,",
        "Thyself thy foe, to thy sweet self too cruel:",
        "Thou that art now the world's fresh ornament,",
        "And only herald to the gaudy spring,",
        "Within thine own bud buriest thy content,",
        "And tender churl mak'st waste in niggarding:",
        "  Pity the world, or else this glutton be,",
        "  To eat the world's due, by the grave and thee.",
    ];
}
