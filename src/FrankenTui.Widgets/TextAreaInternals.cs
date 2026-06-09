// Port of .external/frankentui/crates/ftui-widgets/src/textarea.rs (internals)
// Internal text-engine types required by TextArea: Rope, CursorPosition,
// CursorNavigator, Selection, EditOp, and Editor — all ported from the
// ftui-text crate (editor.rs + cursor.rs + rope.rs) as inline helpers,
// since ftui-text has not been ported to a standalone .NET library yet.
//
// DIVERGENCE: The upstream ftui-text crate types (Editor, CursorPosition,
// CursorNavigator, Selection, Rope) are implemented here as internal helpers
// in FrankenTui.Widgets.TextAreaInternals. This keeps the public FrankenTui.Widgets
// surface clean while making the types accessible to TextAreaTests.cs.

using System.Globalization;
using System.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Widgets.TextAreaInternals;

// ============================================================================
// CursorPosition  (ftui-text::cursor::CursorPosition)
// ============================================================================

/// <summary>
/// Logical + visual cursor position.
/// Port of ftui_text::cursor::CursorPosition.
/// </summary>
public struct CursorPosition : IEquatable<CursorPosition>
{
    /// <summary>Line index (0-based).</summary>
    public int Line;
    /// <summary>Grapheme index within the line (0-based).</summary>
    public int Grapheme;
    /// <summary>Visual column in cells (0-based).</summary>
    public int VisualCol;

    public CursorPosition(int line, int grapheme, int visualCol)
    {
        Line = line; Grapheme = grapheme; VisualCol = visualCol;
    }

    public static CursorPosition Default => new(0, 0, 0);

    public bool Equals(CursorPosition other) =>
        Line == other.Line && Grapheme == other.Grapheme && VisualCol == other.VisualCol;

    public override bool Equals(object? obj) => obj is CursorPosition c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(Line, Grapheme, VisualCol);
    public static bool operator ==(CursorPosition a, CursorPosition b) => a.Equals(b);
    public static bool operator !=(CursorPosition a, CursorPosition b) => !a.Equals(b);
    public override string ToString() => $"({Line},{Grapheme},{VisualCol})";
}

// ============================================================================
// Selection  (ftui-text::editor::Selection)
// ============================================================================

/// <summary>
/// Selection defined by anchor (fixed) and head (moving with cursor).
/// Port of ftui_text::editor::Selection.
/// </summary>
public struct Selection
{
    /// <summary>The fixed end of the selection.</summary>
    public CursorPosition Anchor;
    /// <summary>The moving end (same as cursor).</summary>
    public CursorPosition Head;

    /// <summary>Whether the selection is empty (anchor == head).</summary>
    public bool IsEmpty() => Anchor == Head;

    /// <summary>Byte range of the selection (start, end) where start &lt;= end.</summary>
    public (int start, int end) ByteRange(CursorNavigator nav)
    {
        int a = nav.ToByteIndex(Anchor);
        int b = nav.ToByteIndex(Head);
        return a <= b ? (a, b) : (b, a);
    }
}

// ============================================================================
// Rope  (ftui-text::rope::Rope)
// ============================================================================

/// <summary>
/// Minimal Rope implementation backed by a list of strings.
/// Port of ftui_text::rope::Rope — only the API surface needed by Editor
/// and CursorNavigator is implemented here.
/// DIVERGENCE: The upstream Rope is a gap-buffer / B-tree rope. This port uses
/// a List&lt;string&gt; of lines for simplicity; the API contract is identical.
/// </summary>
public sealed class Rope
{
    // Lines are stored WITHOUT the trailing newline, except possibly the last
    // line when the text ends with \n. Line count mirrors upstream behaviour:
    //   text = "a\nb\nc"  => lines = ["a","b","c"]        line_count=3
    //   text = "a\nb\n"   => lines = ["a","b",""]         line_count=3
    //   text = ""         => lines = [""]                  line_count=1
    private readonly List<string> _lines;

    public Rope()
    {
        _lines = [""];
    }

    public Rope(string text)
    {
        _lines = SplitIntoLines(text);
    }

    private static List<string> SplitIntoLines(string text)
    {
        text = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                   .Replace('\r', '\n');
        var parts = text.Split('\n');
        return new List<string>(parts);
    }

    public static Rope FromText(string text) => new(text);

    /// <summary>Number of lines (always &gt;= 1).</summary>
    public int LenLines() => _lines.Count;

    /// <summary>Total byte length of the stored text.</summary>
    public int LenBytes()
    {
        int total = 0;
        for (int i = 0; i < _lines.Count; i++)
        {
            total += Encoding.UTF8.GetByteCount(_lines[i]);
            if (i < _lines.Count - 1) total += 1; // newline byte
        }
        return total;
    }

    public bool IsEmpty() => _lines.Count == 1 && _lines[0].Length == 0;

    /// <summary>Get a line by index. Returns null if out of bounds.</summary>
    public string? Line(int index)
    {
        if (index < 0 || index >= _lines.Count) return null;
        return _lines[index];
    }

    /// <summary>Full text as a string.</summary>
    public override string ToString()
    {
        return string.Join('\n', _lines);
    }

    /// <summary>Replace all content.</summary>
    public void Replace(string text)
    {
        _lines.Clear();
        _lines.AddRange(SplitIntoLines(text));
    }

    /// <summary>Clear all content.</summary>
    public void Clear()
    {
        _lines.Clear();
        _lines.Add("");
    }

    // ── Byte/char index conversion helpers ───────────────────────────────────

    /// <summary>Convert a byte offset in the full text to (lineIdx, byteOffsetInLine).</summary>
    public (int line, int byteInLine) ByteToLineCol(int byteOffset)
    {
        int remaining = byteOffset;
        for (int i = 0; i < _lines.Count; i++)
        {
            int lineBytes = Encoding.UTF8.GetByteCount(_lines[i]);
            if (remaining <= lineBytes || i == _lines.Count - 1)
                return (i, Math.Min(remaining, lineBytes));
            remaining -= lineBytes + 1; // +1 for '\n'
        }
        return (_lines.Count - 1, Encoding.UTF8.GetByteCount(_lines[^1]));
    }

    /// <summary>Get the byte offset of the start of the given line.</summary>
    public int LineStartByte(int lineIdx)
    {
        int offset = 0;
        for (int i = 0; i < lineIdx && i < _lines.Count; i++)
            offset += Encoding.UTF8.GetByteCount(_lines[i]) + 1; // +1 for '\n'
        return offset;
    }

    // ── Mutation helpers ─────────────────────────────────────────────────────

    /// <summary>Insert text at the given byte offset in the full text.</summary>
    public void InsertAtByte(int byteOffset, string text)
    {
        var (lineIdx, byteInLine) = ByteToLineCol(byteOffset);
        if (lineIdx >= _lines.Count) lineIdx = _lines.Count - 1;

        string lineSrc = _lines[lineIdx];
        int charInLine = ByteOffsetToCharOffset(lineSrc, byteInLine);
        string before = lineSrc[..charInLine];
        string after  = lineSrc[charInLine..];

        text = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

        if (!text.Contains('\n'))
        {
            _lines[lineIdx] = before + text + after;
        }
        else
        {
            string[] newParts = text.Split('\n');
            _lines[lineIdx] = before + newParts[0];
            for (int i = 1; i < newParts.Length - 1; i++)
                _lines.Insert(lineIdx + i, newParts[i]);
            _lines.Insert(lineIdx + newParts.Length - 1, newParts[^1] + after);
        }
    }

    /// <summary>Remove bytes in [startByte, endByte) from the full text.</summary>
    public void RemoveBytes(int startByte, int endByte)
    {
        if (startByte >= endByte) return;
        string full   = ToString();
        byte[] utf8   = Encoding.UTF8.GetBytes(full);
        startByte     = Math.Clamp(startByte, 0, utf8.Length);
        endByte       = Math.Clamp(endByte,   0, utf8.Length);
        byte[] before = utf8[..startByte];
        byte[] after  = utf8[endByte..];
        string result = Encoding.UTF8.GetString(before) + Encoding.UTF8.GetString(after);
        Replace(result);
    }

    /// <summary>Slice bytes [startByte, endByte) from the full text.</summary>
    public string SliceBytes(int startByte, int endByte)
    {
        string full = ToString();
        byte[] utf8 = Encoding.UTF8.GetBytes(full);
        startByte   = Math.Clamp(startByte, 0, utf8.Length);
        endByte     = Math.Clamp(endByte,   0, utf8.Length);
        if (startByte >= endByte) return "";
        return Encoding.UTF8.GetString(utf8[startByte..endByte]);
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>Convert a byte offset within a UTF-8 string to a char (UTF-16) index.</summary>
    public static int ByteOffsetToCharOffset(string s, int byteOffset)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(s);
        byteOffset  = Math.Clamp(byteOffset, 0, utf8.Length);
        return Encoding.UTF8.GetCharCount(utf8, 0, byteOffset);
    }

    /// <summary>Convert a char (UTF-16) offset in a string to a byte offset.</summary>
    public static int CharOffsetToByteOffset(string s, int charOffset)
    {
        charOffset = Math.Clamp(charOffset, 0, s.Length);
        return Encoding.UTF8.GetByteCount(s[..charOffset]);
    }
}

// ============================================================================
// CursorNavigator  (ftui-text::cursor::CursorNavigator)
// ============================================================================

/// <summary>
/// Cursor navigation helper for rope-backed text.
/// Port of ftui_text::cursor::CursorNavigator.
/// </summary>
public sealed class CursorNavigator
{
    private readonly Rope _rope;

    public CursorNavigator(Rope rope) => _rope = rope;

    // ── Clamping ─────────────────────────────────────────────────────────────

    public CursorPosition Clamp(CursorPosition pos)
    {
        int line = Math.Clamp(pos.Line, 0, Math.Max(0, _rope.LenLines() - 1));
        string[] graphemes = GetGraphemes(GetLineText(line));
        int grapheme = Math.Clamp(pos.Grapheme, 0, graphemes.Length);
        int visualCol = VisualColForGrapheme(graphemes, grapheme);
        return new CursorPosition(line, grapheme, visualCol);
    }

    // ── Build from line+grapheme ──────────────────────────────────────────────

    public CursorPosition FromLineGrapheme(int line, int grapheme)
    {
        line = Math.Clamp(line, 0, Math.Max(0, _rope.LenLines() - 1));
        string[] graphemes = GetGraphemes(GetLineText(line));
        grapheme = Math.Clamp(grapheme, 0, graphemes.Length);
        int visualCol = VisualColForGrapheme(graphemes, grapheme);
        return new CursorPosition(line, grapheme, visualCol);
    }

    // ── Build from byte index ─────────────────────────────────────────────────

    public CursorPosition FromByteIndex(int byteIndex)
    {
        var (lineIdx, byteInLine) = _rope.ByteToLineCol(byteIndex);
        string lineText = GetLineText(lineIdx);
        byte[] utf8Line = Encoding.UTF8.GetBytes(lineText);
        byteInLine = Math.Clamp(byteInLine, 0, utf8Line.Length);
        string prefix  = Encoding.UTF8.GetString(utf8Line, 0, byteInLine);
        string[] graphemes       = GetGraphemes(lineText);
        string[] prefixGraphemes = GetGraphemes(prefix);
        int grapheme  = Math.Min(prefixGraphemes.Length, graphemes.Length);
        int visualCol = VisualColForGrapheme(graphemes, grapheme);
        return new CursorPosition(lineIdx, grapheme, visualCol);
    }

    // ── Convert to byte index ─────────────────────────────────────────────────

    public int ToByteIndex(CursorPosition pos)
    {
        pos = Clamp(pos);
        int lineStart  = _rope.LineStartByte(pos.Line);
        string lineText = GetLineText(pos.Line);
        string[] graphemes = GetGraphemes(lineText);
        int byteInLine = 0;
        for (int i = 0; i < pos.Grapheme && i < graphemes.Length; i++)
            byteInLine += Encoding.UTF8.GetByteCount(graphemes[i]);
        return lineStart + byteInLine;
    }

    // ── Movement ─────────────────────────────────────────────────────────────

    public CursorPosition MoveLeft(CursorPosition pos)
    {
        if (pos.Grapheme > 0)
        {
            string[] gs = GetGraphemes(GetLineText(pos.Line));
            int newG = pos.Grapheme - 1;
            return new CursorPosition(pos.Line, newG, VisualColForGrapheme(gs, newG));
        }
        if (pos.Line > 0)
        {
            int newLine = pos.Line - 1;
            string[] gs = GetGraphemes(GetLineText(newLine));
            return new CursorPosition(newLine, gs.Length, VisualColForGrapheme(gs, gs.Length));
        }
        return pos;
    }

    public CursorPosition MoveRight(CursorPosition pos)
    {
        string[] gs = GetGraphemes(GetLineText(pos.Line));
        if (pos.Grapheme < gs.Length)
        {
            int newG = pos.Grapheme + 1;
            return new CursorPosition(pos.Line, newG, VisualColForGrapheme(gs, newG));
        }
        if (pos.Line + 1 < _rope.LenLines())
            return new CursorPosition(pos.Line + 1, 0, 0);
        return pos;
    }

    public CursorPosition MoveUp(CursorPosition pos)
    {
        if (pos.Line == 0) return pos;
        int newLine   = pos.Line - 1;
        string[] gs   = GetGraphemes(GetLineText(newLine));
        int grapheme  = GraphemeAtVisualCol(gs, pos.VisualCol);
        return new CursorPosition(newLine, grapheme, VisualColForGrapheme(gs, grapheme));
    }

    public CursorPosition MoveDown(CursorPosition pos)
    {
        if (pos.Line + 1 >= _rope.LenLines()) return pos;
        int newLine   = pos.Line + 1;
        string[] gs   = GetGraphemes(GetLineText(newLine));
        int grapheme  = GraphemeAtVisualCol(gs, pos.VisualCol);
        return new CursorPosition(newLine, grapheme, VisualColForGrapheme(gs, grapheme));
    }

    public CursorPosition MoveWordLeft(CursorPosition pos)
    {
        if (pos.Grapheme == 0 && pos.Line == 0) return pos;
        if (pos.Grapheme == 0)
        {
            int prevLine = pos.Line - 1;
            string[] gs  = GetGraphemes(GetLineText(prevLine));
            return new CursorPosition(prevLine, gs.Length, VisualColForGrapheme(gs, gs.Length));
        }
        string[] graphemes = GetGraphemes(GetLineText(pos.Line));
        int i = pos.Grapheme - 1;
        // Skip trailing whitespace
        while (i > 0 && IsWhitespace(graphemes[i])) i--;
        // Skip non-whitespace word
        if (i > 0)
        {
            byte cls = GetGraphemeClass(graphemes[i]);
            while (i > 0 && GetGraphemeClass(graphemes[i - 1]) == cls) i--;
        }
        else if (IsWhitespace(graphemes[i])) i = 0;
        return new CursorPosition(pos.Line, i, VisualColForGrapheme(graphemes, i));
    }

    public CursorPosition MoveWordRight(CursorPosition pos)
    {
        string[] graphemes = GetGraphemes(GetLineText(pos.Line));
        int i = pos.Grapheme;
        if (i >= graphemes.Length)
        {
            if (pos.Line + 1 < _rope.LenLines())
                return new CursorPosition(pos.Line + 1, 0, 0);
            return pos;
        }
        // Skip current-class run
        byte startClass = GetGraphemeClass(graphemes[i]);
        if (startClass != 0)
            while (i < graphemes.Length && GetGraphemeClass(graphemes[i]) == startClass) i++;
        // Skip whitespace
        while (i < graphemes.Length && IsWhitespace(graphemes[i])) i++;
        return new CursorPosition(pos.Line, i, VisualColForGrapheme(graphemes, i));
    }

    public CursorPosition LineStart(CursorPosition pos) => new(pos.Line, 0, 0);

    public CursorPosition LineEnd(CursorPosition pos)
    {
        string[] gs = GetGraphemes(GetLineText(pos.Line));
        return new CursorPosition(pos.Line, gs.Length, VisualColForGrapheme(gs, gs.Length));
    }

    public CursorPosition DocumentStart() => new(0, 0, 0);

    public CursorPosition DocumentEnd()
    {
        int lastLine = Math.Max(0, _rope.LenLines() - 1);
        string[] gs  = GetGraphemes(GetLineText(lastLine));
        return new CursorPosition(lastLine, gs.Length, VisualColForGrapheme(gs, gs.Length));
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    internal string GetLineText(int lineIdx)
    {
        return _rope.Line(lineIdx) ?? "";
    }

    internal static string[] GetGraphemes(string text)
    {
        if (text.Length == 0) return Array.Empty<string>();
        var list = new List<string>();
        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext())
            list.Add(te.GetTextElement());
        return list.ToArray();
    }

    internal static int DisplayWidth(string grapheme)
    {
        return WidgetDrawing.GraphemeWidth(grapheme);
    }

    internal static int VisualColForGrapheme(string[] graphemes, int graphemeIdx)
    {
        int col = 0;
        for (int i = 0; i < graphemeIdx && i < graphemes.Length; i++)
            col += DisplayWidth(graphemes[i]);
        return col;
    }

    internal static int GraphemeAtVisualCol(string[] graphemes, int targetVisualCol)
    {
        int col = 0;
        for (int i = 0; i < graphemes.Length; i++)
        {
            int w = DisplayWidth(graphemes[i]);
            if (col + w > targetVisualCol) return i;
            col += w;
        }
        return graphemes.Length;
    }

    internal static bool IsWhitespace(string g) => g.All(char.IsWhiteSpace);

    internal static byte GetGraphemeClass(string g)
    {
        if (IsWhitespace(g)) return 0;
        if (g.All(c => char.IsLetterOrDigit(c) || c == '_')) return 1;
        return 2;
    }
}

// ============================================================================
// EditOp  (ftui-text::editor, private)
// ============================================================================

internal abstract record EditOp
{
    public sealed record Insert(int ByteOffset, string Text) : EditOp;
    public sealed record Delete(int ByteOffset, string Text) : EditOp;
    public sealed record Replace(int ByteOffset, string Deleted, string Inserted) : EditOp;

    public EditOp Inverse() => this switch
    {
        Insert i  => new Delete(i.ByteOffset, i.Text),
        Delete d  => new Insert(d.ByteOffset, d.Text),
        Replace r => new Replace(r.ByteOffset, r.Inserted, r.Deleted),
        _         => throw new InvalidOperationException("unknown EditOp"),
    };

    public int ByteLen() => this switch
    {
        Insert i  => Encoding.UTF8.GetByteCount(i.Text),
        Delete d  => Encoding.UTF8.GetByteCount(d.Text),
        Replace r => Encoding.UTF8.GetByteCount(r.Deleted) + Encoding.UTF8.GetByteCount(r.Inserted),
        _         => 0,
    };
}

// ============================================================================
// Editor  (ftui-text::editor::Editor)
// ============================================================================

/// <summary>
/// Core text editor combining Rope storage with cursor management.
///
/// Provides insert/delete/move operations with grapheme-aware cursor
/// handling, undo/redo, and selection support.
/// Cursor is always kept in valid bounds.
///
/// Port of ftui_text::editor::Editor.
/// </summary>
public sealed class Editor
{
    private Rope _rope;
    private CursorPosition _cursor;
    private Selection? _selection;
    private readonly List<(EditOp op, CursorPosition cursorBefore)> _undoStack = [];
    private readonly List<(EditOp op, CursorPosition cursorBefore)> _redoStack = [];
    private int _maxHistory = 1000;
    private int _currentUndoSize;
    private int _maxUndoSize = 10 * 1024 * 1024;

    /// <summary>Create an empty editor.</summary>
    public Editor()
    {
        _rope = new Rope();
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Create an editor with initial text. Cursor starts at the end.</summary>
    public static Editor WithText(string text)
    {
        var ed = new Editor();
        ed._rope = new Rope(text);
        ed._cursor = new CursorNavigator(ed._rope).DocumentEnd();
        return ed;
    }

    // ── Configuration ──────────────────────────────────────────────────────────

    public void SetMaxHistory(int max) => _maxHistory = max;

    public void SetMaxUndoSize(int bytes)
    {
        _maxUndoSize = bytes;
        while (_currentUndoSize > _maxUndoSize && _undoStack.Count > 0)
        {
            _currentUndoSize -= _undoStack[0].op.ByteLen();
            _undoStack.RemoveAt(0);
        }
    }

    // ── Accessors ──────────────────────────────────────────────────────────────

    /// <summary>Get the full text content as a string.</summary>
    public string Text() => _rope.ToString();

    /// <summary>Get a reference to the underlying rope.</summary>
    public Rope Rope() => _rope;

    /// <summary>Get the current cursor position.</summary>
    public CursorPosition Cursor() => _cursor;

    /// <summary>Set cursor position (clamped to valid bounds). Clears selection.</summary>
    public void SetCursor(CursorPosition pos)
    {
        _cursor    = new CursorNavigator(_rope).Clamp(pos);
        _selection = null;
    }

    /// <summary>Current selection, if any.</summary>
    public Selection? Selection() => _selection;

    /// <summary>Whether undo is available.</summary>
    public bool CanUndo() => _undoStack.Count > 0;

    /// <summary>Whether redo is available.</summary>
    public bool CanRedo() => _redoStack.Count > 0;

    /// <summary>Check if the editor is empty.</summary>
    public bool IsEmpty() => _rope.IsEmpty();

    /// <summary>Number of lines in the buffer.</summary>
    public int LineCount() => _rope.LenLines();

    /// <summary>Get the text of a specific line (without trailing newline).</summary>
    public string? LineText(int line) => _rope.Line(line);

    // ── Insert operations ─────────────────────────────────────────────────────

    /// <summary>Insert a single character at the cursor position.</summary>
    public void InsertChar(char ch) => InsertText(ch.ToString());

    /// <summary>
    /// Insert text at the cursor position. Deletes selection first if active.
    /// Control characters (except newline and tab) are stripped.
    /// </summary>
    public void InsertText(string text)
    {
        if (text.Length == 0) return;

        var sanitized = new StringBuilder();
        foreach (char c in text)
            if (!char.IsControl(c) || c == '\n' || c == '\t')
                sanitized.Append(c);

        if (sanitized.Length == 0) return;
        string san = sanitized.ToString();

        if (ExtractSelection() is { } extracted)
        {
            var (startByte, deleted) = extracted;
            PushUndo(new EditOp.Replace(startByte, deleted, san));
            _rope.InsertAtByte(startByte, san);
            _cursor = new CursorNavigator(_rope).FromByteIndex(startByte + Encoding.UTF8.GetByteCount(san));
        }
        else
        {
            var nav    = new CursorNavigator(_rope);
            int byteIdx = nav.ToByteIndex(_cursor);
            PushUndo(new EditOp.Insert(byteIdx, san));
            _rope.InsertAtByte(byteIdx, san);
            _cursor = new CursorNavigator(_rope).FromByteIndex(byteIdx + Encoding.UTF8.GetByteCount(san));
        }
    }

    /// <summary>Insert a newline at the cursor position.</summary>
    public void InsertNewline() => InsertText("\n");

    // ── Delete operations ─────────────────────────────────────────────────────

    /// <summary>Delete the character before the cursor (backspace).</summary>
    public bool DeleteBackward()
    {
        if (DeleteSelectionInner()) return true;
        var nav     = new CursorNavigator(_rope);
        var oldPos  = _cursor;
        var newPos  = nav.MoveLeft(oldPos);
        if (newPos == oldPos) return false;

        int startByte = nav.ToByteIndex(newPos);
        int endByte   = nav.ToByteIndex(oldPos);
        string deleted = _rope.SliceBytes(startByte, endByte);
        PushUndo(new EditOp.Delete(startByte, deleted));
        _rope.RemoveBytes(startByte, endByte);
        _cursor = new CursorNavigator(_rope).FromByteIndex(startByte);
        return true;
    }

    /// <summary>Delete the character after the cursor (delete key).</summary>
    public bool DeleteForward()
    {
        if (DeleteSelectionInner()) return true;
        var nav     = new CursorNavigator(_rope);
        var oldPos  = _cursor;
        var nextPos = nav.MoveRight(oldPos);
        if (nextPos == oldPos) return false;

        int startByte = nav.ToByteIndex(oldPos);
        int endByte   = nav.ToByteIndex(nextPos);
        string deleted = _rope.SliceBytes(startByte, endByte);
        PushUndo(new EditOp.Delete(startByte, deleted));
        _rope.RemoveBytes(startByte, endByte);
        _cursor = new CursorNavigator(_rope).Clamp(_cursor);
        return true;
    }

    /// <summary>Delete the word before the cursor (Ctrl+Backspace).</summary>
    public bool DeleteWordBackward()
    {
        if (DeleteSelectionInner()) return true;
        var nav       = new CursorNavigator(_rope);
        var oldPos    = _cursor;
        var wordStart = nav.MoveWordLeft(oldPos);
        if (wordStart == oldPos) return false;

        int startByte = nav.ToByteIndex(wordStart);
        int endByte   = nav.ToByteIndex(oldPos);
        string deleted = _rope.SliceBytes(startByte, endByte);
        PushUndo(new EditOp.Delete(startByte, deleted));
        _rope.RemoveBytes(startByte, endByte);
        _cursor = new CursorNavigator(_rope).FromByteIndex(startByte);
        return true;
    }

    /// <summary>Delete the word after the cursor (Ctrl+Delete).</summary>
    public bool DeleteWordForward()
    {
        if (DeleteSelectionInner()) return true;
        var nav     = new CursorNavigator(_rope);
        var oldPos  = _cursor;
        var wordEnd = nav.MoveWordRight(oldPos);
        if (wordEnd == oldPos) return false;

        int startByte = nav.ToByteIndex(oldPos);
        int endByte   = nav.ToByteIndex(wordEnd);
        string deleted = _rope.SliceBytes(startByte, endByte);
        PushUndo(new EditOp.Delete(startByte, deleted));
        _rope.RemoveBytes(startByte, endByte);
        _cursor = new CursorNavigator(_rope).Clamp(_cursor);
        return true;
    }

    /// <summary>Delete from cursor to end of line (Ctrl+K).</summary>
    public bool DeleteToEndOfLine()
    {
        if (DeleteSelectionInner()) return true;
        var nav     = new CursorNavigator(_rope);
        var oldPos  = _cursor;
        var lineEnd = nav.LineEnd(oldPos);
        if (lineEnd == oldPos)
            return DeleteForward(); // join lines

        int startByte = nav.ToByteIndex(oldPos);
        int endByte   = nav.ToByteIndex(lineEnd);
        string deleted = _rope.SliceBytes(startByte, endByte);
        PushUndo(new EditOp.Delete(startByte, deleted));
        _rope.RemoveBytes(startByte, endByte);
        _cursor = new CursorNavigator(_rope).Clamp(_cursor);
        return true;
    }

    // ── Undo / redo ───────────────────────────────────────────────────────────

    private void PushUndo(EditOp op)
    {
        _undoStack.Add((op, _cursor));
        _currentUndoSize += op.ByteLen();

        if (_undoStack.Count > _maxHistory)
        {
            _currentUndoSize -= _undoStack[0].op.ByteLen();
            _undoStack.RemoveAt(0);
        }
        while (_currentUndoSize > _maxUndoSize && _undoStack.Count > 0)
        {
            _currentUndoSize -= _undoStack[0].op.ByteLen();
            _undoStack.RemoveAt(0);
        }
        _redoStack.Clear();
    }

    /// <summary>Undo the last edit operation.</summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;
        var (op, cursorBefore) = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _currentUndoSize -= op.ByteLen();
        var inverse = op.Inverse();
        ApplyOp(inverse);
        _redoStack.Add((inverse, _cursor));
        _cursor    = cursorBefore;
        _selection = null;
        return true;
    }

    /// <summary>Redo the last undone operation.</summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;
        var (op, cursorBefore) = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        var inverse = op.Inverse();
        ApplyOp(inverse);
        _undoStack.Add((inverse, _cursor));
        _currentUndoSize += inverse.ByteLen();
        while (_currentUndoSize > _maxUndoSize && _undoStack.Count > 0)
        {
            _currentUndoSize -= _undoStack[0].op.ByteLen();
            _undoStack.RemoveAt(0);
        }
        _cursor    = cursorBefore;
        _selection = null;
        _cursor    = new CursorNavigator(_rope).Clamp(_cursor);
        return true;
    }

    private void ApplyOp(EditOp op)
    {
        switch (op)
        {
            case EditOp.Insert i:
                _rope.InsertAtByte(i.ByteOffset, i.Text);
                break;
            case EditOp.Delete d:
                _rope.RemoveBytes(d.ByteOffset, d.ByteOffset + Encoding.UTF8.GetByteCount(d.Text));
                break;
            case EditOp.Replace r:
                _rope.RemoveBytes(r.ByteOffset, r.ByteOffset + Encoding.UTF8.GetByteCount(r.Deleted));
                _rope.InsertAtByte(r.ByteOffset, r.Inserted);
                break;
        }
    }

    // ── Selection helpers ─────────────────────────────────────────────────────

    private (int startByte, string deleted)? ExtractSelection()
    {
        if (_selection is not Selection sel) return null;
        if (sel.IsEmpty()) { _selection = null; return null; }

        _selection = null;
        var nav    = new CursorNavigator(_rope);
        var (startByte, endByte) = sel.ByteRange(nav);
        string deleted = _rope.SliceBytes(startByte, endByte);
        _rope.RemoveBytes(startByte, endByte);
        _cursor = new CursorNavigator(_rope).FromByteIndex(startByte);
        return (startByte, deleted);
    }

    private bool DeleteSelectionInner()
    {
        if (ExtractSelection() is not { } pair) return false;
        PushUndo(new EditOp.Delete(pair.startByte, pair.deleted));
        return true;
    }

    // ── Cursor movement ───────────────────────────────────────────────────────

    public void MoveLeft()  { _selection = null; _cursor = new CursorNavigator(_rope).MoveLeft(_cursor);  }
    public void MoveRight() { _selection = null; _cursor = new CursorNavigator(_rope).MoveRight(_cursor); }
    public void MoveUp()    { _selection = null; _cursor = new CursorNavigator(_rope).MoveUp(_cursor);    }
    public void MoveDown()  { _selection = null; _cursor = new CursorNavigator(_rope).MoveDown(_cursor);  }
    public void MoveWordLeft()  { _selection = null; _cursor = new CursorNavigator(_rope).MoveWordLeft(_cursor);  }
    public void MoveWordRight() { _selection = null; _cursor = new CursorNavigator(_rope).MoveWordRight(_cursor); }
    public void MoveToLineStart()     { _selection = null; _cursor = new CursorNavigator(_rope).LineStart(_cursor); }
    public void MoveToLineEnd()       { _selection = null; _cursor = new CursorNavigator(_rope).LineEnd(_cursor);   }
    public void MoveToDocumentStart() { _selection = null; _cursor = new CursorNavigator(_rope).DocumentStart();    }
    public void MoveToDocumentEnd()   { _selection = null; _cursor = new CursorNavigator(_rope).DocumentEnd();      }

    // ── Selection extension ───────────────────────────────────────────────────

    public void SelectLeft()      => ExtendSelection(nav => nav.MoveLeft(_cursor));
    public void SelectRight()     => ExtendSelection(nav => nav.MoveRight(_cursor));
    public void SelectUp()        => ExtendSelection(nav => nav.MoveUp(_cursor));
    public void SelectDown()      => ExtendSelection(nav => nav.MoveDown(_cursor));
    public void SelectWordLeft()  => ExtendSelection(nav => nav.MoveWordLeft(_cursor));
    public void SelectWordRight() => ExtendSelection(nav => nav.MoveWordRight(_cursor));

    public void SelectAll()
    {
        var nav   = new CursorNavigator(_rope);
        var start = nav.DocumentStart();
        var end   = nav.DocumentEnd();
        _selection = new Selection { Anchor = start, Head = end };
        _cursor    = end;
    }

    public void ClearSelection() => _selection = null;

    /// <summary>Get selected text, if any non-empty selection exists.</summary>
    public string? SelectedText()
    {
        if (_selection is not Selection sel || sel.IsEmpty()) return null;
        var nav = new CursorNavigator(_rope);
        var (start, end) = sel.ByteRange(nav);
        return _rope.SliceBytes(start, end);
    }

    private void ExtendSelection(Func<CursorNavigator, CursorPosition> moveFunc)
    {
        var nav     = new CursorNavigator(_rope);
        var newHead = moveFunc(nav);
        ExtendSelectionTo(newHead);
    }

    /// <summary>Extend selection to a specific cursor position.</summary>
    public void ExtendSelectionTo(CursorPosition newHead)
    {
        var anchor  = _selection is Selection sel ? sel.Anchor : _cursor;
        var nav     = new CursorNavigator(_rope);
        newHead     = nav.Clamp(newHead);
        _cursor     = newHead;
        _selection  = new Selection { Anchor = anchor, Head = newHead };
    }

    // ── Content replacement ───────────────────────────────────────────────────

    /// <summary>Replace all content and reset cursor to end. Clears undo history.</summary>
    public void SetText(string text)
    {
        _rope.Replace(text);
        _cursor    = new CursorNavigator(_rope).DocumentEnd();
        _selection = null;
        _undoStack.Clear();
        _redoStack.Clear();
        _currentUndoSize = 0;
    }

    /// <summary>Clear all content and reset cursor. Clears undo history.</summary>
    public void Clear()
    {
        _rope.Clear();
        _cursor    = CursorPosition.Default;
        _selection = null;
        _undoStack.Clear();
        _redoStack.Clear();
        _currentUndoSize = 0;
    }
}
