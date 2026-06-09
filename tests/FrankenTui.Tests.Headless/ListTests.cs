// Upstream source: crates/ftui-widgets/src/list.rs (tests module)
// Full 1-1 port of all upstream list.rs tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Tests ported from the <c>#[cfg(test)] mod tests</c> block in list.rs.
/// </summary>
public class ListTests
{
    // ── Helper functions ───────────────────────────────────────────────────────
    // Port of row_text and raw_row_text from Rust test helpers.

    static string RowText(Frame frame, ushort y)
    {
        ushort width = frame.Width;
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var rune = frame.Buffer.Get(x, y)?.Content.AsRune();
            sb.Append(rune.HasValue ? (char)rune.Value.Value : ' ');
        }
        return sb.ToString().TrimEnd();
    }

    static string RawRowText(Frame frame, ushort y)
    {
        ushort width = frame.Width;
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var rune = frame.Buffer.Get(x, y)?.Content.AsRune();
            sb.Append(rune.HasValue ? (char)rune.Value.Value : ' ');
        }
        return sb.ToString();
    }

    static char? GetChar(Frame frame, ushort x, ushort y)
    {
        var rune = frame.Buffer.Get(x, y)?.Content.AsRune();
        return rune.HasValue ? (char)rune.Value.Value : null;
    }

    // ── Render tests ───────────────────────────────────────────────────────────

    [Fact]
    public void RenderEmptyList()
    {
        var list = new ListWidget(Array.Empty<ListItem>());
        var area = new Rect(0, 0, 10, 5);
        var pool = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        ((IWidget)list).Render(area, frame);
        // Should not throw — just renders "No items"
    }

    [Fact]
    public void RenderSimpleList()
    {
        var items = new[]
        {
            ListItem.New("Item A"),
            ListItem.New("Item B"),
            ListItem.New("Item C"),
        };
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        var state = new ListState();
        list.Render(area, frame, state);

        Assert.Equal('I', GetChar(frame, 0, 0));
        Assert.Equal('A', GetChar(frame, 5, 0));
        Assert.Equal('B', GetChar(frame, 5, 1));
        Assert.Equal('C', GetChar(frame, 5, 2));
    }

    [Fact]
    public void ListStateSelect()
    {
        var state = new ListState();
        Assert.Null(state.GetSelected());

        state.Select(2);
        Assert.Equal(2, state.GetSelected());

        state.Select(null);
        Assert.Null(state.GetSelected());
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void ListScrollsToSelected()
    {
        var items = Enumerable.Range(0, 10).Select(i => ListItem.New($"Item {i}")).ToArray();
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        var state = new ListState();
        state.Select(5);

        list.Render(area, frame, state);
        // offset should have been adjusted so item 5 is visible
        Assert.True(state.Offset <= 5);
        Assert.True(state.Offset + 3 > 5);
    }

    [Fact]
    public void ListClampsSelection()
    {
        var items = new[] { ListItem.New("A"), ListItem.New("B") };
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        var state = new ListState();
        state.Select(10); // out of bounds

        list.Render(area, frame, state);
        // should clamp to last item
        Assert.Equal(1, state.GetSelected());
    }

    [Fact]
    public void RenderListWithHighlightSymbol()
    {
        var items = new[] { ListItem.New("A"), ListItem.New("B") };
        var list = new ListWidget(items).WithHighlightSymbol(">");
        var area = new Rect(0, 0, 10, 2);
        var pool = new GraphemePool();
        var frame = new Frame(10, 2, pool);
        var state = new ListState();
        state.Select(0);

        list.Render(area, frame, state);
        // First item should have ">" symbol
        Assert.Equal('>', GetChar(frame, 0, 0));
    }

    [Fact]
    public void RenderZeroArea()
    {
        var list = new ListWidget(new[] { ListItem.New("A") });
        var area = new Rect(0, 0, 0, 0);
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        var state = new ListState();
        list.Render(area, frame, state);
        // Should not throw
    }

    [Fact]
    public void ListItemFromStr()
    {
        var item = ListItem.New("hello");
        Assert.Equal("hello", item.Content.Lines[0].Spans[0].Content);
        Assert.Equal("", item.Marker);
    }

    [Fact]
    public void ListItemWithMarker()
    {
        var items = new[]
        {
            ListItem.New("A").WithMarker("•"),
            ListItem.New("B").WithMarker("•"),
        };
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 10, 2);
        var pool = new GraphemePool();
        var frame = new Frame(10, 2, pool);
        var state = new ListState();
        list.Render(area, frame, state);

        // Marker should be rendered at the start
        Assert.Equal('•', GetChar(frame, 0, 0));
        Assert.Equal('•', GetChar(frame, 0, 1));
    }

    [Fact]
    public void ListStateDeselectResetsOffset()
    {
        var state = new ListState { Offset = 5 };
        state.Select(10);
        Assert.Equal(5, state.Offset); // select doesn't reset offset

        state.Select(null);
        Assert.Equal(0, state.Offset); // deselect resets offset
    }

    [Fact]
    public void ListScrollsUpWhenSelectionAboveViewport()
    {
        var items = Enumerable.Range(0, 10).Select(i => ListItem.New($"Item {i}")).ToArray();
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        var state = new ListState();

        // First scroll down
        state.Select(8);
        list.Render(area, frame, state);
        Assert.True(state.Offset > 0);

        // Now select item 0 — should scroll back up
        state.Select(0);
        list.Render(area, frame, state);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void ListClampsOffsetToFillViewportOnResize()
    {
        var items = Enumerable.Range(0, 10).Select(i => ListItem.New($"Item {i}")).ToArray();
        var list = new ListWidget(items);
        var pool = new GraphemePool();
        var state = new ListState { Offset = 7 };

        // Small viewport: show 7, 8, 9.
        var areaSmall = new Rect(0, 0, 10, 3);
        var frameSmall = new Frame(10, 3, pool);
        list.Render(areaSmall, frameSmall, state);
        Assert.Equal(7, state.Offset);
        Assert.StartsWith("Item 7", RowText(frameSmall, 0));
        Assert.StartsWith("Item 9", RowText(frameSmall, 2));

        // Larger viewport: offset should pull back to fill the viewport (5..9).
        var areaLarge = new Rect(0, 0, 10, 5);
        var frameLarge = new Frame(10, 5, pool);
        list.Render(areaLarge, frameLarge, state);
        Assert.Equal(5, state.Offset);
        Assert.StartsWith("Item 5", RowText(frameLarge, 0));
        Assert.StartsWith("Item 9", RowText(frameLarge, 4));
    }

    [Fact]
    public void RenderListMoreItemsThanViewport()
    {
        var items = Enumerable.Range(0, 20).Select(i => ListItem.New($"{i}")).ToArray();
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        var state = new ListState();
        list.Render(area, frame, state);

        // Only first 3 should render
        Assert.Equal('0', GetChar(frame, 0, 0));
        Assert.Equal('1', GetChar(frame, 0, 1));
        Assert.Equal('2', GetChar(frame, 0, 2));
    }

    [Fact]
    public void WidgetRenderUsesDefaultState()
    {
        var items = new[] { ListItem.New("X") };
        var list = new ListWidget(items);
        var area = new Rect(0, 0, 5, 1);
        var pool = new GraphemePool();
        var frame = new Frame(5, 1, pool);
        // Using IWidget (stateless)
        ((IWidget)list).Render(area, frame);
        Assert.Equal('X', GetChar(frame, 0, 0));
    }

    [Fact]
    public void ListRegistersHitRegions()
    {
        var items = new[] { ListItem.New("A"), ListItem.New("B"), ListItem.New("C") };
        var list = new ListWidget(items).WithHitId(HitId.New(42));
        var area = new Rect(0, 0, 10, 3);
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(10, 3, pool);
        var state = new ListState();
        list.Render(area, frame, state);

        // Each row should have a hit region with the item index as data
        var hit0 = frame.HitTest(5, 0);
        var hit1 = frame.HitTest(5, 1);
        var hit2 = frame.HitTest(5, 2);

        Assert.Equal((HitId.New(42), HitRegionKind.Content, 0UL), hit0);
        Assert.Equal((HitId.New(42), HitRegionKind.Content, 1UL), hit1);
        Assert.Equal((HitId.New(42), HitRegionKind.Content, 2UL), hit2);
    }

    [Fact]
    public void ListNoHitWithoutHitId()
    {
        var items = new[] { ListItem.New("A") };
        var list = new ListWidget(items); // No hit_id
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(10, 1, pool);
        var state = new ListState();
        list.Render(area, frame, state);

        // No hit region should be registered
        Assert.Null(frame.HitTest(5, 0));
    }

    [Fact]
    public void ListNoHitWithoutHitGrid()
    {
        var items = new[] { ListItem.New("A") };
        var list = new ListWidget(items).WithHitId(HitId.New(1));
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool); // No hit grid
        var state = new ListState();
        list.Render(area, frame, state);

        // hit_test returns null when no hit grid
        Assert.Null(frame.HitTest(5, 0));
    }

    // ── MeasurableWidget tests ─────────────────────────────────────────────────

    [Fact]
    public void ListItemMeasureSimple()
    {
        var item = ListItem.New("Hello"); // 5 chars
        var list = new ListWidget(new[] { item });
        var constraints = list.Measure(Size.Max);

        Assert.Equal(new Size(5, 1), constraints.Preferred);
        Assert.Equal(new Size(0, 1), constraints.Min);
    }

    [Fact]
    public void ListItemMeasureWithMarker()
    {
        // • + space + Hi = 1 + 1 + 2 = 4
        var item = ListItem.New("Hi").WithMarker("•");
        var list = new ListWidget(new[] { item });
        var constraints = list.Measure(Size.Max);

        Assert.Equal(4, constraints.Preferred.Width);
        Assert.Equal(1, constraints.Preferred.Height);
    }

    [Fact]
    public void ListItemHasIntrinsicSize()
    {
        var list = new ListWidget(new[] { ListItem.New("test") });
        Assert.True(list.HasIntrinsicSize());
    }

    [Fact]
    public void ListMeasureEmpty()
    {
        var list = new ListWidget(Array.Empty<ListItem>());
        var constraints = list.Measure(Size.Max);

        Assert.Equal(new Size(0, 0), constraints.Preferred);
        Assert.False(list.HasIntrinsicSize());
    }

    [Fact]
    public void ListMeasureSingleItem()
    {
        var items = new[] { ListItem.New("Hello") }; // 5 chars, 1 line
        var list = new ListWidget(items);
        var constraints = list.Measure(Size.Max);

        Assert.Equal(new Size(5, 1), constraints.Preferred);
        Assert.Equal(1, constraints.Min.Height);
    }

    [Fact]
    public void ListMeasureMultipleItems()
    {
        var items = new[]
        {
            ListItem.New("Short"),      // 5 chars
            ListItem.New("LongerItem"), // 10 chars
            ListItem.New("Tiny"),       // 4 chars
        };
        var list = new ListWidget(items);
        var constraints = list.Measure(Size.Max);

        // Width is max of all items = 10
        Assert.Equal(10, constraints.Preferred.Width);
        // Height is sum of all items = 3
        Assert.Equal(3, constraints.Preferred.Height);
    }

    [Fact]
    public void ListMeasureWithBlock()
    {
        var block = Block.Bordered(); // 4x4 chrome (borders + padding)
        var items = new[] { ListItem.New("Hi") }; // 2 chars, 1 line
        var list = new ListWidget(items).WithBlock(block);
        var constraints = list.Measure(Size.Max);

        // 2 (text) + 4 (chrome) = 6 width
        // 1 (line) + 4 (chrome) = 5 height
        Assert.Equal(new Size(6, 5), constraints.Preferred);
    }

    [Fact]
    public void ListMeasureWithHighlightSymbol()
    {
        var items = new[] { ListItem.New("Item") }; // 4 chars
        var list = new ListWidget(items).WithHighlightSymbol(">"); // 1 char + space = 2

        var constraints = list.Measure(Size.Max);

        // 4 (text) + 2 (symbol + space) = 6
        Assert.Equal(6, constraints.Preferred.Width);
    }

    [Fact]
    public void ListHasIntrinsicSize()
    {
        var items = new[] { ListItem.New("X") };
        var list = new ListWidget(items);
        Assert.True(list.HasIntrinsicSize());
    }

    [Fact]
    public void ListMinHeightIsOneRow()
    {
        var items = Enumerable.Range(0, 100).Select(i => ListItem.New($"Item {i}")).ToArray();
        var list = new ListWidget(items);
        var constraints = list.Measure(Size.Max);

        // Min height should be 1 (can scroll to see rest)
        Assert.Equal(1, constraints.Min.Height);
        // Preferred height is all items
        Assert.Equal(100, constraints.Preferred.Height);
    }

    [Fact]
    public void ListMeasureIsPure()
    {
        var items = new[] { ListItem.New("Test") };
        var list = new ListWidget(items);
        var a = list.Measure(new Size(100, 50));
        var b = list.Measure(new Size(100, 50));
        Assert.Equal(a, b);
    }

    // ── Undo Support tests ─────────────────────────────────────────────────────

    [Fact]
    public void ListStateUndoIdIsStable()
    {
        var state = new ListState();
        var id1 = state.UndoId();
        var id2 = state.UndoId();
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ListStateUndoIdUniquePerInstance()
    {
        var state1 = new ListState();
        var state2 = new ListState();
        Assert.NotEqual(state1.UndoId(), state2.UndoId());
    }

    [Fact]
    public void ListStateSnapshotAndRestore()
    {
        var state = new ListState();
        state.Select(5);
        state.Offset = 3;

        var snapshot = ((IUndoSupport)state).CreateSnapshot();

        // Modify state
        state.Select(10);
        state.Offset = 8;
        Assert.Equal(10, state.GetSelected());
        Assert.Equal(8, state.Offset);

        // Restore
        Assert.True(((IUndoSupport)state).RestoreSnapshot(snapshot));
        Assert.Equal(5, state.GetSelected());
        Assert.Equal(3, state.Offset);
    }

    [Fact]
    public void ListStateUndoExtMethods()
    {
        var state = new ListState();
        Assert.Null(((IListUndoExt)state).SelectedIndex());

        ((IListUndoExt)state).SetSelectedIndex(3);
        Assert.Equal(3, ((IListUndoExt)state).SelectedIndex());

        ((IListUndoExt)state).SetSelectedIndex(null);
        Assert.Null(((IListUndoExt)state).SelectedIndex());
        Assert.Equal(0, state.Offset); // reset on deselect
    }

    // ── Stateful Persistence tests ──────────────────────────────────────────────

    [Fact]
    public void ListStateWithPersistenceId()
    {
        var state = new ListState().WithPersistenceId("sidebar-menu");
        Assert.Equal("sidebar-menu", state.PersistenceId());
    }

    [Fact]
    public void ListStateDefaultNoPersistenceId()
    {
        var state = new ListState();
        Assert.Null(state.PersistenceId());
    }

    [Fact]
    public void ListStateSaveRestoreRoundTrip()
    {
        var state = new ListState().WithPersistenceId("test");
        state.Select(7);
        state.Offset = 4;

        var saved = ((IStateful<ListPersistState>)state).SaveState();
        Assert.Equal(7, saved.Selected);
        Assert.Equal(4, saved.Offset);

        // Reset state
        state.Select(null);
        Assert.Null(state.Selected);
        Assert.Equal(0, state.Offset);

        // Restore
        ((IStateful<ListPersistState>)state).RestoreState(saved);
        Assert.Equal(7, state.Selected);
        Assert.Equal(4, state.Offset);
    }

    [Fact]
    public void ListStateKeyUsesPersistenceId()
    {
        var state = new ListState().WithPersistenceId("file-browser");
        var key = ((IStateful<ListPersistState>)state).StateKey;
        Assert.Equal("List", key.WidgetType);
        Assert.Equal("file-browser", key.InstanceId);
    }

    [Fact]
    public void ListStateKeyDefaultWhenNoId()
    {
        var state = new ListState();
        var key = ((IStateful<ListPersistState>)state).StateKey;
        Assert.Equal("List", key.WidgetType);
        Assert.Equal("default", key.InstanceId);
    }

    [Fact]
    public void ListPersistStateDefault()
    {
        var persist = new ListPersistState();
        Assert.Null(persist.Selected);
        Assert.Equal(0, persist.Offset);
    }

    // ── Mouse handling tests ────────────────────────────────────────────────────

    [Fact]
    public void ListStateClickSelects()
    {
        var state = new ListState();
        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Selected(3), result);
        Assert.Equal(3, state.GetSelected());
    }

    [Fact]
    public void ListStateClickWrongIdIgnored()
    {
        var state = new ListState();
        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 2);
        var hit = ((HitId.New(99), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
        Assert.Null(state.GetSelected());
    }

    [Fact]
    public void ListStateClickOutOfRange()
    {
        var state = new ListState();
        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 15UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
        Assert.Null(state.GetSelected());
    }

    [Fact]
    public void ListStateClickNoHitIgnored()
    {
        var state = new ListState();
        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 2);
        var result = state.HandleMouse(@event, null, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void ListStateScrollUp()
    {
        var state = new ListState { Offset = 10 };
        state.ScrollUp(3);
        Assert.Equal(7, state.Offset);
    }

    [Fact]
    public void ListStateScrollUpClampsToZero()
    {
        var state = new ListState { Offset = 1 };
        state.ScrollUp(5);
        Assert.Equal(0, state.Offset);
    }

    [Fact]
    public void ListStateScrollDown()
    {
        var state = new ListState();
        state.ScrollDown(3, 20);
        Assert.Equal(3, state.Offset);
    }

    [Fact]
    public void ListStateScrollDownClamps()
    {
        var state = new ListState { Offset = 18 };
        state.ScrollDown(5, 20);
        Assert.Equal(19, state.Offset); // item_count - 1
    }

    [Fact]
    public void ListStateScrollWheelUp()
    {
        var state = new ListState { Offset = 10 };
        var @event = new MouseEvent(MouseEventKind.ScrollUp.Instance, 0, 0);
        var result = state.HandleMouse(@event, null, HitId.New(1), 20);
        Assert.Equal(MouseResult.Scrolled, result);
        Assert.Equal(7, state.Offset);
    }

    [Fact]
    public void ListStateScrollWheelDown()
    {
        var state = new ListState();
        var @event = new MouseEvent(MouseEventKind.ScrollDown.Instance, 0, 0);
        var result = state.HandleMouse(@event, null, HitId.New(1), 20);
        Assert.Equal(MouseResult.Scrolled, result);
        Assert.Equal(3, state.Offset);
    }

    [Fact]
    public void ListStateSelectNext()
    {
        var state = new ListState();
        state.SelectNext(5);
        Assert.Equal(0, state.GetSelected());
        state.SelectNext(5);
        Assert.Equal(1, state.GetSelected());
    }

    [Fact]
    public void ListStateSelectNextClamps()
    {
        var state = new ListState();
        state.Select(4);
        state.SelectNext(5);
        Assert.Equal(4, state.GetSelected()); // already at last
    }

    [Fact]
    public void ListStateSelectNextEmpty()
    {
        var state = new ListState();
        state.SelectNext(0);
        Assert.Null(state.GetSelected()); // no items, no change
    }

    [Fact]
    public void ListStateSelectPrevious()
    {
        var state = new ListState();
        state.Select(3);
        state.SelectPrevious();
        Assert.Equal(2, state.GetSelected());
    }

    [Fact]
    public void ListStateSelectPreviousClamps()
    {
        var state = new ListState();
        state.Select(0);
        state.SelectPrevious();
        Assert.Equal(0, state.GetSelected()); // already at first
    }

    [Fact]
    public void ListStateSelectPreviousFromNone()
    {
        var state = new ListState();
        state.SelectPrevious();
        Assert.Equal(0, state.GetSelected());
    }

    // ── Keyboard navigation tests ──────────────────────────────────────────────

    [Fact]
    public void ListHandleKeyDownFromNoneSelectsFirst()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("a"), ListItem.New("b"), ListItem.New("c"),
        });
        var state = new ListState();
        Assert.Null(state.GetSelected());

        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Down())));
        Assert.Equal(0, state.GetSelected());
    }

    [Fact]
    public void ListHandleKeyUpFromNoneSelectsLast()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("a"), ListItem.New("b"), ListItem.New("c"),
        });
        var state = new ListState();
        Assert.Null(state.GetSelected());

        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Up())));
        Assert.Equal(2, state.GetSelected());
    }

    [Fact]
    public void ListHandleKeyNavigationSupportsJkAndArrows()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("a"), ListItem.New("b"), ListItem.New("c"),
        });
        var state = new ListState();
        state.Select(0);

        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Down())));
        Assert.Equal(1, state.GetSelected());
        Assert.True(list.HandleKey(state, KeyEvent.Char('j')));
        Assert.Equal(2, state.GetSelected());
        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Up())));
        Assert.Equal(1, state.GetSelected());
        Assert.True(list.HandleKey(state, KeyEvent.Char('k')));
        Assert.Equal(0, state.GetSelected());
    }

    [Fact]
    public void ListHandleKeyFilterIsIncrementalAndEditable()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("banana"),
            ListItem.New("beta"),
        });
        var state = new ListState();

        Assert.True(list.HandleKey(state, KeyEvent.Char('b')));
        Assert.Equal("b", state.FilterQuery());
        Assert.Equal(1, state.GetSelected());

        Assert.True(list.HandleKey(state, KeyEvent.Char('e')));
        Assert.Equal("be", state.FilterQuery());
        Assert.Equal(2, state.GetSelected());

        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Backspace())));
        Assert.Equal("b", state.FilterQuery());

        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Escape())));
        Assert.Equal("", state.FilterQuery());
    }

    [Fact]
    public void ListRenderFilterNoMatchesShowsEmptyState()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha"), ListItem.New("beta") });
        var state = new ListState();
        state.SetFilterQuery("zzz");

        var pool = new GraphemePool();
        var frame = new Frame(14, 3, pool);
        list.Render(new Rect(0, 0, 14, 3), frame, state);

        Assert.Equal("No matches", RowText(frame, 0));
    }

    [Fact]
    public void ListRenderShorterItemClearsStaleRowSuffix()
    {
        var pool = new GraphemePool();
        var frame = new Frame(12, 2, pool);
        var state = new ListState();
        var area = new Rect(0, 0, 12, 2);

        var longList = new ListWidget(new[] { ListItem.New("alphabet") });
        longList.Render(area, frame, state);

        var shortList = new ListWidget(new[] { ListItem.New("a") });
        shortList.Render(area, frame, state);

        Assert.Equal("a           ", RawRowText(frame, 0));
    }

    [Fact]
    public void ListRenderEmptyStateClearsStaleRowsAndTail()
    {
        var list = new ListWidget(Array.Empty<ListItem>());
        var pool = new GraphemePool();
        var frame = new Frame(12, 3, pool);
        var area = new Rect(0, 0, 12, 3);
        frame.Buffer.Fill(area, Cell.FromChar('X'));

        ((IWidget)list).Render(area, frame);

        Assert.Equal("No items    ", RawRowText(frame, 0));
        Assert.Equal("            ", RawRowText(frame, 1));
        Assert.Equal("            ", RawRowText(frame, 2));
    }

    [Fact]
    public void ListRenderNoMatchesClearsStaleRowsAndTail()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha"), ListItem.New("beta") });
        var state = new ListState();
        state.SetFilterQuery("zzz");

        var pool = new GraphemePool();
        var frame = new Frame(12, 3, pool);
        var area = new Rect(0, 0, 12, 3);
        frame.Buffer.Fill(area, Cell.FromChar('X'));

        list.Render(area, frame, state);

        Assert.Equal("No matches  ", RawRowText(frame, 0));
        Assert.Equal("            ", RawRowText(frame, 1));
        Assert.Equal("            ", RawRowText(frame, 2));
    }

    [Fact]
    public void ListMultiSelectToggleWithSpace()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("beta"),
            ListItem.New("gamma"),
        });
        var state = new ListState();
        state.SetMultiSelect(true);
        state.Select(0);

        Assert.True(list.HandleKey(state, KeyEvent.Char(' ')));
        Assert.Contains(0, state.SelectedIndices());

        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Down())));
        Assert.True(list.HandleKey(state, KeyEvent.Char(' ')));
        Assert.Contains(1, state.SelectedIndices());
        Assert.Equal(2, state.SelectedCount());
    }

    [Fact]
    public void ListRenderDrawsScrollIndicators()
    {
        var items = Enumerable.Range(0, 8).Select(i => ListItem.New($"Item {i}")).ToArray();
        var list = new ListWidget(items);
        var state = new ListState
        {
            Selected = 4,
            Offset = 2,
            ScrollIntoViewRequested = false,
        };
        var pool = new GraphemePool();
        var frame = new Frame(8, 3, pool);
        list.Render(new Rect(0, 0, 8, 3), frame, state);

        Assert.Equal('↑', GetChar(frame, 7, 0));
        Assert.Equal('↓', GetChar(frame, 7, 2));
    }

    // DIVERGENCE: list_tracing_span_and_selection_events_are_emitted test is omitted.
    // Rust feature gate `#[cfg(feature = "tracing")]` has no .NET equivalent.
    // The tracing instrumentation path is not ported.

    [Fact]
    public void ListStateRightClickIgnored()
    {
        var state = new ListState();
        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Right), 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void ListStateClickBorderRegionIgnored()
    {
        var state = new ListState();
        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Border, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
    }

    [Fact]
    public void ListStateSecondClickActivates()
    {
        var state = new ListState();
        state.Select(3);

        var @event = new MouseEvent(new MouseEventKind.Down(MouseButton.Left), 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Activated(3), result);
        Assert.Equal(3, state.GetSelected());
    }

    [Fact]
    public void ListStateHoverUpdates()
    {
        var state = new ListState();
        var @event = new MouseEvent(MouseEventKind.Moved.Instance, 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.HoverChanged, result);
        Assert.Equal(3, state.Hovered);
    }

    [Fact]
    public void ListStateHoverSameIndexIgnored()
    {
        var state = new ListState { Hovered = 3 };
        var @event = new MouseEvent(MouseEventKind.Moved.Instance, 5, 2);
        var hit = ((HitId.New(1), HitRegionKind.Content, 3UL) as (HitId, HitRegionKind, ulong)?);
        var result = state.HandleMouse(@event, hit, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
        Assert.Equal(3, state.Hovered);
    }

    [Fact]
    public void ListStateHoverClears()
    {
        var state = new ListState { Hovered = 5 };
        var @event = new MouseEvent(MouseEventKind.Moved.Instance, 5, 2);
        // No hit (mouse moved off the list)
        var result = state.HandleMouse(@event, null, HitId.New(1), 10);
        Assert.Equal(MouseResult.HoverChanged, result);
        Assert.Null(state.Hovered);
    }

    [Fact]
    public void ListStateHoverClearWhenAlreadyNone()
    {
        var state = new ListState();
        var @event = new MouseEvent(MouseEventKind.Moved.Instance, 5, 2);
        var result = state.HandleMouse(@event, null, HitId.New(1), 10);
        Assert.Equal(MouseResult.Ignored, result);
    }

    // ── bd-1lg.27: Selection & filter interaction tests ───────────────────────

    [Fact]
    public void ListNavigateDownWhileFilterActive()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("banana"),
            ListItem.New("beta"),
            ListItem.New("gamma"),
        });
        var state = new ListState();
        // Type "b" to filter → matches banana(1) and beta(2)
        Assert.True(list.HandleKey(state, KeyEvent.Char('b')));
        Assert.Equal("b", state.FilterQuery());
        Assert.Equal(1, state.GetSelected()); // banana

        // Navigate down in filtered list → should move to beta(2)
        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Down())));
        Assert.Equal(2, state.GetSelected()); // beta

        // Navigate down at end → should stay at beta(2)
        Assert.False(list.HandleKey(state, new KeyEvent(new KeyCode.Down())));
        Assert.Equal(2, state.GetSelected());
    }

    [Fact]
    public void ListNavigateUpWhileFilterActive()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("banana"),
            ListItem.New("beta"),
            ListItem.New("gamma"),
        });
        var state = new ListState();
        state.SetFilterQuery("b");
        // Force selection to beta(2) — last filtered match
        state.Select(2);

        // Navigate up → should move to banana(1)
        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Up())));
        Assert.Equal(1, state.GetSelected());

        // Navigate up at top → should stay at banana(1)
        Assert.False(list.HandleKey(state, new KeyEvent(new KeyCode.Up())));
        Assert.Equal(1, state.GetSelected());
    }

    [Fact]
    public void ListFilterCaseInsensitive()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("Alpha"),
            ListItem.New("BANANA"),
            ListItem.New("beta"),
        });
        var state = new ListState();
        // Type uppercase 'B' → should match "BANANA" and "beta"
        Assert.True(list.HandleKey(state, KeyEvent.Char('B')));
        Assert.Equal(1, state.GetSelected()); // BANANA is first match
    }

    [Fact]
    public void ListFilterMatchesMarker()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("apple").WithMarker("fruit"),
            ListItem.New("carrot").WithMarker("veggie"),
            ListItem.New("berry").WithMarker("fruit"),
        });
        var state = new ListState();
        // Set "veg" → should match only carrot (via marker)
        state.SetFilterQuery("veg");
        var filtered = list.FilteredIndices(state);
        Assert.Equal(new[] { 1 }, filtered); // only carrot
    }

    [Fact]
    public void ListMultiSelectToggleWhileFiltered()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("banana"),
            ListItem.New("beta"),
            ListItem.New("gamma"),
        });
        var state = new ListState();
        state.SetMultiSelect(true);
        state.SetFilterQuery("b");

        // Select banana(1) first
        state.Select(1);
        // Toggle multi-select on banana
        Assert.True(list.HandleKey(state, KeyEvent.Char(' ')));
        Assert.Contains(1, state.SelectedIndices());

        // Navigate to beta and toggle
        Assert.True(list.HandleKey(state, new KeyEvent(new KeyCode.Down())));
        Assert.True(list.HandleKey(state, KeyEvent.Char(' ')));
        Assert.Contains(2, state.SelectedIndices());
        Assert.Equal(2, state.SelectedCount());
    }

    [Fact]
    public void ListDisableMultiSelectClearsExtras()
    {
        var state = new ListState();
        state.SetMultiSelect(true);
        state.ToggleMultiSelected(0);
        state.ToggleMultiSelected(1);
        state.ToggleMultiSelected(2);
        Assert.Equal(3, state.SelectedCount());
        // toggle_multi_selected sets selected to the last toggled index
        Assert.Equal(2, state.GetSelected());

        // Disable multi-select → should keep only the current selection
        state.SetMultiSelect(false);
        Assert.Equal(1, state.SelectedCount());
        Assert.Contains(2, state.SelectedIndices()); // current selected
    }

    [Fact]
    public void ListNavigationWithCtrlModifierIgnored()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha"), ListItem.New("beta") });
        var state = new ListState();
        state.Select(0);

        var ctrlDown = new KeyEvent(new KeyCode.Down(), KeyModifiers.Ctrl);
        Assert.False(list.HandleKey(state, ctrlDown));
        Assert.Equal(0, state.GetSelected()); // unchanged
    }

    [Fact]
    public void ListSpaceWithNoSelectionInMultiSelectIsNoop()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha"), ListItem.New("beta") });
        var state = new ListState();
        state.SetMultiSelect(true);
        // No selection
        Assert.False(list.HandleKey(state, KeyEvent.Char(' ')));
        Assert.Equal(0, state.SelectedCount());
    }

    [Fact]
    public void ListBackspaceOnEmptyFilterReturnsFalse()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha") });
        var state = new ListState();
        Assert.Equal("", state.FilterQuery());
        Assert.False(list.HandleKey(state, new KeyEvent(new KeyCode.Backspace())));
    }

    [Fact]
    public void ListEscapeOnEmptyFilterReturnsFalse()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha") });
        var state = new ListState();
        Assert.Equal("", state.FilterQuery());
        Assert.False(list.HandleKey(state, new KeyEvent(new KeyCode.Escape())));
    }

    [Fact]
    public void ListNavigateInEmptyFilteredResult()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha"), ListItem.New("beta") });
        var state = new ListState();
        state.Select(0);
        state.SetFilterQuery("zzz"); // nothing matches

        // Navigate should deselect since no filtered items
        var handled = list.HandleKey(state, new KeyEvent(new KeyCode.Down()));
        // Either handled (deselected) or not, selection should be None
        if (handled)
            Assert.Null(state.GetSelected());
    }

    [Fact]
    public void ListFilterPreservesSelectionWhenStillVisible()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("banana"),
            ListItem.New("beta"),
        });
        var state = new ListState();
        state.Select(2); // beta

        // Type "b" → beta still matches, selection should stay
        Assert.True(list.HandleKey(state, KeyEvent.Char('b')));
        Assert.Equal(2, state.GetSelected()); // beta still selected
    }

    [Fact]
    public void ListFilterMovesSelectionWhenCurrentHidden()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("banana"),
            ListItem.New("cherry"),
        });
        var state = new ListState();
        state.Select(2); // cherry

        // Type "b" → cherry doesn't match, should move to banana(1)
        Assert.True(list.HandleKey(state, KeyEvent.Char('b')));
        Assert.Equal(1, state.GetSelected()); // banana (first match)
    }

    [Fact]
    public void ListSetFilterQueryResetsOffset()
    {
        var state = new ListState { Offset = 10 };
        state.SetFilterQuery("abc");
        Assert.Equal(0, state.Offset);
        Assert.Equal("abc", state.FilterQuery());
    }

    [Fact]
    public void ListClearFilterQueryResetsOffset()
    {
        var state = new ListState();
        state.SetFilterQuery("abc");
        state.Offset = 5;
        state.ClearFilterQuery();
        Assert.Equal(0, state.Offset);
        Assert.Equal("", state.FilterQuery());
    }

    [Fact]
    public void ListClearFilterQueryNoopWhenEmpty()
    {
        var state = new ListState { Offset = 5 };
        state.ClearFilterQuery(); // already empty
        Assert.Equal(5, state.Offset); // unchanged
    }

    [Fact]
    public void ListSelectNextInMultiSelectPreservesOthers()
    {
        var state = new ListState();
        state.SetMultiSelect(true);
        state.ToggleMultiSelected(0);
        state.ToggleMultiSelected(2);
        // toggle_multi_selected sets selected to last toggled (2)
        Assert.Equal(2, state.GetSelected());
        Assert.Equal(2, state.SelectedCount());

        // Navigate down should not clear multi_selected
        state.SelectNext(5);
        Assert.Equal(3, state.GetSelected()); // moved from 2 to 3
        Assert.Contains(0, state.SelectedIndices());
        Assert.Contains(2, state.SelectedIndices());
    }

    [Fact]
    public void ListDeselectClearsMultiSelected()
    {
        var state = new ListState();
        state.SetMultiSelect(true);
        state.ToggleMultiSelected(0);
        state.ToggleMultiSelected(1);
        state.ToggleMultiSelected(2);
        Assert.Equal(3, state.SelectedCount());

        state.Select(null);
        Assert.Equal(0, state.SelectedCount());
        Assert.Empty(state.SelectedIndices());
    }

    [Fact]
    public void ListViJMovesThoughFilteredItems()
    {
        var list = new ListWidget(new[]
        {
            ListItem.New("xylophone"),
            ListItem.New("berry"),
            ListItem.New("box"),
            ListItem.New("cat"),
        });
        var state = new ListState();
        state.SetFilterQuery("b");
        // Filtered: berry(1), box(2)
        state.Select(1); // berry

        // j should move to box(2), skipping xylophone(0) and cat(3)
        Assert.True(list.HandleKey(state, KeyEvent.Char('j')));
        Assert.Equal(2, state.GetSelected()); // box

        // j at end → should stay at box(2)
        Assert.False(list.HandleKey(state, KeyEvent.Char('j')));
        Assert.Equal(2, state.GetSelected());
    }

    [Fact]
    public void ListJkNavigateNotFilterEvenWhenEmpty()
    {
        // j/k are always vi-navigation and must never be appended to the
        // filter query, even when the filter is empty. (bd-2pp0c)
        var list = new ListWidget(new[]
        {
            ListItem.New("alpha"),
            ListItem.New("jam"),
            ListItem.New("kite"),
        });
        var state = new ListState();
        Assert.Equal("", state.FilterQuery());

        // j navigates down, does NOT start a filter for "j"
        Assert.True(list.HandleKey(state, KeyEvent.Char('j')));
        Assert.Equal("", state.FilterQuery());
        Assert.Equal(0, state.GetSelected()); // first selection from None

        // j again moves to next item
        Assert.True(list.HandleKey(state, KeyEvent.Char('j')));
        Assert.Equal("", state.FilterQuery());
        Assert.Equal(1, state.GetSelected());

        // k navigates up, does NOT start a filter for "k"
        Assert.True(list.HandleKey(state, KeyEvent.Char('k')));
        Assert.Equal("", state.FilterQuery());
        Assert.Equal(0, state.GetSelected());
    }

    [Fact]
    public void ListMultiSelectUntoggleRemovesFromSet()
    {
        var state = new ListState();
        state.SetMultiSelect(true);
        state.Select(0);
        state.ToggleMultiSelected(0);
        Assert.Contains(0, state.SelectedIndices());
        Assert.Equal(1, state.SelectedCount());

        // Toggle again removes from multi_selected
        state.ToggleMultiSelected(0);
        Assert.DoesNotContain(0, state.SelectedIndices());
    }

    [Fact]
    public void ListWidgetRenderUsesDefaultState()
    {
        var list = new ListWidget(new[] { ListItem.New("alpha"), ListItem.New("beta") });
        var state = new ListState();
        var pool = new GraphemePool();
        var frame = new Frame(10, 3, pool);
        list.Render(new Rect(0, 0, 10, 3), frame, state);
        // No selection, first item should render at row 0
        Assert.Equal("alpha", RowText(frame, 0));
        Assert.Equal("beta", RowText(frame, 1));
    }
}
