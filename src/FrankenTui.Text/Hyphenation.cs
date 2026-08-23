// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-text/src/hyphenation.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: Rust char/usize coordinates are represented by Rune/Int32;
// negative managed margins and offsets are rejected. Unicode lower-casing uses
// the platform's invariant Unicode tables rather than Rust's tables.

using System.Collections.ObjectModel;
using System.Text;

namespace FrankenTui.Text;

/// <summary>A compiled Liang/TeX hyphenation pattern.</summary>
public sealed class HyphenationPattern : IEquatable<HyphenationPattern>
{
    private readonly ReadOnlyCollection<Rune> _chars;
    private readonly ReadOnlyCollection<byte> _levels;

    public HyphenationPattern(IEnumerable<Rune> chars, IEnumerable<byte> levels)
    {
        ArgumentNullException.ThrowIfNull(chars);
        ArgumentNullException.ThrowIfNull(levels);
        _chars = Array.AsReadOnly(chars.ToArray());
        _levels = Array.AsReadOnly(levels.ToArray());
    }

    /// <summary>The pattern's alphabetic characters, with digits removed.</summary>
    public IReadOnlyList<Rune> Chars => _chars;

    /// <summary>Levels at each inter-character position.</summary>
    public IReadOnlyList<byte> Levels => _levels;

    public bool Equals(HyphenationPattern? other) =>
        other is not null
        && _chars.SequenceEqual(other._chars)
        && _levels.SequenceEqual(other._levels);

    public override bool Equals(object? obj) => Equals(obj as HyphenationPattern);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _chars)
        {
            hash.Add(value);
        }

        foreach (var value in _levels)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

/// <summary>A valid hyphenation point within a word.</summary>
public readonly record struct HyphenBreakPoint
{
    public HyphenBreakPoint(int offset, byte level)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset cannot be negative.");
        }

        Offset = offset;
        Level = level;
    }

    /// <summary>
    /// Unicode-scalar offset after which a hyphen may be inserted. This follows
    /// the upstream implementation (whose documentation calls it a grapheme offset).
    /// </summary>
    public int Offset { get; }

    /// <summary>The odd pattern level that admitted the break.</summary>
    public byte Level { get; }

    /// <summary>Convert this point into the wrap objective's flagged penalty.</summary>
    public BreakPenalty ToPenalty() => new(
        Level switch
        {
            1 => 50,
            3 => 40,
            _ => 30,
        },
        true);
}

/// <summary>Trie-based pattern storage for deterministic O(n²) word lookup.</summary>
public sealed class PatternTrie
{
    private sealed class Node
    {
        public Dictionary<Rune, int> Children { get; } = [];

        public byte[]? Levels { get; set; }
    }

    private readonly List<Node> _nodes = [new()];

    public PatternTrie(IEnumerable<HyphenationPattern> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        foreach (var pattern in patterns)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            Insert(pattern);
        }
    }

    private void Insert(HyphenationPattern pattern)
    {
        var nodeIndex = 0;
        foreach (var value in pattern.Chars)
        {
            if (!_nodes[nodeIndex].Children.TryGetValue(value, out var nextIndex))
            {
                nextIndex = _nodes.Count;
                _nodes.Add(new Node());
                _nodes[nodeIndex].Children.Add(value, nextIndex);
            }

            nodeIndex = nextIndex;
        }

        _nodes[nodeIndex].Levels = pattern.Levels.ToArray();
    }

    internal void ApplyAt(IReadOnlyList<Rune> wordChars, int start, Span<byte> outputLevels)
    {
        ArgumentNullException.ThrowIfNull(wordChars);
        if ((uint)start >= (uint)wordChars.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must identify a word character.");
        }

        var nodeIndex = 0;
        for (var index = start; index < wordChars.Count; index++)
        {
            if (!_nodes[nodeIndex].Children.TryGetValue(wordChars[index], out var nextIndex))
            {
                break;
            }

            nodeIndex = nextIndex;
            var levels = _nodes[nodeIndex].Levels;
            if (levels is null)
            {
                continue;
            }

            for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                var position = start + levelIndex;
                if (position < outputLevels.Length)
                {
                    outputLevels[position] = Math.Max(outputLevels[position], levels[levelIndex]);
                }
            }
        }
    }

    internal byte[] ApplyAll(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var runes = value.EnumerateRunes().ToArray();
        var levels = new byte[runes.Length + 1];
        for (var start = 0; start < runes.Length; start++)
        {
            ApplyAt(runes, start, levels);
        }

        return levels;
    }
}

/// <summary>A deterministic Liang hyphenation dictionary for one language.</summary>
public sealed class HyphenationDict
{
    private const int LeftHyphenMinimum = 2;
    private const int RightHyphenMinimum = 3;

    private readonly PatternTrie _trie;
    private readonly IReadOnlyDictionary<string, int[]> _exceptions;

    public HyphenationDict(
        string language,
        IEnumerable<string> patterns,
        IEnumerable<string> exceptions)
        : this(
            language,
            CompileTrie(patterns),
            CompileExceptions(exceptions),
            LeftHyphenMinimum,
            RightHyphenMinimum)
    {
    }

    private HyphenationDict(
        string language,
        PatternTrie trie,
        IReadOnlyDictionary<string, int[]> exceptions,
        int leftMinimum,
        int rightMinimum)
    {
        ArgumentNullException.ThrowIfNull(language);
        Language = language;
        _trie = trie;
        _exceptions = exceptions;
        LeftMin = leftMinimum;
        RightMin = rightMinimum;
    }

    /// <summary>ISO 639-1 language code, when the caller uses that convention.</summary>
    public string Language { get; }

    /// <summary>Minimum Unicode scalars before the first break.</summary>
    public int LeftMin { get; }

    /// <summary>Minimum Unicode scalars after the last break.</summary>
    public int RightMin { get; }

    /// <summary>Return a dictionary view with custom left/right margins.</summary>
    public HyphenationDict WithMargins(int left, int right)
    {
        ValidateMargin(left, nameof(left));
        ValidateMargin(right, nameof(right));
        return new HyphenationDict(Language, _trie, _exceptions, left, right);
    }

    /// <summary>Find all valid break points, sorted by scalar offset.</summary>
    public IReadOnlyList<HyphenBreakPoint> Hyphenate(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        var lower = word.ToLowerInvariant();
        var wordRunes = lower.EnumerateRunes().ToArray();
        var runeCount = wordRunes.Length;

        if (_exceptions.TryGetValue(lower, out var exceptionOffsets))
        {
            var lastAllowed = Math.Max(runeCount - RightMin, 0);
            return exceptionOffsets
                .Where(offset => offset >= LeftMin && offset <= lastAllowed)
                .Select(offset => new HyphenBreakPoint(offset, 1))
                .ToArray();
        }

        if ((long)runeCount < (long)LeftMin + RightMin)
        {
            return [];
        }

        var delimited = new Rune[runeCount + 2];
        delimited[0] = new Rune('.');
        wordRunes.CopyTo(delimited, 1);
        delimited[^1] = new Rune('.');

        var levels = new byte[delimited.Length + 1];
        for (var start = 0; start < delimited.Length; start++)
        {
            _trie.ApplyAt(delimited, start, levels);
        }

        var breaks = new List<HyphenBreakPoint>();
        var maximumOffset = Math.Max(runeCount - RightMin, 0);
        for (var offset = LeftMin; offset <= maximumOffset; offset++)
        {
            var level = levels[offset + 1];
            if ((level & 1) == 1)
            {
                breaks.Add(new HyphenBreakPoint(offset, level));
            }
        }

        return breaks;
    }

    /// <summary>Whether a word has at least one valid break point.</summary>
    public bool CanHyphenate(string word) => Hyphenate(word).Count > 0;

    private static PatternTrie CompileTrie(IEnumerable<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        var compiled = new List<HyphenationPattern>();
        foreach (var pattern in patterns)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            var result = Hyphenation.CompilePattern(pattern);
            if (result is not null)
            {
                compiled.Add(result);
            }
        }

        return new PatternTrie(compiled);
    }

    private static IReadOnlyDictionary<string, int[]> CompileExceptions(IEnumerable<string> exceptions)
    {
        ArgumentNullException.ThrowIfNull(exceptions);
        var compiled = new Dictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var exception in exceptions)
        {
            ArgumentNullException.ThrowIfNull(exception);
            var (word, breaks) = Hyphenation.ParseException(exception);
            compiled[word] = breaks;
        }

        return new ReadOnlyDictionary<string, int[]>(compiled);
    }

    private static void ValidateMargin(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Margin cannot be negative.");
        }
    }
}

/// <summary>Top-level operations and built-in data from the upstream module.</summary>
public static class Hyphenation
{
    private static readonly ReadOnlyCollection<string> EnglishPatterns = Array.AsReadOnly(
        new[]
        {
            ".hy3p", ".re1i", ".in1t", ".un1d", ".ex1a", ".dis1c", ".pre1v", ".over3f", ".semi5", ".auto3",
            "a2l", "an2t", "as3ter", "at5omi", "be5ra", "bl2", "br2", "ca4t", "ch2", "cl2", "co2n",
            "com5ma", "cr2", "de4moc", "di3vis", "dr2", "en3tic", "er1i", "fl2", "fr2", "gl2", "gr2",
            "hy3pe", "i1a", "ism3", "ist3", "i2z", "li4ber", "m2p", "n2t", "ph2", "pl2", "pr2", "qu2",
            "sc2", "sh2", "sk2", "sl2", "sm2", "sn2", "sp2", "st2", "sw2", "th2", "tr2", "tw2", "ty4p",
            "wh2", "wr2", "ber3", "cial4", "ful3", "gy5n", "ing1", "ment3", "ness3", "tion5", "sion5",
            "tu4al", "able3", "ible3", "ment1a", "ment1i", "n2kl", "n2gl", "n4gri", "mp3t", "nk3i", "ns2",
            "nt2", "nc2", "nd2", "ng2", "nf2", "ct2", "pt2", "ps2", "ld2", "lf2", "lk2", "lm2", "lt2",
            "lv2", "rb2", "rc2", "rd2", "rf2", "rg2", "rk2", "rl2", "rm2", "rn2", "rp2", "rs2", "rt2",
            "rv2", "rw2", "4ism.", "4ist.", "4ment.", "4ness.", "5tion.", "5sion.", "3ful.", "3less.",
            "3ous.", "3ive.", "3able.", "3ible.", "3ment.", "3ness.",
        });

    private static readonly ReadOnlyCollection<string> EnglishExceptions = Array.AsReadOnly(
        new[]
        {
            "as-so-ciate",
            "as-so-ciates",
            "dec-li-na-tion",
            "oblig-a-tory",
            "phil-an-thropic",
            "present",
            "presents",
            "project",
            "projects",
            "reci-procity",
            "ta-ble",
        });

    /// <summary>Minimal built-in English TeX patterns from the upstream fixture.</summary>
    public static IReadOnlyList<string> EnglishPatternsMini => EnglishPatterns;

    /// <summary>Minimal built-in English exception words from the upstream fixture.</summary>
    public static IReadOnlyList<string> EnglishExceptionsMini => EnglishExceptions;

    /// <summary>Compile a TeX-format hyphenation pattern.</summary>
    public static HyphenationPattern? CompilePattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var chars = new List<Rune>();
        var levels = new List<byte>();
        byte? pendingDigit = null;

        foreach (var value in pattern.EnumerateRunes())
        {
            if (value.Value is >= '0' and <= '9')
            {
                pendingDigit = (byte)(value.Value - '0');
            }
            else if (Rune.IsLetter(value) || value.Value == '.')
            {
                levels.Add(pendingDigit ?? 0);
                pendingDigit = null;
                chars.Add(ToAsciiLower(value));
            }
        }

        levels.Add(pendingDigit ?? 0);
        return chars.Count == 0 ? null : new HyphenationPattern(chars, levels);
    }

    /// <summary>Create the minimal English demonstration dictionary.</summary>
    public static HyphenationDict EnglishDictMini() =>
        new("en", EnglishPatterns, EnglishExceptions);

    /// <summary>Map break points to flagged paragraph-objective penalties.</summary>
    public static IReadOnlyList<(int Offset, BreakPenalty Penalty)> BreakPenalties(
        IEnumerable<HyphenBreakPoint> breaks)
    {
        ArgumentNullException.ThrowIfNull(breaks);
        return breaks.Select(value => (value.Offset, value.ToPenalty())).ToArray();
    }

    internal static (string Word, int[] Breaks) ParseException(string exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var word = new StringBuilder();
        var breaks = new List<int>();
        var runeCount = 0;

        foreach (var value in exception.EnumerateRunes())
        {
            if (value.Value == '-')
            {
                breaks.Add(runeCount);
            }
            else
            {
                word.Append(ToAsciiLower(value).ToString());
                runeCount++;
            }
        }

        return (word.ToString(), breaks.ToArray());
    }

    private static Rune ToAsciiLower(Rune value) =>
        value.Value is >= 'A' and <= 'Z' ? new Rune(value.Value + ('a' - 'A')) : value;
}
