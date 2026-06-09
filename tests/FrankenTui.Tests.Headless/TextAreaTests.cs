// Upstream source: .external/frankentui/crates/ftui-widgets/src/textarea.rs (tests module)
// Full 1-1 port of all upstream TextArea tests.
//
// DIVERGENCE: The upstream proptests (proptest crate) are ported as [Theory] with
// a fixed set of representative inputs. proptest is not available for xUnit in this
// repository.
//
// DIVERGENCE: Upstream render tests reference ftui_render::grapheme_pool::GraphemePool
// and use Widget::render / StatefulWidget::render. C# uses IWidget.Render and
// IStatefulWidget<TextAreaState>.Render.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using FrankenTui.Widgets.TextAreaInternals;

namespace FrankenTui.Tests.Headless;

// ── Test helpers ─────────────────────────────────────────────────────────────

file static class TextAreaTestHelpers
{
    /// <summary>
    /// Get the raw text of a row as a padded string, one char per cell.
    /// Maps to upstream raw_row_text().
    /// </summary>
    public static string RawRowText(Frame frame, ushort y, ushort width)
    {
        var chars = new char[width];
        for (ushort x = 0; x < width; x++)
        {
            var cell = frame.Buffer.Get(x, y);
            if (cell is null || cell.Value.IsEmpty || cell.Value.IsContinuation)
            {
                chars[x] = ' ';
                continue;
            }
            chars[x] = (char?)cell.Value.Content.AsRune()?.Value ?? ' ';
        }
        return new string(chars);
    }

    public static (Frame frame, GraphemePool pool) MakeFrame(ushort w, ushort h)
    {
        var pool  = new GraphemePool();
        var frame = new Frame(w, h, pool);
        return (frame, pool);
    }
}

// ── mod tests ─────────────────────────────────────────────────────────────────

public sealed class TextAreaTests
{
    [Fact]
    public void NewTextareaIsEmpty()
    {
        var ta = new TextArea();
        Assert.True(ta.IsEmpty());
        Assert.Equal("", ta.Text());
        Assert.Equal(1, ta.LineCount()); // empty rope has 1 line
    }

    [Fact]
    public void WithTextBuilder()
    {
        var ta = new TextArea().WithText("hello\nworld");
        Assert.Equal("hello\nworld", ta.Text());
        Assert.Equal(2, ta.LineCount());
    }

    [Fact]
    public void InsertTextAndNewline()
    {
        var ta = new TextArea();
        ta.InsertText("hello");
        ta.InsertNewline();
        ta.InsertText("world");
        Assert.Equal("hello\nworld", ta.Text());
        Assert.Equal(2, ta.LineCount());
    }

    [Fact]
    public void DeleteBackwardWorks()
    {
        var ta = new TextArea().WithText("hello");
        ta.MoveToDocumentEnd();
        ta.DeleteBackward();
        Assert.Equal("hell", ta.Text());
    }

    [Fact]
    public void CursorMovement()
    {
        var ta = new TextArea().WithText("abc\ndef\nghi");
        ta.MoveToDocumentStart();
        Assert.Equal(0, ta.Cursor().Line);
        Assert.Equal(0, ta.Cursor().Grapheme);

        ta.MoveDown();
        Assert.Equal(1, ta.Cursor().Line);

        ta.MoveToLineEnd();
        Assert.Equal(3, ta.Cursor().Grapheme);

        ta.MoveToDocumentEnd();
        Assert.Equal(2, ta.Cursor().Line);
    }

    [Fact]
    public void UndoRedo()
    {
        var ta = new TextArea();
        ta.InsertText("abc");
        Assert.Equal("abc", ta.Text());
        ta.Undo();
        Assert.Equal("", ta.Text());
        ta.Redo();
        Assert.Equal("abc", ta.Text());
    }

    [Fact]
    public void SelectionAndDelete()
    {
        var ta = new TextArea().WithText("hello world");
        ta.MoveToDocumentStart();
        for (int i = 0; i < 5; i++) ta.SelectRight();
        Assert.Equal("hello", ta.SelectedText());
        ta.DeleteBackward();
        Assert.Equal(" world", ta.Text());
    }

    [Fact]
    public void SelectAll()
    {
        var ta = new TextArea().WithText("abc\ndef");
        ta.SelectAll();
        Assert.Equal("abc\ndef", ta.SelectedText());
    }

    [Fact]
    public void SetTextResets()
    {
        var ta = new TextArea().WithText("old");
        ta.InsertText(" stuff");
        ta.SetText("new");
        Assert.Equal("new", ta.Text());
    }

    [Fact]
    public void ScrollFollowsCursor()
    {
        var ta = new TextArea();
        for (int i = 0; i < 50; i++) ta.InsertText($"line {i}\n");
        // Cursor should be at the bottom, scroll anchor adjusted.
        Assert.True(ta.ScrollAnchor.line > 0);
        Assert.True(ta.Cursor().Line >= 49);

        // Move to top
        ta.MoveToDocumentStart();
        Assert.Equal(0, ta.ScrollAnchor.line);
    }

    [Fact]
    public void GutterWidthWithoutLineNumbers()
    {
        var ta = new TextArea();
        Assert.Equal(0, ta.GutterWidth());
    }

    [Fact]
    public void GutterWidthWithLineNumbers()
    {
        var ta = new TextArea().WithLineNumbers(true);
        ta.InsertText("a\nb\nc");
        Assert.Equal(3, ta.GutterWidth()); // 1 digit + space + separator
    }

    [Fact]
    public void GutterWidthManyLines()
    {
        var ta = new TextArea().WithLineNumbers(true);
        for (int i = 0; i < 100; i++) ta.InsertText($"line {i}\n");
        Assert.Equal(5, ta.GutterWidth()); // 3 digits + space + separator
    }

    [Fact]
    public void FocusState()
    {
        var ta = new TextArea();
        Assert.False(ta.IsFocused());
        ta.SetFocused(true);
        Assert.True(ta.IsFocused());
    }

    [Fact]
    public void WordMovement()
    {
        var ta = new TextArea().WithText("hello world foo");
        ta.MoveToDocumentStart();
        ta.MoveWordRight();
        Assert.Equal(6, ta.Cursor().Grapheme);
        ta.MoveWordLeft();
        Assert.Equal(0, ta.Cursor().Grapheme);
    }

    [Fact]
    public void PageUpDown()
    {
        var ta = new TextArea();
        for (int i = 0; i < 50; i++) ta.InsertText($"line {i}\n");
        ta.MoveToDocumentStart();
        var state = new TextAreaState { LastViewportHeight = 10, LastViewportWidth = 80 };
        ta.PageDown(state);
        Assert.True(ta.Cursor().Line >= 10);
        ta.PageUp(state);
        Assert.Equal(0, ta.Cursor().Line);
    }

    [Fact]
    public void InsertReplacesSelection()
    {
        var ta = new TextArea().WithText("hello world");
        ta.MoveToDocumentStart();
        for (int i = 0; i < 5; i++) ta.SelectRight();
        ta.InsertText("goodbye");
        Assert.Equal("goodbye world", ta.Text());
    }

    [Fact]
    public void InsertSingleChar()
    {
        var ta = new TextArea();
        ta.InsertChar('X');
        Assert.Equal("X", ta.Text());
        Assert.Equal(1, ta.Cursor().Grapheme);
    }

    [Fact]
    public void InsertMultilineText()
    {
        var ta = new TextArea();
        ta.InsertText("line1\nline2\nline3");
        Assert.Equal(3, ta.LineCount());
        Assert.Equal(2, ta.Cursor().Line);
    }

    [Fact]
    public void DeleteForwardWorks()
    {
        var ta = new TextArea().WithText("hello");
        ta.MoveToDocumentStart();
        ta.DeleteForward();
        Assert.Equal("ello", ta.Text());
    }

    [Fact]
    public void DeleteBackwardAtLineStartJoinsLines()
    {
        var ta = new TextArea().WithText("abc\ndef");
        ta.MoveToDocumentStart();
        ta.MoveDown();
        ta.MoveToLineStart();
        ta.DeleteBackward();
        Assert.Equal("abcdef", ta.Text());
        Assert.Equal(1, ta.LineCount());
    }

    [Fact]
    public void CursorHorizontalMovement()
    {
        var ta = new TextArea().WithText("abc");
        ta.MoveToDocumentStart();
        ta.MoveRight();
        Assert.Equal(1, ta.Cursor().Grapheme);
        ta.MoveRight();
        Assert.Equal(2, ta.Cursor().Grapheme);
        ta.MoveLeft();
        Assert.Equal(1, ta.Cursor().Grapheme);
    }

    [Fact]
    public void CursorVerticalMaintainsColumn()
    {
        var ta = new TextArea().WithText("abcde\nfg\nhijkl");
        ta.MoveToDocumentStart();
        ta.MoveToLineEnd(); // col 5
        ta.MoveDown();      // line 1 only has 2 chars, should clamp
        Assert.Equal(1, ta.Cursor().Line);
        ta.MoveDown();      // line 2 has 5 chars, should restore col
        Assert.Equal(2, ta.Cursor().Line);
    }

    [Fact]
    public void SelectionShiftArrow()
    {
        var ta = new TextArea().WithText("abcdef");
        ta.MoveToDocumentStart();
        ta.SelectRight();
        ta.SelectRight();
        ta.SelectRight();
        Assert.Equal("abc", ta.SelectedText());
    }

    [Fact]
    public void SelectionExtendsUpDown()
    {
        var ta = new TextArea().WithText("line1\nline2\nline3");
        ta.MoveToDocumentStart();
        ta.SelectDown();
        var sel = ta.SelectedText();
        Assert.NotNull(sel);
        Assert.Contains('\n', sel);
    }

    [Fact]
    public void UndoChain()
    {
        var ta = new TextArea();
        ta.InsertText("a");
        ta.InsertText("b");
        ta.InsertText("c");
        Assert.Equal("abc", ta.Text());
        ta.Undo();
        ta.Undo();
        ta.Undo();
        Assert.Equal("", ta.Text());
    }

    [Fact]
    public void RedoDiscardedOnNewEdit()
    {
        var ta = new TextArea();
        ta.InsertText("abc");
        ta.Undo();
        ta.InsertText("xyz");
        ta.Redo(); // should be no-op
        Assert.Equal("xyz", ta.Text());
    }

    [Fact]
    public void ClearSelection()
    {
        var ta = new TextArea().WithText("hello");
        ta.SelectAll();
        Assert.NotNull(ta.Selection());
        ta.ClearSelection();
        Assert.Null(ta.Selection());
    }

    [Fact]
    public void DeleteWordBackward()
    {
        var ta = new TextArea().WithText("hello world");
        ta.MoveToDocumentEnd();
        ta.DeleteWordBackward();
        Assert.Equal("hello ", ta.Text());
    }

    [Fact]
    public void DeleteToEndOfLine()
    {
        var ta = new TextArea().WithText("hello world");
        ta.MoveToDocumentStart();
        ta.MoveRight(); // after 'h'
        ta.DeleteToEndOfLine();
        Assert.Equal("h", ta.Text());
    }

    [Fact]
    public void PlaceholderBuilder()
    {
        // Upstream: assert!(ta.is_empty()); assert_eq!(ta.placeholder, "Enter text...");
        var ta = new TextArea().WithPlaceholder("Enter text...");
        Assert.True(ta.IsEmpty());
        Assert.Equal("Enter text...", ta.Placeholder);
    }

    [Fact]
    public void SoftWrapBuilder()
    {
        // Upstream: assert!(ta.soft_wrap);
        var ta = new TextArea().WithSoftWrap(true);
        Assert.True(ta.SoftWrap);
    }

    [Fact]
    public void SoftWrapRendersWrappedLines()
    {
        var ta = new TextArea().WithSoftWrap(true).WithText("abcdef");
        var area = new Rect(0, 0, 3, 2);
        var (frame, _) = TextAreaTestHelpers.MakeFrame(3, 2);
        ta.Render(area, frame);

        Assert.Equal('a', (char)(frame.Buffer.Get(0, 0)!.Value.Content.AsRune()!.Value.Value));
        Assert.Equal('c', (char)(frame.Buffer.Get(2, 0)!.Value.Content.AsRune()!.Value.Value));
        Assert.Equal('d', (char)(frame.Buffer.Get(0, 1)!.Value.Content.AsRune()!.Value.Value));
        Assert.Equal('f', (char)(frame.Buffer.Get(2, 1)!.Value.Content.AsRune()!.Value.Value));
    }

    [Fact]
    public void MaxHeightBuilder()
    {
        // Upstream: assert_eq!(ta.max_height, 10);
        var ta = new TextArea().WithMaxHeight(10);
        Assert.Equal(10, ta.MaxHeight);
    }

    [Fact]
    public void EditorAccess()
    {
        var ta = new TextArea().WithText("test");
        Assert.Equal("test", ta.Editor().Text());
        ta.EditorMut().InsertChar('!');
        Assert.Contains('!', ta.Text());
    }

    [Fact]
    public void MoveToLineStartAndEnd()
    {
        var ta = new TextArea().WithText("hello world");
        ta.MoveToDocumentStart();
        ta.MoveToLineEnd();
        Assert.Equal(11, ta.Cursor().Grapheme);
        ta.MoveToLineStart();
        Assert.Equal(0, ta.Cursor().Grapheme);
    }

    [Fact]
    public void RenderEmptyWithPlaceholder()
    {
        var ta = new TextArea().WithPlaceholder("Type here").WithFocus(true);
        var (frame, _) = TextAreaTestHelpers.MakeFrame(20, 5);
        var area = new Rect(0, 0, 20, 5);
        ta.Render(area, frame);
        // Placeholder should be rendered
        var cell = frame.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        Assert.Equal('T', (char)(cell!.Value.Content.AsRune()!.Value.Value));
        // Cursor should be set
        Assert.True(frame.CursorPosition.HasValue);
    }

    [Fact]
    public void RenderWithContent()
    {
        var ta = new TextArea().WithText("abc\ndef").WithFocus(true);
        var (frame, _) = TextAreaTestHelpers.MakeFrame(20, 5);
        var area = new Rect(0, 0, 20, 5);
        ta.Render(area, frame);
        var cell = frame.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        Assert.Equal('a', (char)(cell!.Value.Content.AsRune()!.Value.Value));
    }

    [Fact]
    public void RenderShorterTextClearsStaleLines()
    {
        var area = new Rect(0, 0, 8, 3);
        var (frame, _) = TextAreaTestHelpers.MakeFrame(8, 3);

        new TextArea().WithText("abcdef\nghijkl").Render(area, frame);
        new TextArea().WithText("hi").Render(area, frame);

        Assert.Equal("hi      ", TextAreaTestHelpers.RawRowText(frame, 0, 8));
        Assert.Equal("        ", TextAreaTestHelpers.RawRowText(frame, 1, 8));
        Assert.Equal("        ", TextAreaTestHelpers.RawRowText(frame, 2, 8));
    }

    [Fact]
    public void RenderLineNumbersWithoutStyling()
    {
        var ta = new TextArea().WithText("a\nb").WithLineNumbers(true);
        var (frame, _) = TextAreaTestHelpers.MakeFrame(8, 2);
        frame.SetDegradation(DegradationLevel.NoStyling);
        ta.Render(new Rect(0, 0, 8, 2), frame);
        var cell = frame.Buffer.Get(0, 0);
        Assert.NotNull(cell);
        Assert.Equal('1', (char)(cell!.Value.Content.AsRune()!.Value.Value));
    }

    [Fact]
    public void StatefulRenderUpdatesViewportState()
    {
        var ta    = new TextArea();
        var state = new TextAreaState();
        var (frame, _) = TextAreaTestHelpers.MakeFrame(10, 3);
        var area = new Rect(0, 0, 10, 3);

        ta.Render(area, frame, state);

        Assert.Equal(3,  state.LastViewportHeight);
        Assert.Equal(10, state.LastViewportWidth);
    }

    [Fact]
    public void RenderZeroAreaNoPanic()
    {
        var ta = new TextArea().WithText("test");
        var (frame, _) = TextAreaTestHelpers.MakeFrame(10, 10);
        ta.Render(new Rect(0, 0, 0, 0), frame); // must not throw
    }

    [Fact]
    public void IsEssential()
    {
        var ta = new TextArea();
        Assert.True(ta.IsEssential());
    }

    [Fact]
    public void DefaultImpl()
    {
        var ta = new TextArea();
        Assert.True(ta.IsEmpty());
    }

    [Fact]
    public void InsertNewlineSplitsLine()
    {
        var ta = new TextArea().WithText("abcdef");
        ta.MoveToDocumentStart();
        ta.MoveRight();
        ta.MoveRight();
        ta.MoveRight();
        ta.InsertNewline();
        Assert.Equal(2, ta.LineCount());
        Assert.Equal(1, ta.Cursor().Line);
    }

    [Fact]
    public void UnicodeGraphemeCluster()
    {
        var ta = new TextArea();
        ta.InsertText("café");
        // 'é' is a single grapheme even if composed
        Assert.Equal("café", ta.Text());
    }
}

// ── mod proptests (ported as Theory with fixed representative inputs) ─────────

public sealed class TextAreaPropTests
{
    // DIVERGENCE: Rust uses proptest! { #[test] fn insert_delete_inverse(text in "[a-zA-Z0-9 ]{1,50}") }
    // C# uses [Theory] with fixed representative inputs; proptest is not available in this repo.

    [Theory]
    [InlineData("hello")]
    [InlineData("abc def")]
    [InlineData("x")]
    [InlineData("HelloWorld123")]
    [InlineData("the quick brown fox")]
    public void InsertDeleteInverse(string text)
    {
        var ta = new TextArea();
        ta.InsertText(text);
        // Delete all characters backwards — must use grapheme count (text.Length chars ≥ graphemes)
        int maxDeletes = text.Length + 2;
        for (int i = 0; i < maxDeletes; i++)
            ta.DeleteBackward();
        Assert.True(ta.IsEmpty() || ta.Text().Length == 0);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("abc")]
    [InlineData("xyz123")]
    [InlineData("test")]
    public void UndoRedoInverse(string text)
    {
        var ta = new TextArea();
        ta.InsertText(text);
        string afterInsert = ta.Text();
        ta.Undo();
        ta.Redo();
        Assert.Equal(afterInsert, ta.Text());
    }

    [Theory]
    [InlineData(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
    [InlineData(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 })]
    [InlineData(new byte[] { 0, 0, 1, 1, 2, 2, 3, 3 })]
    [InlineData(new byte[] { 7, 7, 7, 5, 5, 5, 0, 0, 0 })]
    public void CursorAlwaysValid(byte[] ops)
    {
        var ta = new TextArea().WithText("abc\ndef\nghi\njkl");
        foreach (byte op in ops)
        {
            switch (op % 10)
            {
                case 0: ta.MoveLeft();           break;
                case 1: ta.MoveRight();          break;
                case 2: ta.MoveUp();             break;
                case 3: ta.MoveDown();           break;
                case 4: ta.MoveToLineStart();    break;
                case 5: ta.MoveToLineEnd();      break;
                case 6: ta.MoveToDocumentStart();break;
                case 7: ta.MoveToDocumentEnd();  break;
                case 8: ta.MoveWordLeft();       break;
                default: ta.MoveWordRight();     break;
            }
            var cursor = ta.Cursor();
            Assert.True(cursor.Line < ta.LineCount(),
                $"cursor line {cursor.Line} >= line_count {ta.LineCount()}");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(19)]
    public void SelectionOrdered(int n)
    {
        var ta = new TextArea().WithText("hello world foo bar");
        ta.MoveToDocumentStart();
        for (int i = 0; i < n; i++) ta.SelectRight();
        var sel = ta.Selection();
        if (sel is Selection s)
        {
            Assert.True(s.Anchor.Line < s.Head.Line
                || (s.Anchor.Line == s.Head.Line && s.Anchor.Grapheme <= s.Head.Grapheme));
        }
    }
}
