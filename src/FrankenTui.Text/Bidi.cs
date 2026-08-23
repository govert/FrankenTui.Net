// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-text/src/bidi.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Buffers;
using System.Text;
using FrankenTui.Text.BidiInternals;

namespace FrankenTui.Text;

/// <summary>A resolved Unicode Bidirectional Algorithm embedding level.</summary>
public readonly record struct BidiLevel
{
    /// <summary>Create a valid UAX #9 level.</summary>
    public BidiLevel(byte value)
    {
        if (value > 125) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    /// <summary>Numeric embedding level.</summary>
    public byte Value { get; }

    /// <summary>Whether this level resolves left-to-right.</summary>
    public bool IsLeftToRight => (Value & 1) == 0;

    /// <summary>Whether this level resolves right-to-left.</summary>
    public bool IsRightToLeft => (Value & 1) != 0;

    /// <summary>Base left-to-right level.</summary>
    public static BidiLevel LeftToRight => new(0);

    /// <summary>Base right-to-left level.</summary>
    public static BidiLevel RightToLeft => new(1);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>A contiguous logical-scalar run sharing a resolved bidi level.</summary>
public sealed record BidiRun
{
    public BidiRun(int start, int end, BidiLevel level, TextDirection direction)
    {
        if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end));
        if (direction == TextDirection.Neutral)
            throw new ArgumentOutOfRangeException(nameof(direction), "A resolved run cannot be neutral.");
        Start = start;
        End = end;
        Level = level;
        Direction = direction;
    }

    /// <summary>Inclusive start in logical Unicode-scalar coordinates.</summary>
    public int Start { get; }

    /// <summary>Exclusive end in logical Unicode-scalar coordinates.</summary>
    public int End { get; }

    public BidiLevel Level { get; }

    public TextDirection Direction { get; }

    public int Length => End - Start;

    public bool IsEmpty => Start == End;
}

/// <summary>
/// Precomputed bidirectional analysis with O(1) logical/visual scalar mapping.
/// </summary>
/// <remarks>
/// Every index is a Unicode-scalar index. It is neither a UTF-8 byte offset nor
/// a UTF-16 code-unit offset. Grapheme and terminal-cell coordinates remain the
/// responsibility of <see cref="ClusterMap"/> and the renderer.
/// </remarks>
public sealed class BidiSegment
{
    private readonly Rune[] chars;
    private readonly BidiLevel[] levels;
    private readonly BidiRun[] runs;
    private readonly int[] visualToLogical;
    private readonly int[] logicalToVisual;

    /// <summary>
    /// Analyze text. A null base auto-detects the first strong scalar; a forced
    /// base must be left-to-right or right-to-left.
    /// </summary>
    public BidiSegment(string text, TextDirection? baseDirection = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Bidi.ValidateText(text);
        if (baseDirection == TextDirection.Neutral)
            throw new ArgumentOutOfRangeException(nameof(baseDirection), "Use null for auto detection.");

        Text = text;
        BidiResolution resolution = BidiResolver.Resolve(text, baseDirection);
        chars = resolution.Chars;
        levels = resolution.Levels.Select(static level => new BidiLevel(checked((byte)level))).ToArray();
        runs = ComputeRuns(levels);
        visualToLogical = ComputeVisualOrder(levels);
        logicalToVisual = InvertPermutation(visualToLogical);
    }

    public string Text { get; }

    /// <summary>Unicode scalars in logical order.</summary>
    public IReadOnlyList<Rune> Chars => chars;

    /// <summary>Resolved level for each logical Unicode scalar.</summary>
    public IReadOnlyList<BidiLevel> Levels => levels;

    public IReadOnlyList<BidiRun> Runs => runs;

    /// <summary><c>VisualToLogical[visual] == logical</c>.</summary>
    public IReadOnlyList<int> VisualToLogical => visualToLogical;

    /// <summary><c>LogicalToVisual[logical] == visual</c>.</summary>
    public IReadOnlyList<int> LogicalToVisual => logicalToVisual;

    public int Length => chars.Length;

    public bool IsEmpty => chars.Length == 0;

    public int VisualPosition(int logical)
        => (uint)logical < (uint)logicalToVisual.Length ? logicalToVisual[logical] : logical;

    public int LogicalPosition(int visual)
        => (uint)visual < (uint)visualToLogical.Length ? visualToLogical[visual] : visual;

    public bool IsRightToLeft(int logical)
        => (uint)logical < (uint)levels.Length && levels[logical].IsRightToLeft;

    /// <summary>Map a logical insertion boundary in <c>0..=Length</c> to visual space.</summary>
    public int VisualCursorPosition(int logical)
    {
        ValidateLogicalCursor(logical);
        int n = chars.Length;
        if (n == 0) return 0;

        byte baseLevel = BaseDirection == TextDirection.LeftToRight ? (byte)0 : (byte)1;
        byte levelLeft = logical > 0 ? levels[logical - 1].Value : baseLevel;
        byte levelRight = logical < n ? levels[logical].Value : baseLevel;

        if (levelLeft <= levelRight)
        {
            if (logical == 0) return (baseLevel & 1) == 0 ? 0 : n;
            int previous = logical - 1;
            int visual = logicalToVisual[previous];
            return levels[previous].IsRightToLeft ? visual : visual + 1;
        }

        if (logical == n) return (baseLevel & 1) == 0 ? n : 0;
        int currentVisual = logicalToVisual[logical];
        return levels[logical].IsRightToLeft ? currentVisual + 1 : currentVisual;
    }

    /// <summary>Map a visual insertion boundary to logical scalar space.</summary>
    public int LogicalCursorPosition(int visual)
    {
        if (visual < 0) throw new ArgumentOutOfRangeException(nameof(visual));
        int n = chars.Length;
        if (n == 0) return 0;

        if (visual >= n)
        {
            int logicalLeft = LogicalPosition(n - 1);
            return levels[logicalLeft].IsLeftToRight ? logicalLeft + 1 : logicalLeft;
        }

        if (visual == 0)
        {
            int logicalRight = LogicalPosition(0);
            return levels[logicalRight].IsLeftToRight ? logicalRight : logicalRight + 1;
        }

        int left = LogicalPosition(visual - 1);
        int right = LogicalPosition(visual);
        bool leftLtr = levels[left].IsLeftToRight;
        bool rightLtr = levels[right].IsLeftToRight;
        int candidateLeft = leftLtr ? left + 1 : left;
        int candidateRight = rightLtr ? right : right + 1;
        if (candidateLeft == candidateRight) return candidateLeft;

        bool baseLtr = BaseDirection == TextDirection.LeftToRight;
        return leftLtr == baseLtr ? candidateLeft : candidateRight;
    }

    public int MoveRight(int logical)
    {
        int visual = VisualCursorPosition(logical);
        return visual < chars.Length ? LogicalCursorPosition(visual + 1) : logical;
    }

    public int MoveLeft(int logical)
    {
        int visual = VisualCursorPosition(logical);
        return visual > 0 ? LogicalCursorPosition(visual - 1) : logical;
    }

    public TextDirection BaseDirection
    {
        get
        {
            byte minimum = levels.Length == 0 ? (byte)0 : levels.Min(static level => level.Value);
            return (minimum & 1) == 0 ? TextDirection.LeftToRight : TextDirection.RightToLeft;
        }
    }

    public Rune? CharacterAtVisual(int visual)
        => (uint)visual < (uint)visualToLogical.Length ? chars[visualToLogical[visual]] : null;

    public string VisualString()
    {
        var result = new StringBuilder(Text.Length);
        foreach (int logical in visualToLogical) result.Append(chars[logical].ToString());
        return result.ToString();
    }

    private void ValidateLogicalCursor(int logical)
    {
        if ((uint)logical > (uint)chars.Length) throw new ArgumentOutOfRangeException(nameof(logical));
    }

    private static BidiRun[] ComputeRuns(IReadOnlyList<BidiLevel> resolvedLevels)
    {
        if (resolvedLevels.Count == 0) return Array.Empty<BidiRun>();
        var result = new List<BidiRun>();
        int start = 0;
        BidiLevel current = resolvedLevels[0];
        for (int i = 1; i < resolvedLevels.Count; i++)
        {
            if (resolvedLevels[i] == current) continue;
            result.Add(CreateRun(start, i, current));
            start = i;
            current = resolvedLevels[i];
        }
        result.Add(CreateRun(start, resolvedLevels.Count, current));
        return result.ToArray();
    }

    private static BidiRun CreateRun(int start, int end, BidiLevel level)
        => new(start, end, level,
            level.IsRightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight);

    private static int[] ComputeVisualOrder(IReadOnlyList<BidiLevel> resolvedLevels)
    {
        int n = resolvedLevels.Count;
        int[] order = Enumerable.Range(0, n).ToArray();
        if (n == 0) return order;

        byte maximum = resolvedLevels.Max(static level => level.Value);
        byte? minimumOdd = resolvedLevels
            .Where(static level => level.IsRightToLeft)
            .Select(static level => (byte?)level.Value)
            .Min();
        if (minimumOdd is null) return order;

        for (int targetLevel = maximum; targetLevel >= minimumOdd.Value; targetLevel--)
        {
            int i = 0;
            while (i < n)
            {
                if (resolvedLevels[order[i]].Value < targetLevel)
                {
                    i++;
                    continue;
                }

                int start = i;
                while (i < n && resolvedLevels[order[i]].Value >= targetLevel) i++;
                Array.Reverse(order, start, i - start);
            }
        }
        return order;
    }

    private static int[] InvertPermutation(IReadOnlyList<int> permutation)
    {
        var inverse = new int[permutation.Count];
        for (int visual = 0; visual < permutation.Count; visual++) inverse[permutation[visual]] = visual;
        return inverse;
    }
}

/// <summary>Unicode Bidirectional Algorithm utilities.</summary>
public static class Bidi
{
    /// <summary>Unicode version of the pinned upstream bidi property table.</summary>
    public static Version UnicodeDataVersion { get; } = new(16, 0, 0);

    /// <summary>
    /// Reorder text for left-to-right terminal emission. <see cref="TextDirection.Neutral"/>
    /// means UAX #9 auto paragraph direction.
    /// </summary>
    public static string Reorder(string text, TextDirection direction = TextDirection.Neutral)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateText(text);
        TextDirection? forced = direction == TextDirection.Neutral ? null : direction;
        BidiResolution resolution = BidiResolver.Resolve(text, forced);
        if (resolution.Chars.Length == 0) return string.Empty;

        var result = new StringBuilder(text.Length);
        foreach (BidiParagraph paragraph in resolution.Paragraphs)
        {
            int[] order = Enumerable.Range(paragraph.Start, paragraph.Length).ToArray();
            ReorderByLevels(order, resolution.Levels);
            foreach (int logical in order) result.Append(resolution.Chars[logical].ToString());
        }
        return result.ToString();
    }

    /// <summary>
    /// Resolve one level per UTF-8 byte, matching <c>unicode-bidi::BidiInfo.levels</c>.
    /// Use <see cref="BidiSegment.Levels"/> for one level per Unicode scalar.
    /// </summary>
    public static IReadOnlyList<BidiLevel> ResolveLevels(
        string text,
        TextDirection direction = TextDirection.Neutral)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateText(text);
        TextDirection? forced = direction == TextDirection.Neutral ? null : direction;
        BidiResolution resolution = BidiResolver.Resolve(text, forced);
        var result = new List<BidiLevel>(Encoding.UTF8.GetByteCount(text));
        for (int i = 0; i < resolution.Chars.Length; i++)
        {
            var level = new BidiLevel(checked((byte)resolution.Levels[i]));
            for (int j = 0; j < resolution.Chars[i].Utf8SequenceLength; j++) result.Add(level);
        }
        return result;
    }

    /// <summary>Whether any scalar has an intrinsically RTL bidi class.</summary>
    public static bool HasRightToLeft(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateText(text);
        foreach (Rune rune in text.EnumerateRunes())
        {
            BidiCharacterType type = UnicodeBidiData.CharacterType(rune.Value);
            if (type is BidiCharacterType.RightToLeft or BidiCharacterType.ArabicLetter or
                BidiCharacterType.RightToLeftEmbedding or BidiCharacterType.RightToLeftOverride or
                BidiCharacterType.RightToLeftIsolate)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Detected direction of the first paragraph. Empty text is LTR.</summary>
    public static TextDirection ParagraphLevel(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateText(text);
        if (text.Length == 0) return TextDirection.LeftToRight;
        BidiResolution resolution = BidiResolver.Resolve(text, null);
        return resolution.Paragraphs.Count == 0 || resolution.Paragraphs[0].BaseLevel % 2 == 0
            ? TextDirection.LeftToRight
            : TextDirection.RightToLeft;
    }

    internal static void ValidateText(string text)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            if (Rune.DecodeFromUtf16(text.AsSpan(offset), out _, out int consumed) != OperationStatus.Done)
                throw new ArgumentException("Text must contain well-formed UTF-16.", nameof(text));
            offset += consumed;
        }
    }

    private static void ReorderByLevels(int[] order, IReadOnlyList<sbyte> levels)
    {
        if (order.Length == 0) return;
        int maximum = order.Max(index => levels[index]);
        int minimumOdd = order
            .Select(index => (int)levels[index])
            .Where(static level => (level & 1) != 0)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        if (minimumOdd == int.MaxValue) return;

        for (int target = maximum; target >= minimumOdd; target--)
        {
            int i = 0;
            while (i < order.Length)
            {
                if (levels[order[i]] < target)
                {
                    i++;
                    continue;
                }
                int start = i;
                while (i < order.Length && levels[order[i]] >= target) i++;
                Array.Reverse(order, start, i - start);
            }
        }
    }
}

internal sealed record BidiResolution(
    Rune[] Chars,
    sbyte[] Levels,
    IReadOnlyList<BidiParagraph> Paragraphs);

internal readonly record struct BidiParagraph(int Start, int Length, sbyte BaseLevel);

internal static class BidiResolver
{
    public static BidiResolution Resolve(string text, TextDirection? forcedDirection)
    {
        Rune[] chars = text.EnumerateRunes().ToArray();
        if (chars.Length == 0)
            return new BidiResolution(chars, Array.Empty<sbyte>(), Array.Empty<BidiParagraph>());

        var types = new BidiCharacterType[chars.Length];
        var bracketTypes = new BidiPairedBracketType[chars.Length];
        var bracketValues = new int[chars.Length];
        for (int i = 0; i < chars.Length; i++)
        {
            types[i] = UnicodeBidiData.CharacterType(chars[i].Value);
            UnicodeBidiData.PairedBracket(chars[i].Value, out bracketTypes[i], out bracketValues[i]);
        }

        var levels = new sbyte[chars.Length];
        var paragraphs = new List<BidiParagraph>();
        int start = 0;
        for (int i = 0; i < chars.Length; i++)
        {
            if (types[i] != BidiCharacterType.ParagraphSeparator) continue;
            ResolveParagraph(start, i - start + 1);
            start = i + 1;
        }
        if (start < chars.Length) ResolveParagraph(start, chars.Length - start);

        return new BidiResolution(chars, levels, paragraphs);

        void ResolveParagraph(int paragraphStart, int length)
        {
            var paragraphTypes = new BidiCharacterType[length];
            var paragraphBracketTypes = new BidiPairedBracketType[length];
            var paragraphBracketValues = new int[length];
            Array.Copy(types, paragraphStart, paragraphTypes, 0, length);
            Array.Copy(bracketTypes, paragraphStart, paragraphBracketTypes, 0, length);
            Array.Copy(bracketValues, paragraphStart, paragraphBracketValues, 0, length);

            sbyte requestedLevel = forcedDirection switch
            {
                TextDirection.LeftToRight => 0,
                TextDirection.RightToLeft => 1,
                null => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(forcedDirection)),
            };

            var algorithm = new BidiAlgorithm();
            algorithm.Process(
                paragraphTypes,
                paragraphBracketTypes,
                paragraphBracketValues,
                requestedLevel,
                hasBrackets: true,
                hasEmbeddings: true,
                hasIsolates: true,
                outLevels: null);

            for (int i = 0; i < length; i++) levels[paragraphStart + i] = algorithm.ResolvedLevels[i];
            paragraphs.Add(new BidiParagraph(
                paragraphStart,
                length,
                checked((sbyte)algorithm.ResolvedParagraphEmbeddingLevel)));
        }
    }
}

internal static class UnicodeBidiData
{
    private const int RangeRecordSize = 9;
    private const int BracketRecordSize = 12;

    // Generated mechanically from unicode-bidi 0.3.18 hardcoded data (Unicode 16.0.0),
    // Cargo checksum 5c1cb5db39152898a79168971543b1cb5020dff7fe43c8dc468b0885f5e29df5.
    private const string RangeDataBase64 = "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";
    private const string BracketDataBase64 = "KAAAACkAAAAoAAAAWwAAAF0AAABbAAAAewAAAH0AAAB7AAAAOg8AADsPAAA6DwAAPA8AAD0PAAA8DwAAmxYAAJwWAACbFgAARSAAAEYgAABFIAAAfSAAAH4gAAB9IAAAjSAAAI4gAACNIAAACCMAAAkjAAAIIwAACiMAAAsjAAAKIwAAKSMAACojAAAIMAAAaCcAAGknAABoJwAAaicAAGsnAABqJwAAbCcAAG0nAABsJwAAbicAAG8nAABuJwAAcCcAAHEnAABwJwAAcicAAHMnAAByJwAAdCcAAHUnAAB0JwAAxScAAMYnAADFJwAA5icAAOcnAADmJwAA6CcAAOknAADoJwAA6icAAOsnAADqJwAA7CcAAO0nAADsJwAA7icAAO8nAADuJwAAgykAAIQpAACDKQAAhSkAAIYpAACFKQAAhykAAIgpAACHKQAAiSkAAIopAACJKQAAiykAAIwpAACLKQAAjSkAAJApAACNKQAAjykAAI4pAACPKQAAkSkAAJIpAACRKQAAkykAAJQpAACTKQAAlSkAAJYpAACVKQAAlykAAJgpAACXKQAA2CkAANkpAADYKQAA2ikAANspAADaKQAA/CkAAP0pAAD8KQAAIi4AACMuAAAiLgAAJC4AACUuAAAkLgAAJi4AACcuAAAmLgAAKC4AACkuAAAoLgAAVS4AAFYuAABVLgAAVy4AAFguAABXLgAAWS4AAFouAABZLgAAWy4AAFwuAABbLgAACDAAAAkwAAAIMAAACjAAAAswAAAKMAAADDAAAA0wAAAMMAAADjAAAA8wAAAOMAAAEDAAABEwAAAQMAAAFDAAABUwAAAUMAAAFjAAABcwAAAWMAAAGDAAABkwAAAYMAAAGjAAABswAAAaMAAAWf4AAFr+AABZ/gAAW/4AAFz+AABb/gAAXf4AAF7+AABd/gAACP8AAAn/AAAI/wAAO/8AAD3/AAA7/wAAW/8AAF3/AABb/wAAX/8AAGD/AABf/wAAYv8AAGP/AABi/wAA";

    private static readonly byte[] RangeData = Convert.FromBase64String(RangeDataBase64);
    private static readonly byte[] BracketData = Convert.FromBase64String(BracketDataBase64);

    public static BidiCharacterType CharacterType(int scalar)
    {
        int low = 0;
        int high = RangeData.Length / RangeRecordSize - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            int offset = middle * RangeRecordSize;
            int start = ReadInt32(RangeData, offset);
            int end = ReadInt32(RangeData, offset + 4);
            if (scalar < start) high = middle - 1;
            else if (scalar > end) low = middle + 1;
            else return MapClass(RangeData[offset + 8]);
        }
        return BidiCharacterType.LeftToRight;
    }

    public static void PairedBracket(
        int scalar,
        out BidiPairedBracketType bracketType,
        out int normalizedOpening)
    {
        for (int offset = 0; offset < BracketData.Length; offset += BracketRecordSize)
        {
            int opening = ReadInt32(BracketData, offset);
            int closing = ReadInt32(BracketData, offset + 4);
            if (scalar != opening && scalar != closing) continue;
            bracketType = scalar == opening ? BidiPairedBracketType.Open : BidiPairedBracketType.Close;
            normalizedOpening = ReadInt32(BracketData, offset + 8);
            return;
        }
        bracketType = BidiPairedBracketType.None;
        normalizedOpening = 0;
    }

    private static int ReadInt32(byte[] data, int offset)
        => data[offset] |
           (data[offset + 1] << 8) |
           (data[offset + 2] << 16) |
           (data[offset + 3] << 24);

    private static BidiCharacterType MapClass(byte value) => value switch
    {
        0 => BidiCharacterType.ArabicLetter,
        1 => BidiCharacterType.ArabicNumber,
        2 => BidiCharacterType.ParagraphSeparator,
        3 => BidiCharacterType.BoundaryNeutral,
        4 => BidiCharacterType.CommonSeparator,
        5 => BidiCharacterType.EuropeanNumber,
        6 => BidiCharacterType.EuropeanSeparator,
        7 => BidiCharacterType.EuropeanTerminator,
        8 => BidiCharacterType.FirstStrongIsolate,
        9 => BidiCharacterType.LeftToRight,
        10 => BidiCharacterType.LeftToRightEmbedding,
        11 => BidiCharacterType.LeftToRightIsolate,
        12 => BidiCharacterType.LeftToRightOverride,
        13 => BidiCharacterType.NonspacingMark,
        14 => BidiCharacterType.OtherNeutral,
        15 => BidiCharacterType.PopDirectionalFormat,
        16 => BidiCharacterType.PopDirectionalIsolate,
        17 => BidiCharacterType.RightToLeft,
        18 => BidiCharacterType.RightToLeftEmbedding,
        19 => BidiCharacterType.RightToLeftIsolate,
        20 => BidiCharacterType.RightToLeftOverride,
        21 => BidiCharacterType.SegmentSeparator,
        22 => BidiCharacterType.Whitespace,
        _ => throw new InvalidOperationException("Invalid generated bidi class."),
    };
}
