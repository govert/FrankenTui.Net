// Upstream source: crates/ftui-widgets/src/log_viewer.rs (tests module)
// Full 1-1 port of all upstream log_viewer tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Tests ported from the <c>#[cfg(test)] mod tests</c> block in log_viewer.rs.
/// </summary>
public class LogViewerTests
{
    // ── Helper: extract line text from a frame buffer row ─────────────────────

    /// <summary>
    /// Port of <c>fn line_text(frame: &Frame, y: u16, width: u16) -> String</c>.
    /// </summary>
    static string LineText(Frame frame, ushort y, ushort width)
    {
        var sb = new System.Text.StringBuilder(width);
        for (ushort x = 0; x < width; x++)
        {
            char ch = frame.Buffer.Get(x, y)?.Content.AsChar() ?? ' ';
            sb.Append(ch);
        }
        return sb.ToString();
    }

    // ── Basic push / capacity tests ────────────────────────────────────────────

    [Fact]
    public void TestPushAppendsToEnd()
    {
        var log = new LogViewer(100);
        log.Push("line 1");
        log.Push("line 2");
        Assert.Equal(2, log.LineCount());
    }

    [Fact]
    public void TestCircularBufferEviction()
    {
        var log = new LogViewer(3);
        log.Push("line 1");
        log.Push("line 2");
        log.Push("line 3");
        log.Push("line 4"); // Should evict "line 1"
        Assert.Equal(3, log.LineCount());
    }

    [Fact]
    public void TestAutoScrollStaysAtBottom()
    {
        var log = new LogViewer(100);
        log.Push("line 1");
        Assert.True(log.IsAtBottom());
        log.Push("line 2");
        Assert.True(log.IsAtBottom());
    }

    [Fact]
    public void TestManualScrollDisablesAutoScroll()
    {
        var log = new LogViewer(100);
        log.Virt.SetVisibleCount(10);
        for (int i = 0; i < 50; i++)
            log.Push($"line {i}");
        log.ScrollUp(10);
        Assert.False(log.AutoScrollEnabled());
        log.Push("new line");
        Assert.False(log.AutoScrollEnabled()); // Still scrolled up
    }

    [Fact]
    public void TestScrollToBottomReengagesAutoScroll()
    {
        var log = new LogViewer(100);
        log.Virt.SetVisibleCount(10);
        for (int i = 0; i < 50; i++)
            log.Push($"line {i}");
        log.ScrollUp(10);
        log.ScrollToBottom();
        Assert.True(log.IsAtBottom());
        Assert.True(log.AutoScrollEnabled());
    }

    [Fact]
    public void TestScrollDownReengagesAtBottom()
    {
        var log = new LogViewer(100);
        log.Virt.SetVisibleCount(10);
        for (int i = 0; i < 50; i++)
            log.Push($"line {i}");
        log.ScrollUp(5);
        Assert.False(log.AutoScrollEnabled());

        log.ScrollDown(5);
        if (log.IsAtBottom())
            Assert.True(log.AutoScrollEnabled());
    }

    [Fact]
    public void TestScrollToTop()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 50; i++)
            log.Push($"line {i}");
        log.ScrollToTop();
        Assert.False(log.AutoScrollEnabled());
    }

    [Fact]
    public void TestPageUpDown()
    {
        var log = new LogViewer(100);
        log.Virt.SetVisibleCount(10);
        for (int i = 0; i < 50; i++)
            log.Push($"line {i}");

        var state = new LogViewerState { LastViewportHeight = 10 };

        Assert.True(log.IsAtBottom());

        log.PageUp(state);
        Assert.False(log.IsAtBottom());

        log.PageDown(state);
        // After paging down from near-bottom, should be closer to bottom
    }

    [Fact]
    public void TestClear()
    {
        var log = new LogViewer(100);
        log.Push("line 1");
        log.Push("line 2");
        log.Clear();
        Assert.Equal(0, log.LineCount());
    }

    [Fact]
    public void TestPushMany()
    {
        var log = new LogViewer(100);
        log.PushMany(new[] { "line 1", "line 2", "line 3" });
        Assert.Equal(3, log.LineCount());
    }

    // ── Render tests ───────────────────────────────────────────────────────────

    [Fact]
    public void TestRenderEmpty()
    {
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        var log = new LogViewer(100);
        var state = new LogViewerState();

        log.Render(new Rect(0, 0, 80, 24), frame, state);

        Assert.Equal(0, state.LastVisibleLines);
    }

    [Fact]
    public void TestRenderEmptyClearsStaleContent()
    {
        var pool = new GraphemePool();
        var frame = new Frame(20, 3, pool);
        var log = new LogViewer(100);
        var state = new LogViewerState();
        var area = new Rect(0, 0, 20, 3);

        log.Push("first line");
        log.Push("second line");
        log.Render(area, frame, state);

        log.Clear();
        log.Render(area, frame, state);

        Assert.Equal(0, state.LastVisibleLines);
        Assert.Equal(new string(' ', 20), LineText(frame, 0, 20));
        Assert.Equal(new string(' ', 20), LineText(frame, 1, 20));
        Assert.Equal(new string(' ', 20), LineText(frame, 2, 20));
    }

    [Fact]
    public void TestRenderSomeLines()
    {
        var pool = new GraphemePool();
        var frame = new Frame(80, 10, pool);
        var log = new LogViewer(100);

        for (int i = 0; i < 5; i++)
            log.Push($"Line {i}");

        var state = new LogViewerState();
        log.Render(new Rect(0, 0, 80, 10), frame, state);

        Assert.Equal(10, state.LastViewportHeight);
        Assert.Equal(5, state.LastVisibleLines);
    }

    [Fact]
    public void TestToggleFollow()
    {
        var log = new LogViewer(100);
        Assert.True(log.AutoScrollEnabled());
        log.ToggleFollow();
        Assert.False(log.AutoScrollEnabled());
        log.ToggleFollow();
        Assert.True(log.AutoScrollEnabled());
    }

    // ── Filter tests ───────────────────────────────────────────────────────────

    [Fact]
    public void TestFilterShowsMatchingLines()
    {
        var log = new LogViewer(100);
        log.Push("INFO: starting");
        log.Push("ERROR: something failed");
        log.Push("INFO: processing");
        log.Push("ERROR: another failure");
        log.Push("INFO: done");

        log.SetFilter("ERROR");
        Assert.Equal(2, log.FilteredIndices!.Count);

        // Clear filter
        log.SetFilter(null);
        Assert.Null(log.FilteredIndices);
    }

    [Fact]
    public void TestRenderFilterNoMatchesClearsStaleContent()
    {
        var pool = new GraphemePool();
        var frame = new Frame(20, 3, pool);
        var log = new LogViewer(100);
        var state = new LogViewerState();
        var area = new Rect(0, 0, 20, 3);

        log.Push("INFO: starting");
        log.Push("ERROR: something failed");
        log.Render(area, frame, state);

        log.SetFilter("MISSING");
        log.Render(area, frame, state);

        Assert.Equal(0, state.LastVisibleLines);
        Assert.Equal(new string(' ', 20), LineText(frame, 0, 20));
        Assert.Equal(new string(' ', 20), LineText(frame, 1, 20));
        Assert.Equal(new string(' ', 20), LineText(frame, 2, 20));
    }

    // ── Search tests ───────────────────────────────────────────────────────────

    [Fact]
    public void TestSearchFindsMatches()
    {
        var log = new LogViewer(100);
        log.Push("hello world");
        log.Push("goodbye world");
        log.Push("hello again");

        int count = log.Search("hello");
        Assert.Equal(2, count);
        Assert.Equal((1, 2), log.SearchInfo());
    }

    [Fact]
    public void TestSearchRespectsFilter()
    {
        var log = new LogViewer(100);
        log.Push("INFO: ok");
        log.Push("ERROR: first");
        log.Push("WARN: mid");
        log.Push("ERROR: second");

        log.SetFilter("ERROR");
        Assert.Equal(0, log.Search("WARN"));
        Assert.Equal(2, log.Search("ERROR"));
    }

    [Fact]
    public void TestFilterClearsSearch()
    {
        var log = new LogViewer(100);
        log.Push("alpha");
        log.Search("alpha");
        Assert.NotNull(log.SearchInfo());

        log.SetFilter("alpha");
        Assert.Null(log.SearchInfo());
    }

    [Fact]
    public void TestSearchSetsFilteredScrollOffset()
    {
        var log = new LogViewer(100);
        log.Push("match one");
        log.Push("line two");
        log.Push("match three");
        log.Push("match four");

        log.SetFilter("match");
        log.Search("match");

        Assert.Equal(0, log.FilteredScrollOffset);
        log.NextMatch();
        Assert.Equal(1, log.FilteredScrollOffset);
    }

    [Fact]
    public void TestSearchNextPrev()
    {
        var log = new LogViewer(100);
        log.Push("match A");
        log.Push("nothing here");
        log.Push("match B");
        log.Push("match C");

        log.Search("match");
        Assert.Equal((1, 3), log.SearchInfo());

        log.NextMatch();
        Assert.Equal((2, 3), log.SearchInfo());

        log.NextMatch();
        Assert.Equal((3, 3), log.SearchInfo());

        log.NextMatch(); // wraps around
        Assert.Equal((1, 3), log.SearchInfo());

        log.PrevMatch(); // wraps back
        Assert.Equal((3, 3), log.SearchInfo());
    }

    [Fact]
    public void TestClearSearch()
    {
        var log = new LogViewer(100);
        log.Push("hello");
        log.Search("hello");
        Assert.NotNull(log.SearchInfo());

        log.ClearSearch();
        Assert.Null(log.SearchInfo());
    }

    [Fact]
    public void TestFilterWithPush()
    {
        var log = new LogViewer(100);
        log.SetFilter("ERROR");
        log.Push("INFO: ok");
        log.Push("ERROR: bad");
        log.Push("INFO: fine");

        Assert.Equal(1, log.FilteredIndices!.Count);
        Assert.Equal(1, log.FilteredIndices[0]);
    }

    [Fact]
    public void TestEvictionAdjustsFilterIndices()
    {
        var log = new LogViewer(3);
        log.SetFilter("x");
        log.Push("x1");
        log.Push("y2");
        log.Push("x3");
        // At capacity: indices [0, 2]
        Assert.Equal(new List<int> { 0, 2 }, log.FilteredIndices);

        log.Push("y4"); // evicts "x1", indices should adjust
        // After eviction of 1 item: "x3" was at 2, now at 1
        Assert.Equal(new List<int> { 1 }, log.FilteredIndices);
    }

    [Fact]
    public void TestFilterScrollOffsetTracksUnfilteredPosition()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 20; i++)
        {
            if (i == 2 || i == 10 || i == 15)
                log.Push($"match {i}");
            else
                log.Push($"line {i}");
        }

        log.Virt.ScrollTo(12);
        log.SetFilter("match");

        // Matches before index 12 are at 2 and 10 -> offset should be 2.
        Assert.Equal(2, log.FilteredScrollOffset);
    }

    [Fact]
    public void TestFilteredScrollDownMovesWithinFilteredList()
    {
        var log = new LogViewer(100);
        log.Push("match one");
        log.Push("line two");
        log.Push("match three");
        log.Push("line four");
        log.Push("match five");

        log.SetFilter("match");
        log.ScrollToTop();
        log.ScrollDown(1);

        Assert.Equal(1, log.FilteredScrollOffset);
    }

    // ── Incremental filter/search tests (bd-1b5h.11) ─────────────────────────

    [Fact]
    public void TestIncrementalFilterOnPushTracksStats()
    {
        var log = new LogViewer(100);
        log.SetFilter("ERROR");
        // set_filter triggers one full rescan (on empty log).
        Assert.Equal(1UL, log.FilterStats().FullRescans);

        log.Push("INFO: ok");
        log.Push("ERROR: bad");
        log.Push("INFO: fine");
        log.Push("ERROR: worse");

        // 4 lines pushed with filter active → 4 incremental checks.
        Assert.Equal(4UL, log.FilterStats().IncrementalChecks);
        // 2 matched.
        Assert.Equal(2UL, log.FilterStats().IncrementalMatches);
        // No additional full rescans.
        Assert.Equal(1UL, log.FilterStats().FullRescans);
    }

    [Fact]
    public void TestIncrementalSearchOnPush()
    {
        var log = new LogViewer(100);
        log.Push("hello world");
        log.Push("goodbye world");

        // Full search scan.
        int count = log.Search("hello");
        Assert.Equal(1, count);
        Assert.Equal(1UL, log.FilterStats().FullRescans);

        // Push new lines while search is active → incremental search update.
        log.Push("hello again");
        log.Push("nothing here");

        // Search matches should include the new "hello again" line.
        Assert.Equal((1, 2), log.SearchInfo());
        Assert.Equal(1UL, log.FilterStats().IncrementalSearchMatches);
    }

    [Fact]
    public void TestIncrementalSearchRespectsActiveFilter()
    {
        var log = new LogViewer(100);
        log.Push("ERROR: hello");
        log.Push("INFO: hello");

        log.SetFilter("ERROR");
        int count = log.Search("hello");
        Assert.Equal(1, count); // Only ERROR line passes filter.

        // Push new lines: only those passing filter should be search-matched.
        log.Push("ERROR: hello again");
        log.Push("INFO: hello again"); // Doesn't pass filter.

        Assert.Equal((1, 2), log.SearchInfo()); // Original + new ERROR.
        Assert.Equal(1UL, log.FilterStats().IncrementalSearchMatches);
    }

    [Fact]
    public void TestIncrementalSearchWithoutFilter()
    {
        var log = new LogViewer(100);
        log.Push("first");
        log.Search("match");
        Assert.Null(log.SearchInfo()); // No matches.

        // Push matching line without any filter active.
        log.Push("match found");
        Assert.Equal((1, 1), log.SearchInfo());
        Assert.Equal(1UL, log.FilterStats().IncrementalSearchMatches);
    }

    [Fact]
    public void TestFilterStatsResetOnClear()
    {
        var log = new LogViewer(100);
        log.SetFilter("x");
        log.Push("x1");
        log.Push("y2");

        Assert.True(log.FilterStats().IncrementalChecks > 0);
        log.Clear();
        Assert.Equal(0UL, log.FilterStats().IncrementalChecks);
        Assert.Equal(0UL, log.FilterStats().FullRescans);
    }

    [Fact]
    public void TestFilterStatsFullRescanOnFilterChange()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 100; i++)
            log.Push($"line {i}");

        log.SetFilter("line 5");
        Assert.Equal(1UL, log.FilterStats().FullRescans);
        Assert.Equal(100UL, log.FilterStats().FullRescanLines);

        log.SetFilter("line 9");
        Assert.Equal(2UL, log.FilterStats().FullRescans);
        Assert.Equal(200UL, log.FilterStats().FullRescanLines);
    }

    [Fact]
    public void TestFilterStatsManualReset()
    {
        var log = new LogViewer(100);
        log.SetFilter("x");
        log.Push("x1");
        Assert.True(log.FilterStats().IncrementalChecks > 0);

        log.FilterStatsMut().Reset();
        Assert.Equal(0UL, log.FilterStats().IncrementalChecks);

        // Subsequent pushes still tracked after reset.
        log.Push("x2");
        Assert.Equal(1UL, log.FilterStats().IncrementalChecks);
    }

    [Fact]
    public void TestIncrementalEvictionAdjustsSearchMatches()
    {
        var log = new LogViewer(3);
        log.Push("match A");
        log.Push("no");
        log.Push("match B");
        log.Search("match");
        Assert.Equal((1, 2), log.SearchInfo());

        // Push beyond capacity: evicts "match A".
        log.Push("match C"); // Incremental search match.

        // "match A" evicted. "match B" index adjusted. "match C" added.
        var search = log.SearchState_!;
        Assert.Equal(2, search.Matches.Count);
        // All search match indices should be valid (< log.LineCount()).
        foreach (int idx in search.Matches)
        {
            Assert.True(idx < log.LineCount(), $"Search index {idx} out of range");
        }
    }

    [Fact]
    public void TestNoStatsWhenNoFilterOrSearch()
    {
        var log = new LogViewer(100);
        log.Push("line 1");
        log.Push("line 2");

        Assert.Equal(0UL, log.FilterStats().IncrementalChecks);
        Assert.Equal(0UL, log.FilterStats().FullRescans);
        Assert.Equal(0UL, log.FilterStats().IncrementalSearchMatches);
    }

    [Fact]
    public void TestSearchFullRescanCountsLines()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 50; i++)
            log.Push($"line {i}");

        log.Search("line 1");
        Assert.Equal(1UL, log.FilterStats().FullRescans);
        Assert.Equal(50UL, log.FilterStats().FullRescanLines);
    }

    [Fact]
    public void TestSearchFullRescanOnFilteredCountsFilteredLines()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 50; i++)
        {
            if (i % 2 == 0)
                log.Push($"even {i}");
            else
                log.Push($"odd {i}");
        }

        log.SetFilter("even");
        ulong initialRescans = log.FilterStats().FullRescans;
        ulong initialLines = log.FilterStats().FullRescanLines;

        log.Search("even 4");
        Assert.Equal(initialRescans + 1, log.FilterStats().FullRescans);
        // Search scanned only filtered lines (25 even lines).
        Assert.Equal(initialLines + 25, log.FilterStats().FullRescanLines);
    }

    // ── Enhanced search tests (bd-1b5h.2) ─────────────────────────────────────

    [Fact]
    public void TestSearchLiteralCaseSensitive()
    {
        var log = new LogViewer(100);
        log.Push("Hello World");
        log.Push("hello world");
        log.Push("HELLO WORLD");

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = true, ContextLines = 0 };
        int count = log.SearchWithConfig("Hello", config);
        Assert.Equal(1, count);
        Assert.Equal((1, 1), log.SearchInfo());
    }

    [Fact]
    public void TestSearchLiteralCaseInsensitive()
    {
        var log = new LogViewer(100);
        log.Push("Hello World");
        log.Push("hello world");
        log.Push("HELLO WORLD");
        log.Push("no match here");

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = false, ContextLines = 0 };
        int count = log.SearchWithConfig("hello", config);
        Assert.Equal(3, count);
    }

    [Fact]
    public void TestSearchAsciiCaseInsensitiveFastPathRanges()
    {
        var log = new LogViewer(100);
        log.Push("Alpha beta ALPHA beta alpha");

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = false, ContextLines = 0 };
        int count = log.SearchWithConfig("alpha", config);
        Assert.Equal(1, count);

        var ranges = log.HighlightRangesForLine(0);
        Assert.NotNull(ranges);
        Assert.Equal(new (int, int)[] { (0, 5), (11, 16), (22, 27) }, ranges);
    }

    [Fact]
    public void TestSearchUnicodeFallbackRanges()
    {
        var log = new LogViewer(100);
        string line = "café résumé café";
        log.Push(line);

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = false, ContextLines = 0 };
        int count = log.SearchWithConfig("café", config);
        Assert.Equal(1, count);

        // Compute expected via exact search (same algorithm as upstream search_exact fallback)
        var expected = LogViewer.SearchExact(line, "café");
        var ranges = log.HighlightRangesForLine(0);
        Assert.NotNull(ranges);
        Assert.Equal(expected.Count, ranges.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.Equal(expected[i], ranges[i]);
    }

    [Fact]
    public void TestSearchHighlightRangesStableAfterPush()
    {
        var log = new LogViewer(100);
        log.Push("Alpha beta ALPHA beta alpha");

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = false, ContextLines = 0 };
        log.SearchWithConfig("alpha", config);
        var before = log.HighlightRangesForLine(0)!.ToList();

        log.Push("no match here");
        var after = log.HighlightRangesForLine(0)!.ToList();

        Assert.Equal(before, after);
    }

    [Fact]
    public void TestSearchRegexBasic()
    {
        var log = new LogViewer(100);
        log.Push("error: code 42");
        log.Push("error: code 99");
        log.Push("info: all good");
        log.Push("error: code 7");

        var config = new SearchConfig { Mode = SearchMode.Regex, CaseSensitive = true, ContextLines = 0 };
        int count = log.SearchWithConfig(@"error: code \d+", config);
        Assert.Equal(3, count);
    }

    [Fact]
    public void TestSearchRegexInvalidPattern()
    {
        var log = new LogViewer(100);
        log.Push("something");

        var config = new SearchConfig { Mode = SearchMode.Regex, CaseSensitive = true, ContextLines = 0 };
        // Invalid regex (unmatched paren)
        int count = log.SearchWithConfig(@"(unclosed", config);
        Assert.Equal(0, count);
        Assert.Null(log.SearchInfo());
    }

    [Fact]
    public void TestSearchHighlightRanges()
    {
        var log = new LogViewer(100);
        log.Push("foo bar foo baz foo");

        int count = log.Search("foo");
        Assert.Equal(1, count);

        var ranges = log.HighlightRangesForLine(0);
        Assert.NotNull(ranges);
        Assert.Equal(3, ranges.Count);
        Assert.Equal((0, 3), ranges[0]);
        Assert.Equal((8, 11), ranges[1]);
        Assert.Equal((16, 19), ranges[2]);
    }

    [Fact]
    public void TestSearchContextLines()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 10; i++)
            log.Push($"line {i}");

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = true, ContextLines = 1 };
        // "line 5" is at index 5
        int count = log.SearchWithConfig("line 5", config);
        Assert.Equal(1, count);

        var ctx = log.ContextLineIndices();
        Assert.NotNull(ctx);
        // Should include lines 4, 5, 6
        Assert.Contains(4, ctx);
        Assert.Contains(5, ctx);
        Assert.Contains(6, ctx);
        Assert.DoesNotContain(3, ctx);
        Assert.DoesNotContain(7, ctx);
    }

    [Fact]
    public void TestSearchIncrementalWithConfig()
    {
        var log = new LogViewer(100);
        log.Push("Hello World");

        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = false, ContextLines = 0 };
        int count = log.SearchWithConfig("hello", config);
        Assert.Equal(1, count);

        // Push new line that matches case-insensitively
        log.Push("HELLO again");
        Assert.Equal((1, 2), log.SearchInfo());

        // Push line that doesn't match
        log.Push("goodbye");
        Assert.Equal((1, 2), log.SearchInfo());
    }

    [Fact]
    public void TestSearchModeSwitch()
    {
        var log = new LogViewer(100);
        log.Push("error 42");
        log.Push("error 99");
        log.Push("info ok");

        // First search: literal
        int count = log.Search("error");
        Assert.Equal(2, count);

        // Switch to case-insensitive
        var config = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = false, ContextLines = 0 };
        count = log.SearchWithConfig("ERROR", config);
        Assert.Equal(2, count);

        // Switch back to case-sensitive — "ERROR" shouldn't match "error"
        var config2 = new SearchConfig { Mode = SearchMode.Literal, CaseSensitive = true, ContextLines = 0 };
        count = log.SearchWithConfig("ERROR", config2);
        Assert.Equal(0, count);
    }

    [Fact]
    public void TestSearchEmptyQuery()
    {
        var log = new LogViewer(100);
        log.Push("something");

        int count = log.Search("");
        Assert.Equal(0, count);
        Assert.Null(log.SearchInfo());

        var config = SearchConfig.Default();
        count = log.SearchWithConfig("", config);
        Assert.Equal(0, count);
        Assert.Null(log.SearchInfo());
    }

    [Fact]
    public void TestHighlightRangesWithinBounds()
    {
        var log = new LogViewer(100);
        string[] lines = { "short", "hello world hello", "café résumé café", "🌍 emoji 🌍", "" };
        foreach (var line in lines)
            log.Push(line);

        log.Search("hello");

        // Check all highlight ranges are valid char ranges
        for (int matchIdx = 0; matchIdx < log.LineCount(); matchIdx++)
        {
            var ranges = log.HighlightRangesForLine(matchIdx);
            if (ranges != null)
            {
                var item = log.Virt.Get(matchIdx);
                if (item != null)
                {
                    // Get plain text via the same helper path
                    string plain = GetPlainText(item);
                    foreach (var (start, end) in ranges)
                    {
                        Assert.True(start <= end,
                            $"Invalid range: start={start} > end={end} on line {matchIdx}");
                        Assert.True(end <= plain.Length,
                            $"Out of bounds: end={end} > len={plain.Length} on line {matchIdx}");
                    }
                }
            }
        }
    }

    [Fact]
    public void TestSearchMatchRateHint()
    {
        var log = new LogViewer(100);
        log.SetFilter("x");
        log.Push("x match");
        log.Search("match");
        log.Push("x match again");
        log.Push("x no");

        // 3 incremental checks (filter), some search matches
        double rate = log.SearchMatchRateHint();
        Assert.True(rate > 0.0);
        Assert.True(rate <= 1.0);
    }

    [Fact]
    public void TestLargeScrollbackEvictionAndScrollBounds()
    {
        var log = new LogViewer(1_000);
        log.Virt.SetVisibleCount(25);

        for (int i = 0; i < 5_000; i++)
            log.Push($"line {i}");

        Assert.Equal(1_000, log.LineCount());

        var first = log.Virt.Get(0);
        Assert.NotNull(first);
        Assert.Equal("line 4000", GetPlainText(first!).Split('\n')[0]);

        var last = log.Virt.Get(log.LineCount() - 1);
        Assert.NotNull(last);
        Assert.Equal("line 4999", GetPlainText(last!).Split('\n')[0]);

        log.ScrollToTop();
        Assert.False(log.AutoScrollEnabled());

        log.ScrollDown(10_000);
        Assert.True(log.IsAtBottom());
        Assert.True(log.AutoScrollEnabled());

        int maxOffset = log.LineCount() - log.Virt.VisibleCount();
        Assert.True(log.Virt.ScrollOffset() <= maxOffset);
    }

    [Fact]
    public void TestLargeScrollbackRenderTopAndBottomLines()
    {
        var log = new LogViewer(1_000);
        log.Virt.SetVisibleCount(3);
        for (int i = 0; i < 5_000; i++)
            log.Push($"line {i}");

        var pool = new GraphemePool();
        var state = new LogViewerState();

        log.ScrollToTop();
        var frame = new Frame(20, 3, pool);
        log.Render(new Rect(0, 0, 20, 3), frame, state);
        string topLine = LineText(frame, 0, 20);
        Assert.True(topLine.TrimEnd().StartsWith("line 4000"),
            $"expected top line to start with line 4000, got: {topLine:?}");

        log.ScrollToBottom();
        var frame2 = new Frame(20, 3, pool);
        log.Render(new Rect(0, 0, 20, 3), frame2, state);
        string bottomLine = LineText(frame2, 2, 20);
        Assert.True(bottomLine.TrimEnd().StartsWith("line 4999"),
            $"expected bottom line to start with line 4999, got: {bottomLine:?}");
    }

    [Fact]
    public void TestFilteredAutoscrollRespectsManualPosition()
    {
        var log = new LogViewer(200);
        log.Virt.SetVisibleCount(2);

        log.Push("match 1");
        log.Push("skip");
        log.Push("match 2");
        log.Push("match 3");
        log.Push("skip again");
        log.Push("match 4");
        log.Push("match 5");

        log.SetFilter("match");
        Assert.True(log.IsAtBottom());

        log.ScrollUp(2);
        int offsetBefore = log.FilteredScrollOffset;
        Assert.False(log.IsAtBottom());

        log.Push("match 6");
        Assert.Equal(offsetBefore, log.FilteredScrollOffset);

        log.ScrollToBottom();
        int offsetAtBottom = log.FilteredScrollOffset;
        log.Push("match 7");
        Assert.True(log.FilteredScrollOffset >= offsetAtBottom);
        Assert.True(log.IsAtBottom());
    }

    [Fact]
    public void TestMarkupParsingPreservesSpans()
    {
        // DIVERGENCE: upstream uses ftui_text::markup::parse_markup which produces
        // a rich ftui_text::Text with styled spans. In .NET we construct a
        // TextContent with styled spans directly to test the same invariants.
        var log = new LogViewer(100);
        // Simulate a bold "Hello" span followed by a plain " " and a red "world" + "!"
        var boldStyle = new WidgetStyle(null, null, CellStyleFlags.Bold);
        var redStyle = new WidgetStyle(PackedRgba.Red, null, null);
        var line = TextLine.FromSpans(new[]
        {
            TextSpan.Styled("Hello", boldStyle),
            TextSpan.Raw(" "),
            TextSpan.Styled("world", redStyle),
            TextSpan.Raw("!"),
        });
        log.Push(TextContent.FromLines(new[] { line }));

        var item = log.Virt.Get(0);
        Assert.NotNull(item);
        string plain = GetPlainText(item!);
        Assert.Equal("Hello world!", plain);

        var spans = item!.Lines[0].Spans;
        Assert.True(spans.Any(s => !s.Style.IsEmpty));
        Assert.True(spans.Any(s => s.Style.Attrs.HasValue && s.Style.Attrs.Value.HasFlag(CellStyleFlags.Bold)));
    }

    [Fact]
    public void TestMarkupRendersBoldCells()
    {
        // DIVERGENCE: upstream parses "[bold]Hello[/bold] world" via markup parser.
        // Here we construct the equivalent TextContent directly.
        var log = new LogViewer(10);
        var boldStyle = new WidgetStyle(null, null, CellStyleFlags.Bold);
        var line = TextLine.FromSpans(new[]
        {
            TextSpan.Styled("Hello", boldStyle),
            TextSpan.Raw(" world"),
        });
        log.Push(TextContent.FromLines(new[] { line }));

        var pool = new GraphemePool();
        var frame = new Frame(16, 1, pool);
        var state = new LogViewerState();
        log.Render(new Rect(0, 0, 16, 1), frame, state);

        string rendered = LineText(frame, 0, 16);
        Assert.True(rendered.TrimEnd().StartsWith("Hello world"));
        for (ushort x = 0; x < 5; x++)
        {
            var cell = frame.Buffer.Get(x, 0);
            Assert.NotNull(cell);
            Assert.True(cell!.Value.Attributes.HasFlag(CellStyleFlags.Bold),
                $"expected bold at x={x}, attrs={cell.Value.Attributes.Flags}");
        }
    }

    [Fact]
    public void TestToggleFollowDisablesAutoscrollOnPush()
    {
        var log = new LogViewer(100);
        log.Virt.SetVisibleCount(3);
        for (int i = 0; i < 5; i++)
            log.Push($"line {i}");
        Assert.True(log.IsAtBottom());

        log.ToggleFollow();
        Assert.False(log.AutoScrollEnabled());

        log.Push("new line");
        Assert.False(log.AutoScrollEnabled());
        Assert.False(log.IsAtBottom());
    }

    [Fact]
    public void TestSearchMatchRateHintRatio()
    {
        var log = new LogViewer(100);
        Assert.Equal(0.0, log.SearchMatchRateHint());

        log.SetFilter("ERR");
        log.Search("ERR");

        log.Push("ERR one");
        log.Push("INFO skip");
        log.Push("ERR two");
        log.Push("WARN skip");

        Assert.Equal(4UL, log.FilterStats().IncrementalChecks);
        Assert.Equal(2UL, log.FilterStats().IncrementalSearchChecks);
        Assert.Equal(2UL, log.FilterStats().IncrementalSearchMatches);
        Assert.Equal(1.0, log.SearchMatchRateHint());
    }

    [Fact]
    public void TestRenderCharWrapSplitsLines()
    {
        var log = new LogViewer(10);
        log.WithWrapMode(LogWrapMode.CharWrap);
        log.Push("abcdefghij");

        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        var state = new LogViewerState();
        log.Render(new Rect(0, 0, 5, 3), frame, state);

        Assert.Equal("abcde", LineText(frame, 0, 5));
        Assert.Equal("fghij", LineText(frame, 1, 5));
    }

    [Fact]
    public void TestRenderScrollIndicatorWhenNotAtBottom()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 5; i++)
            log.Push($"line {i}");

        log.ScrollToTop();

        var pool = new GraphemePool();
        var frame = new Frame(10, 2, pool);
        var state = new LogViewerState();
        log.Render(new Rect(0, 0, 10, 2), frame, state);

        string indicator = " 3 ";
        string bottomLine = LineText(frame, 1, 10);
        Assert.Equal(indicator, bottomLine.Substring(7, 3));
    }

    [Fact]
    public void TestRenderSearchIndicatorWhenActive()
    {
        var log = new LogViewer(100);
        for (int i = 0; i < 5; i++)
            log.Push($"line {i}");
        log.Search("line");

        var pool = new GraphemePool();
        var frame = new Frame(12, 2, pool);
        var state = new LogViewerState();
        log.Render(new Rect(0, 0, 12, 2), frame, state);

        string indicator = " 1/5 ";
        string bottomLine = LineText(frame, 1, 12);
        Assert.Equal(indicator, bottomLine.Substring(0, indicator.Length));
    }

    [Fact]
    public void TestSearchAsciiCaseInsensitiveRangesLongNeedle()
    {
        var ranges = LogViewer.SearchAsciiCaseInsensitiveRanges("hi", "hello");
        Assert.Empty(ranges);
    }

    [Fact]
    public void TestSearchAsciiCaseInsensitiveRangesLargeWorkFallback()
    {
        string haystack = new string('a', 500) + "HELLO" + new string('b', 500);
        var ranges = LogViewer.SearchAsciiCaseInsensitiveRanges(haystack, "hello");
        Assert.Equal(new List<(int, int)> { (500, 505) }, ranges);
    }

    // ── Private test helpers ──────────────────────────────────────────────────

    /// <summary>Extract plain text from a TextContent (mirrors the private helper in LogViewer).</summary>
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
}
