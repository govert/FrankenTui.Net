// Port of .external/frankentui/crates/ftui-widgets/src/textarea.rs
// Multi-line text editing widget with viewport scrolling, soft-wrap, and cursor display.
//
// DIVERGENCE: The upstream depends on ftui-text::editor::{Editor,Selection},
// ftui-text::{CursorPosition,CursorNavigator}, and ftui-text::rope::Rope.
// None of those crates have been ported to a standalone .NET library yet.
// This port implements the required Rope, CursorPosition, CursorNavigator,
// Selection, and Editor types in TextAreaInternals.cs in the sub-namespace
// FrankenTui.Widgets.TextAreaInternals so they remain accessible to TextAreaTests.cs
// without polluting the public FrankenTui.Widgets surface.
//
// DIVERGENCE: Rust uses std::cell::Cell<T> for interior-mutability of
// scroll_anchor, scroll_left, last_viewport_height, and last_viewport_width
// during rendering (which takes &self). C# TextArea uses plain fields; render
// takes `this` (a reference), so this is equivalent with no thread-safety loss.
//
// DIVERGENCE: Upstream proptest-based property tests are ported as [Theory] with
// a fixed set of representative inputs. proptest is not available for xUnit in this
// repository.

using System.Globalization;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets.TextAreaInternals;

namespace FrankenTui.Widgets;

// ============================================================================
// TextArea widget  (textarea.rs)
// ============================================================================

/// <summary>
/// Render state tracked across frames.
/// Port of ftui_widgets::textarea::TextAreaState.
/// </summary>
public sealed class TextAreaState
{
    /// <summary>Viewport height from last render.</summary>
    public ushort LastViewportHeight { get; set; }
    /// <summary>Viewport width from last render.</summary>
    public ushort LastViewportWidth  { get; set; }
}

/// <summary>
/// Multi-line text editor widget.
/// Port of ftui_widgets::textarea::TextArea.
/// </summary>
public sealed class TextArea : IWidget, IStatefulWidget<TextAreaState>
{
    // ── Fields ────────────────────────────────────────────────────────

    private Editor _editor;
    /// <summary>Placeholder text shown when empty.</summary>
    private string _placeholder;
    /// <summary>Whether the widget has input focus.</summary>
    private bool _focused;
    /// <summary>Show line numbers in gutter.</summary>
    private bool _showLineNumbers;
    /// <summary>Base style.</summary>
    private WidgetStyle _style;
    /// <summary>Cursor line highlight style.</summary>
    private WidgetStyle? _cursorLineStyle;
    /// <summary>Selection highlight style.</summary>
    private WidgetStyle _selectionStyle;
    /// <summary>Placeholder style.</summary>
    private WidgetStyle _placeholderStyle;
    /// <summary>Line number style.</summary>
    private WidgetStyle _lineNumberStyle;
    /// <summary>Soft-wrap long lines.</summary>
    private bool _softWrap;
    /// <summary>Maximum height in lines (0 = unlimited / fill area).</summary>
    private int _maxHeight;
    // DIVERGENCE: Rust uses std::cell::Cell<(usize,usize)> for interior mutability.
    // C# uses plain fields; render takes `this` via a non-const reference, equivalent.
    /// <summary>Viewport scroll anchor (logical_line_idx, visual_wrap_offset).</summary>
    private (int line, int vrow) _scrollAnchor;
    /// <summary>Horizontal scroll offset (visual columns).</summary>
    private int _scrollLeft;
    /// <summary>Last viewport height for page movement and visibility checks.</summary>
    private int _lastViewportHeight;
    /// <summary>Last viewport width for visibility checks.</summary>
    private int _lastViewportWidth;

    // ── Constructor / Default ─────────────────────────────────────────

    /// <summary>Create a new empty text area.</summary>
    public TextArea()
    {
        _editor             = new Editor();
        _placeholder        = "";
        _focused            = false;
        _showLineNumbers    = false;
        _style              = WidgetStyle.Default;
        _cursorLineStyle    = null;
        _selectionStyle     = new WidgetStyle(null, null, CellStyleFlags.Reverse);
        _placeholderStyle   = new WidgetStyle(null, null, CellStyleFlags.Dim);
        _lineNumberStyle    = new WidgetStyle(null, null, CellStyleFlags.Dim);
        _softWrap           = false;
        _maxHeight          = 0;
        _scrollAnchor       = (int.MaxValue, 0); // sentinel
        _scrollLeft         = 0;
        _lastViewportHeight = 0;
        _lastViewportWidth  = 0;
    }

    // ── Event Handling ────────────────────────────────────────────────

    /// <summary>
    /// Handle a terminal event.
    /// Returns <c>true</c> if the state changed.
    /// </summary>
    public bool HandleEvent(InputEvent @event)
    {
        return @event switch
        {
            InputEvent.Key k when k.KeyEvent.Kind is KeyEventKind.Press or KeyEventKind.Repeat
                => HandleKey(k.KeyEvent),
            InputEvent.Paste p => HandlePaste(p.PasteEvent),
            _                  => false,
        };
    }

    private bool HandlePaste(PasteEvent paste)
    {
        _editor.InsertText(paste.Text);
        EnsureCursorVisible();
        return true;
    }

    private bool HandleKey(KeyEvent key)
    {
        bool ctrl  = key.Ctrl();
        bool shift = key.Shift();

        switch (key.Code)
        {
            case KeyCode.Char ch when !ctrl:
                InsertChar(ch.Character);
                return true;
            case KeyCode.Enter:
                InsertNewline();
                return true;
            case KeyCode.Backspace:
                if (ctrl) DeleteWordBackward(); else DeleteBackward();
                return true;
            case KeyCode.Delete:
                if (ctrl) DeleteWordForward(); else DeleteForward();
                return true;
            case KeyCode.Left:
                if (ctrl && shift) SelectWordLeft();
                else if (ctrl)     MoveWordLeft();
                else if (shift)    SelectLeft();
                else               MoveLeft();
                return true;
            case KeyCode.Right:
                if (ctrl && shift) SelectWordRight();
                else if (ctrl)     MoveWordRight();
                else if (shift)    SelectRight();
                else               MoveRight();
                return true;
            case KeyCode.Up:
                if (shift) SelectUp(); else MoveUp();
                return true;
            case KeyCode.Down:
                if (shift) SelectDown(); else MoveDown();
                return true;
            case KeyCode.Home:
                MoveToLineStart();
                return true;
            case KeyCode.End:
                MoveToLineEnd();
                return true;
            case KeyCode.PageUp:
            {
                int page = Math.Max(_lastViewportHeight, 1);
                if (_softWrap) MoveCursorVisualUp(page, shift);
                else { for (int i = 0; i < page; i++) { if (shift) _editor.SelectUp(); else _editor.MoveUp(); } }
                EnsureCursorVisible();
                return true;
            }
            case KeyCode.PageDown:
            {
                int page = Math.Max(_lastViewportHeight, 1);
                if (_softWrap) MoveCursorVisualDown(page, shift);
                else { for (int i = 0; i < page; i++) { if (shift) _editor.SelectDown(); else _editor.MoveDown(); } }
                EnsureCursorVisible();
                return true;
            }
            case KeyCode.Char ctrlA when ctrl && ctrlA.Character == 'a':
                SelectAll();
                return true;
            case KeyCode.Char ctrlK when ctrl && ctrlK.Character == 'k':
                DeleteToEndOfLine();
                return true;
            case KeyCode.Char ctrlZ when ctrl && ctrlZ.Character == 'z':
                Undo();
                return true;
            case KeyCode.Char ctrlY when ctrl && ctrlY.Character == 'y':
                Redo();
                return true;
            default:
                return false;
        }
    }

    // ── Builder methods ───────────────────────────────────────────────

    /// <summary>Set initial text content (builder).</summary>
    public TextArea WithText(string text)
    {
        _editor = TextAreaInternals.Editor.WithText(text);
        _editor.MoveToDocumentStart();
        return this;
    }

    /// <summary>Set placeholder text (builder).</summary>
    public TextArea WithPlaceholder(string text)
    {
        _placeholder = text;
        return this;
    }

    /// <summary>Set focused state (builder).</summary>
    public TextArea WithFocus(bool focused)
    {
        _focused = focused;
        return this;
    }

    /// <summary>Enable line numbers (builder).</summary>
    public TextArea WithLineNumbers(bool show)
    {
        _showLineNumbers = show;
        return this;
    }

    /// <summary>Set base style (builder).</summary>
    public TextArea WithStyle(WidgetStyle style)
    {
        _style = style;
        return this;
    }

    /// <summary>Set cursor line highlight style (builder).</summary>
    public TextArea WithCursorLineStyle(WidgetStyle style)
    {
        _cursorLineStyle = style;
        return this;
    }

    /// <summary>Set selection style (builder).</summary>
    public TextArea WithSelectionStyle(WidgetStyle style)
    {
        _selectionStyle = style;
        return this;
    }

    /// <summary>Enable soft wrapping (builder).</summary>
    public TextArea WithSoftWrap(bool wrap)
    {
        _softWrap = wrap;
        return this;
    }

    /// <summary>Set maximum height in lines (builder). 0 = fill available area.</summary>
    public TextArea WithMaxHeight(int max)
    {
        _maxHeight = max;
        return this;
    }

    // ── State access ──────────────────────────────────────────────────

    /// <summary>Get the full text content.</summary>
    public string Text() => _editor.Text();

    /// <summary>Set the full text content (resets cursor and undo history).</summary>
    public void SetText(string text)
    {
        _editor.SetText(text);
        _scrollAnchor = (0, 0);
        _scrollLeft   = 0;
    }

    /// <summary>Number of lines.</summary>
    public int LineCount() => _editor.LineCount();

    /// <summary>Current cursor position.</summary>
    public CursorPosition Cursor() => _editor.Cursor();

    /// <summary>Set cursor position (clamped to bounds). Clears selection.</summary>
    public void SetCursorPosition(CursorPosition pos)
    {
        _editor.SetCursor(pos);
        EnsureCursorVisible();
    }

    /// <summary>Whether the textarea is empty.</summary>
    public bool IsEmpty() => _editor.IsEmpty();

    /// <summary>Current selection, if any.</summary>
    public Selection? Selection() => _editor.Selection();

    /// <summary>Get selected text.</summary>
    public string? SelectedText() => _editor.SelectedText();

    /// <summary>Whether the widget has focus.</summary>
    public bool IsFocused() => _focused;

    /// <summary>Set focus state.</summary>
    public void SetFocused(bool focused) => _focused = focused;

    /// <summary>Access the underlying editor.</summary>
    public Editor Editor() => _editor;

    /// <summary>Mutable access to the underlying editor.</summary>
    public Editor EditorMut() => _editor;

    // Test-access: expose scroll anchor for assertion in tests.
    // DIVERGENCE: Rust tests use pub(crate) struct field access.
    // C# exposes via a public property.
    public (int line, int vrow) ScrollAnchor => _scrollAnchor;

    // Test-access for builder-set fields: upstream tests directly read
    // ta.placeholder / ta.soft_wrap / ta.max_height (Rust struct fields are
    // pub-in-module inside #[cfg(test)]). C# exposes these as internal
    // properties via the existing InternalsVisibleTo("FrankenTui.Tests.Headless").
    internal string   Placeholder => _placeholder;
    internal bool     SoftWrap    => _softWrap;
    internal int      MaxHeight   => _maxHeight;

    // ── Editing operations (delegated to Editor) ──────────────────────

    /// <summary>Insert text at cursor.</summary>
    public void InsertText(string text) { _editor.InsertText(text); EnsureCursorVisible(); }

    /// <summary>Insert a single character.</summary>
    public void InsertChar(char ch) { _editor.InsertChar(ch); EnsureCursorVisible(); }

    /// <summary>Insert a newline.</summary>
    public void InsertNewline() { _editor.InsertNewline(); EnsureCursorVisible(); }

    /// <summary>Delete backward (backspace).</summary>
    public void DeleteBackward() { _editor.DeleteBackward(); EnsureCursorVisible(); }

    /// <summary>Delete forward (delete key).</summary>
    public void DeleteForward() { _editor.DeleteForward(); EnsureCursorVisible(); }

    /// <summary>Delete word backward (Ctrl+Backspace).</summary>
    public void DeleteWordBackward() { _editor.DeleteWordBackward(); EnsureCursorVisible(); }

    /// <summary>Delete word forward (Ctrl+Delete).</summary>
    public void DeleteWordForward() { _editor.DeleteWordForward(); EnsureCursorVisible(); }

    /// <summary>Delete to end of line (Ctrl+K).</summary>
    public void DeleteToEndOfLine() { _editor.DeleteToEndOfLine(); EnsureCursorVisible(); }

    /// <summary>Undo last edit.</summary>
    public void Undo() { _editor.Undo(); EnsureCursorVisible(); }

    /// <summary>Redo last undo.</summary>
    public void Redo() { _editor.Redo(); EnsureCursorVisible(); }

    // ── Navigation ────────────────────────────────────────────────────

    /// <summary>Move cursor left.</summary>
    public void MoveLeft() { _editor.MoveLeft(); EnsureCursorVisible(); }

    /// <summary>Move cursor right.</summary>
    public void MoveRight() { _editor.MoveRight(); EnsureCursorVisible(); }

    /// <summary>Move cursor up.</summary>
    public void MoveUp()
    {
        if (_softWrap) MoveCursorVisualUp(1, false);
        else _editor.MoveUp();
        EnsureCursorVisible();
    }

    /// <summary>Move cursor down.</summary>
    public void MoveDown()
    {
        if (_softWrap) MoveCursorVisualDown(1, false);
        else _editor.MoveDown();
        EnsureCursorVisible();
    }

    /// <summary>Move cursor left by word.</summary>
    public void MoveWordLeft() { _editor.MoveWordLeft(); EnsureCursorVisible(); }

    /// <summary>Move cursor right by word.</summary>
    public void MoveWordRight() { _editor.MoveWordRight(); EnsureCursorVisible(); }

    /// <summary>Extend selection left by word.</summary>
    public void SelectWordLeft() { _editor.SelectWordLeft(); EnsureCursorVisible(); }

    /// <summary>Extend selection right by word.</summary>
    public void SelectWordRight() { _editor.SelectWordRight(); EnsureCursorVisible(); }

    /// <summary>Move to start of line.</summary>
    public void MoveToLineStart() { _editor.MoveToLineStart(); EnsureCursorVisible(); }

    /// <summary>Move to end of line.</summary>
    public void MoveToLineEnd() { _editor.MoveToLineEnd(); EnsureCursorVisible(); }

    /// <summary>Move to start of document.</summary>
    public void MoveToDocumentStart() { _editor.MoveToDocumentStart(); EnsureCursorVisible(); }

    /// <summary>Move cursor to end of document.</summary>
    public void MoveToDocumentEnd() { _editor.MoveToDocumentEnd(); EnsureCursorVisible(); }

    private void MoveCursorVisualDown(int count, bool extendSelection)
    {
        int width = _lastViewportWidth;
        if (width == 0)
        {
            for (int i = 0; i < count; i++)
                if (extendSelection) _editor.SelectDown(); else _editor.MoveDown();
            return;
        }

        var rope = _editor.Rope();
        var cursor = _editor.Cursor();
        int remaining = count;

        string lineText = GetLineText(rope, cursor.Line);
        var (currentVRow, _) = CursorWrapPosition(lineText, width, cursor.VisualCol);

        var initialSlices = WrapLineSlices(lineText, width);
        int initialSliceStart = currentVRow < initialSlices.Count ? initialSlices[currentVRow].StartCol : 0;
        int targetScreenX = Math.Max(0, cursor.VisualCol - initialSliceStart);

        while (remaining > 0)
        {
            string lt = GetLineText(rope, cursor.Line);
            int wrapCount = MeasureWrapCount(lt, width);
            int availableInLine = Math.Max(0, Math.Max(0, wrapCount - 1) - currentVRow);

            if (remaining <= availableInLine)
            {
                currentVRow += remaining;
                remaining = 0;
            }
            else
            {
                remaining -= availableInLine + 1;
                if (cursor.Line + 1 < _editor.LineCount())
                {
                    cursor.Line += 1;
                    currentVRow = 0;
                }
                else
                {
                    currentVRow = Math.Max(0, wrapCount - 1);
                    remaining = 0;
                }
            }
        }

        string finalLineText = GetLineText(rope, cursor.Line);
        var slices = WrapLineSlices(finalLineText, width);
        if (currentVRow < slices.Count)
        {
            var slice = slices[currentVRow];
            int targetInSlice = Math.Min(targetScreenX, slice.Width);
            var (gIdx, _) = FindGraphemeAtWidth(slice.Text, targetInSlice);
            var nav = new CursorNavigator(rope);
            int lineStartByte = nav.ToByteIndex(nav.FromLineGrapheme(cursor.Line, 0));
            int sliceByteOffset = Encoding.UTF8.GetByteCount(slice.Text[..GrapemeByteLength(slice.Text, gIdx)]);
            int finalByte = lineStartByte + slice.StartByte + sliceByteOffset;
            cursor = nav.FromByteIndex(finalByte);
        }

        if (extendSelection) _editor.ExtendSelectionTo(cursor);
        else _editor.SetCursor(cursor);
    }

    private void MoveCursorVisualUp(int count, bool extendSelection)
    {
        int width = _lastViewportWidth;
        if (width == 0)
        {
            for (int i = 0; i < count; i++)
                if (extendSelection) _editor.SelectUp(); else _editor.MoveUp();
            return;
        }

        var rope = _editor.Rope();
        var cursor = _editor.Cursor();
        int remaining = count;

        string lineText = GetLineText(rope, cursor.Line);
        var (currentVRow, _) = CursorWrapPosition(lineText, width, cursor.VisualCol);

        var initialSlices = WrapLineSlices(lineText, width);
        int initialSliceStart = currentVRow < initialSlices.Count ? initialSlices[currentVRow].StartCol : 0;
        int targetScreenX = Math.Max(0, cursor.VisualCol - initialSliceStart);

        while (remaining > 0)
        {
            if (remaining <= currentVRow)
            {
                currentVRow -= remaining;
                remaining = 0;
            }
            else
            {
                remaining -= currentVRow + 1;
                if (cursor.Line > 0)
                {
                    cursor.Line -= 1;
                    string lt = GetLineText(rope, cursor.Line);
                    int wrapCount = MeasureWrapCount(lt, width);
                    currentVRow = Math.Max(0, wrapCount - 1);
                }
                else
                {
                    currentVRow = 0;
                    remaining = 0;
                }
            }
        }

        string finalLineText = GetLineText(rope, cursor.Line);
        var slices = WrapLineSlices(finalLineText, width);
        if (currentVRow < slices.Count)
        {
            var slice = slices[currentVRow];
            int targetInSlice = Math.Min(targetScreenX, slice.Width);
            var (gIdx, _) = FindGraphemeAtWidth(slice.Text, targetInSlice);
            var nav = new CursorNavigator(rope);
            int lineStartByte = nav.ToByteIndex(nav.FromLineGrapheme(cursor.Line, 0));
            int sliceByteOffset = Encoding.UTF8.GetByteCount(slice.Text[..GrapemeByteLength(slice.Text, gIdx)]);
            int finalByte = lineStartByte + slice.StartByte + sliceByteOffset;
            cursor = nav.FromByteIndex(finalByte);
        }

        if (extendSelection) _editor.ExtendSelectionTo(cursor);
        else _editor.SetCursor(cursor);
    }

    // Helper: get line text trimmed of trailing newlines
    private static string GetLineText(Rope rope, int lineIdx)
    {
        string? raw = rope.Line(lineIdx);
        if (raw is null) return "";
        return raw.TrimEnd('\n', '\r');
    }

    // ── Selection ─────────────────────────────────────────────────────

    /// <summary>Extend selection left.</summary>
    public void SelectLeft()  { _editor.SelectLeft();  EnsureCursorVisible(); }

    /// <summary>Extend selection right.</summary>
    public void SelectRight() { _editor.SelectRight(); EnsureCursorVisible(); }

    /// <summary>Extend selection up.</summary>
    public void SelectUp()
    {
        if (_softWrap) MoveCursorVisualUp(1, true); else _editor.SelectUp();
        EnsureCursorVisible();
    }

    /// <summary>Extend selection down.</summary>
    public void SelectDown()
    {
        if (_softWrap) MoveCursorVisualDown(1, true); else _editor.SelectDown();
        EnsureCursorVisible();
    }

    /// <summary>Select all.</summary>
    public void SelectAll() { _editor.SelectAll(); }

    /// <summary>Clear selection.</summary>
    public void ClearSelection() { _editor.ClearSelection(); }

    // ── Viewport management ───────────────────────────────────────────

    /// <summary>Page up (move viewport and cursor up by viewport height).</summary>
    public void PageUp(TextAreaState state)
    {
        int page = Math.Max((int)state.LastViewportHeight, 1);
        int textAreaWidth = Math.Max(0, (int)state.LastViewportWidth - (int)GutterWidth());
        _lastViewportHeight = page;
        _lastViewportWidth  = textAreaWidth;
        if (_softWrap)
        {
            if (textAreaWidth > 0) MoveCursorVisualUp(page, false);
            else { for (int i = 0; i < page; i++) _editor.MoveUp(); }
        }
        else { for (int i = 0; i < page; i++) _editor.MoveUp(); }
        EnsureCursorVisible();
    }

    /// <summary>Page down (move viewport and cursor down by viewport height).</summary>
    public void PageDown(TextAreaState state)
    {
        int page = Math.Max((int)state.LastViewportHeight, 1);
        int textAreaWidth = Math.Max(0, (int)state.LastViewportWidth - (int)GutterWidth());
        _lastViewportHeight = page;
        _lastViewportWidth  = textAreaWidth;
        if (_softWrap)
        {
            if (textAreaWidth > 0) MoveCursorVisualDown(page, false);
            else { for (int i = 0; i < page; i++) _editor.MoveDown(); }
        }
        else { for (int i = 0; i < page; i++) _editor.MoveDown(); }
        EnsureCursorVisible();
    }

    /// <summary>Width of the line number gutter.</summary>
    public ushort GutterWidth()
    {
        if (!_showLineNumbers) return 0;
        int count = Math.Max(LineCount(), 1);
        ushort digits = 0;
        while (count > 0) { digits++; count /= 10; }
        return (ushort)(digits + 2); // digit width + space + separator
    }

    // ── Wrap helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Count how many wrapped lines this text will occupy.
    /// Port of TextArea::measure_wrap_count.
    /// </summary>
    private static int MeasureWrapCount(string lineText, int maxWidth)
    {
        if (lineText.Length == 0) return 1;

        int count = 0;
        bool hasContent = false;

        RunWrappingLogic(lineText, maxWidth, (_, width, flush) =>
        {
            if (flush)
            {
                if (width > 0) count++;
                hasContent = false;
            }
            else
            {
                hasContent = true;
            }
        });

        if (hasContent || count == 0) count++;
        return count;
    }

    /// <summary>
    /// Core wrapping logic.
    /// Port of TextArea::run_wrapping_logic.
    /// </summary>
    private static void RunWrappingLogic(string lineText, int maxWidth, Action<int, int, bool> callback)
    {
        int currentWidth = 0;
        int byteCursor   = 0;

        foreach (string segment in SplitWordBounds(lineText))
        {
            int segLen   = Encoding.UTF8.GetByteCount(segment);
            int segWidth = SegmentDisplayWidth(segment);

            if (maxWidth > 0 && currentWidth + segWidth > maxWidth)
            {
                callback(byteCursor, currentWidth, true);
                currentWidth = 0;
            }

            if (maxWidth > 0 && segWidth > maxWidth)
            {
                foreach (string g in CursorNavigator.GetGraphemes(segment))
                {
                    int gWidth = CursorNavigator.DisplayWidth(g);
                    int gLen   = Encoding.UTF8.GetByteCount(g);

                    if (maxWidth > 0 && currentWidth + gWidth > maxWidth && currentWidth > 0)
                    {
                        callback(byteCursor, currentWidth, true);
                        currentWidth = 0;
                    }

                    currentWidth += gWidth;
                    byteCursor   += gLen;
                    callback(byteCursor, currentWidth, false);
                }
                continue;
            }

            currentWidth += segWidth;
            byteCursor   += segLen;
            callback(byteCursor, currentWidth, false);
        }
    }

    private sealed record WrappedSlice(string Text, int StartByte, int StartCol, int Width);

    /// <summary>
    /// Build wrap slices for a line.
    /// Port of TextArea::wrap_line_slices.
    /// </summary>
    private static List<WrappedSlice> WrapLineSlices(string lineText, int maxWidth)
    {
        if (lineText.Length == 0)
            return [new WrappedSlice("", 0, 0, 0)];

        var slices = new List<WrappedSlice>();
        int currentWidth   = 0;
        int sliceStartByte = 0;
        int sliceStartCol  = 0;
        int byteCursor     = 0;
        int colCursor      = 0;

        void PushCurrent()
        {
            if (byteCursor == sliceStartByte && currentWidth == 0) return;
            slices.Add(new WrappedSlice(
                lineText[Rope.ByteOffsetToCharOffset(lineText, sliceStartByte)
                        ..Rope.ByteOffsetToCharOffset(lineText, byteCursor)],
                sliceStartByte, sliceStartCol, currentWidth));
            sliceStartByte = byteCursor;
            sliceStartCol  = colCursor;
            currentWidth   = 0;
        }

        foreach (string segment in SplitWordBounds(lineText))
        {
            int segLen   = Encoding.UTF8.GetByteCount(segment);
            int segWidth = SegmentDisplayWidth(segment);

            if (maxWidth > 0 && currentWidth + segWidth > maxWidth)
                PushCurrent();

            if (maxWidth > 0 && segWidth > maxWidth)
            {
                foreach (string g in CursorNavigator.GetGraphemes(segment))
                {
                    int gWidth = CursorNavigator.DisplayWidth(g);
                    int gLen   = Encoding.UTF8.GetByteCount(g);

                    if (maxWidth > 0 && currentWidth + gWidth > maxWidth && currentWidth > 0)
                        PushCurrent();

                    currentWidth += gWidth;
                    byteCursor   += gLen;
                    colCursor    += gWidth;
                }
                continue;
            }

            currentWidth += segWidth;
            byteCursor   += segLen;
            colCursor    += segWidth;
        }

        if (byteCursor > sliceStartByte || currentWidth > 0 || slices.Count == 0)
        {
            slices.Add(new WrappedSlice(
                lineText[Rope.ByteOffsetToCharOffset(lineText, sliceStartByte)
                        ..Rope.ByteOffsetToCharOffset(lineText, byteCursor)],
                sliceStartByte, sliceStartCol, currentWidth));
        }

        return slices;
    }

    private static (int vRow, int colInSlice) CursorWrapPosition(string lineText, int maxWidth, int cursorCol)
    {
        var slices = WrapLineSlices(lineText, maxWidth);
        if (slices.Count == 0) return (0, 0);

        for (int idx = 0; idx < slices.Count; idx++)
        {
            var slice = slices[idx];
            int endCol = slice.StartCol + slice.Width;
            bool isLast = idx == slices.Count - 1;
            if (cursorCol < endCol || (cursorCol == endCol && isLast))
            {
                int colInSlice = Math.Min(cursorCol - slice.StartCol, slice.Width);
                if (colInSlice < 0) colInSlice = 0;
                return (idx, colInSlice);
            }
        }
        return (0, 0);
    }

    // ── Horizontal scroll helpers ─────────────────────────────────────

    private int GetPrevCharWidth()
    {
        var cursor = _editor.Cursor();
        if (cursor.Grapheme == 0) return 0;
        var rope = _editor.Rope();
        string? line = rope.Line(cursor.Line);
        if (line is null) return 0;
        string[] gs = CursorNavigator.GetGraphemes(line.TrimEnd('\n', '\r'));
        int idx = cursor.Grapheme - 1;
        if (idx >= 0 && idx < gs.Length)
            return CursorNavigator.DisplayWidth(gs[idx]);
        return 0;
    }

    // ── Cursor visibility ─────────────────────────────────────────────

    private void EnsureCursorVisible()
    {
        var cursor = _editor.Cursor();
        int lastHeight = _lastViewportHeight;
        int vpHeight   = lastHeight == 0 ? 20 : lastHeight;
        int lastWidth  = _lastViewportWidth;
        int vpWidth    = lastWidth == 0 ? 80 : lastWidth;

        if (_scrollAnchor.line == int.MaxValue)
            _scrollAnchor = (0, 0);

        EnsureCursorVisibleInternal(vpHeight, vpWidth, cursor);
    }

    private void EnsureCursorVisibleInternal(int vpHeight, int vpWidth, CursorPosition cursor)
    {
        var (anchorLine, anchorVrow) = _scrollAnchor;

        if (!_softWrap)
        {
            // Vertical scroll (logical lines)
            if (cursor.Line < anchorLine)
                _scrollAnchor = (cursor.Line, 0);
            else if (vpHeight > 0 && cursor.Line >= anchorLine + vpHeight)
                _scrollAnchor = (Math.Max(0, cursor.Line - (vpHeight - 1)), 0);

            // Horizontal scroll
            int visualCol = cursor.VisualCol;
            if (visualCol < _scrollLeft)
                _scrollLeft = visualCol;
            else if (vpWidth > 0 && visualCol >= _scrollLeft + vpWidth)
            {
                int candidateScroll = Math.Max(0, visualCol - (vpWidth - 1));
                int prevWidth       = GetPrevCharWidth();
                int maxScrollForPrev = Math.Max(0, visualCol - prevWidth);
                _scrollLeft = vpWidth > prevWidth
                    ? Math.Min(candidateScroll, maxScrollForPrev)
                    : candidateScroll;
            }
            return;
        }

        // Soft wrap logic
        var rope = _editor.Rope();

        // 1. Is cursor before anchor?
        if (cursor.Line < anchorLine)
        {
            string lt = GetLineText(rope, cursor.Line);
            var (vRow, _) = CursorWrapPosition(lt, vpWidth, cursor.VisualCol);
            _scrollAnchor = (cursor.Line, vRow);
            return;
        }

        if (cursor.Line == anchorLine)
        {
            string lt = GetLineText(rope, cursor.Line);
            var (vRow, _) = CursorWrapPosition(lt, vpWidth, cursor.VisualCol);
            if (vRow < anchorVrow)
            {
                _scrollAnchor = (cursor.Line, vRow);
                return;
            }
        }

        // 2. Is cursor after viewport? Trace forward from anchor.
        int visualRowsCapacity = vpHeight;
        int currentLine   = anchorLine;
        int currentVStart = anchorVrow;

        while (true)
        {
            if (currentLine > cursor.Line) return; // visible

            string lt = GetLineText(rope, currentLine);
            int wrapCount = MeasureWrapCount(lt, vpWidth);

            if (currentLine == cursor.Line)
            {
                var (cursorVRow, _) = CursorWrapPosition(lt, vpWidth, cursor.VisualCol);
                if (cursorVRow >= currentVStart)
                {
                    int displayedRowIndex = cursorVRow - currentVStart;
                    if (displayedRowIndex < visualRowsCapacity) return; // visible
                    break; // not visible (below)
                }
                else return; // handled by step 1
            }

            int rowsRemainingInLine = Math.Max(0, wrapCount - currentVStart);
            if (rowsRemainingInLine >= visualRowsCapacity) break;

            visualRowsCapacity -= rowsRemainingInLine;
            currentLine++;
            currentVStart = 0;

            if (currentLine >= _editor.LineCount()) break;
        }

        // 3. Scroll Down (Backwards Scan)
        int needed   = vpHeight;
        int scanLine = cursor.Line;

        string cursorLineText = GetLineText(rope, scanLine);
        var (cursorV, _) = CursorWrapPosition(cursorLineText, vpWidth, cursor.VisualCol);

        int rowsAbove = cursorV + 1;
        if (rowsAbove >= needed)
        {
            int newVStart = cursorV + 1 - needed;
            _scrollAnchor = (scanLine, newVStart);
            return;
        }

        needed -= rowsAbove;

        while (scanLine > 0)
        {
            scanLine--;
            string lt       = GetLineText(rope, scanLine);
            int wrapCount   = MeasureWrapCount(lt, vpWidth);
            if (wrapCount >= needed)
            {
                int newVStart = wrapCount - needed;
                _scrollAnchor = (scanLine, newVStart);
                return;
            }
            needed -= wrapCount;
        }

        _scrollAnchor = (0, 0);
    }

    // ── IWidget.Render ────────────────────────────────────────────────

    /// <summary>Render the text area widget.</summary>
    public void Render(Rect area, Frame frame)
    {
        if (area.Width < 1 || area.Height < 1) return;

        _lastViewportHeight = area.Height;

        var deg       = frame.Degradation;
        var baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, area, baseStyle);

        ushort gutterW   = GutterWidth();
        ushort textAreaX = (ushort)(area.X + gutterW);
        int    textAreaW = Math.Max(0, area.Width - gutterW);
        int    vpHeight  = area.Height;

        _lastViewportWidth = textAreaW;

        var cursor = _editor.Cursor();
        EnsureCursorVisible();

        var (scrollTopLine, scrollTopVrow) = _scrollAnchor;
        int scrollLeft = _scrollLeft;

        var rope = _editor.Rope();
        var nav  = new CursorNavigator(rope);

        // Selection byte range for highlighting
        (int selStart, int selEnd)? selRange = null;
        if (_editor.Selection() is Selection sel && !sel.IsEmpty())
        {
            var (a, b) = sel.ByteRange(nav);
            selRange = (a, b);
        }

        // Show placeholder if empty
        if (_editor.IsEmpty() && _placeholder.Length > 0)
        {
            var pStyle = deg.ApplyStyling() ? _placeholderStyle : WidgetStyle.Default;
            WidgetDrawing.DrawTextSpan(frame, textAreaX, area.Y, _placeholder, pStyle, area.Right);
            if (_focused)
                frame.SetCursor(((ushort)textAreaX, area.Y));
            return;
        }

        if (_softWrap)
        {
            _scrollLeft = 0;
            scrollLeft  = 0;

            // Pre-calculate cursor wrap position
            string cursorLineText = GetLineText(rope, cursor.Line);
            var (cursorWrapIdx, cursorColInWrap) =
                CursorWrapPosition(cursorLineText, textAreaW, cursor.VisualCol);

            ushort currentY = area.Y;
            ushort bottomY  = area.Bottom;
            int lineCount   = _editor.LineCount();

            for (int lineIdx = scrollTopLine; lineIdx < lineCount; lineIdx++)
            {
                if (currentY >= bottomY) break;

                string lineText = GetLineText(rope, lineIdx);
                int lineStartByte = nav.ToByteIndex(nav.FromLineGrapheme(lineIdx, 0));
                var slices = WrapLineSlices(lineText, textAreaW);

                int startSlice = lineIdx == scrollTopLine ? scrollTopVrow : 0;

                for (int sliceIdx = startSlice; sliceIdx < slices.Count; sliceIdx++)
                {
                    if (currentY >= bottomY) break;

                    var slice = slices[sliceIdx];

                    // Line number gutter (only for first wrapped slice)
                    if (_showLineNumbers && sliceIdx == 0)
                    {
                        var lnStyle = deg.ApplyStyling() ? _lineNumberStyle : WidgetStyle.Default;
                        int digitW = gutterW - 2;
                        string numStr = (lineIdx + 1).ToString().PadLeft(digitW) + " ";
                        WidgetDrawing.DrawTextSpan(frame, area.X, currentY, numStr, lnStyle, textAreaX);
                    }

                    // Cursor line highlight
                    if (lineIdx == cursor.Line && sliceIdx == cursorWrapIdx
                        && _cursorLineStyle is WidgetStyle clStyle && deg.ApplyStyling())
                    {
                        for (ushort cx = textAreaX; cx < area.Right; cx++)
                        {
                            var cell = frame.Buffer.Get(cx, currentY);
                            if (cell is Cell c)
                            {
                                var mc = c;
                                WidgetDrawing.ApplyStyle(ref mc, clStyle);
                                frame.Buffer.SetFast(cx, currentY, mc);
                            }
                        }
                    }

                    // Render graphemes inside the wrapped slice
                    int visualX = 0;
                    int graphemeByteOffset = lineStartByte + slice.StartByte;

                    foreach (string g in CursorNavigator.GetGraphemes(slice.Text))
                    {
                        int gWidth = CursorNavigator.DisplayWidth(g);
                        int gByteLen = Encoding.UTF8.GetByteCount(g);

                        if (visualX >= textAreaW) break;

                        ushort px = (ushort)(textAreaX + visualX);

                        var gStyle = baseStyle;
                        if (selRange is { } sr
                            && graphemeByteOffset >= sr.selStart
                            && graphemeByteOffset < sr.selEnd
                            && deg.ApplyStyling())
                        {
                            gStyle = MergeStyles(gStyle, _selectionStyle);
                        }

                        if (gWidth > 0)
                            WidgetDrawing.DrawTextSpan(frame, px, currentY, g, gStyle, area.Right);

                        visualX            += gWidth;
                        graphemeByteOffset += gByteLen;
                    }

                    // Set cursor position if focused
                    if (_focused && lineIdx == cursor.Line && sliceIdx == cursorWrapIdx)
                    {
                        ushort cursorScreenX = (ushort)(textAreaX + cursorColInWrap);
                        if (cursorScreenX < area.Right)
                            frame.SetCursor((cursorScreenX, currentY));
                    }

                    currentY++;
                }
            }

            return;
        }

        // No soft wrap
        for (int row = 0; row < vpHeight; row++)
        {
            int lineIdx = scrollTopLine + row;
            ushort y = (ushort)(area.Y + row);

            if (lineIdx >= _editor.LineCount()) break;

            // Line number gutter
            if (_showLineNumbers)
            {
                var lnStyle = deg.ApplyStyling() ? _lineNumberStyle : WidgetStyle.Default;
                int digitW  = gutterW - 2;
                string numStr = (lineIdx + 1).ToString().PadLeft(digitW) + " ";
                WidgetDrawing.DrawTextSpan(frame, area.X, y, numStr, lnStyle, textAreaX);
            }

            // Cursor line highlight
            if (lineIdx == cursor.Line && _cursorLineStyle is WidgetStyle cls && deg.ApplyStyling())
            {
                for (ushort cx = textAreaX; cx < area.Right; cx++)
                {
                    var cell = frame.Buffer.Get(cx, y);
                    if (cell is Cell c)
                    {
                        var mc = c;
                        WidgetDrawing.ApplyStyle(ref mc, cls);
                        frame.Buffer.SetFast(cx, y, mc);
                    }
                }
            }

            string lineText = GetLineText(rope, lineIdx);
            int lineStartByte = nav.ToByteIndex(nav.FromLineGrapheme(lineIdx, 0));
            int visualX = 0;
            int graphemeByteOffset = lineStartByte;

            foreach (string g in CursorNavigator.GetGraphemes(lineText))
            {
                int gWidth   = CursorNavigator.DisplayWidth(g);
                int gByteLen = Encoding.UTF8.GetByteCount(g);

                // Determine style
                var gStyle = baseStyle;
                if (selRange is { } sr
                    && graphemeByteOffset >= sr.selStart
                    && graphemeByteOffset < sr.selEnd
                    && deg.ApplyStyling())
                {
                    gStyle = MergeStyles(gStyle, _selectionStyle);
                }

                // Skip graphemes before horizontal scroll
                if (visualX + gWidth <= scrollLeft)
                {
                    visualX            += gWidth;
                    graphemeByteOffset += gByteLen;
                    continue;
                }

                // Handle partial overlap at left edge
                if (visualX < scrollLeft)
                {
                    int endX        = visualX + gWidth;
                    int visibleWidth = endX - scrollLeft;
                    for (int i = 0; i < visibleWidth; i++)
                    {
                        ushort px = (ushort)(textAreaX + i);
                        if (px < area.Right)
                            WidgetDrawing.DrawTextSpan(frame, px, y, " ", gStyle, area.Right);
                    }
                    visualX            += gWidth;
                    graphemeByteOffset += gByteLen;
                    continue;
                }

                int screenX = visualX - scrollLeft;
                if (screenX >= textAreaW) break;

                ushort gpx = (ushort)(textAreaX + screenX);
                if (gWidth > 0)
                    WidgetDrawing.DrawTextSpan(frame, gpx, y, g, gStyle, area.Right);

                visualX            += gWidth;
                graphemeByteOffset += gByteLen;
            }
        }

        // Set cursor position if focused
        if (_focused)
        {
            int cursorRow = cursor.Line - scrollTopLine;
            if (cursorRow >= 0 && cursorRow < vpHeight)
            {
                ushort cursorScreenX = (ushort)(cursor.VisualCol - scrollLeft + textAreaX);
                ushort cursorScreenY = (ushort)(area.Y + cursorRow);
                if (cursorScreenX < area.Right && cursorScreenY < area.Bottom)
                    frame.SetCursor((cursorScreenX, cursorScreenY));
            }
        }
    }

    /// <summary>TextArea is essential.</summary>
    public bool IsEssential() => true;

    // ── IStatefulWidget<TextAreaState>.Render ─────────────────────────

    /// <summary>Render updating the external state record.</summary>
    public void Render(Rect area, Frame frame, TextAreaState state)
    {
        state.LastViewportHeight = area.Height;
        state.LastViewportWidth  = area.Width;
        Render(area, frame);
    }

    // ── Internal static helpers ───────────────────────────────────────

    private static WidgetStyle MergeStyles(WidgetStyle a, WidgetStyle b)
    {
        var fg    = b.Fg ?? a.Fg;
        var bg    = b.Bg ?? a.Bg;
        CellStyleFlags? attrs = (a.Attrs, b.Attrs) switch
        {
            (CellStyleFlags af, CellStyleFlags bf) => af | bf,
            (null, CellStyleFlags bf)              => bf,
            (CellStyleFlags af, null)              => af,
            _                                      => null,
        };
        return new WidgetStyle(fg, bg, attrs);
    }

    private static int SegmentDisplayWidth(string segment)
    {
        int w = 0;
        foreach (string g in CursorNavigator.GetGraphemes(segment))
            w += CursorNavigator.DisplayWidth(g);
        return w;
    }

    /// <summary>
    /// Split a string into word-bound segments.
    /// Approximates Rust's unicode_segmentation::UnicodeSegmentation::split_word_bounds.
    /// DIVERGENCE: .NET does not expose Unicode word-break segmentation directly;
    /// we approximate using StringInfo grapheme clusters, grouping by category
    /// (letter/digit, whitespace, punctuation).
    /// </summary>
    private static IEnumerable<string> SplitWordBounds(string text)
    {
        if (text.Length == 0) yield break;
        string[] graphemes = CursorNavigator.GetGraphemes(text);
        if (graphemes.Length == 0) yield break;

        static byte GraphemeClass(string g)
        {
            if (CursorNavigator.IsWhitespace(g)) return 0;
            if (g.All(c => char.IsLetterOrDigit(c) || c == '_')) return 1;
            return 2;
        }

        byte curClass = GraphemeClass(graphemes[0]);
        var sb = new StringBuilder();
        sb.Append(graphemes[0]);

        for (int i = 1; i < graphemes.Length; i++)
        {
            byte cls = GraphemeClass(graphemes[i]);
            if (cls != curClass)
            {
                yield return sb.ToString();
                sb.Clear();
                curClass = cls;
            }
            sb.Append(graphemes[i]);
        }
        if (sb.Length > 0) yield return sb.ToString();
    }

    /// <summary>
    /// Find the grapheme index and accumulated width at the given target visual width in a string.
    /// Returns (grapheme_count, accumulated_width).
    /// </summary>
    private static (int gIdx, int accWidth) FindGraphemeAtWidth(string text, int targetWidth)
    {
        int vW   = 0;
        int gIdx = 0;
        foreach (string g in CursorNavigator.GetGraphemes(text))
        {
            int w = CursorNavigator.DisplayWidth(g);
            if (vW + w > targetWidth) break;
            vW   += w;
            gIdx += 1;
        }
        return (gIdx, vW);
    }

    /// <summary>
    /// Return the UTF-16 char length of the first gIdx graphemes of text.
    /// </summary>
    private static int GrapemeByteLength(string text, int gIdx)
    {
        // We need byte (UTF-16 char) length for the [..x] slice
        // but our underlying storage is C# strings (UTF-16).
        // Return char length: count chars in first gIdx graphemes.
        int charLen = 0;
        int i = 0;
        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext() && i < gIdx)
        {
            charLen += te.GetTextElement().Length;
            i++;
        }
        return charLen;
    }
}
