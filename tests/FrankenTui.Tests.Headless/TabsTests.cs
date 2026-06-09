// Upstream source: crates/ftui-widgets/src/tabs.rs (tests module)
// Full 1-1 port of all upstream tabs.rs tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Tests ported from the <c>#[cfg(test)] mod tests</c> block in tabs.rs.
/// </summary>
public class TabsTests
{
    // ── Helper: row_text ───────────────────────────────────────────────────────
    // Port of row_text() from Rust tests: reads each cell in row y as a char.

    static string RowText(Frame frame, ushort y)
    {
        ushort width = frame.Buffer.Width;
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var cell = frame.Buffer.Get(x, y);
            char ch = ' ';
            if (cell is { } c)
            {
                var rune = c.Content.AsRune();
                if (rune.HasValue)
                    ch = (char)rune.Value.Value;
                else if (c.Content.IsGrapheme && c.Content.GraphemeId.HasValue)
                    ch = ' '; // wide grapheme continuation, treat as space
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    // ── tabs_render_basic ─────────────────────────────────────────────────────

    [Fact]
    public void TabsRenderBasic()
    {
        var tabs = new Tabs(new[] { Tab.New("One"), Tab.New("Two"), Tab.New("Three") });
        var state = new TabsState();
        state.Select(1, 3);
        var pool = new GraphemePool();
        var frame = new Frame(30, 1, pool);
        tabs.Render(new Rect(0, 0, 30, 1), frame, state);
        var row = RowText(frame, 0);
        Assert.Contains("[Two]", row);
    }

    // ── tabs_keyboard_switching_arrows_and_numbers ─────────────────────────────

    [Fact]
    public void TabsKeyboardSwitchingArrowsAndNumbers()
    {
        var state = new TabsState();
        Assert.True(state.HandleKey(KeyEvent.New(new KeyCode.Right()), 4));
        Assert.Equal(1, state.Active);
        Assert.True(state.HandleKey(KeyEvent.New(new KeyCode.Left()), 4));
        Assert.Equal(0, state.Active);
        Assert.True(state.HandleKey(KeyEvent.New(new KeyCode.Char('3')), 4));
        Assert.Equal(2, state.Active);
        Assert.False(state.HandleKey(KeyEvent.New(new KeyCode.Char('9')), 4));
        Assert.Equal(2, state.Active);
    }

    // ── tabs_overflow_markers_render_when_needed ───────────────────────────────

    [Fact]
    public void TabsOverflowMarkersRenderWhenNeeded()
    {
        var tabs = new Tabs(Enumerable.Range(0, 8).Select(i => Tab.New($"Tab{i}")));
        var state = new TabsState();
        state.Select(0, 8);
        var pool = new GraphemePool();
        var frame = new Frame(12, 1, pool);
        tabs.Render(new Rect(0, 0, 12, 1), frame, state);
        Assert.Equal('>', (char)(frame.Buffer.Get(11, 0)?.Content.AsRune()?.Value ?? ' '));

        state.Select(7, 8);
        tabs.Render(new Rect(0, 0, 12, 1), frame, state);
        Assert.Equal('<', (char)(frame.Buffer.Get(0, 0)?.Content.AsRune()?.Value ?? ' '));
    }

    // ── tabs_close_active_respects_closable ────────────────────────────────────

    [Fact]
    public void TabsCloseActiveRespectsClosable()
    {
        var tabs = new Tabs(new[]
        {
            Tab.New("Pinned").Closable(false),
            Tab.New("Temp").Closable(true),
        });
        var state = new TabsState();
        state.Select(0, 2);
        Assert.Null(tabs.CloseActive(state));
        state.Select(1, 2);
        Assert.NotNull(tabs.CloseActive(state));
        Assert.Equal(1, tabs.GetTabs().Count);
        Assert.Equal("Pinned", tabs.GetTabs()[0].Title());
    }

    // ── tabs_reorder_active_left_and_right ────────────────────────────────────

    [Fact]
    public void TabsReorderActiveLeftAndRight()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B"), Tab.New("C") });
        var state = new TabsState();
        state.Select(1, 3);
        Assert.True(tabs.MoveActiveLeft(state));
        Assert.Equal(0, state.Active);
        Assert.Equal("B", tabs.GetTabs()[0].Title());
        Assert.True(tabs.MoveActiveRight(state));
        Assert.Equal(1, state.Active);
        Assert.Equal("B", tabs.GetTabs()[1].Title());
    }

    // ── tabs_hit_regions_encode_tab_index ─────────────────────────────────────

    [Fact]
    public void TabsHitRegionsEncodeTabIndex()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B") }).HitId(HitId.New(5));
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(20, 1, pool);
        tabs.Render(new Rect(0, 0, 20, 1), frame, state);
        var hitA = frame.HitTest(1, 0);
        var hitB = frame.HitTest(6, 0);
        Assert.Equal((ulong)0, hitA?.Item3);
        Assert.Equal((ulong)1, hitB?.Item3);
    }

    // DIVERGENCE: tabs_tracing_span_and_switch_event_emitted is skipped.
    // Upstream test uses the Rust tracing feature flag (#[cfg(feature = "tracing")]).
    // .NET has no equivalent tracing span/subscriber infrastructure in this codebase.
    // The test is omitted rather than silently degraded.

    // ── Selection & switching tests (bd-1lg.28) ────────────────────────────────

    [Fact]
    public void TabsSelectZeroCountResets()
    {
        var state = new TabsState { Active = 3, Offset = 2 };
        Assert.False(state.Select(0, 0));
        Assert.Equal(0, state.Active);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void TabsSelectSameTabReturnsFalse()
    {
        var state = new TabsState();
        state.Select(2, 5);
        Assert.False(state.Select(2, 5));
    }

    [Fact]
    public void TabsSelectOutOfRangeClamps()
    {
        var state = new TabsState();
        Assert.True(state.Select(100, 5));
        Assert.Equal(4, state.Active); // clamped to last
    }

    [Fact]
    public void TabsSelectUpdatesOffsetWhenActiveBeforeOffset()
    {
        var state = new TabsState { Active = 3, Offset = 3 };
        Assert.True(state.Select(1, 5));
        Assert.Equal(1, state.Active);
        Assert.Equal(1, state.Offset); // offset scrolled back to active
    }

    [Fact]
    public void TabsNextAtLastTabReturnsFalse()
    {
        var state = new TabsState();
        state.Select(4, 5);
        Assert.False(state.Next(5));
        Assert.Equal(4, state.Active);
    }

    [Fact]
    public void TabsNextEmptyReturnsFalse()
    {
        var state = new TabsState();
        Assert.False(state.Next(0));
    }

    [Fact]
    public void TabsPreviousAtFirstTabReturnsFalse()
    {
        var state = new TabsState();
        Assert.False(state.Previous(5));
        Assert.Equal(0, state.Active);
    }

    [Fact]
    public void TabsPreviousEmptyReturnsFalse()
    {
        var state = new TabsState();
        Assert.False(state.Previous(0));
    }

    [Fact]
    public void TabsHandleKeyUnhandledReturnsFalse()
    {
        var state = new TabsState();
        Assert.False(state.HandleKey(KeyEvent.New(new KeyCode.Enter()), 3));
        Assert.False(state.HandleKey(KeyEvent.New(new KeyCode.Escape()), 3));
        Assert.False(state.HandleKey(KeyEvent.New(new KeyCode.Up()), 3));
    }

    [Fact]
    public void TabsHandleKeyNumberAtExactTabCountReturnsFalse()
    {
        var state = new TabsState();
        // 3 tabs: valid are '1','2','3'; '4' is out of range
        Assert.False(state.HandleKey(KeyEvent.New(new KeyCode.Char('4')), 3));
    }

    [Fact]
    public void TabsHandleKeyNumberOneSelectsFirst()
    {
        var state = new TabsState();
        state.Select(2, 5);
        Assert.True(state.HandleKey(KeyEvent.New(new KeyCode.Char('1')), 5));
        Assert.Equal(0, state.Active);
    }

    // ── Mouse handling tests ────────────────────────────────────────────────────

    [Fact]
    public void TabsMouseClickSelects()
    {
        var state = new TabsState();
        var evt = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 0);
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Content, 2UL);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Selected(2), result);
        Assert.Equal(2, state.Active);
    }

    [Fact]
    public void TabsMouseClickSameTabActivates()
    {
        var state = new TabsState();
        state.Select(2, 5);
        var evt = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 0);
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Content, 2UL);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Activated(2), result);
    }

    [Fact]
    public void TabsMouseClickWrongIdIgnored()
    {
        var state = new TabsState();
        var evt = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 0);
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(99), HitRegionKind.Content, 2UL);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void TabsMouseRightClickIgnored()
    {
        var state = new TabsState();
        var evt = new MouseEvent(new MouseEventKind.Down(MouseButton.Right), 5, 0);
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Content, 2UL);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void TabsMouseClickOutOfRange()
    {
        var state = new TabsState();
        var evt = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 0);
        var hit = ((HitId, HitRegionKind, ulong)?)(HitId.New(1), HitRegionKind.Content, 20UL);
        var result = state.HandleMouse(evt, hit, HitId.New(1), 5);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void TabsMouseNoHitIgnored()
    {
        var state = new TabsState();
        var evt = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 0);
        var result = state.HandleMouse(evt, null, HitId.New(1), 5);
        Assert.Equal(MouseResult.Ignored, result);
    }

    // ── Close active tests ─────────────────────────────────────────────────────

    [Fact]
    public void TabsCloseActiveEmptyReturnsNone()
    {
        var tabs = new Tabs(Array.Empty<Tab>());
        var state = new TabsState();
        Assert.Null(tabs.CloseActive(state));
    }

    [Fact]
    public void TabsCloseActiveLastRemainingResetsState()
    {
        var tabs = new Tabs(new[] { Tab.New("Only").Closable(true) });
        var state = new TabsState();
        var removed = tabs.CloseActive(state);
        Assert.NotNull(removed);
        Assert.Equal("Only", removed!.Title());
        Assert.Empty(tabs.GetTabs());
        Assert.Equal(0, state.Active);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void TabsCloseActiveMiddleShiftsActive()
    {
        var tabs = new Tabs(new[]
        {
            Tab.New("A"),
            Tab.New("B").Closable(true),
            Tab.New("C"),
        });
        var state = new TabsState();
        state.Select(1, 3); // select B
        var removed = tabs.CloseActive(state);
        Assert.Equal("B", removed!.Title());
        Assert.Equal(2, tabs.GetTabs().Count);
        // Active should stay at 1 (now "C"), or adjust if at end
        Assert.True(state.Active < tabs.GetTabs().Count);
    }

    [Fact]
    public void TabsCloseActiveAtEndMovesActiveBack()
    {
        var tabs = new Tabs(new[]
        {
            Tab.New("A"),
            Tab.New("B"),
            Tab.New("C").Closable(true),
        });
        var state = new TabsState();
        state.Select(2, 3); // select C (last)
        tabs.CloseActive(state);
        Assert.Equal(2, tabs.GetTabs().Count);
        Assert.Equal(1, state.Active); // moved back to B
    }

    // ── Reorder tests ──────────────────────────────────────────────────────────

    [Fact]
    public void TabsMoveActiveLeftAtBoundaryReturnsFalse()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B") });
        var state = new TabsState(); // active = 0
        Assert.False(tabs.MoveActiveLeft(state));
    }

    [Fact]
    public void TabsMoveActiveRightAtBoundaryReturnsFalse()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B") });
        var state = new TabsState();
        state.Select(1, 2); // active = last
        Assert.False(tabs.MoveActiveRight(state));
    }

    [Fact]
    public void TabsMoveActiveSingleTabReturnsFalse()
    {
        var tabs = new Tabs(new[] { Tab.New("Only") });
        var state = new TabsState();
        Assert.False(tabs.MoveActiveLeft(state));
        Assert.False(tabs.MoveActiveRight(state));
    }

    // ── Render tests ────────────────────────────────────────────────────────────

    [Fact]
    public void TabsRenderEmpty()
    {
        var tabs = new Tabs(Array.Empty<Tab>());
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);
        tabs.Render(new Rect(0, 0, 20, 1), frame, state);
        // Should not panic; row should be blank
        var row = RowText(frame, 0);
        Assert.Equal("", row.Trim());
    }

    [Fact]
    public void TabsRenderSingleTab()
    {
        var tabs = new Tabs(new[] { Tab.New("Solo") });
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);
        tabs.Render(new Rect(0, 0, 20, 1), frame, state);
        var row = RowText(frame, 0);
        Assert.Contains("[Solo]", row);
    }

    [Fact]
    public void TabsRenderEmptyClearsStaleRow()
    {
        var populated = new Tabs(new[] { Tab.New("LongTab"), Tab.New("Other") });
        var empty = new Tabs(Array.Empty<Tab>());
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);

        populated.Render(new Rect(0, 0, 20, 1), frame, state);
        Assert.NotEqual(new string(' ', 20), RowText(frame, 0));

        empty.Render(new Rect(0, 0, 20, 1), frame, state);
        Assert.Equal(new string(' ', 20), RowText(frame, 0));
    }

    [Fact]
    public void TabsRenderShorterTitlesClearStaleSuffix()
    {
        var longTabs = new Tabs(new[] { Tab.New("LongTitle"), Tab.New("Second") });
        var shortTabs = new Tabs(new[] { Tab.New("A"), Tab.New("B") });
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);

        longTabs.Render(new Rect(0, 0, 20, 1), frame, state);
        shortTabs.Render(new Rect(0, 0, 20, 1), frame, state);

        Assert.Equal("[A]  B              ", RowText(frame, 0));
    }

    [Fact]
    public void TabsNoStylingDropsConfiguredStyles()
    {
        var tabs = new Tabs(new[]
            {
                Tab.New("One").Style(new WidgetStyle(null, null, CellStyleFlags.Italic))
            })
            .Style(new WidgetStyle(null, null, CellStyleFlags.Bold))
            .ActiveStyle(new WidgetStyle(null, null, CellStyleFlags.Underline));

        var plainTabs = new Tabs(new[] { Tab.New("One") });
        var state = new TabsState();
        var plainState = new TabsState();
        var pool = new GraphemePool();
        var plainPool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        var plainFrame = new Frame(10, 1, plainPool);
        frame.SetDegradation(DegradationLevel.NoStyling);
        plainFrame.SetDegradation(DegradationLevel.NoStyling);

        tabs.Render(new Rect(0, 0, 10, 1), frame, state);
        plainTabs.Render(new Rect(0, 0, 10, 1), plainFrame, plainState);

        for (ushort x = 0; x < 10; x++)
        {
            var cell = frame.Buffer.Get(x, 0);
            var plain = plainFrame.Buffer.Get(x, 0);
            Assert.NotNull(cell);
            Assert.NotNull(plain);
            Assert.Equal(cell!.Value, plain!.Value);
        }
    }

    [Fact]
    public void TabsRenderZeroArea()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B") });
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);
        // Zero width area
        tabs.Render(new Rect(0, 0, 0, 1), frame, state);
        // Should not panic
    }

    [Fact]
    public void TabsRenderClosableShowsMarker()
    {
        var tabs = new Tabs(new[] { Tab.New("File").Closable(true) });
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);
        tabs.Render(new Rect(0, 0, 20, 1), frame, state);
        var row = RowText(frame, 0);
        Assert.Contains("x", row); // closable tab should show close marker
    }

    [Fact]
    public void TabsRenderActiveTabBracketed()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B"), Tab.New("C") });
        var state = new TabsState();
        state.Select(1, 3);
        var pool = new GraphemePool();
        var frame = new Frame(30, 1, pool);
        tabs.Render(new Rect(0, 0, 30, 1), frame, state);
        var row = RowText(frame, 0);
        // Active tab B should be bracketed, A and C should not
        Assert.Contains("[B]", row); // active tab should be bracketed
        Assert.Contains(" A ", row); // inactive tab A should be space-padded
        Assert.Contains(" C ", row); // inactive tab C should be space-padded
    }

    [Fact]
    public void TabsNoOverflowWhenAllFit()
    {
        var tabs = new Tabs(new[] { Tab.New("A"), Tab.New("B") });
        var state = new TabsState();
        var pool = new GraphemePool();
        var frame = new Frame(30, 1, pool);
        tabs.Render(new Rect(0, 0, 30, 1), frame, state);
        var row = RowText(frame, 0);
        // Should not contain overflow markers
        Assert.False(row.StartsWith('<'), "no left overflow marker expected");
        Assert.False(row.TrimEnd().EndsWith('>'), "no right overflow marker expected");
    }

    // ── Tab struct tests ───────────────────────────────────────────────────────

    [Fact]
    public void TabNewDefaults()
    {
        var tab = Tab.New("test");
        Assert.Equal("test", tab.Title());
        Assert.False(tab.IsClosable());
    }

    [Fact]
    public void TabClosableBuilder()
    {
        var tab = Tab.New("temp").Closable(true);
        Assert.True(tab.IsClosable());
    }

    // ── Widget trait stateless render ──────────────────────────────────────────

    [Fact]
    public void TabsWidgetStatelessRender()
    {
        var tabs = new Tabs(new[] { Tab.New("X"), Tab.New("Y") });
        var pool = new GraphemePool();
        var frame = new Frame(20, 1, pool);
        ((IWidget)tabs).Render(new Rect(0, 0, 20, 1), frame);
        var row = RowText(frame, 0);
        // Default state: active=0, so X should be bracketed
        Assert.Contains("[X]", row);
    }

    // ── Overflow visible range tests ───────────────────────────────────────────

    [Fact]
    public void TabsOverflowBothSides()
    {
        var tabs = new Tabs(Enumerable.Range(0, 10).Select(i => Tab.New($"Tab{i}")));
        var state = new TabsState();
        state.Select(5, 10); // middle tab
        var pool = new GraphemePool();
        var frame = new Frame(15, 1, pool);
        tabs.Render(new Rect(0, 0, 15, 1), frame, state);
        var row = RowText(frame, 0);
        // Should show both overflow markers
        Assert.True(row.StartsWith('<'), "expected left overflow marker");
        Assert.True(row.TrimEnd().EndsWith('>'), "expected right overflow marker");
    }
}
