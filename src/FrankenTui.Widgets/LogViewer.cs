// Port of .external/frankentui/crates/ftui-widgets/src/log_viewer.rs
// Scrolling log viewer with wrap, filter, search, and Virtualized storage.

// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// ── type alias ───────────────────────────────────────────────────────────────
// Upstream: type Text = FtuiText<'static>;  — here TextContent plays that role.
// TextContent wraps styled lines/spans; plain strings are converted via TextContent.Raw.

// ── LogWrapMode ──────────────────────────────────────────────────────────────

/// <summary>Line wrapping mode for log lines.</summary>
public enum LogWrapMode
{
    /// <summary>No wrapping, truncate long lines.</summary>
    NoWrap,
    /// <summary>Wrap at any character boundary.</summary>
    CharWrap,
    /// <summary>Wrap at word boundaries (Unicode-aware).</summary>
    WordWrap,
}

// ── SearchMode ───────────────────────────────────────────────────────────────

/// <summary>Search mode for log search.</summary>
public enum SearchMode
{
    /// <summary>Plain substring matching.</summary>
    Literal,
    /// <summary>
    /// Regular expression matching.
    /// DIVERGENCE: The upstream crate gates this behind a "regex-search" feature flag.
    /// In .NET we always include the Regex variant but the search falls back to literal
    /// when no regex engine is wired up (i.e. the enum value exists, Regex matching is
    /// attempted via <see cref="System.Text.RegularExpressions.Regex"/>).
    /// </summary>
    Regex,
}

// ── SearchConfig ─────────────────────────────────────────────────────────────

/// <summary>Search configuration.</summary>
public sealed class SearchConfig
{
    /// <summary>Search mode (literal or regex).</summary>
    public SearchMode Mode { get; set; } = SearchMode.Literal;
    /// <summary>Whether the search is case-sensitive.</summary>
    public bool CaseSensitive { get; set; } = true;
    /// <summary>Number of context lines around matches (0 = only matching lines).</summary>
    public int ContextLines { get; set; } = 0;

    /// <summary>Return a default <see cref="SearchConfig"/> (literal, case-sensitive, no context).</summary>
    public static SearchConfig Default() => new SearchConfig();
}

// ── SearchState ──────────────────────────────────────────────────────────────

/// <summary>Search state for text search within the log.</summary>
sealed class SearchState
{
    /// <summary>The search query string (retained for re-search after eviction).</summary>
    public string Query = "";
    /// <summary>Lowercase query for case-insensitive search optimization.</summary>
    public string? QueryLower;
    /// <summary>Current search configuration.</summary>
    public SearchConfig Config = new();
    /// <summary>Indices of matching lines.</summary>
    public List<int> Matches = new();
    /// <summary>Current match index within the matches vector.</summary>
    public int Current;
    /// <summary>Per-match-line byte ranges for highlighting. Indexed by position in <see cref="Matches"/>.</summary>
    public List<List<(int Start, int End)>> HighlightRanges = new();
    /// <summary>
    /// Compiled regex pattern.
    /// DIVERGENCE: upstream uses an optional compiled_regex behind a feature flag;
    /// here we compile on demand from <see cref="Query"/> and <see cref="Config"/>.
    /// </summary>
    public System.Text.RegularExpressions.Regex? CompiledRegex;
    /// <summary>
    /// Indices including context lines around matches (sorted, deduped).
    /// <c>null</c> when <see cref="SearchConfig.ContextLines"/> == 0.
    /// </summary>
    public List<int>? ContextExpanded;
}

// ── FilterStats ──────────────────────────────────────────────────────────────

/// <summary>
/// Statistics tracking incremental vs full-rescan filter/search operations.
///
/// Useful for monitoring the efficiency of the streaming update path.
/// Reset with <see cref="Reset"/>.
/// </summary>
public sealed class FilterStats
{
    /// <summary>Lines checked incrementally (O(1) per push).</summary>
    public ulong IncrementalChecks { get; set; }
    /// <summary>Lines that matched during incremental checks.</summary>
    public ulong IncrementalMatches { get; set; }
    /// <summary>Full rescans triggered (e.g., by <c>SetFilter</c> or <c>Search</c>).</summary>
    public ulong FullRescans { get; set; }
    /// <summary>Total lines scanned during full rescans.</summary>
    public ulong FullRescanLines { get; set; }
    /// <summary>Search matches added incrementally on push.</summary>
    public ulong IncrementalSearchMatches { get; set; }
    /// <summary>Lines checked incrementally for search matches.</summary>
    public ulong IncrementalSearchChecks { get; set; }

    /// <summary>Reset all counters to zero.</summary>
    public void Reset()
    {
        IncrementalChecks = 0;
        IncrementalMatches = 0;
        FullRescans = 0;
        FullRescanLines = 0;
        IncrementalSearchMatches = 0;
        IncrementalSearchChecks = 0;
    }
}

// ── LogViewerState ───────────────────────────────────────────────────────────

/// <summary>Separate state for StatefulWidget pattern.</summary>
public sealed class LogViewerState
{
    /// <summary>Viewport height from last render (for page up/down).</summary>
    public ushort LastViewportHeight { get; set; }
    /// <summary>Total visible line count from last render.</summary>
    public int LastVisibleLines { get; set; }
    /// <summary>Selected line index (for copy/selection features).</summary>
    public int? SelectedLine { get; set; }
}

// ── LogViewer ────────────────────────────────────────────────────────────────

/// <summary>
/// A scrolling log viewer optimized for streaming append-only content.
///
/// Internally uses <see cref="Virtualized{T}"/> for storage and scroll management,
/// adding capacity enforcement, wrapping, filtering, and search on top.
///
/// # Design Rationale
/// - Virtualized handles scroll offset, follow mode, momentum, page navigation
/// - LogViewer adds max_lines eviction (Virtualized has no built-in capacity limit)
/// - Separate scroll semantics: Virtualized uses "offset from top"; LogViewer
///   exposes "follow mode" (newest at bottom) as the default behavior
/// - wrap_mode configurable per-instance for different use cases
/// - Stateful widget pattern for scroll state preservation across renders
/// </summary>
public sealed class LogViewer : IStatefulWidget<LogViewerState>
{
    // DIVERGENCE: Upstream stores Virtualized<Text> where Text = FtuiText<'static>
    // (a rich styled text type). In the C# port TextContent plays that role for
    // styled content; the Virtualized<TextContent> is the storage unit.
    private Virtualized<TextContent> _virt;
    /// <summary>Maximum lines to retain (memory bound).</summary>
    private int _maxLines;
    /// <summary>Line wrapping mode.</summary>
    private LogWrapMode _wrapMode;
    /// <summary>Default style for lines.</summary>
    private WidgetStyle _style;
    /// <summary>Highlight style for selected/focused line.</summary>
    private WidgetStyle? _highlightStyle;
    /// <summary>Highlight style for search matches within a line.</summary>
    private WidgetStyle? _searchHighlightStyle;
    /// <summary>Active filter pattern (plain substring match).</summary>
    private string? _filter;
    /// <summary>Indices of lines matching the filter (null = show all).</summary>
    private List<int>? _filteredIndices;
    /// <summary>Scroll offset within the filtered set (top index of filtered list).</summary>
    private int _filteredScrollOffset;
    /// <summary>Active search state.</summary>
    private SearchState? _search;
    /// <summary>Incremental filter/search statistics.</summary>
    private FilterStats _filterStats;

    // Expose virt for test access (upstream tests access virt fields directly in pub(crate) tests).
    // DIVERGENCE: upstream uses pub(crate) field; here internal matches the intent.
    internal Virtualized<TextContent> Virt => _virt;

    // Expose filtered scroll offset for test assertions.
    internal int FilteredScrollOffset => _filteredScrollOffset;

    // Expose filtered indices for test assertions.
    internal List<int>? FilteredIndices => _filteredIndices;

    // Expose search state for test assertions.
    internal SearchState? SearchState_ => _search;

    /// <summary>
    /// Create a new LogViewer with specified max line capacity.
    ///
    /// # Arguments
    /// * <paramref name="maxLines"/> - Maximum lines to retain. When exceeded, oldest lines
    ///   are evicted. Recommend 10,000-100,000 for typical agent use cases.
    /// </summary>
    public LogViewer(int maxLines)
    {
        _virt = Virtualized<TextContent>.New(maxLines).WithFollow(true);
        _maxLines = maxLines;
        _wrapMode = LogWrapMode.NoWrap;
        _style = WidgetStyle.Default;
        _highlightStyle = null;
        _searchHighlightStyle = null;
        _filter = null;
        _filteredIndices = null;
        _filteredScrollOffset = 0;
        _search = null;
        _filterStats = new FilterStats();
    }

    /// <summary>Set the wrap mode.</summary>
    public LogViewer WithWrapMode(LogWrapMode mode)
    {
        _wrapMode = mode;
        return this;
    }

    /// <summary>Set the default style for lines.</summary>
    public LogViewer WithStyle(WidgetStyle style)
    {
        _style = style;
        return this;
    }

    /// <summary>Set the highlight style for selected lines.</summary>
    public LogViewer WithHighlightStyle(WidgetStyle style)
    {
        _highlightStyle = style;
        return this;
    }

    /// <summary>Set the highlight style for search matches within lines.</summary>
    public LogViewer WithSearchHighlightStyle(WidgetStyle style)
    {
        _searchHighlightStyle = style;
        return this;
    }

    /// <summary>Returns the total number of log lines.</summary>
    public int Len() => _virt.Len();

    /// <summary>Returns true if there are no log lines.</summary>
    public bool IsEmpty() => _virt.IsEmpty();

    /// <summary>
    /// Append a single log line.
    ///
    /// # Performance
    /// - O(1) amortized for append
    /// - O(1) for eviction when at capacity
    ///
    /// # Auto-scroll Behavior
    /// If follow mode is enabled, view stays at bottom after push.
    /// </summary>
    public void Push(TextContent text)
    {
        bool followFiltered = _filteredIndices != null &&
            IsFilteredAtBottom(_filteredIndices.Count, _virt.VisibleCount());

        // Split multi-line text into individual items for smooth scrolling.
        foreach (var line in text.Lines)
        {
            var item = TextContent.FromLines(new[] { line });
            string plain = GetPlainText(item);

            // Incremental filter check: test new line against active filter.
            bool filterMatched = false;
            if (_filter != null)
            {
                _filterStats.IncrementalChecks++;
                filterMatched = plain.Contains(_filter, StringComparison.Ordinal);
                if (filterMatched)
                {
                    _filteredIndices?.Add(_virt.Len());
                    _filterStats.IncrementalMatches++;
                }
            }

            // Incremental search check: test new line against active search query.
            // Only add to search matches if (a) there is no filter or (b) the
            // line passed the filter, because search results respect the filter.
            if (_search != null)
            {
                bool shouldCheck = _filter == null || filterMatched;
                if (shouldCheck)
                {
                    _filterStats.IncrementalSearchChecks++;
                    var ranges = FindMatchRanges(plain, _search.Query, _search.QueryLower, _search.Config, _search.CompiledRegex);
                    if (ranges.Count > 0)
                    {
                        int idx = _virt.Len();
                        _search.Matches.Add(idx);
                        _search.HighlightRanges.Add(ranges);
                        _filterStats.IncrementalSearchMatches++;
                    }
                }
            }

            _virt.Push(item);

            // Enforce capacity
            if (_virt.Len() > _maxLines)
            {
                int removed = _virt.TrimFront(_maxLines);

                // Adjust filtered indices
                if (_filteredIndices != null)
                {
                    int filteredRemoved = 0;
                    for (int i = _filteredIndices.Count - 1; i >= 0; i--)
                    {
                        if (_filteredIndices[i] < removed)
                        {
                            filteredRemoved++;
                            _filteredIndices.RemoveAt(i);
                        }
                        else
                        {
                            _filteredIndices[i] -= removed;
                        }
                    }
                    if (filteredRemoved > 0)
                    {
                        _filteredScrollOffset = Math.Max(0, _filteredScrollOffset - filteredRemoved);
                    }
                    if (_filteredIndices.Count == 0)
                    {
                        _filteredScrollOffset = 0;
                    }
                }

                // Adjust search match indices and corresponding highlight_ranges
                if (_search != null)
                {
                    var keep = new List<int>(_search.Matches.Count);
                    var newHighlights = new List<List<(int, int)>>(_search.HighlightRanges.Count);
                    int evictedMatches = 0;
                    for (int i = 0; i < _search.Matches.Count; i++)
                    {
                        int midx = _search.Matches[i];
                        if (midx < removed)
                        {
                            evictedMatches++;
                        }
                        else
                        {
                            keep.Add(midx - removed);
                            if (i < _search.HighlightRanges.Count)
                                newHighlights.Add(_search.HighlightRanges[i]);
                        }
                    }
                    _search.Matches = keep;
                    _search.HighlightRanges = newHighlights;
                    _search.Current = Math.Max(0, _search.Current - evictedMatches);
                    // Clamp current to valid range
                    if (_search.Matches.Count > 0)
                        _search.Current = Math.Min(_search.Current, _search.Matches.Count - 1);
                    else
                        _search.Current = 0;
                    // Recompute context expansion if needed
                    if (_search.Config.ContextLines > 0)
                        _search.ContextExpanded = ExpandContext(_search.Matches, _search.Config.ContextLines, _virt.Len());
                }
            }

            if (followFiltered && _filteredIndices != null && _filteredIndices.Count > 0)
            {
                _filteredScrollOffset = _filteredIndices.Count - 1;
            }
        }
    }

    /// <summary>Push a plain-text log line.</summary>
    public void Push(string line) => Push(TextContent.Raw(line));

    /// <summary>Append multiple lines efficiently.</summary>
    public void PushMany(IEnumerable<TextContent> lines)
    {
        foreach (var line in lines)
            Push(line);
    }

    /// <summary>Append multiple plain-text lines efficiently.</summary>
    public void PushMany(IEnumerable<string> lines)
    {
        foreach (var line in lines)
            Push(line);
    }

    /// <summary>Scroll up by N lines. Disables follow mode.</summary>
    public void ScrollUp(int lines)
    {
        if (_filteredIndices != null)
        {
            // Clamp usize::MAX sentinel (set by scroll_to_bottom when viewport unknown)
            int filteredTotal = _filteredIndices.Count;
            int max = Math.Max(0, filteredTotal - 1);
            if (_filteredScrollOffset > max)
                _filteredScrollOffset = max;
            _filteredScrollOffset = Math.Max(0, _filteredScrollOffset - lines);
            _virt.SetFollow(false);
        }
        else
        {
            int delta = lines > int.MaxValue ? int.MaxValue : (int)lines;
            _virt.Scroll(-delta);
        }
    }

    /// <summary>Scroll down by N lines. Re-enables follow mode if at bottom.</summary>
    public void ScrollDown(int lines)
    {
        if (_filteredIndices != null)
        {
            int filteredTotal = _filteredIndices.Count;
            if (filteredTotal == 0)
            {
                _filteredScrollOffset = 0;
            }
            else
            {
                // Clamp sentinel before arithmetic
                int visibleCount = _virt.VisibleCount();
                int maxOffset = Math.Max(0, filteredTotal - Math.Max(1, visibleCount));
                if (_filteredScrollOffset > maxOffset)
                    _filteredScrollOffset = maxOffset;
                _filteredScrollOffset = Math.Min(_filteredScrollOffset + lines, maxOffset);
            }
            // Re-enable follow if we scrolled to the bottom of the filtered set.
            int vc = _virt.VisibleCount();
            if (IsFilteredAtBottom(_filteredIndices.Count, vc))
                _virt.SetFollow(true);
        }
        else
        {
            int delta = lines > int.MaxValue ? int.MaxValue : (int)lines;
            _virt.Scroll(delta);
            if (_virt.IsAtBottom())
                _virt.SetFollow(true);
        }
    }

    /// <summary>Jump to top of log history. Disables follow mode.</summary>
    public void ScrollToTop()
    {
        if (_filteredIndices != null)
        {
            _filteredScrollOffset = 0;
            _virt.SetFollow(false);
        }
        else
        {
            _virt.ScrollToTop();
        }
    }

    /// <summary>Jump to bottom and re-enable follow mode.</summary>
    public void ScrollToBottom()
    {
        if (_filteredIndices != null)
        {
            int filteredTotal = _filteredIndices.Count;
            if (filteredTotal == 0)
            {
                _filteredScrollOffset = 0;
            }
            else
            {
                int visibleCount = _virt.VisibleCount();
                if (visibleCount == 0)
                {
                    // Viewport unknown; keep sentinel and clamp during render.
                    _filteredScrollOffset = int.MaxValue;
                }
                else
                {
                    _filteredScrollOffset = Math.Max(0, filteredTotal - visibleCount);
                }
            }
            _virt.SetFollow(true);
        }
        else
        {
            _virt.ScrollToEnd();
        }
    }

    /// <summary>
    /// Page up (scroll by viewport height).
    ///
    /// Uses the visible count tracked by the Virtualized container.
    /// The <paramref name="state"/> parameter is accepted for API compatibility.
    /// </summary>
    public void PageUp(LogViewerState state)
    {
        if (_filteredIndices != null)
        {
            int lines = state.LastViewportHeight;
            if (lines > 0)
                ScrollUp(lines);
        }
        else
        {
            _virt.PageUp();
        }
    }

    /// <summary>
    /// Page down (scroll by viewport height).
    ///
    /// Uses the visible count tracked by the Virtualized container.
    /// The <paramref name="state"/> parameter is accepted for API compatibility.
    /// </summary>
    public void PageDown(LogViewerState state)
    {
        if (_filteredIndices != null)
        {
            int lines = state.LastViewportHeight;
            if (lines > 0)
                ScrollDown(lines);
        }
        else
        {
            _virt.PageDown();
            if (_virt.IsAtBottom())
                _virt.SetFollow(true);
        }
    }

    /// <summary>
    /// Check if currently scrolled to the bottom.
    ///
    /// Returns <c>true</c> when follow mode is active (even before first render
    /// when the viewport size is unknown).
    /// </summary>
    public bool IsAtBottom()
    {
        if (_filteredIndices != null)
            return IsFilteredAtBottom(_filteredIndices.Count, _virt.VisibleCount());
        return _virt.FollowMode() || _virt.IsAtBottom();
    }

    /// <summary>Total line count in buffer.</summary>
    public int LineCount() => _virt.Len();

    /// <summary>Check if follow mode (auto-scroll) is enabled.</summary>
    public bool AutoScrollEnabled() => _virt.FollowMode();

    /// <summary>Set follow mode (auto-scroll) state.</summary>
    public void SetAutoScroll(bool enabled) => _virt.SetFollow(enabled);

    /// <summary>Toggle follow mode on/off.</summary>
    public void ToggleFollow()
    {
        bool current = _virt.FollowMode();
        _virt.SetFollow(!current);
    }

    /// <summary>Clear all lines.</summary>
    public void Clear()
    {
        _virt.Clear();
        _filteredIndices = _filter != null ? new List<int>() : null;
        _filteredScrollOffset = 0;
        _search = null;
        _filterStats.Reset();
    }

    /// <summary>
    /// Get a reference to the incremental filter/search statistics.
    ///
    /// Use this to monitor how often the streaming incremental path is used
    /// versus full rescans.
    /// </summary>
    public FilterStats FilterStats() => _filterStats;

    /// <summary>Get a mutable reference to the filter statistics (for resetting).</summary>
    public FilterStats FilterStatsMut() => _filterStats;

    /// <summary>
    /// Set a filter pattern (plain substring match).
    ///
    /// Only lines containing the pattern will be shown. Pass <c>null</c> to clear.
    /// </summary>
    public void SetFilter(string? pattern)
    {
        if (!string.IsNullOrEmpty(pattern))
        {
            // Full rescan: rebuild filtered indices from all lines.
            _filterStats.FullRescans++;
            _filterStats.FullRescanLines += (ulong)_virt.Len();
            var indices = new List<int>();
            for (int idx = 0; idx < _virt.Len(); idx++)
            {
                var item = _virt.Get(idx);
                if (item != null && GetPlainText(item).Contains(pattern, StringComparison.Ordinal))
                    indices.Add(idx);
            }
            _filter = pattern;
            _filteredIndices = indices;
            // Position filtered scroll at matching unfiltered position
            if (_filteredIndices.Count == 0)
            {
                _filteredScrollOffset = 0;
            }
            else if (_virt.FollowMode() || _virt.IsAtBottom())
            {
                _filteredScrollOffset = _filteredIndices.Count - 1;
            }
            else
            {
                int scrollOffset = _virt.ScrollOffset();
                _filteredScrollOffset = PartitionPoint(_filteredIndices, scrollOffset);
            }
            _search = null;
        }
        else
        {
            _filter = null;
            _filteredIndices = null;
            _filteredScrollOffset = 0;
            _search = null;
        }
    }

    /// <summary>
    /// Search for text and return match count.
    ///
    /// Convenience wrapper using default config (literal, case-sensitive, no context).
    /// Sets up search state for navigation with <see cref="NextMatch"/> / <see cref="PrevMatch"/>.
    /// </summary>
    public int Search(string query) => SearchWithConfig(query, SearchConfig.Default());

    /// <summary>
    /// Search with full configuration (mode, case sensitivity, context lines).
    ///
    /// Returns match count. Sets up state for <see cref="NextMatch"/> / <see cref="PrevMatch"/>.
    /// </summary>
    public int SearchWithConfig(string query, SearchConfig config)
    {
        if (string.IsNullOrEmpty(query))
        {
            _search = null;
            return 0;
        }

        // Compile regex if needed
        System.Text.RegularExpressions.Regex? compiledRegex = null;
        if (config.Mode == SearchMode.Regex)
        {
            compiledRegex = CompileRegex(query, config);
            if (compiledRegex == null)
            {
                // Invalid regex — clear search and return 0
                _search = null;
                return 0;
            }
        }

        // Pre-compute lowercase query for optimization
        string? queryLower = config.CaseSensitive ? null : query.ToLowerInvariant();

        // Full rescan for search matches.
        _filterStats.FullRescans++;
        var matches = new List<int>();
        var highlightRanges = new List<List<(int, int)>>();

        IEnumerable<int> iter;
        if (_filteredIndices != null)
        {
            _filterStats.FullRescanLines += (ulong)_filteredIndices.Count;
            iter = _filteredIndices;
        }
        else
        {
            _filterStats.FullRescanLines += (ulong)_virt.Len();
            iter = System.Linq.Enumerable.Range(0, _virt.Len());
        }

        foreach (int idx in iter)
        {
            var item = _virt.Get(idx);
            if (item != null)
            {
                string plain = GetPlainText(item);
                var ranges = FindMatchRanges(plain, query, queryLower, config, compiledRegex);
                if (ranges.Count > 0)
                {
                    matches.Add(idx);
                    highlightRanges.Add(ranges);
                }
            }
        }

        int count = matches.Count;

        List<int>? contextExpanded = config.ContextLines > 0
            ? ExpandContext(matches, config.ContextLines, _virt.Len())
            : null;

        _search = new SearchState
        {
            Query = query,
            QueryLower = queryLower,
            Config = config,
            Matches = matches,
            Current = 0,
            HighlightRanges = highlightRanges,
            CompiledRegex = compiledRegex,
            ContextExpanded = contextExpanded,
        };

        // Jump to first match
        if (_search.Matches.Count > 0)
            ScrollToMatch(_search.Matches[0]);

        return count;
    }

    /// <summary>Jump to next search match.</summary>
    public void NextMatch()
    {
        if (_search != null && _search.Matches.Count > 0)
        {
            _search.Current = (_search.Current + 1) % _search.Matches.Count;
            int idx = _search.Matches[_search.Current];
            ScrollToMatch(idx);
        }
    }

    /// <summary>Jump to previous search match.</summary>
    public void PrevMatch()
    {
        if (_search != null && _search.Matches.Count > 0)
        {
            _search.Current = _search.Current == 0
                ? _search.Matches.Count - 1
                : _search.Current - 1;
            int idx = _search.Matches[_search.Current];
            ScrollToMatch(idx);
        }
    }

    /// <summary>Clear active search.</summary>
    public void ClearSearch() => _search = null;

    /// <summary>Get current search match info: (current_match_1indexed, total_matches).</summary>
    public (int Current, int Total)? SearchInfo()
    {
        if (_search == null || _search.Matches.Count == 0)
            return null;
        return (_search.Current + 1, _search.Matches.Count);
    }

    /// <summary>
    /// Get the highlight byte ranges for a given line index, if any.
    ///
    /// Returns the ranges when the line is a search match.
    /// </summary>
    public IReadOnlyList<(int Start, int End)>? HighlightRangesForLine(int lineIdx)
    {
        if (_search == null)
            return null;
        int pos = _search.Matches.IndexOf(lineIdx);
        if (pos < 0)
            return null;
        return pos < _search.HighlightRanges.Count ? _search.HighlightRanges[pos] : null;
    }

    /// <summary>
    /// Get the context-expanded line indices, if context lines are configured.
    ///
    /// Returns <c>null</c> when no search is active or <c>context_lines == 0</c>.
    /// </summary>
    public IReadOnlyList<int>? ContextLineIndices() =>
        _search?.ContextExpanded;

    /// <summary>
    /// Returns the fraction of recent pushes that matched the active search.
    ///
    /// Useful for callers integrating with an <c>EProcessThrottle</c> to decide
    /// when to trigger a full UI refresh vs. deferring.
    /// Returns 0.0 when no search is active or no incremental checks occurred.
    /// </summary>
    public double SearchMatchRateHint()
    {
        if (_filterStats.IncrementalSearchChecks == 0)
            return 0.0;
        return (double)_filterStats.IncrementalSearchMatches / (double)_filterStats.IncrementalSearchChecks;
    }

    // ── StatefulWidget implementation ─────────────────────────────────────────

    /// <summary>Render the log viewer into the given frame area.</summary>
    public void Render(Rect area, Frame frame, LogViewerState state)
    {
        if (area.Width == 0 || area.Height == 0)
            return;

        WidgetDrawing.ClearTextArea(frame, area, _style);

        // Keep Virtualized's visible_count in sync even in filtered mode.
        _ = _virt.VisibleRange(area.Height);

        // Update state with current viewport info
        state.LastViewportHeight = area.Height;

        int totalLines = _virt.Len();
        if (totalLines == 0)
        {
            state.LastVisibleLines = 0;
            return;
        }

        // Use filtered indices if a filter is active
        IReadOnlyList<int>? renderIndices = _filteredIndices;

        // Calculate visible range using Virtualized's scroll state
        int visibleCount = area.Height;

        // Determine which lines to show
        int startIdx, endIdx;
        if (renderIndices != null)
        {
            // Filtered mode: show lines matching the filter
            int filteredTotal = renderIndices.Count;
            if (filteredTotal == 0)
            {
                state.LastVisibleLines = 0;
                return;
            }
            // Clamp scroll to filtered set
            int maxOffset = Math.Max(0, filteredTotal - visibleCount);
            int offset = Math.Min(_filteredScrollOffset, maxOffset);
            startIdx = offset;
            endIdx = Math.Min(offset + visibleCount, filteredTotal);
        }
        else
        {
            // Unfiltered mode: use Virtualized's range directly
            var range = _virt.VisibleRange(area.Height);
            startIdx = range.Start;
            endIdx = range.End;
        }

        ushort y = area.Y;
        int linesRendered = 0;
        int? lastRenderedIndex = null;

        for (int displayIdx = startIdx; displayIdx < endIdx; displayIdx++)
        {
            if (y >= area.Bottom)
                break;

            // Resolve to actual line index
            int lineIdx = renderIndices != null ? renderIndices[displayIdx] : displayIdx;

            var line = _virt.Get(lineIdx);
            if (line == null)
                continue;

            bool isSelected = state.SelectedLine == lineIdx;

            ushort linesUsed = RenderLine(line, lineIdx, area.X, y, area.Width, area.Bottom, frame, isSelected);

            y = (ushort)Math.Min(y + linesUsed, ushort.MaxValue);
            linesRendered++;
            lastRenderedIndex = displayIdx;
        }

        state.LastVisibleLines = linesRendered;

        // Correct visible count in Virtualized based on actual wrapped rendering
        _virt.SetVisibleCount(linesRendered);

        // Determine if we are truly at the bottom (rendered the last item)
        bool atBottom;
        if (renderIndices != null)
        {
            atBottom = lastRenderedIndex.HasValue && lastRenderedIndex.Value >= renderIndices.Count - 1;
        }
        else
        {
            atBottom = lastRenderedIndex.HasValue && lastRenderedIndex.Value >= totalLines - 1;
        }

        // Render scroll indicator if not at bottom
        if (!atBottom && area.Width >= 4)
        {
            int linesBelow = renderIndices != null
                ? renderIndices.Count - endIdx
                : totalLines - endIdx;
            string indicator = $" {linesBelow} ";
            int indicatorLen = DisplayWidth(indicator);
            if (indicatorLen < area.Width)
            {
                ushort indicatorX = (ushort)Math.Max(0, area.Right - indicatorLen);
                ushort indicatorY = (ushort)Math.Max(0, area.Bottom - 1);
                WidgetDrawing.DrawTextSpan(
                    frame,
                    indicatorX,
                    indicatorY,
                    indicator,
                    new WidgetStyle(null, null, CellStyleFlags.Bold),
                    area.Right);
            }
        }

        // Render search indicator if active
        var searchInfo = SearchInfo();
        if (searchInfo.HasValue && area.Width >= 10)
        {
            string searchIndicator = $" {searchInfo.Value.Current}/{searchInfo.Value.Total} ";
            int indLen = DisplayWidth(searchIndicator);
            if (indLen < area.Width)
            {
                ushort indX = area.X;
                ushort indY = (ushort)Math.Max(0, area.Bottom - 1);
                WidgetDrawing.DrawTextSpan(
                    frame,
                    indX,
                    indY,
                    searchIndicator,
                    new WidgetStyle(null, null, CellStyleFlags.Bold),
                    (ushort)Math.Min(indX + indLen, area.Right));
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Render a single line with optional wrapping and search highlighting.</summary>
    private ushort RenderLine(
        TextContent text,
        int lineIdx,
        ushort x,
        ushort y,
        ushort width,
        ushort maxY,
        Frame frame,
        bool isSelected)
    {
        WidgetStyle effectiveStyle = isSelected
            ? (_highlightStyle ?? _style)
            : _style;

        TextLine? firstLine = text.Lines.Length > 0 ? text.Lines[0] : (TextLine?)null;
        string content = GetPlainText(text);
        int contentWidth = DisplayWidth(content);
        var hlRanges = HighlightRangesForLine(lineIdx);

        switch (_wrapMode)
        {
            case LogWrapMode.NoWrap:
            {
                if (y < maxY)
                {
                    if (hlRanges != null && hlRanges.Count > 0)
                    {
                        DrawHighlightedLine(content, hlRanges, x, y, (ushort)(x + width), frame, effectiveStyle);
                    }
                    else
                    {
                        DrawTextLine(firstLine, content, x, y, (ushort)(x + width), frame, effectiveStyle);
                    }
                }
                return 1;
            }
            case LogWrapMode.CharWrap:
            case LogWrapMode.WordWrap:
            {
                if (contentWidth <= width)
                {
                    if (y < maxY)
                    {
                        if (hlRanges != null && hlRanges.Count > 0)
                        {
                            DrawHighlightedLine(content, hlRanges, x, y, (ushort)(x + width), frame, effectiveStyle);
                        }
                        else
                        {
                            DrawTextLine(firstLine, content, x, y, (ushort)(x + width), frame, effectiveStyle);
                        }
                    }
                    return 1;
                }
                else
                {
                    // DIVERGENCE: upstream delegates to ftui_text::wrap_with_options.
                    // In .NET we perform a simple grapheme-boundary char wrap.
                    var wrapped = WrapContent(content, width, _wrapMode);
                    ushort linesRendered = 0;
                    for (int i = 0; i < wrapped.Count; i++)
                    {
                        ushort lineY = (ushort)(y + i);
                        if (lineY >= maxY)
                            break;
                        WidgetDrawing.DrawTextSpan(frame, x, lineY, wrapped[i], effectiveStyle, (ushort)(x + width));
                        linesRendered++;
                    }
                    return (ushort)Math.Max(1, (int)linesRendered);
                }
            }
            default:
                return 1;
        }
    }

    /// <summary>Draw a line with search match highlights.</summary>
    private void DrawHighlightedLine(
        string content,
        IReadOnlyList<(int Start, int End)> ranges,
        ushort x,
        ushort y,
        ushort maxX,
        Frame frame,
        WidgetStyle baseStyle)
    {
        WidgetStyle hlStyle = _searchHighlightStyle
            ?? new WidgetStyle(null, null, CellStyleFlags.Bold | CellStyleFlags.Reverse);
        ushort cursorX = x;
        int pos = 0;

        foreach (var (start, end) in ranges)
        {
            int s = Math.Min(start, content.Length);
            int e = Math.Min(end, content.Length);
            if (s > pos)
            {
                // Draw non-highlighted segment
                cursorX = WidgetDrawing.DrawTextSpan(frame, cursorX, y, content[pos..s], baseStyle, maxX);
            }
            if (s < e)
            {
                // Draw highlighted segment
                cursorX = WidgetDrawing.DrawTextSpan(frame, cursorX, y, content[s..e], hlStyle, maxX);
            }
            pos = e;
        }
        // Draw trailing non-highlighted text
        if (pos < content.Length)
            WidgetDrawing.DrawTextSpan(frame, cursorX, y, content[pos..], baseStyle, maxX);
    }

    /// <summary>Draw a styled line using span information when available.</summary>
    private void DrawTextLine(
        TextLine? line,
        string fallback,
        ushort x,
        ushort y,
        ushort maxX,
        Frame frame,
        WidgetStyle baseStyle)
    {
        if (line.HasValue)
        {
            ushort cursorX = x;
            foreach (var span in line.Value.Spans)
            {
                if (cursorX >= maxX)
                    break;
                WidgetStyle spanStyle = span.Style.IsEmpty ? baseStyle : span.Style;
                cursorX = WidgetDrawing.DrawTextSpan(frame, cursorX, y, span.Content, spanStyle, maxX);
            }
        }
        else
        {
            WidgetDrawing.DrawTextSpan(frame, x, y, fallback, baseStyle, maxX);
        }
    }

    private void ScrollToMatch(int idx)
    {
        if (_filteredIndices != null)
        {
            int position = PartitionPoint(_filteredIndices, idx);
            _filteredScrollOffset = Math.Min(position, Math.Max(0, _filteredIndices.Count - 1));
        }
        else
        {
            _virt.ScrollTo(idx);
        }
    }

    private bool IsFilteredAtBottom(int total, int visibleCount)
    {
        if (visibleCount == 0)
            return false;
        if (total == 0)
            return true;
        return _filteredScrollOffset >= Math.Max(0, total - visibleCount);
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>Extract plain text string from a <see cref="TextContent"/>.</summary>
    private static string GetPlainText(TextContent item)
    {
        if (item.Lines.Length == 0)
            return "";
        if (item.Lines.Length == 1)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var span in item.Lines[0].Spans)
                sb.Append(span.Content);
            return sb.ToString();
        }
        var sb2 = new System.Text.StringBuilder();
        for (int i = 0; i < item.Lines.Length; i++)
        {
            if (i > 0) sb2.Append('\n');
            foreach (var span in item.Lines[i].Spans)
                sb2.Append(span.Content);
        }
        return sb2.ToString();
    }

    /// <summary>Display column width for a string (Unicode-aware).</summary>
    private static int DisplayWidth(string s)
    {
        int w = 0;
        var te = StringInfo.GetTextElementEnumerator(s);
        while (te.MoveNext())
            w += WidgetDrawing.GraphemeWidth(te.GetTextElement());
        return w;
    }

    /// <summary>
    /// Wrap content at the given width using char or word boundaries.
    /// DIVERGENCE: upstream delegates to <c>ftui_text::wrap_with_options</c>;
    /// here we implement a simple grapheme-column-boundary wrap.
    /// </summary>
    private static List<string> WrapContent(string content, int width, LogWrapMode mode)
    {
        var result = new List<string>();
        if (width <= 0)
        {
            result.Add(content);
            return result;
        }

        if (mode == LogWrapMode.WordWrap)
        {
            // Word wrap: break at spaces when possible.
            int col = 0;
            int lineStart = 0;
            int lastSpace = -1;
            int lastSpaceCol = 0;

            var elements = new List<(string g, int w, int byteIdx)>();
            var te = StringInfo.GetTextElementEnumerator(content);
            int byteOff = 0;
            while (te.MoveNext())
            {
                string g = te.GetTextElement();
                int gw = WidgetDrawing.GraphemeWidth(g);
                elements.Add((g, gw, byteOff));
                byteOff += System.Text.Encoding.UTF8.GetByteCount(g);
            }

            // Work character-index based
            int charIdx = 0;
            int lineStartChar = 0;
            int lastSpaceChar = -1;
            col = 0;
            foreach (var (g, gw, _) in elements)
            {
                if (g == " ") { lastSpaceChar = charIdx; lastSpaceCol = col; }
                if (col + gw > width)
                {
                    if (lastSpaceChar > lineStartChar)
                    {
                        // Break at last space
                        result.Add(BuildStringFromElements(elements, lineStartChar, lastSpaceChar));
                        lineStartChar = lastSpaceChar + 1; // skip the space
                        col = SumWidths(elements, lineStartChar, charIdx + 1);
                        lastSpaceChar = -1;
                    }
                    else
                    {
                        // No space found — char wrap fallback
                        result.Add(BuildStringFromElements(elements, lineStartChar, charIdx));
                        lineStartChar = charIdx;
                        col = gw;
                    }
                }
                else
                {
                    col += gw;
                }
                charIdx++;
            }
            if (lineStartChar < elements.Count)
                result.Add(BuildStringFromElements(elements, lineStartChar, elements.Count));
        }
        else
        {
            // Char wrap
            var sb = new System.Text.StringBuilder();
            int col2 = 0;
            var te2 = StringInfo.GetTextElementEnumerator(content);
            while (te2.MoveNext())
            {
                string g = te2.GetTextElement();
                int gw = WidgetDrawing.GraphemeWidth(g);
                if (col2 + gw > width && sb.Length > 0)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    col2 = 0;
                }
                sb.Append(g);
                col2 += gw;
            }
            if (sb.Length > 0)
                result.Add(sb.ToString());
        }

        if (result.Count == 0)
            result.Add("");
        return result;
    }

    private static string BuildStringFromElements(List<(string g, int w, int byteIdx)> elements, int fromChar, int toChar)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = fromChar; i < toChar && i < elements.Count; i++)
            sb.Append(elements[i].g);
        return sb.ToString();
    }

    private static int SumWidths(List<(string g, int w, int byteIdx)> elements, int fromChar, int toChar)
    {
        int total = 0;
        for (int i = fromChar; i < toChar && i < elements.Count; i++)
            total += elements[i].w;
        return total;
    }

    /// <summary>
    /// Find match byte ranges within a single line using the given config.
    /// </summary>
    private static List<(int Start, int End)> FindMatchRanges(
        string plain,
        string query,
        string? queryLower,
        SearchConfig config,
        System.Text.RegularExpressions.Regex? compiledRegex)
    {
        switch (config.Mode)
        {
            case SearchMode.Literal:
                if (config.CaseSensitive)
                    return SearchExact(plain, query);
                else if (queryLower != null)
                    return SearchAsciiCaseInsensitiveRanges(plain, queryLower);
                else
                    return SearchAsciiCaseInsensitiveRanges(plain, query.ToLowerInvariant());
            case SearchMode.Regex:
                if (compiledRegex != null)
                {
                    var result = new List<(int, int)>();
                    foreach (System.Text.RegularExpressions.Match m in compiledRegex.Matches(plain))
                        result.Add((m.Index, m.Index + m.Length));
                    return result;
                }
                return new List<(int, int)>();
            default:
                return new List<(int, int)>();
        }
    }

    /// <summary>
    /// Find all exact (case-sensitive) byte-offset match ranges for query in haystack.
    /// DIVERGENCE: upstream uses ftui_text::search::search_exact; here we use
    /// IndexOf in a loop, which returns char offsets (not byte offsets).
    /// For ASCII content char offsets == byte offsets. For non-ASCII multi-byte
    /// strings the offsets diverge, but since the ranges are used only for
    /// display highlighting (slicing the plain-text string), char offsets are
    /// correct for C# strings. The test for unicode fallback verifies this.
    /// </summary>
    internal static List<(int Start, int End)> SearchExact(string haystack, string needle)
    {
        var result = new List<(int, int)>();
        if (string.IsNullOrEmpty(needle))
            return result;
        int start = 0;
        while (true)
        {
            int idx = haystack.IndexOf(needle, start, StringComparison.Ordinal);
            if (idx < 0)
                break;
            result.Add((idx, idx + needle.Length));
            start = idx + needle.Length;
            if (start >= haystack.Length)
                break;
        }
        return result;
    }

    /// <summary>
    /// Find match ranges using allocation-free case-insensitive search.
    /// Fast path for ASCII content; falls back to culture-invariant for Unicode.
    /// </summary>
    internal static List<(int Start, int End)> SearchAsciiCaseInsensitiveRanges(string haystack, string needleLower)
    {
        var results = new List<(int, int)>();
        if (string.IsNullOrEmpty(needleLower))
            return results;

        // Fast ASCII path
        bool haystackAscii = IsAscii(haystack);
        bool needleAscii = IsAscii(needleLower);
        if (haystackAscii && needleAscii)
        {
            int needleLen = needleLower.Length;
            if (needleLen > haystack.Length)
                return results;

            const int MaxWork = 4096;
            if (haystack.Length * needleLen <= MaxWork)
            {
                int i = 0;
                while (i <= haystack.Length - needleLen)
                {
                    bool match = true;
                    for (int j = 0; j < needleLen; j++)
                    {
                        if (char.ToLowerInvariant(haystack[i + j]) != needleLower[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        results.Add((i, i + needleLen));
                        i += needleLen;
                    }
                    else
                    {
                        i++;
                    }
                }
                return results;
            }
        }

        // Fallback: use InvariantCultureIgnoreCase IndexOf loop
        int start = 0;
        string needleOriginalCase = needleLower; // already lower for our purposes
        while (true)
        {
            int idx = CultureInfo.InvariantCulture.CompareInfo.IndexOf(
                haystack, needleOriginalCase, start, CompareOptions.IgnoreCase);
            if (idx < 0)
                break;
            results.Add((idx, idx + needleOriginalCase.Length));
            start = idx + needleOriginalCase.Length;
            if (start >= haystack.Length)
                break;
        }
        return results;
    }

    private static bool IsAscii(string s)
    {
        foreach (char c in s)
            if (c > 127) return false;
        return true;
    }

    /// <summary>Compile a regex from the query, respecting case sensitivity.</summary>
    private static System.Text.RegularExpressions.Regex? CompileRegex(string query, SearchConfig config)
    {
        try
        {
            var options = config.CaseSensitive
                ? System.Text.RegularExpressions.RegexOptions.None
                : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            return new System.Text.RegularExpressions.Regex(query, options, TimeSpan.FromSeconds(1));
        }
        catch (System.Text.RegularExpressions.RegexParseException)
        {
            return null;
        }
    }

    /// <summary>Expand match indices by ±N context lines, dedup and sort.</summary>
    private static List<int> ExpandContext(List<int> matches, int contextLines, int totalLines)
    {
        var expanded = new List<int>();
        foreach (int idx in matches)
        {
            int start = Math.Max(0, idx - contextLines);
            int end = Math.Min(idx + contextLines + 1, totalLines);
            for (int i = start; i < end; i++)
                expanded.Add(i);
        }
        expanded.Sort();
        // Dedup
        for (int i = expanded.Count - 1; i > 0; i--)
        {
            if (expanded[i] == expanded[i - 1])
                expanded.RemoveAt(i);
        }
        return expanded;
    }

    /// <summary>
    /// Returns the index of the first element >= value (like Rust's partition_point).
    /// </summary>
    private static int PartitionPoint(List<int> list, int value)
    {
        int lo = 0, hi = list.Count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (list[mid] < value) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }
}
