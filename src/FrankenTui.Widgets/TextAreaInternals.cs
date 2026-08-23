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
/// Mutable UTF-8-coordinate text storage.
/// Port of <c>ftui_text::rope::Rope</c> at upstream basis
/// <c>15cc6543f76b814394c590f9e7719dedd6684e4c</c>.
/// </summary>
/// <remarks>
/// The managed realization deliberately uses an immutable <see cref="string"/> as its
/// backing store rather than Ropey's B-tree. Public coordinates named <c>char</c> in
/// upstream are Unicode scalar coordinates and are represented here by <see cref="Rune"/>
/// indices, never UTF-16 code-unit indices.
///
/// Two pre-existing editor contracts remain as compatibility projections:
/// <see cref="Line(int)"/> excludes a line terminator, while the source-equivalent
/// <see cref="LineWithTerminator(int)"/> includes it; <see cref="ByteToLineCol(int)"/>
/// returns a UTF-8 byte column, while <see cref="ByteToScalarLineCol(int)"/> is the
/// source-equivalent scalar-column operation.
/// </remarks>
public sealed class Rope : ICloneable
{
    private string _text;

    private readonly record struct LineSpan(int Start, int ContentEnd, int End);

    /// <summary>Create an empty rope.</summary>
    public Rope() : this(string.Empty) { }

    /// <summary>Create a rope from valid UTF-16 text.</summary>
    /// <exception cref="ArgumentException">The text contains an unpaired surrogate.</exception>
    public Rope(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        _text = text;
    }

    /// <summary>Create a rope from text.</summary>
    public static Rope FromText(string text) => new(text);

    /// <summary>Parse text into a rope. Parsing cannot fail for valid UTF-16 text.</summary>
    public static Rope Parse(string text) => new(text);

    /// <summary>Try to parse text into a rope.</summary>
    public static bool TryParse(string? text, out Rope? rope)
    {
        if (text is null || !IsValidUtf16(text))
        {
            rope = null;
            return false;
        }

        rope = new Rope(text);
        return true;
    }

    /// <summary>Source <c>From&lt;&amp;str&gt;</c>/<c>From&lt;String&gt;</c> projection.</summary>
    public static implicit operator Rope(string text) => new(text);

    /// <summary>Create an independent copy.</summary>
    public Rope Clone() => new(_text);

    object ICloneable.Clone() => Clone();

    /// <summary>Total length in UTF-8 bytes.</summary>
    public int LenBytes() => Encoding.UTF8.GetByteCount(_text);

    /// <summary>Total length in Unicode scalar values.</summary>
    public int LenChars() => CountScalars(_text);

    /// <summary>Total line count. An empty rope has one line.</summary>
    public int LenLines() => GetLineSpans(_text).Count;

    /// <summary>Whether the rope contains no text.</summary>
    public bool IsEmpty() => _text.Length == 0;

    /// <summary>
    /// Get a line without its terminator. This preserves the original TextArea contract.
    /// Use <see cref="LineWithTerminator(int)"/> for source <c>Rope::line</c> semantics.
    /// </summary>
    public string? Line(int index)
    {
        List<LineSpan> spans = GetLineSpans(_text);
        if (index < 0 || index >= spans.Count) return null;
        LineSpan span = spans[index];
        return _text[span.Start..span.ContentEnd];
    }

    /// <summary>Get a line including its terminator, matching upstream <c>Rope::line</c>.</summary>
    public string? LineWithTerminator(int index)
    {
        List<LineSpan> spans = GetLineSpans(_text);
        if (index < 0 || index >= spans.Count) return null;
        LineSpan span = spans[index];
        return _text[span.Start..span.End];
    }

    /// <summary>Enumerate lines including their terminators.</summary>
    public IEnumerable<string> Lines()
    {
        string snapshot = _text;
        foreach (LineSpan span in GetLineSpans(snapshot))
            yield return snapshot[span.Start..span.End];
    }

    /// <summary>Get a Unicode-scalar slice. Invalid ranges return the empty string.</summary>
    public string Slice(Range range)
    {
        int max = LenChars();
        if (!TryResolveRange(range, max, out int start, out int end)) return string.Empty;
        return Slice(start, end);
    }

    /// <summary>Get the half-open Unicode-scalar slice <c>[start, end)</c>.</summary>
    public string Slice(int start, int end)
    {
        int max = LenChars();
        if (start < 0 || end < start || start > max || end > max) return string.Empty;
        int utf16Start = Utf16OffsetFromScalarIndex(start);
        int utf16End = Utf16OffsetFromScalarIndex(end);
        return _text[utf16Start..utf16End];
    }

    /// <summary>Get the inclusive Unicode-scalar slice <c>[start, end]</c>.</summary>
    public string SliceInclusive(int start, int end)
    {
        if (end == int.MaxValue) return string.Empty;
        return Slice(start, end + 1);
    }

    /// <summary>Insert text at a Unicode-scalar index. Indices beyond the end clamp.</summary>
    public void Insert(int charIndex, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        int offset = Utf16OffsetFromScalarIndex(Math.Clamp(charIndex, 0, LenChars()));
        _text = _text.Insert(offset, text);
    }

    /// <summary>Insert text at an extended-grapheme-cluster index.</summary>
    public void InsertGrapheme(int graphemeIndex, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        int offset = Utf16OffsetFromGraphemeIndex(Math.Max(0, graphemeIndex));
        _text = _text.Insert(offset, text);
    }

    /// <summary>Remove a half-open Unicode-scalar range.</summary>
    public void Remove(Range range)
    {
        int max = LenChars();
        if (!TryResolveRemovalRange(range, max, out int start, out int end)) return;
        Remove(start, end);
    }

    /// <summary>
    /// Remove a Unicode-scalar range. Set <paramref name="endInclusive"/> to represent
    /// Rust's inclusive <c>RangeBounds</c>; endpoints are clamped like upstream.
    /// </summary>
    public void Remove(int start, int end, bool endInclusive = false)
    {
        (start, end) = NormalizeRange(start, end, endInclusive, LenChars());
        if (start >= end) return;
        int utf16Start = Utf16OffsetFromScalarIndex(start);
        int utf16End = Utf16OffsetFromScalarIndex(end);
        _text = _text.Remove(utf16Start, utf16End - utf16Start);
    }

    /// <summary>Remove a half-open extended-grapheme-cluster range.</summary>
    public void RemoveGraphemeRange(Range range)
    {
        int max = GraphemeCount();
        if (!TryResolveRemovalRange(range, max, out int start, out int end)) return;
        RemoveGraphemeRange(start, end);
    }

    /// <summary>Remove an extended-grapheme-cluster range.</summary>
    public void RemoveGraphemeRange(int start, int end, bool endInclusive = false)
    {
        if (endInclusive && end < int.MaxValue) end++;
        start = Math.Max(0, start);
        end = Math.Max(0, end);
        start = Math.Min(start, end);
        if (start >= end) return;

        int utf16Start = Utf16OffsetFromGraphemeIndex(start);
        int utf16End = Utf16OffsetFromGraphemeIndex(end);
        if (utf16Start < utf16End)
            _text = _text.Remove(utf16Start, utf16End - utf16Start);
    }

    /// <summary>Replace the entire contents.</summary>
    public void Replace(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        _text = text;
    }

    /// <summary>Append text to the end.</summary>
    public void Append(string text) => Insert(LenChars(), text);

    /// <summary>Clear all content.</summary>
    public void Clear() => _text = string.Empty;

    /// <summary>Convert a Unicode-scalar index to a UTF-8 byte index.</summary>
    public int CharToByte(int charIndex)
    {
        int offset = Utf16OffsetFromScalarIndex(Math.Clamp(charIndex, 0, LenChars()));
        return Encoding.UTF8.GetByteCount(_text.AsSpan(0, offset));
    }

    /// <summary>
    /// Convert a UTF-8 byte index to a Unicode-scalar index. An index inside a
    /// multibyte sequence maps to the scalar containing that byte, as Ropey does.
    /// </summary>
    public int ByteToChar(int byteIndex)
    {
        int target = Math.Clamp(byteIndex, 0, LenBytes());
        int bytes = 0;
        int scalars = 0;
        foreach (Rune rune in _text.EnumerateRunes())
        {
            int next = bytes + rune.Utf8SequenceLength;
            if (target < next) return scalars;
            bytes = next;
            scalars++;
        }
        return scalars;
    }

    /// <summary>Convert a Unicode-scalar index to a line index.</summary>
    public int CharToLine(int charIndex)
    {
        int target = Math.Clamp(charIndex, 0, LenChars());
        List<LineSpan> spans = GetLineSpans(_text);
        int line = 0;
        for (int i = 1; i < spans.Count; i++)
        {
            int lineStart = ScalarIndexFromUtf16Offset(spans[i].Start);
            if (lineStart > target) break;
            line = i;
        }
        return line;
    }

    /// <summary>Get the Unicode-scalar index at the start of a line.</summary>
    public int LineToChar(int lineIndex)
    {
        List<LineSpan> spans = GetLineSpans(_text);
        if (lineIndex < 0) return 0;
        if (lineIndex >= spans.Count) return LenChars();
        return ScalarIndexFromUtf16Offset(spans[lineIndex].Start);
    }

    /// <summary>
    /// Convert a UTF-8 byte index to a line and UTF-8 byte column. This is the
    /// historical TextArea projection; use <see cref="ByteToScalarLineCol(int)"/>
    /// for the source-equivalent Unicode-scalar column.
    /// </summary>
    public (int line, int byteInLine) ByteToLineCol(int byteOffset)
    {
        int byteIndex = Math.Clamp(byteOffset, 0, LenBytes());
        int line = CharToLine(ByteToChar(byteIndex));
        return (line, byteIndex - LineStartByte(line));
    }

    /// <summary>Convert a UTF-8 byte index to a line and Unicode-scalar column.</summary>
    public (int line, int column) ByteToScalarLineCol(int byteIndex)
    {
        int charIndex = ByteToChar(byteIndex);
        int line = CharToLine(charIndex);
        return (line, charIndex - LineToChar(line));
    }

    /// <summary>Convert a line and Unicode-scalar column to a UTF-8 byte index.</summary>
    public int LineColToByte(int lineIndex, int column)
    {
        if (lineIndex < 0) return 0;
        int lineStart = LineToChar(lineIndex);
        int nextLineStart = lineIndex < LenLines() - 1
            ? LineToChar(lineIndex + 1)
            : LenChars();
        int lineLength = Math.Max(0, nextLineStart - lineStart);
        int charIndex = Math.Min(LenChars(), lineStart + Math.Clamp(column, 0, lineLength));
        return CharToByte(charIndex);
    }

    /// <summary>Get the UTF-8 byte index at the start of a line.</summary>
    public int LineStartByte(int lineIndex) => CharToByte(LineToChar(lineIndex));

    /// <summary>Enumerate Unicode scalar values.</summary>
    public IEnumerable<Rune> Chars()
    {
        string snapshot = _text;
        int offset = 0;
        while (offset < snapshot.Length)
        {
            Rune.DecodeFromUtf16(snapshot.AsSpan(offset), out Rune rune, out int consumed);
            offset += consumed;
            yield return rune;
        }
    }

    /// <summary>Return all extended grapheme clusters.</summary>
    public IReadOnlyList<string> Graphemes()
    {
        if (_text.Length == 0) return Array.Empty<string>();
        int[] starts = StringInfo.ParseCombiningCharacters(_text);
        var result = new string[starts.Length];
        for (int i = 0; i < starts.Length; i++)
        {
            int end = i + 1 < starts.Length ? starts[i + 1] : _text.Length;
            result[i] = _text[starts[i]..end];
        }
        return result;
    }

    /// <summary>Count extended grapheme clusters.</summary>
    public int GraphemeCount() => StringInfo.ParseCombiningCharacters(_text).Length;

    /// <summary>Insert text at a UTF-8 byte boundary (legacy editor helper).</summary>
    /// <exception cref="ArgumentException">The byte index is inside a UTF-8 sequence.</exception>
    public void InsertAtByte(int byteOffset, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        int utf16Offset = Utf16OffsetFromByteBoundary(byteOffset);
        _text = _text.Insert(utf16Offset, text);
    }

    /// <summary>Remove UTF-8 bytes in <c>[startByte, endByte)</c> (legacy editor helper).</summary>
    /// <exception cref="ArgumentException">Either index is inside a UTF-8 sequence.</exception>
    public void RemoveBytes(int startByte, int endByte)
    {
        startByte = Math.Clamp(startByte, 0, LenBytes());
        endByte = Math.Clamp(endByte, 0, LenBytes());
        if (startByte >= endByte) return;
        int start = Utf16OffsetFromByteBoundary(startByte);
        int end = Utf16OffsetFromByteBoundary(endByte);
        _text = _text.Remove(start, end - start);
    }

    /// <summary>Slice UTF-8 bytes in <c>[startByte, endByte)</c> (legacy editor helper).</summary>
    /// <exception cref="ArgumentException">Either index is inside a UTF-8 sequence.</exception>
    public string SliceBytes(int startByte, int endByte)
    {
        startByte = Math.Clamp(startByte, 0, LenBytes());
        endByte = Math.Clamp(endByte, 0, LenBytes());
        if (startByte >= endByte) return string.Empty;
        int start = Utf16OffsetFromByteBoundary(startByte);
        int end = Utf16OffsetFromByteBoundary(endByte);
        return _text[start..end];
    }

    /// <summary>
    /// Convert a UTF-8 byte offset to a UTF-16 code-unit offset. An interior byte
    /// maps to the start of its containing scalar. This is a managed-only adapter.
    /// </summary>
    public static int ByteOffsetToCharOffset(string text, int byteOffset)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        int target = Math.Clamp(byteOffset, 0, Encoding.UTF8.GetByteCount(text));
        int bytes = 0;
        int utf16 = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            int next = bytes + rune.Utf8SequenceLength;
            if (target < next) return utf16;
            bytes = next;
            utf16 += rune.Utf16SequenceLength;
        }
        return utf16;
    }

    /// <summary>
    /// Convert a UTF-16 code-unit offset to a UTF-8 byte offset. An offset between
    /// a surrogate pair maps to the start of the represented scalar.
    /// </summary>
    public static int CharOffsetToByteOffset(string text, int charOffset)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateUtf16(text);
        int offset = Math.Clamp(charOffset, 0, text.Length);
        if (offset > 0 && offset < text.Length &&
            char.IsHighSurrogate(text[offset - 1]) && char.IsLowSurrogate(text[offset]))
        {
            offset--;
        }
        return Encoding.UTF8.GetByteCount(text.AsSpan(0, offset));
    }

    /// <summary>Full text, matching the source Display implementation.</summary>
    public override string ToString() => _text;

    private int Utf16OffsetFromScalarIndex(int scalarIndex)
    {
        int target = Math.Clamp(scalarIndex, 0, LenChars());
        int scalars = 0;
        int utf16 = 0;
        foreach (Rune rune in _text.EnumerateRunes())
        {
            if (scalars == target) return utf16;
            scalars++;
            utf16 += rune.Utf16SequenceLength;
        }
        return utf16;
    }

    private int ScalarIndexFromUtf16Offset(int utf16Offset)
    {
        int target = Math.Clamp(utf16Offset, 0, _text.Length);
        int scalars = 0;
        int utf16 = 0;
        foreach (Rune rune in _text.EnumerateRunes())
        {
            if (utf16 >= target) break;
            utf16 += rune.Utf16SequenceLength;
            scalars++;
        }
        return scalars;
    }

    private int Utf16OffsetFromGraphemeIndex(int graphemeIndex)
    {
        if (graphemeIndex <= 0 || _text.Length == 0) return 0;
        int[] starts = StringInfo.ParseCombiningCharacters(_text);
        return graphemeIndex >= starts.Length ? _text.Length : starts[graphemeIndex];
    }

    private int Utf16OffsetFromByteBoundary(int byteOffset)
    {
        int target = Math.Clamp(byteOffset, 0, LenBytes());
        int bytes = 0;
        int utf16 = 0;
        foreach (Rune rune in _text.EnumerateRunes())
        {
            if (bytes == target) return utf16;
            int next = bytes + rune.Utf8SequenceLength;
            if (target < next)
                throw new ArgumentException("The byte index must be on a UTF-8 scalar boundary.", nameof(byteOffset));
            bytes = next;
            utf16 += rune.Utf16SequenceLength;
        }
        return utf16;
    }

    private static (int start, int end) NormalizeRange(int start, int end, bool endInclusive, int max)
    {
        start = Math.Clamp(start, 0, max);
        if (endInclusive && end < int.MaxValue) end++;
        end = Math.Clamp(end, 0, max);
        return end < start ? (start, start) : (start, end);
    }

    private static bool TryResolveRange(Range range, int max, out int start, out int end)
    {
        if (!TryResolveIndex(range.Start, max, out start) ||
            !TryResolveIndex(range.End, max, out end) || end < start)
        {
            start = end = 0;
            return false;
        }
        return true;
    }

    private static bool TryResolveRemovalRange(Range range, int max, out int start, out int end)
    {
        if (!TryResolveRemovalIndex(range.Start, max, out start) ||
            !TryResolveRemovalIndex(range.End, max, out end))
        {
            start = end = 0;
            return false;
        }
        return true;
    }

    private static bool TryResolveRemovalIndex(Index index, int max, out int value)
    {
        if (!index.IsFromEnd)
        {
            value = index.Value;
            return true;
        }

        if (index.Value > max)
        {
            value = 0;
            return false;
        }
        value = max - index.Value;
        return true;
    }

    private static bool TryResolveIndex(Index index, int max, out int value)
    {
        if (index.IsFromEnd)
        {
            if (index.Value > max)
            {
                value = 0;
                return false;
            }
            value = max - index.Value;
            return true;
        }

        value = index.Value;
        return value <= max;
    }

    private static int CountScalars(string text)
    {
        int count = 0;
        foreach (Rune _ in text.EnumerateRunes()) count++;
        return count;
    }

    private static List<LineSpan> GetLineSpans(string text)
    {
        var result = new List<LineSpan>();
        int lineStart = 0;
        int offset = 0;
        while (offset < text.Length)
        {
            int terminatorLength = text[offset] switch
            {
                '\r' when offset + 1 < text.Length && text[offset + 1] == '\n' => 2,
                '\r' or '\n' or '\v' or '\f' or '\u0085' or '\u2028' or '\u2029' => 1,
                _ => 0,
            };

            if (terminatorLength > 0)
            {
                result.Add(new LineSpan(lineStart, offset, offset + terminatorLength));
                offset += terminatorLength;
                lineStart = offset;
                continue;
            }

            Rune.DecodeFromUtf16(text.AsSpan(offset), out _, out int consumed);
            offset += consumed;
        }

        result.Add(new LineSpan(lineStart, text.Length, text.Length));
        return result;
    }

    private static bool IsValidUtf16(string text)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            if (Rune.DecodeFromUtf16(text.AsSpan(offset), out _, out int consumed) !=
                System.Buffers.OperationStatus.Done)
            {
                return false;
            }
            offset += consumed;
        }
        return true;
    }

    private static void ValidateUtf16(string text)
    {
        if (!IsValidUtf16(text))
            throw new ArgumentException("Text must contain well-formed UTF-16.", nameof(text));
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
