// Port of .external/frankentui/crates/ftui-widgets/src/input.rs — #[cfg(test)] mod tests
// and #[cfg(test)] mod scroll_edge_tests.
// Full 1-to-1 port of all upstream test cases.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

// ── Test helpers ────────────────────────────────────────────────────────────

file static class InputTestHelpers
{
    /// <summary>
    /// Get the char content of a cell at (x,y) in the frame buffer.
    /// Returns null for grapheme, continuation, or empty cells.
    /// Maps to upstream cell.content.as_char().
    /// </summary>
    public static char? CellChar(Frame frame, ushort x, ushort y)
    {
        var cell = frame.Buffer.Get(x, y);
        if (cell is null) return null;
        return (char?)cell.Value.Content.AsRune()?.Value;
    }

    /// <summary>
    /// Read the raw text of a row as a padded string, one char per cell.
    /// Grapheme cells map to their first code point; empty/continuation become space.
    /// Maps to upstream raw_row_text().
    /// </summary>
    public static string RawRowText(Frame frame, ushort y, ushort width)
    {
        var chars = new char[width];
        for (ushort x = 0; x < width; x++)
        {
            var cell = frame.Buffer.Get(x, y);
            if (cell is null) { chars[x] = ' '; continue; }
            if (cell.Value.IsEmpty || cell.Value.IsContinuation) { chars[x] = ' '; continue; }
            chars[x] = (char?)cell.Value.Content.AsRune()?.Value ?? ' ';
        }
        return new string(chars);
    }

    /// <summary>Build a Frame for render tests.</summary>
    public static (Frame frame, GraphemePool pool) MakeFrame(ushort w, ushort h)
    {
        var pool = new GraphemePool();
        var frame = new Frame(w, h, pool);
        return (frame, pool);
    }
}

// ── mod tests ───────────────────────────────────────────────────────────────

public sealed class InputTests
{
    // ── basic construction & value access ───────────────────────────────────

    [Fact]
    public void TestEmptyInput()
    {
        var input = new TextInput();
        Assert.True(input.Value().Length == 0);
        Assert.Equal(0, input.Cursor());
        Assert.Null(input.SelectedText());
    }

    [Fact]
    public void TestWithValue()
    {
        var input = new TextInput().WithValue("hello");
        input.SetFocused(true);
        Assert.Equal("hello", input.Value());
        Assert.Equal(5, input.Cursor());
    }

    [Fact]
    public void TestSetValue()
    {
        var input = new TextInput().WithValue("hello world");
        input.CursorForTest = 11;
        input.SetValue("hi");
        Assert.Equal("hi", input.Value());
        Assert.Equal(2, input.Cursor());
    }

    [Fact]
    public void TestClear()
    {
        var input = new TextInput().WithValue("hello");
        input.SetFocused(true);
        input.Clear();
        Assert.True(input.Value().Length == 0);
        Assert.Equal(0, input.Cursor());
    }

    // ── char insertion ──────────────────────────────────────────────────────

    [Fact]
    public void TestInsertChar()
    {
        var input = new TextInput();
        input.InsertChar('a');
        input.InsertChar('b');
        input.InsertChar('c');
        Assert.Equal("abc", input.Value());
        Assert.Equal(3, input.Cursor());
    }

    [Fact]
    public void TestInsertCharMid()
    {
        var input = new TextInput().WithValue("ac");
        input.CursorForTest = 1;
        input.InsertChar('b');
        Assert.Equal("abc", input.Value());
        Assert.Equal(2, input.Cursor());
    }

    [Fact]
    public void TestMaxLength()
    {
        var input = new TextInput().WithMaxLength(3);
        foreach (char c in "abcdef")
            input.InsertChar(c);
        Assert.Equal("abc", input.Value());
        Assert.Equal(3, input.Cursor());
    }

    // ── deletion ────────────────────────────────────────────────────────────

    [Fact]
    public void TestDeleteCharBack()
    {
        var input = new TextInput().WithValue("hello");
        input.DeleteCharBack();
        Assert.Equal("hell", input.Value());
        Assert.Equal(4, input.Cursor());
    }

    [Fact]
    public void TestDeleteCharBackAtStart()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 0;
        input.DeleteCharBack();
        Assert.Equal("hello", input.Value());
    }

    [Fact]
    public void TestDeleteCharForward()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 0;
        input.DeleteCharForward();
        Assert.Equal("ello", input.Value());
        Assert.Equal(0, input.Cursor());
    }

    [Fact]
    public void TestDeleteCharForwardAtEnd()
    {
        var input = new TextInput().WithValue("hello");
        input.DeleteCharForward();
        Assert.Equal("hello", input.Value());
    }

    // ── cursor movement ─────────────────────────────────────────────────────

    [Fact]
    public void TestCursorLeftRight()
    {
        var input = new TextInput().WithValue("hello");
        Assert.Equal(5, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(4, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(3, input.Cursor());
        input.MoveCursorRight();
        Assert.Equal(4, input.Cursor());
    }

    [Fact]
    public void TestCursorBounds()
    {
        var input = new TextInput().WithValue("hi");
        input.CursorForTest = 0;
        input.MoveCursorLeft();
        Assert.Equal(0, input.Cursor());
        input.CursorForTest = 2;
        input.MoveCursorRight();
        Assert.Equal(2, input.Cursor());
    }

    // ── word movement ───────────────────────────────────────────────────────

    [Fact]
    public void TestWordMovementLeft()
    {
        var input = new TextInput().WithValue("hello world test");
        // cursor starts at 16 (end)
        input.MoveCursorWordLeft(false);
        Assert.Equal(12, input.Cursor()); // "hello world |test"

        input.MoveCursorWordLeft(false);
        Assert.Equal(6, input.Cursor()); // "hello |world test"

        input.MoveCursorWordLeft(false);
        Assert.Equal(0, input.Cursor()); // "|hello world test"
    }

    [Fact]
    public void TestWordMovementRight()
    {
        var input = new TextInput().WithValue("hello world test");
        input.CursorForTest = 0;

        input.MoveCursorWordRight(false);
        Assert.Equal(6, input.Cursor()); // "hello |world test"

        input.MoveCursorWordRight(false);
        Assert.Equal(12, input.Cursor()); // "hello world |test"

        input.MoveCursorWordRight(false);
        Assert.Equal(16, input.Cursor()); // "hello world test|"
    }

    [Fact]
    public void TestWordMovementSkipsPunctuation()
    {
        var input = new TextInput().WithValue("hello, world");
        input.CursorForTest = 0;

        input.MoveCursorWordRight(false);
        Assert.Equal(5, input.Cursor()); // "hello|, world"

        input.MoveCursorWordRight(false);
        Assert.Equal(7, input.Cursor()); // "hello, |world"

        input.MoveCursorWordRight(false);
        Assert.Equal(12, input.Cursor()); // "hello, world|"

        input.MoveCursorWordLeft(false);
        Assert.Equal(7, input.Cursor()); // "hello, |world"
    }

    [Fact]
    public void TestDeleteWordBack()
    {
        var input = new TextInput().WithValue("hello world");
        // "hello world|"
        input.DeleteWordBack();
        Assert.Equal("hello ", input.Value()); // Deleted "world"

        // "hello |" — word-left skips space and deletes the preceding word
        input.DeleteWordBack();
        Assert.Equal("", input.Value()); // Deleted "hello "
    }

    [Fact]
    public void TestDeleteWordForward()
    {
        var input = new TextInput().WithValue("hello world");
        input.CursorForTest = 0;
        // "|hello world" — word-right skips "hello" then space
        input.DeleteWordForward();
        Assert.Equal("world", input.Value()); // Deleted "hello "

        input.DeleteWordForward();
        Assert.Equal("", input.Value()); // Deleted "world"
    }

    // ── selection ───────────────────────────────────────────────────────────

    [Fact]
    public void TestSelectAll()
    {
        var input = new TextInput().WithValue("hello");
        input.SelectAll();
        Assert.Equal("hello", input.SelectedText());
    }

    [Fact]
    public void TestDeleteSelection()
    {
        var input = new TextInput().WithValue("hello world");
        input.SelectionAnchorNullable = 0;
        input.CursorForTest = 5;
        input.DeleteSelection();
        Assert.Equal(" world", input.Value());
        Assert.Equal(0, input.Cursor());
    }

    [Fact]
    public void TestInsertReplacesSelection()
    {
        var input = new TextInput().WithValue("hello");
        input.SelectAll();
        input.DeleteSelection();
        input.InsertChar('x');
        Assert.Equal("x", input.Value());
    }

    // ── Unicode grapheme handling ────────────────────────────────────────────

    [Fact]
    public void TestUnicodeGraphemeHandling()
    {
        var input = new TextInput();
        input.SetValue("café");
        Assert.Equal(4, input.GraphemeCountPublic());
        input.CursorForTest = 4;
        input.DeleteCharBack();
        Assert.Equal("caf", input.Value());
    }

    [Fact]
    public void TestMultiCodepointGraphemeCursorMovement()
    {
        var input = new TextInput().WithValue("a👩‍💻b");
        Assert.Equal(3, input.GraphemeCountPublic());
        Assert.Equal(3, input.Cursor());

        input.MoveCursorLeft();
        Assert.Equal(2, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(1, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(0, input.Cursor());

        input.MoveCursorRight();
        Assert.Equal(1, input.Cursor());
        input.MoveCursorRight();
        Assert.Equal(2, input.Cursor());
        input.MoveCursorRight();
        Assert.Equal(3, input.Cursor());
    }

    [Fact]
    public void TestDeleteBackMultiCodepointGrapheme()
    {
        var input = new TextInput().WithValue("a👩‍💻b");
        input.CursorForTest = 2; // after the emoji grapheme
        input.DeleteCharBack();
        Assert.Equal("ab", input.Value());
        Assert.Equal(1, input.Cursor());
        Assert.Equal(2, input.GraphemeCountPublic());
    }

    // ── IME composition ─────────────────────────────────────────────────────

    [Fact]
    public void TestImeCompositionStartUpdateCommit()
    {
        var input = new TextInput().WithValue("ab");
        input.CursorForTest = 1;

        input.ImeStartComposition();
        Assert.Equal("", input.ImeComposition());

        input.ImeUpdateComposition("漢");
        Assert.Equal("漢", input.ImeComposition());

        Assert.True(input.ImeCommitComposition());
        Assert.Null(input.ImeComposition());
        Assert.Equal("a漢b", input.Value());
        Assert.Equal(2, input.Cursor());
    }

    [Fact]
    public void TestImeCompositionCancelKeepsValue()
    {
        var input = new TextInput().WithValue("hello");
        input.ImeStartComposition();
        input.ImeUpdateComposition("👩‍💻");
        Assert.Equal("👩‍💻", input.ImeComposition());
        Assert.True(input.ImeCancelComposition());
        Assert.Null(input.ImeComposition());
        Assert.Equal("hello", input.Value());
        Assert.Equal(5, input.Cursor());
    }

    [Fact]
    public void TestImeCommitWithoutSessionIsNoop()
    {
        var input = new TextInput().WithValue("abc");
        Assert.False(input.ImeCommitComposition());
        Assert.Equal("abc", input.Value());
        Assert.Equal(3, input.Cursor());
    }

    // ── HandleEvent for IME ─────────────────────────────────────────────────

    [Fact]
    public void TestHandleEventImeUpdateAndCommit()
    {
        var input = new TextInput().WithValue("ab");
        input.CursorForTest = 1;

        Assert.True(input.HandleEvent(new InputEvent.Ime(ImeEvent.Start_())));
        Assert.True(input.HandleEvent(new InputEvent.Ime(ImeEvent.Update_("漢"))));
        Assert.Equal("漢", input.ImeComposition());
        Assert.True(input.HandleEvent(new InputEvent.Ime(ImeEvent.Commit_("漢"))));
        Assert.Null(input.ImeComposition());
        Assert.Equal("a漢b", input.Value());
        Assert.Equal(2, input.Cursor());
    }

    [Fact]
    public void TestHandleEventImeCancel()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 5;
        Assert.True(input.HandleEvent(new InputEvent.Ime(ImeEvent.Start_())));
        Assert.True(input.HandleEvent(new InputEvent.Ime(ImeEvent.Update_("👩‍💻"))));
        Assert.True(input.HandleEvent(new InputEvent.Ime(ImeEvent.Cancel_())));
        Assert.Null(input.ImeComposition());
        Assert.Equal("hello", input.Value());
        Assert.Equal(5, input.Cursor());
    }

    // ── Unicode edge cases ──────────────────────────────────────────────────

    [Fact]
    public void TestFlagEmojiGraphemeDeleteAndCursor()
    {
        var input = new TextInput().WithValue("a🇺🇸b");
        Assert.Equal(3, input.GraphemeCountPublic());
        input.CursorForTest = 2;
        input.DeleteCharBack();
        Assert.Equal("ab", input.Value());
        Assert.Equal(1, input.Cursor());
    }

    [Fact]
    public void TestCombiningGraphemeDeleteAndCursor()
    {
        var input = new TextInput().WithValue("áb"); // "a" + combining acute + "b"
        Assert.Equal(2, input.GraphemeCountPublic());
        input.CursorForTest = 1;
        input.DeleteCharBack();
        Assert.Equal("b", input.Value());
        Assert.Equal(0, input.Cursor());
    }

    [Fact]
    public void TestBidiLogicalCursorMovementOverGraphemes()
    {
        var input = new TextInput().WithValue("AאבB");
        Assert.Equal(4, input.GraphemeCountPublic());

        input.MoveCursorLeft();
        Assert.Equal(3, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(2, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(1, input.Cursor());
        input.MoveCursorLeft();
        Assert.Equal(0, input.Cursor());

        input.MoveCursorRight();
        Assert.Equal(1, input.Cursor());
        input.MoveCursorRight();
        Assert.Equal(2, input.Cursor());
        input.MoveCursorRight();
        Assert.Equal(3, input.Cursor());
        input.MoveCursorRight();
        Assert.Equal(4, input.Cursor());
    }

    // ── HandleEvent for keyboard ────────────────────────────────────────────

    [Fact]
    public void TestHandleEventChar()
    {
        var input = new TextInput();
        var @event = new InputEvent.Key(new KeyEvent(new KeyCode.Char('a')));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("a", input.Value());
    }

    [Fact]
    public void TestHandleEventBackspace()
    {
        var input = new TextInput().WithValue("ab");
        var @event = new InputEvent.Key(new KeyEvent(new KeyCode.Backspace()));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("a", input.Value());
    }

    [Fact]
    public void TestHandleEventCtrlA()
    {
        var input = new TextInput().WithValue("hello");
        var @event = new InputEvent.Key(new KeyEvent(new KeyCode.Char('a'), KeyModifiers.Ctrl));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("hello", input.SelectedText());
    }

    [Fact]
    public void TestHandleEventCtrlBackspace()
    {
        var input = new TextInput().WithValue("hello world");
        var @event = new InputEvent.Key(new KeyEvent(new KeyCode.Backspace(), KeyModifiers.Ctrl));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("hello ", input.Value());
    }

    [Fact]
    public void TestHandleEventHomeEnd()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 3;
        var home = new InputEvent.Key(new KeyEvent(new KeyCode.Home()));
        Assert.True(input.HandleEvent(home));
        Assert.Equal(0, input.Cursor());
        var end = new InputEvent.Key(new KeyEvent(new KeyCode.End()));
        Assert.True(input.HandleEvent(end));
        Assert.Equal(5, input.Cursor());
    }

    [Fact]
    public void TestShiftLeftCreatesSelection()
    {
        var input = new TextInput().WithValue("hello");
        var @event = new InputEvent.Key(new KeyEvent(new KeyCode.Left(), KeyModifiers.Shift));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal(4, input.Cursor());
        Assert.Equal(5, input.SelectionAnchorNullable);
        Assert.Equal("o", input.SelectedText());
    }

    // ── cursor_position ─────────────────────────────────────────────────────

    [Fact]
    public void TestCursorPosition()
    {
        var input = new TextInput().WithValue("hello");
        var area = new Rect(10, 5, 20, 1);
        var (x, y) = input.CursorPosition(area);
        Assert.Equal(15, x);
        Assert.Equal(5, y);
    }

    [Fact]
    public void TestCursorPositionEmpty()
    {
        var input = new TextInput();
        var area = new Rect(0, 0, 80, 1);
        var (x, y) = input.CursorPosition(area);
        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    // ── password mask ────────────────────────────────────────────────────────

    [Fact]
    public void TestPasswordMask()
    {
        var input = new TextInput().WithMask('*').WithValue("secret");
        Assert.Equal("secret", input.Value());
        // With mask, visual pos = 6 (one cell per grapheme regardless of char width)
        Assert.Equal(6, input.CursorVisualPos());
    }

    // ── render tests ─────────────────────────────────────────────────────────

    [Fact]
    public void TestRenderBasic()
    {
        var input = new TextInput().WithValue("hi");
        var area = new Rect(0, 0, 10, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(10, 1);
        input.Render(area, frame);

        var h = InputTestHelpers.CellChar(frame, 0, 0);
        Assert.Equal('h', h);
        var i = InputTestHelpers.CellChar(frame, 1, 0);
        Assert.Equal('i', i);
    }

    [Fact]
    public void TestRenderSetsCursorWhenFocused()
    {
        var input = new TextInput().WithValue("hi").WithFocused(true);
        var area = new Rect(0, 0, 10, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(10, 1);
        input.Render(area, frame);

        Assert.Equal((ushort)2, frame.CursorPosition?.x);
        Assert.Equal((ushort)0, frame.CursorPosition?.y);
        Assert.True(frame.CursorVisible);
    }

    [Fact]
    public void TestRenderDoesNotSetCursorWhenUnfocused()
    {
        var input = new TextInput().WithValue("hi");
        var area = new Rect(0, 0, 10, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(10, 1);
        input.Render(area, frame);

        Assert.Null(frame.CursorPosition);
    }

    [Fact]
    public void TestRenderGraphemeUsesPool()
    {
        const string grapheme = "👩‍💻";
        var input = new TextInput().WithValue(grapheme);
        var area = new Rect(0, 0, 6, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(6, 1);
        input.Render(area, frame);

        var cell = frame.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        Assert.True(cell.Value.Content.IsGrapheme);
        // If wide: cell at col 1 should be a continuation
        if (cell.Value.Content.WidthHint > 1)
            Assert.True(frame.Buffer.Get(1, 0)?.IsContinuation ?? false);
    }

    [Fact]
    public void TestRenderShorterValueClearsStalePrefix()
    {
        var area = new Rect(0, 0, 8, 2);
        var (frame, _pool) = InputTestHelpers.MakeFrame(8, 2);

        new TextInput().WithValue("abcdef").Render(area, frame);
        new TextInput().WithValue("xy").Render(area, frame);

        Assert.Equal("xy      ", InputTestHelpers.RawRowText(frame, 0, 8));
        Assert.Equal("        ", InputTestHelpers.RawRowText(frame, 1, 8));
    }

    [Fact]
    public void TestLeftCollapsesSelection()
    {
        var input = new TextInput().WithValue("hello");
        input.SelectionAnchorNullable = 1;
        input.CursorForTest = 4;
        input.MoveCursorLeft();
        Assert.Equal(1, input.Cursor());
        Assert.Null(input.SelectionAnchorNullable);
    }

    [Fact]
    public void TestRightCollapsesSelection()
    {
        var input = new TextInput().WithValue("hello");
        input.SelectionAnchorNullable = 1;
        input.CursorForTest = 4;
        input.MoveCursorRight();
        Assert.Equal(4, input.Cursor());
        Assert.Null(input.SelectionAnchorNullable);
    }

    [Fact]
    public void TestRenderSetsFrameCursor()
    {
        var input = new TextInput().WithValue("hello").WithFocused(true);
        var area = new Rect(5, 3, 20, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(30, 10);
        input.Render(area, frame);

        // area.x = 5, cursor_visual_pos = 5, effective_scroll = 0
        // cursor_screen_x = 5 + 5 = 10
        Assert.Equal((ushort)10, frame.CursorPosition?.x);
        Assert.Equal((ushort)3, frame.CursorPosition?.y);
    }

    [Fact]
    public void TestRenderCursorMidText()
    {
        var input = new TextInput().WithValue("hello").WithFocused(true);
        input.CursorForTest = 2; // After "he"
        var area = new Rect(0, 0, 20, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(20, 1);
        input.Render(area, frame);

        // Cursor after "he" = visual position 2
        Assert.Equal((ushort)2, frame.CursorPosition?.x);
        Assert.Equal((ushort)0, frame.CursorPosition?.y);
    }

    // ── undo support tests ──────────────────────────────────────────────────

    [Fact]
    public void TestUndoWidgetIdIsStable()
    {
        var input = new TextInput();
        var id1 = input.UndoId();
        var id2 = input.UndoId();
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void TestUndoWidgetIdUniquePerInstance()
    {
        var input1 = new TextInput();
        var input2 = new TextInput();
        Assert.NotEqual(input1.UndoId(), input2.UndoId());
    }

    [Fact]
    public void TestSnapshotAndRestore()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 3;
        input.SelectionAnchorNullable = 1;

        var snapshot = input.CreateSnapshot();

        // Modify the input
        input.SetValue("world");
        input.CursorForTest = 5;
        input.SelectionAnchorNullable = null;

        Assert.Equal("world", input.Value());
        Assert.Equal(5, input.Cursor());

        // Restore from snapshot
        Assert.True(input.RestoreSnapshot(snapshot));
        Assert.Equal("hello", input.Value());
        Assert.Equal(3, input.Cursor());
        Assert.Equal(1, input.SelectionAnchorNullable);
    }

    [Fact]
    public void TestTextInputUndoExtInsert()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 2;

        input.InsertTextAt(2, " world");
        // "he" + " world" + "llo"
        Assert.Equal("he worldllo", input.Value());
        Assert.Equal(8, input.Cursor()); // cursor moved by 6 graphemes
    }

    [Fact]
    public void TestTextInputUndoExtDelete()
    {
        var input = new TextInput().WithValue("hello world");
        input.CursorForTest = 8;

        input.DeleteTextRange(5, 11); // Delete " world"
        Assert.Equal("hello", input.Value());
        Assert.Equal(5, input.Cursor()); // cursor clamped to end
    }

    [Fact]
    public void TestCreateTextEditCommand()
    {
        var input = new TextInput().WithValue("hello");
        var cmd = input.CreateTextEditCommand(new TextEditOperation.Insert(0, "hi"));
        Assert.NotNull(cmd);
        Assert.Equal(input.UndoId(), cmd!.WidgetId);
        Assert.Equal("Insert text", cmd.Description());
    }

    // ── paste tests ─────────────────────────────────────────────────────────

    [Fact]
    public void TestPasteBulkInsert()
    {
        var input = new TextInput().WithValue("hello");
        input.CursorForTest = 5;
        var @event = new InputEvent.Paste(PasteEvent.Bracketed_(" world"));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("hello world", input.Value());
        Assert.Equal(11, input.Cursor());
    }

    [Fact]
    public void TestPasteMultiGraphemeSequence()
    {
        var input = new TextInput().WithValue("hi");
        input.CursorForTest = 2;
        var @event = new InputEvent.Paste(PasteEvent.New("👩‍💻🔥", false));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("hi👩‍💻🔥", input.Value());
        Assert.Equal(4, input.Cursor());
    }

    [Fact]
    public void TestPasteMaxLength()
    {
        var input = new TextInput().WithValue("abc").WithMaxLength(5);
        input.CursorForTest = 3;
        // Paste "def" (3 chars). Truncated to "de" (2 chars) to fit max 5.
        var @event = new InputEvent.Paste(PasteEvent.Bracketed_("def"));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("abcde", input.Value());
        Assert.Equal(5, input.Cursor());
    }

    [Fact]
    public void TestPasteCombiningMerge()
    {
        var input = new TextInput().WithValue("e");
        input.CursorForTest = 1;
        // Paste combining acute accent (U+0301). Merges with 'e' → 'é'.
        var @event = new InputEvent.Paste(PasteEvent.Bracketed_("́"));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("é", input.Value());
        Assert.Equal(1, input.GraphemeCountPublic());
        Assert.Equal(1, input.Cursor());
    }

    [Fact]
    public void TestPasteCombiningMergeMidString()
    {
        var input = new TextInput().WithValue("ab");
        input.CursorForTest = 1; // between a and b
        var @event = new InputEvent.Paste(PasteEvent.Bracketed_("́"));
        Assert.True(input.HandleEvent(@event));
        Assert.Equal("áb", input.Value());
        Assert.Equal(2, input.GraphemeCountPublic());
        Assert.Equal(1, input.Cursor());
    }

    [Fact]
    public void TestWideCharScrollVisibility()
    {
        const string wideChar = "　"; // Ideographic space, Width 2
        var input = new TextInput().WithValue(wideChar).WithFocused(true);
        input.CursorForTest = 1; // After the char

        var area = new Rect(0, 0, 2, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(2, 1);
        input.Render(area, frame);

        var cell = frame.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        Assert.False(cell.Value.IsEmpty, "Wide char should be visible");
    }

    [Fact]
    public void TestWideCharScrollSnapping()
    {
        // Verify that effective_scroll snaps to grapheme boundaries.
        // Upstream test documents the snapping logic via comments;
        // we validate the render does not show a blank when cursor is before 'b'.
        string text = "a　b"; // "a"(1) + ideographic-space(2) + "b"(1)
        var input = new TextInput().WithValue(text);
        input.CursorForTest = 3; // Before 'b', after wide char

        // Viewport width 2. The snap algorithm ensures we don't land
        // inside the wide char (visual pos 1..3 for wide char), so scroll
        // is clamped to 1 rather than 2.
        var area = new Rect(0, 0, 2, 1);
        var (frame, _pool) = InputTestHelpers.MakeFrame(2, 1);
        input.Render(area, frame);

        // At scroll=1 we expect to see the wide char (at screen col 0)
        // and 'b' (at screen col 2, clipped). At scroll=2 we'd see nothing.
        // We just verify the render completes without error and the frame
        // is not entirely blank (snap prevented the hole).
        var cell0 = frame.Buffer.Get(0, 0);
        var cell1 = frame.Buffer.Get(1, 0);
        // At least one of these should be non-empty if snapping is correct.
        Assert.True(
            (cell0.HasValue && !cell0.Value.IsEmpty) ||
            (cell1.HasValue && !cell1.Value.IsEmpty),
            "Snap should prevent entirely blank viewport");
    }

    // DIVERGENCE: tracing-feature-gated tests (tracing_input_edit_span_tracks_cursor_positions)
    // are omitted. The tracing feature is not ported to .NET.
}

// ── mod scroll_edge_tests ────────────────────────────────────────────────────

public sealed class InputScrollEdgeTests
{
    [Fact]
    public void TestScrollSnapLeftCursorVisibility()
    {
        // Test the scenario: [Char A][Char B][Char C]
        // Viewport width 1.
        // Scroll is at B.
        // Move cursor to A (left).
        // Cursor visual pos = 0.
        // effective_scroll must become 0.

        var input = new TextInput().WithValue("ABC");
        input.CursorForTest = 1; // At B

        // Force internal scroll to 1 (B) by rendering with narrow viewport.
        var area = new Rect(0, 0, 1, 1);
        var pool1 = new GraphemePool();
        var frame1 = new Frame(1, 1, pool1);
        input.Render(area, frame1); // Scroll should be 1

        // Now move left
        input.MoveCursorLeft(); // Cursor at A (0)

        // Render again
        var pool2 = new GraphemePool();
        var frame2 = new Frame(1, 1, pool2);
        input.Render(area, frame2);

        // Cell should be 'A'
        var cell = frame2.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        // The cell content is a char rune
        var ch = (char?)cell.Value.Content.AsRune()?.Value;
        Assert.Equal('A', ch);
    }

    [Fact]
    public void TestMaxLengthReplacementFailure()
    {
        // "abc", max 3. Select "b". Insert "de".
        // Should delete "b" -> "ac".
        // Try insert "de" -> "adec" (len 4).
        // Should revert insert -> "ac".
        // Oversized pastes replace nothing and leave selection intact.

        var input = new TextInput().WithValue("abc").WithMaxLength(3);
        input.SelectionAnchorNullable = 1; // "b" start
        input.CursorForTest = 2; // "b" end

        // Simulate paste "de"
        var @event = new InputEvent.Paste(PasteEvent.New("de", false));
        input.HandleEvent(@event);

        // Oversized pastes replace nothing and leave selection intact.
        Assert.Equal("abc", input.Value());
        Assert.Equal(2, input.Cursor());
        Assert.Equal(1, input.SelectionAnchorNullable);
    }
}
