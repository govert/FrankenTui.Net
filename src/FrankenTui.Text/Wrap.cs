// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-text/src/wrap.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: UTF-8 byte-slice results become managed strings and public
// iterator results become allocation-backed IReadOnlyList values.

using FrankenTui.Core;

namespace FrankenTui.Text;

/// <summary>Result of optimal line breaking, including diagnostic cost evidence.</summary>
public sealed record KpBreakResult(
    IReadOnlyList<string> Lines,
    ulong TotalCost,
    IReadOnlyList<ulong> LineBadness);

public static partial class TextWrapper
{
    internal const ulong BadnessScale = 10_000;
    internal const ulong BadnessInfinity = ulong.MaxValue / 2;
    internal const ulong ForceBreakPenalty = 5_000;
    internal const int OptimalMaxLookahead = 1_024;

    /// <summary>Truncate at grapheme boundaries and append an ellipsis when it fits.</summary>
    public static string TruncateWithEllipsis(string text, int maximumWidth, string ellipsis)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(ellipsis);
        ValidateWidth(maximumWidth);

        if (DisplayWidth(text) <= maximumWidth)
        {
            return text;
        }

        var ellipsisWidth = DisplayWidth(ellipsis);
        if (ellipsisWidth >= maximumWidth)
        {
            return TruncateToWidth(text, maximumWidth);
        }

        return TruncateToWidth(text, maximumWidth - ellipsisWidth) + ellipsis;
    }

    /// <summary>Truncate text to a display width without splitting a grapheme.</summary>
    public static string TruncateToWidth(string text, int maximumWidth)
    {
        var (truncated, _) = TruncateToWidthWithInfo(text, maximumWidth);
        return truncated;
    }

    /// <summary>Return printable ASCII width, or null when the fast path is invalid.</summary>
    public static int? AsciiWidth(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (var value in text)
        {
            if (value is < ' ' or > '~')
            {
                return null;
            }
        }

        return text.Length;
    }

    public static int GraphemeWidth(string grapheme)
    {
        ArgumentNullException.ThrowIfNull(grapheme);
        return TerminalTextWidth.TextElementWidth(grapheme);
    }

    public static int DisplayWidth(string text) => TerminalTextWidth.DisplayWidth(text);

    public static bool HasWideChars(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TerminalTextWidth.EnumerateTextElements(text).Any(grapheme => GraphemeWidth(grapheme) > 1);
    }

    public static bool IsAsciiOnly(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.All(value => value <= 0x7f);
    }

    public static int GraphemeCount(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TerminalTextWidth.EnumerateTextElements(text).Count();
    }

    public static IReadOnlyList<string> Graphemes(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TerminalTextWidth.EnumerateTextElements(text).ToArray();
    }

    public static (string Text, int Width) TruncateToWidthWithInfo(string text, int maximumWidth)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateWidth(maximumWidth);

        var result = new System.Text.StringBuilder();
        var width = 0;
        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(text))
        {
            var graphemeWidth = GraphemeWidth(grapheme);
            if (width + graphemeWidth > maximumWidth)
            {
                break;
            }

            result.Append(grapheme);
            width += graphemeWidth;
        }

        return (result.ToString(), width);
    }

    /// <summary>
    /// Return UTF-8 byte offsets immediately after breaking-whitespace runs,
    /// matching Rust string-coordinate semantics.
    /// </summary>
    public static IReadOnlyList<int> WordBoundaries(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var boundaries = new List<int>();
        var utf8Offset = 0;
        foreach (var segment in SplitWords(text))
        {
            utf8Offset += System.Text.Encoding.UTF8.GetByteCount(segment);
            if (IsBreakingWhitespaceSegment(segment))
            {
                boundaries.Add(utf8Offset);
            }
        }

        return boundaries;
    }

    /// <summary>Split into word and breaking-whitespace segments.</summary>
    public static IReadOnlyList<string> WordSegments(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return SplitWords(text);
    }

    /// <summary>
    /// Compute globally optimal line breaks for one paragraph using cubic slack
    /// badness and deterministic later-break tie breaking.
    /// </summary>
    public static KpBreakResult WrapOptimal(string text, ushort width) =>
        WrapOptimal(text, (int)width);

    public static KpBreakResult WrapOptimal(string text, int width)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width cannot be negative.");
        }

        if (width == 0 || text.Length == 0)
        {
            return new KpBreakResult([text], 0, [0]);
        }

        var words = TokenizeOptimal(text);
        if (words.Count == 0)
        {
            return new KpBreakResult([string.Empty], 0, [0]);
        }

        var wordCount = words.Count;
        var costs = Enumerable.Repeat(BadnessInfinity, wordCount + 1).ToArray();
        var starts = new int[wordCount + 1];
        costs[0] = 0;

        for (var end = 1; end <= wordCount; end++)
        {
            var lineWidth = 0;
            var earliest = Math.Max(0, end - OptimalMaxLookahead);
            for (var start = end - 1; start >= earliest; start--)
            {
                lineWidth += words[start].ContentWidth;
                if (start < end - 1)
                {
                    lineWidth += words[start].SpaceWidth;
                }

                if (lineWidth > width && start < end - 1)
                {
                    break;
                }

                var slack = (long)width - lineWidth;
                var isLastLine = end == wordCount;
                var badness = lineWidth > width
                    ? ForceBreakPenalty
                    : KnuthPlassBadness(slack, width, isLastLine);
                var candidate = SaturatingAdd(costs[start], badness);

                if (candidate < costs[end] || (candidate == costs[end] && start > starts[end]))
                {
                    costs[end] = candidate;
                    starts[end] = start;
                }
            }
        }

        var breaks = new List<int>();
        for (var position = wordCount; position > 0; position = starts[position])
        {
            breaks.Add(starts[position]);
        }

        breaks.Reverse();

        var lines = new List<string>(breaks.Count);
        var lineBadness = new List<ulong>(breaks.Count);
        for (var breakIndex = 0; breakIndex < breaks.Count; breakIndex++)
        {
            var start = breaks[breakIndex];
            var end = breakIndex + 1 < breaks.Count ? breaks[breakIndex + 1] : wordCount;
            var line = new System.Text.StringBuilder();

            for (var wordIndex = start; wordIndex < end; wordIndex++)
            {
                var word = words[wordIndex];
                line.Append(word.Content);
                if (wordIndex < end - 1)
                {
                    line.Append(word.Space);
                }
            }

            var finalized = TrimBreakingWhitespace(line.ToString());
            var lineWidth = TerminalTextWidth.DisplayWidth(finalized);
            var slack = (long)width - lineWidth;
            var isLastLine = breakIndex == breaks.Count - 1;
            var badness = slack < 0
                ? ForceBreakPenalty
                : KnuthPlassBadness(slack, width, isLastLine);

            lines.Add(finalized);
            lineBadness.Add(badness);
        }

        return new KpBreakResult(lines, costs[wordCount], lineBadness);
    }

    /// <summary>Wrap one or more newline-separated paragraphs optimally.</summary>
    public static IReadOnlyList<string> WrapTextOptimal(string text, ushort width) =>
        WrapTextOptimal(text, (int)width);

    public static IReadOnlyList<string> WrapTextOptimal(string text, int width)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateWidth(width);

        var result = new List<string>();
        foreach (var rawParagraph in text.Split('\n', StringSplitOptions.None))
        {
            var paragraph = rawParagraph.EndsWith('\r') ? rawParagraph[..^1] : rawParagraph;
            if (paragraph.Length == 0)
            {
                result.Add(string.Empty);
                continue;
            }

            result.AddRange(WrapOptimal(paragraph, width).Lines);
        }

        return result;
    }

    internal static ulong KnuthPlassBadness(long slack, ushort width, bool isLastLine) =>
        KnuthPlassBadness(slack, (int)width, isLastLine);

    internal static ulong KnuthPlassBadness(long slack, int width, bool isLastLine)
    {
        if (slack < 0)
        {
            return BadnessInfinity;
        }

        if (isLastLine)
        {
            return 0;
        }

        if (width == 0)
        {
            return slack == 0 ? 0 : BadnessInfinity;
        }

        var ratio = slack / (double)width;
        var value = ratio * ratio * ratio * BadnessScale;
        return value >= ulong.MaxValue ? ulong.MaxValue : (ulong)value;
    }

    internal static IReadOnlyList<KpWord> TokenizeOptimal(string text)
    {
        var words = new List<KpWord>();
        var position = 0;

        while (position < text.Length)
        {
            while (position < text.Length && IsBreakingWhitespace(text[position]))
            {
                position++;
            }

            if (position >= text.Length)
            {
                break;
            }

            var contentStart = position;
            while (position < text.Length && !IsBreakingWhitespace(text[position]))
            {
                position++;
            }

            var content = text[contentStart..position];
            var spaceStart = position;
            while (position < text.Length && IsBreakingWhitespace(text[position]))
            {
                position++;
            }

            var space = text[spaceStart..position];
            words.Add(new KpWord(
                content,
                space,
                TerminalTextWidth.DisplayWidth(content),
                TerminalTextWidth.DisplayWidth(space)));
        }

        return words;
    }

    private static IReadOnlyList<string> SplitLinesUnwrapped(string text)
    {
        var result = new List<string>();
        foreach (var rawLine in text.Split('\n', StringSplitOptions.None))
        {
            var line = rawLine.EndsWith('\r') ? rawLine[..^1] : rawLine;
            result.Add(TrimBreakingWhitespace(line));
        }

        return result;
    }

    private static bool IsBreakingWhitespace(char value) =>
        char.IsWhiteSpace(value) && value is not '\u00A0' and not '\u202F';

    private static string TrimBreakingWhitespace(string value)
    {
        var end = value.Length;
        while (end > 0 && IsBreakingWhitespace(value[end - 1]))
        {
            end--;
        }

        return end == value.Length ? value : value[..end];
    }

    private static ulong SaturatingAdd(ulong left, ulong right) =>
        ulong.MaxValue - left < right ? ulong.MaxValue : left + right;

    private static void ValidateWidth(int width)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width cannot be negative.");
        }
    }
}

internal sealed record KpWord(
    string Content,
    string Space,
    int ContentWidth,
    int SpaceWidth);

/// <summary>Fitness class derived from a line's adjustment ratio.</summary>
public enum FitnessClass : byte
{
    Tight = 0,
    Normal = 1,
    Loose = 2,
    VeryLoose = 3,
}

public static class FitnessClassExtensions
{
    public static FitnessClass FromRatio(double ratio) => ratio switch
    {
        < -0.5 => FitnessClass.Tight,
        < 0.5 => FitnessClass.Normal,
        < 1.0 => FitnessClass.Loose,
        _ => FitnessClass.VeryLoose,
    };

    public static bool IsIncompatibleWith(this FitnessClass value, FitnessClass other) =>
        Math.Abs((int)value - (int)other) > 1;
}

/// <summary>Semantic kind of a paragraph break point.</summary>
public enum BreakKind
{
    Space,
    Hyphen,
    Forced,
    Emergency,
}

/// <summary>Cost and flagged state associated with a break point.</summary>
public readonly record struct BreakPenalty(long Value, bool Flagged)
{
    public static BreakPenalty Space { get; } = new(0, false);

    public static BreakPenalty Hyphen { get; } = new(50, true);

    public static BreakPenalty Forced { get; } = new(long.MinValue, false);

    public static BreakPenalty Emergency { get; } = new(5_000, false);
}

/// <summary>
/// Configurable TeX-style paragraph objective. This is the shared seam used by
/// space, explicit-hyphen, forced, and emergency break producers.
/// </summary>
public sealed record ParagraphObjective
{
    public ulong LinePenalty { get; init; } = 10;

    public ulong FitnessDemerit { get; init; } = 100;

    public ulong DoubleHyphenDemerit { get; init; } = 100;

    public ulong FinalHyphenDemerit { get; init; } = 100;

    public double MaxAdjustmentRatio { get; init; } = 2.0;

    public double MinAdjustmentRatio { get; init; } = -1.0;

    public ulong WidowDemerit { get; init; } = 150;

    public int WidowThreshold { get; init; } = 15;

    public ulong OrphanDemerit { get; init; } = 150;

    public int OrphanThreshold { get; init; } = 20;

    public ulong BadnessScale { get; init; } = 10_000;

    public static ParagraphObjective Default => new();

    public static ParagraphObjective Terminal => Default with
    {
        LinePenalty = 20,
        FitnessDemerit = 50,
        MinAdjustmentRatio = 0.0,
        MaxAdjustmentRatio = 3.0,
        WidowDemerit = 50,
        OrphanDemerit = 50,
    };

    public static ParagraphObjective Typographic => Default;

    public ulong? Badness(long slack, int width)
    {
        ValidateNonNegative(width, nameof(width));
        if (width == 0)
        {
            return slack == 0 ? 0UL : null;
        }

        var ratio = slack / (double)width;
        if (ratio < MinAdjustmentRatio || ratio > MaxAdjustmentRatio)
        {
            return null;
        }

        var absolute = Math.Abs(ratio);
        return SaturatingFromDouble(absolute * absolute * absolute * BadnessScale);
    }

    public double AdjustmentRatio(long slack, int width)
    {
        ValidateNonNegative(width, nameof(width));
        return width == 0 ? 0.0 : slack / (double)width;
    }

    public ulong? Demerits(long slack, int width, BreakPenalty penalty)
    {
        var badness = Badness(slack, width);
        if (badness is null)
        {
            return null;
        }

        var basis = SaturatingAdd(LinePenalty, badness.Value);
        var basisSquared = SaturatingMultiply(basis, basis);
        if (penalty.Value == long.MinValue)
        {
            return basisSquared;
        }

        var magnitude = penalty.Value < 0
            ? (ulong)(-penalty.Value)
            : (ulong)penalty.Value;
        var penaltySquared = SaturatingMultiply(magnitude, magnitude);
        return penalty.Value >= 0
            ? SaturatingAdd(basisSquared, penaltySquared)
            : basisSquared >= penaltySquared ? basisSquared - penaltySquared : 0;
    }

    public ulong AdjacencyDemerits(
        FitnessClass previousFitness,
        FitnessClass currentFitness,
        bool previousFlagged,
        bool currentFlagged)
    {
        var result = previousFitness.IsIncompatibleWith(currentFitness)
            ? FitnessDemerit
            : 0;
        if (previousFlagged && currentFlagged)
        {
            result = SaturatingAdd(result, DoubleHyphenDemerit);
        }

        return result;
    }

    public ulong WidowDemerits(int lastLineCharacters)
    {
        ValidateNonNegative(lastLineCharacters, nameof(lastLineCharacters));
        return lastLineCharacters < WidowThreshold ? WidowDemerit : 0;
    }

    public ulong OrphanDemerits(int firstLineCharacters)
    {
        ValidateNonNegative(firstLineCharacters, nameof(firstLineCharacters));
        return firstLineCharacters < OrphanThreshold ? OrphanDemerit : 0;
    }

    private static ulong SaturatingFromDouble(double value)
    {
        if (double.IsNaN(value) || value <= 0.0)
        {
            return 0;
        }

        return value >= ulong.MaxValue ? ulong.MaxValue : (ulong)value;
    }

    private static ulong SaturatingAdd(ulong left, ulong right) =>
        ulong.MaxValue - left < right ? ulong.MaxValue : left + right;

    private static ulong SaturatingMultiply(ulong left, ulong right) =>
        left != 0 && right > ulong.MaxValue / left ? ulong.MaxValue : left * right;

    private static void ValidateNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
        }
    }
}
