// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/input.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Single-line text input widget with cursor management, scrolling, selection,
// word-level operations, IME composition, and grapheme-cluster awareness.

using System.Globalization;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

// ============================================================================
// Event types needed by TextInput (ftui_core::event).
// DIVERGENCE: KeyCode, KeyEvent, KeyEventKind, KeyModifiers, MouseEvent,
//   MouseEventKind, MouseButton, and MouseButtonKind are defined in Mouse.cs
//   (ported from ftui-core/src/event.rs). Only the types NOT already present
//   are defined here: Event, ImeEvent, ImePhase, PasteEvent, ClipboardEvent,
//   ClipboardSource, Modifiers.
//
// KeyCode is defined as a simple enum (matching the existing Mouse.cs API),
// rather than a closed class hierarchy, to preserve backward compatibility
// with TextArea.cs, CommandPalette.cs, and Modal.cs.  The upstream
// KeyCode::Char(char) is represented as KeyCode.Char + KeyEvent.Character.
// ============================================================================

// ── Modifiers ──────────────────────────────────────────────────────────────
// DIVERGENCE: Upstream uses bitflags Modifiers. C# uses KeyModifiers (defined
// in Mouse.cs). Modifiers is an alias for KeyModifiers for clarity.
// Both names are valid in this file.

/// <summary>IME composition lifecycle phase.</summary>
public enum ImePhase
{
    /// <summary>Composition started; preedit is active.</summary>
    Start,
    /// <summary>Preedit text changed.</summary>
    Update,
    /// <summary>Composition committed text to the input stream.</summary>
    Commit,
    /// <summary>Composition was canceled.</summary>
    Cancel,
}

/// <summary>IME composition event. Port of ftui_core::event::ImeEvent.</summary>
public sealed record ImeEvent(ImePhase Phase, string Text)
{
    /// <summary>Create a composition-start event.</summary>
    public static ImeEvent Start_() => new(ImePhase.Start, "");
    /// <summary>Create a composition-update event.</summary>
    public static ImeEvent Update_(string preedit) => new(ImePhase.Update, preedit);
    /// <summary>Create a composition-commit event.</summary>
    public static ImeEvent Commit_(string text) => new(ImePhase.Commit, text);
    /// <summary>Create a composition-cancel event.</summary>
    public static ImeEvent Cancel_() => new(ImePhase.Cancel, "");
}

/// <summary>A paste event from bracketed paste mode. Port of ftui_core::event::PasteEvent.</summary>
public sealed record PasteEvent(string Text, bool Bracketed)
{
    /// <summary>Create a new paste event.</summary>
    public static PasteEvent New(string text, bool bracketed) => new(text, bracketed);
    /// <summary>Create a bracketed paste event (the common case).</summary>
    public static PasteEvent Bracketed_(string text) => new(text, true);
}

/// <summary>The source of clipboard content.</summary>
public enum ClipboardSource
{
    /// <summary>Clipboard content from OSC 52 protocol.</summary>
    Osc52,
    /// <summary>Unknown or unspecified source.</summary>
    Unknown,
}

/// <summary>A clipboard event from OSC 52 response.</summary>
public sealed record ClipboardEvent(string Content, ClipboardSource Source);

/// <summary>
/// Canonical input event. Port of ftui_core::event::Event.
/// Enum variants with associated data → closed class hierarchy per AGENTS.md.
/// </summary>
public abstract record InputEvent
{
    private InputEvent() { }

    /// <summary>A keyboard event.</summary>
    public sealed record Key(KeyEvent KeyEvent) : InputEvent;

    /// <summary>A mouse event.</summary>
    public sealed record Mouse(MouseEvent MouseEvent) : InputEvent;

    /// <summary>Terminal was resized.</summary>
    public sealed record Resize(ushort Width, ushort Height) : InputEvent;

    /// <summary>Paste event (from bracketed paste mode).</summary>
    public sealed record Paste(PasteEvent PasteEvent) : InputEvent;

    /// <summary>IME composition event (preedit lifecycle).</summary>
    public sealed record Ime(ImeEvent ImeEvent) : InputEvent;

    /// <summary>Focus gained or lost. true = focus gained, false = focus lost.</summary>
    public sealed record Focus(bool Focused) : InputEvent;

    /// <summary>Clipboard content received (optional, from OSC 52 response).</summary>
    public sealed record Clipboard(ClipboardEvent ClipboardEvent) : InputEvent;

    /// <summary>A tick event from the runtime.</summary>
    public sealed record Tick : InputEvent;

    /// <summary>Return a static label for the event type (for metrics/tracing).</summary>
    public string EventTypeLabel() => this switch
    {
        Key      => "key",
        Mouse    => "mouse",
        Resize   => "resize",
        Paste    => "paste",
        Ime      => "ime",
        Focus    => "focus",
        Clipboard => "clipboard",
        Tick     => "tick",
        _        => "unknown",
    };
}

// ============================================================================
// Snapshot for undo support (TextInputSnapshot struct in undo_support.rs scope)
// ============================================================================

/// <summary>Snapshot of TextInput state for undo.</summary>
public sealed class TextInputSnapshot
{
    public string  Value           { get; }
    public int     Cursor          { get; }
    public int?    SelectionAnchor { get; }

    public TextInputSnapshot(string value, int cursor, int? selectionAnchor)
    {
        Value = value; Cursor = cursor; SelectionAnchor = selectionAnchor;
    }
}

// ============================================================================
// TextInput widget
// ============================================================================

/// <summary>
/// A single-line text input widget.
///
/// A single-line text input field with cursor management, scrolling, selection,
/// word-level operations, and styling. Grapheme-cluster aware for correct
/// Unicode handling.
/// </summary>
public sealed class TextInput : IWidget, IAccessible, CanonicalA11y.IAccessible, ITextInputUndoExt
{
    // ── Fields ────────────────────────────────────────────────────────────

    /// <summary>Unique ID for undo tracking.</summary>
    private readonly UndoWidgetId _undoId;

    /// <summary>Text value.</summary>
    private string _value = "";

    /// <summary>Cursor position (grapheme index).</summary>
    private int _cursor;

    /// <summary>Scroll offset (visual cells) for horizontal scrolling.
    /// DIVERGENCE: Rust uses std::cell::Cell for interior mutability during render.
    /// C# uses a regular int field.</summary>
    private int _scrollCells;

    /// <summary>Selection anchor (grapheme index). When set, selection spans from anchor to cursor.</summary>
    private int? _selectionAnchor;

    /// <summary>Active IME composition text (preedit), if any.</summary>
    private string? _imeComposition;

    /// <summary>Placeholder text.</summary>
    private string _placeholder = "";

    /// <summary>Mask character for password mode.</summary>
    private char? _maskChar;

    /// <summary>Maximum length in graphemes (null = unlimited).</summary>
    private int? _maxLength;

    /// <summary>Base style.</summary>
    private WidgetStyle _style;

    /// <summary>Cursor style.</summary>
    private WidgetStyle _cursorStyle;

    /// <summary>Placeholder style.</summary>
    private WidgetStyle _placeholderStyle;

    /// <summary>Selection highlight style.</summary>
    private WidgetStyle _selectionStyle;

    /// <summary>Whether the input is focused (controls cursor output).</summary>
    private bool _focused;

    // ── Constructor ───────────────────────────────────────────────────────

    /// <summary>Create a new empty text input.</summary>
    public TextInput() { _undoId = UndoWidgetId.New(); }

    // ── Builder methods ───────────────────────────────────────────────────

    /// <summary>Set the text value (builder).</summary>
    public TextInput WithValue(string value)
    {
        _value = value;
        _cursor = GraphemeCount();
        _selectionAnchor = null;
        return this;
    }

    /// <summary>Set the placeholder text (builder).</summary>
    public TextInput WithPlaceholder(string placeholder) { _placeholder = placeholder; return this; }

    /// <summary>Set password mode with mask character (builder).</summary>
    public TextInput WithMask(char mask) { _maskChar = mask; return this; }

    /// <summary>Set maximum length in graphemes (builder).</summary>
    public TextInput WithMaxLength(int max) { _maxLength = max; return this; }

    /// <summary>Set base style (builder).</summary>
    public TextInput WithStyle(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set cursor style (builder).</summary>
    public TextInput WithCursorStyle(WidgetStyle style) { _cursorStyle = style; return this; }

    /// <summary>Set placeholder style (builder).</summary>
    public TextInput WithPlaceholderStyle(WidgetStyle style) { _placeholderStyle = style; return this; }

    /// <summary>Set selection style (builder).</summary>
    public TextInput WithSelectionStyle(WidgetStyle style) { _selectionStyle = style; return this; }

    /// <summary>Set whether the input is focused (builder).</summary>
    public TextInput WithFocused(bool focused) { _focused = focused; return this; }

    // ── Value access ──────────────────────────────────────────────────────

    /// <summary>Get the current value.</summary>
    public string Value() => _value;

    /// <summary>Set the value, clamping cursor to valid range.</summary>
    public void SetValue(string value)
    {
        _value = value;
        int max = GraphemeCount();
        _cursor = Math.Min(_cursor, max);
        _scrollCells = 0;
        _selectionAnchor = null;
    }

    /// <summary>Clear all text.</summary>
    public void Clear()
    {
        _value = "";
        _cursor = 0;
        _scrollCells = 0;
        _selectionAnchor = null;
    }

    /// <summary>Get the cursor position (grapheme index).</summary>
    public int Cursor() => _cursor;

    /// <summary>Check if the input is focused.</summary>
    public bool Focused() => _focused;

    /// <summary>Set focus state.</summary>
    public void SetFocused(bool focused) => _focused = focused;

    /// <summary>
    /// Get the cursor screen position relative to a render area.
    ///
    /// Returns (x, y) where x is the column and y is the row.
    /// Useful for Frame::set_cursor().
    /// </summary>
    public (ushort x, ushort y) CursorPosition(Rect area)
    {
        int cursorVisual = CursorVisualPos();
        int effectiveScroll = EffectiveScroll(area.Width);
        int relX = Math.Max(0, cursorVisual - effectiveScroll);
        ushort x = (ushort)Math.Min(area.X + relX, Math.Max(0, area.Right - 1));
        return (x, area.Y);
    }

    /// <summary>Get selected text, if any.</summary>
    public string? SelectedText()
    {
        if (_selectionAnchor is not int anchor) return null;
        var (start, end) = SelectionRange(anchor);
        int byteStart = GraphemeByteOffset(start);
        int byteEnd   = GraphemeByteOffset(end);
        return _value[byteStart..byteEnd];
    }

    // ── IME composition ────────────────────────────────────────────────────

    /// <summary>Start an IME composition session.</summary>
    public void ImeStartComposition()
    {
        if (_imeComposition == null)
            DeleteSelection();
        _imeComposition = "";
    }

    /// <summary>
    /// Update active IME preedit text.
    ///
    /// Starts composition automatically if none is active.
    /// </summary>
    public void ImeUpdateComposition(string preedit)
    {
        if (_imeComposition == null)
            DeleteSelection();
        _imeComposition = preedit;
    }

    /// <summary>
    /// Commit active IME preedit text into the input value.
    ///
    /// Returns true if a composition session existed (even if empty).
    /// </summary>
    public bool ImeCommitComposition()
    {
        string? preedit = _imeComposition;
        if (preedit == null) return false;
        _imeComposition = null;
        if (preedit.Length > 0)
            InsertText(preedit);
        return true;
    }

    /// <summary>
    /// Cancel the active IME composition session.
    ///
    /// Returns true if a composition session was active.
    /// </summary>
    public bool ImeCancelComposition()
    {
        bool cancelled = _imeComposition != null;
        _imeComposition = null;
        return cancelled;
    }

    /// <summary>Get active IME preedit text, if any.</summary>
    public string? ImeComposition() => _imeComposition;

    // ── Event handling ─────────────────────────────────────────────────────

    /// <summary>
    /// Handle a terminal event.
    ///
    /// Returns true if the state changed.
    /// </summary>
    public bool HandleEvent(InputEvent @event)
    {
        bool changed = @event switch
        {
            InputEvent.Key k when k.KeyEvent.Kind is KeyEventKind.Press or KeyEventKind.Repeat
                => HandleKey(k.KeyEvent),
            InputEvent.Ime ime  => HandleImeEvent(ime.ImeEvent),
            InputEvent.Paste p  => HandlePaste(p.PasteEvent),
            _                   => false,
        };
        return changed;
    }

    private bool HandlePaste(PasteEvent paste)
    {
        bool hadSelection = _selectionAnchor.HasValue;

        // For replacement pastes under a max-length constraint, reject
        // oversized payloads before deleting the selection.
        if (hadSelection)
        {
            string cleanText = SanitizeInputText(paste.Text);
            if (_maxLength is int max)
            {
                int selectionLen = _selectionAnchor is int anchor
                    ? Math.Max(0, SelectionRange(anchor).end - SelectionRange(anchor).start)
                    : 0;
                int available = Math.Max(0, max - Math.Max(0, GraphemeCount() - selectionLen));
                if (GetGraphemes(cleanText).Length > available)
                    return false;
            }
        }

        DeleteSelection();
        InsertText(paste.Text);
        return true;
    }

    private bool HandleImeEvent(ImeEvent ime)
    {
        switch (ime.Phase)
        {
            case ImePhase.Start:
                ImeStartComposition();
                return true;
            case ImePhase.Update:
                ImeUpdateComposition(ime.Text);
                return true;
            case ImePhase.Commit:
                if (_imeComposition != null)
                {
                    ImeUpdateComposition(ime.Text);
                    return ImeCommitComposition();
                }
                else if (ime.Text.Length > 0)
                {
                    InsertText(ime.Text);
                    return true;
                }
                return false;
            case ImePhase.Cancel:
                return ImeCancelComposition();
            default:
                return false;
        }
    }

    private bool HandleKey(KeyEvent key)
    {
        bool ctrl  = key.Ctrl();
        bool shift = (key.Modifiers & KeyModifiers.Shift) != 0;

        switch (key.Code)
        {
            case KeyCode.Char charKey when !ctrl:
                InsertChar(charKey.Character);
                return true;
            // Ctrl+A: select all
            case KeyCode.Char ctrlCharA when ctrl && ctrlCharA.Character == 'a':
                SelectAll();
                return true;
            // Ctrl+W: delete word back
            case KeyCode.Char ctrlCharW when ctrl && ctrlCharW.Character == 'w':
                DeleteWordBack();
                return true;
            case KeyCode.Backspace:
                if (_selectionAnchor.HasValue)
                    DeleteSelection();
                else if (ctrl)
                    DeleteWordBack();
                else
                    DeleteCharBack();
                return true;
            case KeyCode.Delete:
                if (_selectionAnchor.HasValue)
                    DeleteSelection();
                else if (ctrl)
                    DeleteWordForward();
                else
                    DeleteCharForward();
                return true;
            case KeyCode.Left:
                if (ctrl)
                    MoveCursorWordLeft(shift);
                else if (shift)
                    MoveCursorLeftSelect();
                else
                    MoveCursorLeft();
                return true;
            case KeyCode.Right:
                if (ctrl)
                    MoveCursorWordRight(shift);
                else if (shift)
                    MoveCursorRightSelect();
                else
                    MoveCursorRight();
                return true;
            case KeyCode.Home:
                if (shift) EnsureSelectionAnchor(); else _selectionAnchor = null;
                _cursor = 0;
                _scrollCells = 0;
                return true;
            case KeyCode.End:
                if (shift) EnsureSelectionAnchor(); else _selectionAnchor = null;
                _cursor = GraphemeCount();
                return true;
            default:
                return false;
        }
    }

    // ── Editing operations ─────────────────────────────────────────────────

    private static string SanitizeInputText(string text)
    {
        // Map line breaks/tabs to spaces, filter other control chars
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            char mapped = (c == '\n' || c == '\r' || c == '\t') ? ' ' : c;
            if (!char.IsControl(mapped))
                sb.Append(mapped);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Insert text at the current cursor position.
    ///
    /// This method:
    /// - Replaces newlines and tabs with spaces.
    /// - Filters out other control characters.
    /// - Respects max_length (truncating if necessary).
    /// - Efficiently inserts the result in one operation.
    /// </summary>
    public void InsertText(string text)
    {
        DeleteSelection();

        string cleanText = SanitizeInputText(text);
        if (cleanText.Length == 0) return;

        int currentCount = GraphemeCount();
        int avail = _maxLength is int max
            ? (currentCount >= max ? 1 : max - currentCount)
            : int.MaxValue;

        // Truncate to avail graphemes if needed
        string[] newGraphemes = GetGraphemes(cleanText);
        string toInsert;
        if (newGraphemes.Length > avail)
        {
            int charCount = 0;
            for (int i = 0; i < avail && i < newGraphemes.Length; i++)
                charCount += newGraphemes[i].Length;
            toInsert = cleanText[..charCount];
        }
        else
        {
            toInsert = cleanText;
        }

        if (toInsert.Length == 0) return;

        int byteOffset = GraphemeByteOffset(_cursor);
        _value = _value.Insert(byteOffset, toInsert);

        // Check if we exceeded max_length (combining char edge case)
        int newTotal = GraphemeCount();
        if (_maxLength is int maxLen && newTotal > maxLen)
        {
            // Revert change
            _value = _value.Remove(byteOffset, toInsert.Length);
            return;
        }

        // Advance cursor by inserted graphemes
        _cursor = ByteOffsetToGraphemeIndex(byteOffset + toInsert.Length);
    }

    /// <summary>Insert a single character at the cursor. No-op for control characters.</summary>
    public void InsertChar(char c)
    {
        // Strict control character filtering to prevent terminal corruption
        if (char.IsControl(c)) return;

        DeleteSelection();

        int byteOffset = GraphemeByteOffset(_cursor);
        _value = _value.Insert(byteOffset, c.ToString());

        int newCount = GraphemeCount();

        // Check constraints
        if (_maxLength is int max && newCount > max)
        {
            // Revert change
            _value = _value.Remove(byteOffset, 1);
            return;
        }

        _cursor = ByteOffsetToGraphemeIndex(byteOffset + 1);
    }

    /// <summary>Delete the grapheme before the cursor.</summary>
    public void DeleteCharBack()
    {
        if (_cursor > 0)
        {
            int byteStart = GraphemeByteOffset(_cursor - 1);
            int byteEnd   = GraphemeByteOffset(_cursor);
            _value = _value.Remove(byteStart, byteEnd - byteStart);
            _cursor--;
            int gc = GraphemeCount();
            if (_cursor > gc) _cursor = gc;
        }
    }

    /// <summary>Delete the grapheme after the cursor.</summary>
    public void DeleteCharForward()
    {
        int count = GraphemeCount();
        if (_cursor < count)
        {
            int byteStart = GraphemeByteOffset(_cursor);
            int byteEnd   = GraphemeByteOffset(_cursor + 1);
            _value = _value.Remove(byteStart, byteEnd - byteStart);
            int gc = GraphemeCount();
            if (_cursor > gc) _cursor = gc;
        }
    }

    /// <summary>Delete from cursor to start of previous word.</summary>
    public void DeleteWordBack()
    {
        if (_cursor == 0) return;
        int oldCursor = _cursor;
        MoveCursorWordLeft(false);
        int newCursor = _cursor;
        if (newCursor < oldCursor)
        {
            int byteStart = GraphemeByteOffset(newCursor);
            int byteEnd   = GraphemeByteOffset(oldCursor);
            _value = _value.Remove(byteStart, byteEnd - byteStart);
            _cursor = newCursor;
            int gc = GraphemeCount();
            if (_cursor > gc) _cursor = gc;
        }
    }

    /// <summary>Delete from cursor to end of next word.</summary>
    public void DeleteWordForward()
    {
        int oldCursor = _cursor;
        MoveCursorWordRight(false);
        int newCursor = _cursor;
        // Reset cursor to start (deletion happens forward from here)
        _cursor = oldCursor;
        if (newCursor > oldCursor)
        {
            int byteStart = GraphemeByteOffset(oldCursor);
            int byteEnd   = GraphemeByteOffset(newCursor);
            _value = _value.Remove(byteStart, byteEnd - byteStart);
            int gc = GraphemeCount();
            if (_cursor > gc) _cursor = gc;
        }
    }

    // ── Selection ──────────────────────────────────────────────────────────

    /// <summary>Select all text.</summary>
    public void SelectAll()
    {
        _selectionAnchor = 0;
        _cursor = GraphemeCount();
    }

    /// <summary>Delete selected text. No-op if no selection.</summary>
    public void DeleteSelection()
    {
        if (_selectionAnchor is not int anchor) return;
        _selectionAnchor = null;
        var (start, end) = SelectionRange(anchor);
        int byteStart = GraphemeByteOffset(start);
        int byteEnd   = GraphemeByteOffset(end);
        _value = _value.Remove(byteStart, byteEnd - byteStart);
        _cursor = start;
        int gc = GraphemeCount();
        if (_cursor > gc) _cursor = gc;
    }

    private void EnsureSelectionAnchor()
    {
        if (_selectionAnchor == null)
            _selectionAnchor = _cursor;
    }

    private (int start, int end) SelectionRange(int anchor)
        => anchor <= _cursor ? (anchor, _cursor) : (_cursor, anchor);

    private bool IsInSelection(int graphemeIdx)
    {
        if (_selectionAnchor is not int anchor) return false;
        var (start, end) = SelectionRange(anchor);
        return graphemeIdx >= start && graphemeIdx < end;
    }

    // ── Cursor movement ────────────────────────────────────────────────────

    /// <summary>Move cursor one grapheme to the left. Collapses selection to the start.</summary>
    public void MoveCursorLeft()
    {
        if (_selectionAnchor is int anchor)
        {
            _cursor = Math.Min(_cursor, anchor);
            _selectionAnchor = null;
        }
        else if (_cursor > 0)
        {
            _cursor--;
        }
    }

    /// <summary>Move cursor one grapheme to the right. Collapses selection to the end.</summary>
    public void MoveCursorRight()
    {
        if (_selectionAnchor is int anchor)
        {
            _cursor = Math.Max(_cursor, anchor);
            _selectionAnchor = null;
        }
        else if (_cursor < GraphemeCount())
        {
            _cursor++;
        }
    }

    private void MoveCursorLeftSelect()
    {
        EnsureSelectionAnchor();
        if (_cursor > 0) _cursor--;
    }

    private void MoveCursorRightSelect()
    {
        EnsureSelectionAnchor();
        if (_cursor < GraphemeCount()) _cursor++;
    }

    private static byte GetGraphemeClass(string g)
    {
        bool allWhitespace = true;
        bool anyAlphanumeric = false;
        foreach (char c in g)
        {
            if (!char.IsWhiteSpace(c)) allWhitespace = false;
            if (char.IsLetterOrDigit(c)) anyAlphanumeric = true;
        }
        if (allWhitespace) return 0;
        if (anyAlphanumeric) return 1;
        return 2;
    }

    /// <summary>Move cursor one word to the left. When select=true, extends selection.</summary>
    public void MoveCursorWordLeft(bool select)
    {
        if (select)
            EnsureSelectionAnchor();
        else
            _selectionAnchor = null;

        if (_cursor == 0) return;

        string[] graphemes = GetGraphemes(_value);
        int pos = _cursor;

        // Iterate backwards from cursor, skip leading whitespace then same-class run
        int i = pos - 1;
        while (i >= 0 && GetGraphemeClass(graphemes[i]) == 0)
        {
            pos--;
            i--;
        }

        if (i >= 0)
        {
            byte targetClass = GetGraphemeClass(graphemes[i]);
            pos--;
            i--;
            while (i >= 0 && GetGraphemeClass(graphemes[i]) == targetClass)
            {
                pos--;
                i--;
            }
        }

        _cursor = pos;
    }

    /// <summary>Move cursor one word to the right. When select=true, extends selection.</summary>
    public void MoveCursorWordRight(bool select)
    {
        if (select)
            EnsureSelectionAnchor();
        else
            _selectionAnchor = null;

        string[] graphemes = GetGraphemes(_value);
        int gc  = graphemes.Length;
        int pos = _cursor;

        if (pos < gc)
        {
            byte startClass = GetGraphemeClass(graphemes[pos]);
            if (startClass != 0)
            {
                while (pos < gc && GetGraphemeClass(graphemes[pos]) == startClass)
                    pos++;
            }
        }

        // Skip whitespace
        while (pos < gc && GetGraphemeClass(graphemes[pos]) == 0)
            pos++;

        _cursor = pos;
    }

    // ── Internal helpers ───────────────────────────────────────────────────

    private int GraphemeCount() => GetGraphemes(_value).Length;

    private int GraphemeByteOffset(int graphemeIdx)
    {
        int offset = 0;
        int gi     = 0;
        var te = StringInfo.GetTextElementEnumerator(_value);
        while (te.MoveNext() && gi < graphemeIdx)
        {
            offset += te.GetTextElement().Length;
            gi++;
        }
        return gi < graphemeIdx ? _value.Length : offset;
    }

    private int ByteOffsetToGraphemeIndex(int charOffset)
    {
        int gi  = 0;
        int pos = 0;
        var te = StringInfo.GetTextElementEnumerator(_value);
        while (te.MoveNext())
        {
            if (pos >= charOffset) break;
            pos += te.GetTextElement().Length;
            gi++;
        }
        return gi;
    }

    private int GraphemeWidthOf(string g)
    {
        if (_maskChar is char mask)
            return WidgetDrawing.GraphemeWidth(mask.ToString());
        return WidgetDrawing.GraphemeWidth(g);
    }

    private int PrevGraphemeWidth()
    {
        if (_cursor == 0) return 0;
        string[] gs = GetGraphemes(_value);
        int idx = _cursor - 1;
        return idx < gs.Length ? GraphemeWidthOf(gs[idx]) : 0;
    }

    /// <summary>Compute visual position of cursor (in display cells).</summary>
    public int CursorVisualPos()
    {
        int pos = 0;
        if (_value.Length > 0)
        {
            string[] gs = GetGraphemes(_value);
            for (int i = 0; i < _cursor && i < gs.Length; i++)
                pos += GraphemeWidthOf(gs[i]);
        }
        if (_imeComposition != null)
            foreach (string ig in GetGraphemes(_imeComposition))
                pos += GraphemeWidthOf(ig);
        return pos;
    }

    /// <summary>Compute effective scroll offset (caches result in _scrollCells).</summary>
    internal int EffectiveScroll(int viewportWidth)
    {
        int cursorVisual = CursorVisualPos();
        int scroll = _scrollCells;

        if (cursorVisual < scroll)
            scroll = cursorVisual;

        if (cursorVisual >= scroll + viewportWidth)
        {
            int candidateScroll = cursorVisual - viewportWidth + 1;
            int prevWidth = PrevGraphemeWidth();
            int maxScrollForPrev = Math.Max(0, cursorVisual - prevWidth);

            // Only enforce wide-char visibility if the viewport is wide enough
            if (viewportWidth > prevWidth)
                scroll = Math.Min(candidateScroll, maxScrollForPrev);
            else
                scroll = candidateScroll;
        }

        // Sanitize: ensure scroll aligns with grapheme boundaries
        scroll = SnapScrollToGraphemeBoundary(scroll, viewportWidth);

        _scrollCells = scroll;
        return scroll;
    }

    private int SnapScrollToGraphemeBoundary(int scroll, int viewportWidth)
    {
        int pos = 0;
        int cursorVisual = CursorVisualPos();

        foreach (string g in GetGraphemes(_value))
        {
            int w       = GraphemeWidthOf(g);
            int nextPos = pos + w;

            if (pos < scroll && scroll < nextPos)
            {
                // Try snapping to the start of the character to keep it visible.
                // Only allowed if the cursor remains visible on the right.
                if (cursorVisual <= pos + viewportWidth)
                    return pos;
                else
                    return nextPos;
            }

            if (nextPos > scroll) break;

            pos = nextPos;
        }
        return scroll;
    }

    // ── Static grapheme helpers ────────────────────────────────────────────

    private static string[] GetGraphemes(string s)
    {
        if (s.Length == 0) return Array.Empty<string>();
        var list = new List<string>();
        var te = StringInfo.GetTextElementEnumerator(s);
        while (te.MoveNext())
            list.Add(te.GetTextElement());
        return list.ToArray();
    }

    // ── IWidget.Render ─────────────────────────────────────────────────────

    /// <summary>Render the text input widget.</summary>
    public void Render(Rect area, Frame frame)
    {
        if (area.Width < 1 || area.Height < 1) return;

        var deg = frame.Degradation;

        // TextInput is essential — always render content, but skip styling
        // at NoStyling+. At Skeleton, still render the raw text value.
        // We explicitly DO NOT check deg.RenderContent() here because this widget is essential.
        WidgetStyle baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, area, baseStyle);

        // DIVERGENCE: Rust uses arena allocation when frame.arena is Some.
        // C# always uses heap allocation.
        string[] graphemes = GetGraphemes(_value);

        bool showPlaceholder =
            _value.Length == 0 && _imeComposition == null && _placeholder.Length > 0;

        int viewportWidth   = area.Width;
        int cursorVisualPos = CursorVisualPos();
        int effectiveScroll = EffectiveScroll(viewportWidth);

        int   visualX = 0;
        ushort y = area.Y;

        if (showPlaceholder)
        {
            WidgetStyle placeholderStyle = deg.ApplyStyling() ? _placeholderStyle : WidgetStyle.Default;
            foreach (string g in GetGraphemes(_placeholder))
            {
                int w = GraphemeWidthOf(g);
                if (w == 0) continue;

                if (visualX + w <= effectiveScroll) { visualX += w; continue; }  // fully left-clipped
                if (visualX < effectiveScroll)      { visualX += w; continue; }  // partially left-clipped

                int relX = visualX - effectiveScroll;
                if (relX >= viewportWidth)          break;                        // fully right-clipped
                if (relX + w > viewportWidth)       break;                        // partially right-clipped

                Cell cell = MakeCell(frame, g, w);
                WidgetDrawing.ApplyStyle(ref cell, placeholderStyle);
                frame.Buffer.Set((ushort)(area.X + relX), y, cell);
                PlaceContinuations(frame, area, relX, w, viewportWidth, y);

                visualX += w;
            }
        }
        else
        {
            // Build display spans: (grapheme, style, is_ime)
            var displaySpans = new List<(string g, WidgetStyle style, bool isIme)>();

            for (int gi = 0; gi < graphemes.Length; gi++)
            {
                // Insert IME preedit at cursor position
                if (gi == _cursor && _imeComposition != null)
                {
                    WidgetStyle imeStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
                    foreach (string ig in GetGraphemes(_imeComposition))
                        displaySpans.Add((ig, imeStyle, true));
                }

                WidgetStyle cellStyle = !deg.ApplyStyling()
                    ? WidgetStyle.Default
                    : IsInSelection(gi) ? _selectionStyle : _style;

                displaySpans.Add((graphemes[gi], cellStyle, false));
            }

            // If cursor is at end, append IME preedit
            if (_cursor == graphemes.Length && _imeComposition != null)
            {
                WidgetStyle imeStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
                foreach (string ig in GetGraphemes(_imeComposition))
                    displaySpans.Add((ig, imeStyle, true));
            }

            foreach (var (g, cellStyle, isIme) in displaySpans)
            {
                int w = GraphemeWidthOf(g);
                if (w == 0) continue;

                if (visualX + w <= effectiveScroll) { visualX += w; continue; }  // fully left-clipped
                if (visualX < effectiveScroll)      { visualX += w; continue; }  // partially left-clipped

                int relX = visualX - effectiveScroll;
                if (relX >= viewportWidth)          break;                        // fully right-clipped
                if (relX + w > viewportWidth)       break;                        // partially right-clipped

                Cell cell = _maskChar is char mask
                    ? Cell.FromChar(mask)
                    : MakeCell(frame, g, w);

                WidgetDrawing.ApplyStyle(ref cell, cellStyle);

                if (isIme && deg.ApplyStyling())
                {
                    // Underline IME composition text (StyleFlags::UNDERLINE in upstream)
                    var attrs = cell.Attributes;
                    cell = cell.WithAttributes(attrs.MergedFlags(CellStyleFlags.Underline));
                }

                frame.Buffer.Set((ushort)(area.X + relX), y, cell);
                PlaceContinuations(frame, area, relX, w, viewportWidth, y);

                visualX += w;
            }
        }

        if (_focused)
        {
            // Set cursor style at cursor position
            int cursorRelX = cursorVisualPos - effectiveScroll;
            if (cursorRelX >= 0 && cursorRelX < viewportWidth)
            {
                ushort cursorScreenX = (ushort)(area.X + cursorRelX);
                Cell? maybeCell = frame.Buffer.Get(cursorScreenX, y);
                if (maybeCell is Cell existing)
                {
                    Cell updated;
                    if (!deg.ApplyStyling())
                    {
                        // At NoStyling, just use reverse video for cursor
                        var flags = existing.Attributes.Flags ^ CellStyleFlags.Reverse;
                        updated = existing.WithAttributes(existing.Attributes.WithFlags(flags));
                    }
                    else if (_cursorStyle.IsEmpty)
                    {
                        // Default: toggle reverse video for cursor visibility
                        var flags = existing.Attributes.Flags ^ CellStyleFlags.Reverse;
                        updated = existing.WithAttributes(existing.Attributes.WithFlags(flags));
                    }
                    else
                    {
                        updated = existing;
                        WidgetDrawing.ApplyStyle(ref updated, _cursorStyle);
                    }
                    frame.Buffer.Set(cursorScreenX, y, updated);
                }
            }

            frame.SetCursor(CursorPosition(area));
            frame.SetCursorVisible(true);
        }
    }

    private static void PlaceContinuations(Frame frame, Rect area, int relX, int w, int viewportWidth, ushort y)
    {
        if (w > 1)
            for (int ci = 1; ci < w && relX + ci < viewportWidth; ci++)
                frame.Buffer.Set((ushort)(area.X + relX + ci), y, Cell.Continuation);
    }

    private static Cell MakeCell(Frame frame, string g, int w)
    {
        if (g.Length > 1 || w > 1)
        {
            GraphemeId id = frame.InternWithWidth(g, (byte)w);
            return new Cell(CellContent.FromGrapheme(id), PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);
        }
        char c = g.Length > 0 ? g[0] : ' ';
        return Cell.FromChar(c);
    }

    /// <summary>TextInput is essential — it always renders.</summary>
    public bool IsEssential() => true;

    // ── Accessibility (ftui_a11y::Accessible) ─────────────────────────────

    /// <summary>Get the legacy compatibility projection of this widget's accessibility nodes.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        ulong id = WidgetDrawing.A11yNodeId(area);

        string name;
        if (_value.Length == 0)
            name = _placeholder;
        else if (_maskChar.HasValue)
            name = "password field";
        else
            name = _value;

        var state = new CanonicalA11y.A11yState
        {
            Focused  = _focused,
            Disabled = _maxLength == 0,
        };

        CanonicalA11y.A11yNodeInfo node = CanonicalA11y.A11yNodeInfo
            .New(id, CanonicalA11y.A11yRole.TextInput, area)
            .WithState(state);
        if (name.Length > 0)
            node = node.WithName(name);
        if (_maskChar.HasValue)
            node = node.WithDescription("password input");

        return [node];
    }

    // ── IUndoSupport (snapshot + restore) ─────────────────────────────────

    UndoWidgetId IUndoSupport.UndoWidgetId => _undoId;

    /// <summary>Create a snapshot of the current state for undo.</summary>
    public object CreateSnapshot() =>
        new TextInputSnapshot(_value, _cursor, _selectionAnchor);

    /// <summary>Restore state from a snapshot. Returns true if successful.</summary>
    public bool RestoreSnapshot(object snapshot)
    {
        if (snapshot is TextInputSnapshot snap)
        {
            _value           = snap.Value;
            _cursor          = snap.Cursor;
            _selectionAnchor = snap.SelectionAnchor;
            _scrollCells     = 0; // Reset scroll on restore
            return true;
        }
        return false;
    }

    // ── ITextInputUndoExt ─────────────────────────────────────────────────

    string ITextInputUndoExt.TextValue => _value;

    void ITextInputUndoExt.SetTextValue(string value)
    {
        _value           = value;
        int max          = GraphemeCount();
        _cursor          = Math.Min(_cursor, max);
        _selectionAnchor = null;
    }

    int ITextInputUndoExt.CursorPosition => _cursor;

    void ITextInputUndoExt.SetCursorPosition(int pos)
    {
        _cursor = Math.Min(pos, GraphemeCount());
    }

    /// <summary>Insert text at a grapheme index position.</summary>
    public void InsertTextAt(int position, string text)
    {
        int byteOffset         = GraphemeByteOffset(position);
        _value                 = _value.Insert(byteOffset, text);
        int insertedGraphemes  = GetGraphemes(text).Length;
        if (_cursor >= position)
            _cursor += insertedGraphemes;
    }

    /// <summary>Delete graphemes in [start, end).</summary>
    public void DeleteTextRange(int start, int end)
    {
        if (start >= end) return;
        int byteStart    = GraphemeByteOffset(start);
        int byteEnd      = GraphemeByteOffset(end);
        _value           = _value.Remove(byteStart, byteEnd - byteStart);
        int deletedCount = end - start;
        if (_cursor > end)
            _cursor -= deletedCount;
        else if (_cursor > start)
            _cursor = start;
        int gc = GraphemeCount();
        if (_cursor > gc) _cursor = gc;
    }

    // ── Public undo helpers ────────────────────────────────────────────────

    /// <summary>
    /// Create an undo command for the given text edit operation.
    ///
    /// This creates a command that can be added to a HistoryManager for undo/redo support.
    /// The command includes callbacks that will be called when the operation is undone or redone.
    /// </summary>
    public WidgetTextEditCmd? CreateTextEditCommand(TextEditOperation operation) =>
        new WidgetTextEditCmd(_undoId, operation);

    /// <summary>
    /// Get the undo widget ID.
    ///
    /// This can be used to associate undo commands with this widget instance.
    /// </summary>
    public UndoWidgetId UndoId() => _undoId;

    // DIVERGENCE: Cannot name the method UndoWidgetId() since that conflicts with
    // the UndoWidgetId type name in the constructor. Exposed as UndoId().
    // The IUndoSupport.UndoWidgetId() explicit implementation satisfies the interface.

    // ── Test-access properties ────────────────────────────────────────────
    // DIVERGENCE: Rust tests directly access pub(crate) struct fields.
    // C# exposes public properties for parity test access (Rust field access
    // in tests = C# public property with getter+setter).

    /// <summary>Test-access: get or set the selection anchor (grapheme index, or null).</summary>
    public int? SelectionAnchorNullable
    {
        get => _selectionAnchor;
        set => _selectionAnchor = value;
    }

    /// <summary>Test-access: get or set the raw cursor position (grapheme index).</summary>
    public int CursorForTest
    {
        get => _cursor;
        set => _cursor = value;
    }

    /// <summary>Test-access: return the current grapheme count.</summary>
    public int GraphemeCountPublic() => GraphemeCount();
}
