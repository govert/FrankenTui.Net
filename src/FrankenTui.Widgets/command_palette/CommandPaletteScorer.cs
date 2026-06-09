// Port of .external/frankentui/crates/ftui-widgets/src/command_palette/scorer.rs
// Bayesian Match Scoring for Command Palette — probabilistic scorer with evidence ledger.
// DIVERGENCE: Namespace is FrankenTui.Widgets.CommandPaletteScoring instead of
// FrankenTui.Widgets.CommandPalette because the latter conflicts with the existing
// sealed class CommandPalette in FrankenTui.Widgets (CommandPalette.cs).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrankenTui.Widgets.CommandPaletteScoring;

// ---------------------------------------------------------------------------
// Match Types
// ---------------------------------------------------------------------------

/// <summary>
/// Type of match between query and title.
/// </summary>
/// <remarks>
/// Variants are ordered from weakest to strongest so that <c>CompareTo</c> /
/// sort puts <see cref="NoMatch"/> first and <see cref="Exact"/> last,
/// mirroring the Rust <c>#[derive(Ord)]</c> ascending declaration order.
/// </remarks>
public enum MatchType
{
    /// <summary>No match found.</summary>
    NoMatch,
    /// <summary>Characters found in order but with gaps.</summary>
    Fuzzy,
    /// <summary>Query found as contiguous substring.</summary>
    Substring,
    /// <summary>Query matches start of word boundaries.</summary>
    WordStart,
    /// <summary>Title starts with query.</summary>
    Prefix,
    /// <summary>Query equals title exactly.</summary>
    Exact,
}

/// <summary>
/// Extension methods for <see cref="MatchType"/>.
/// </summary>
public static class MatchTypeExtensions
{
    /// <summary>
    /// Prior odds ratio P(relevant) / P(not_relevant) for this match type.
    ///
    /// These are derived from empirical observations of user intent:
    /// - Exact matches are almost always what the user wants
    /// - Prefix matches are very likely relevant
    /// - Fuzzy matches need additional evidence
    /// </summary>
    public static double PriorOdds(this MatchType matchType) => matchType switch
    {
        MatchType.Exact      => 99.0,   // 99:1 odds → P ≈ 0.99
        MatchType.Prefix     => 9.0,    // 9:1 odds → P ≈ 0.90
        MatchType.WordStart  => 4.0,    // 4:1 odds → P ≈ 0.80
        MatchType.Substring  => 2.0,    // 2:1 odds → P ≈ 0.67
        MatchType.Fuzzy      => 0.333,  // 1:3 odds → P ≈ 0.25
        MatchType.NoMatch    => 0.0,    // Impossible
        _ => 0.0,
    };

    /// <summary>Human-readable description for the evidence ledger.</summary>
    public static string Description(this MatchType matchType) => matchType switch
    {
        MatchType.Exact     => "exact match",
        MatchType.Prefix    => "prefix match",
        MatchType.WordStart => "word-start match",
        MatchType.Substring => "contiguous substring",
        MatchType.Fuzzy     => "fuzzy match",
        MatchType.NoMatch   => "no match",
        _ => "no match",
    };
}

// ---------------------------------------------------------------------------
// Evidence Entry
// ---------------------------------------------------------------------------

/// <summary>Types of evidence that contribute to match scoring.</summary>
public enum EvidenceKind
{
    /// <summary>Base match type (prior).</summary>
    MatchType,
    /// <summary>Match at word boundary.</summary>
    WordBoundary,
    /// <summary>Match position (earlier is better).</summary>
    Position,
    /// <summary>Gap between matched characters.</summary>
    GapPenalty,
    /// <summary>Query also matches a tag.</summary>
    TagMatch,
    /// <summary>Title length factor (shorter is more specific).</summary>
    TitleLength,
}

/// <summary>Human-readable evidence description (lazy formatting).</summary>
public abstract class EvidenceDescription
{
    private EvidenceDescription() { }

    /// <summary>A static string description.</summary>
    public sealed class Static : EvidenceDescription
    {
        public string Msg { get; }
        public Static(string msg) { Msg = msg; }
        public override string ToString() => Msg;
    }

    /// <summary>Title length in chars.</summary>
    public sealed class TitleLengthChars : EvidenceDescription
    {
        public int Len { get; }
        public TitleLengthChars(int len) { Len = len; }
        public override string ToString() => $"title length {Len} chars";
    }

    /// <summary>First matched character position.</summary>
    public sealed class FirstMatchPos : EvidenceDescription
    {
        public int Pos { get; }
        public FirstMatchPos(int pos) { Pos = pos; }
        public override string ToString() => $"first match at position {Pos}";
    }

    /// <summary>Number of word boundary matches.</summary>
    public sealed class WordBoundaryCount : EvidenceDescription
    {
        public int Count { get; }
        public WordBoundaryCount(int count) { Count = count; }
        public override string ToString() => $"{Count} word boundary matches";
    }

    /// <summary>Total gap between matched positions.</summary>
    public sealed class GapTotal : EvidenceDescription
    {
        public int Total { get; }
        public GapTotal(int total) { Total = total; }
        public override string ToString() => $"total gap of {Total} characters";
    }

    /// <summary>Query coverage as a percent of title length.</summary>
    public sealed class CoveragePercent : EvidenceDescription
    {
        public double Percent { get; }
        public CoveragePercent(double percent) { Percent = percent; }
        public override string ToString() => $"query covers {Math.Round(Percent, MidpointRounding.AwayFromZero):0}% of title";
    }
}

/// <summary>
/// A single piece of evidence contributing to the match score.
/// </summary>
public sealed class EvidenceEntry
{
    /// <summary>Type of evidence.</summary>
    public EvidenceKind Kind { get; init; }

    /// <summary>
    /// Bayes factor: likelihood ratio P(evidence | relevant) / P(evidence | ¬relevant).
    /// Values &gt; 1.0 support relevance, &lt; 1.0 oppose it.
    /// </summary>
    public double BayesFactor { get; init; }

    /// <summary>Human-readable explanation.</summary>
    public EvidenceDescription Description { get; init; } = null!;

    /// <inheritdoc/>
    public override string ToString()
    {
        var direction = BayesFactor > 1.0 ? "supports"
                      : BayesFactor < 1.0 ? "opposes"
                      : "neutral";
        return $"{Kind:G}: BF={BayesFactor:F2} ({direction}) - {Description}";
    }
}

// ---------------------------------------------------------------------------
// Evidence Ledger
// ---------------------------------------------------------------------------

/// <summary>
/// A ledger of evidence explaining a match score.
///
/// This provides full transparency into why a match received its score,
/// enabling debugging and user explanations.
/// </summary>
public sealed class EvidenceLedger
{
    private readonly List<EvidenceEntry> _entries = new();

    /// <summary>Create a new empty ledger.</summary>
    public EvidenceLedger() { }

    /// <summary>Add an evidence entry.</summary>
    public void Add(EvidenceKind kind, double bayesFactor, EvidenceDescription description)
    {
        _entries.Add(new EvidenceEntry { Kind = kind, BayesFactor = bayesFactor, Description = description });
    }

    /// <summary>Get all entries.</summary>
    public IReadOnlyList<EvidenceEntry> Entries() => _entries;

    /// <summary>Compute the combined Bayes factor (product of all factors).</summary>
    public double CombinedBayesFactor()
    {
        double product = 1.0;
        foreach (var e in _entries)
            product *= e.BayesFactor;
        return product;
    }

    /// <summary>Get the prior odds (from MatchType entry, if present).</summary>
    public double? PriorOdds()
    {
        foreach (var e in _entries)
        {
            if (e.Kind == EvidenceKind.MatchType)
                return e.BayesFactor;
        }
        return null;
    }

    /// <summary>
    /// Compute posterior probability from prior odds and evidence.
    ///
    /// posterior_prob = posterior_odds / (1 + posterior_odds)
    /// where posterior_odds = prior_odds × combined_bf
    /// </summary>
    public double PosteriorProbability()
    {
        var prior = PriorOdds() ?? 1.0;
        // Exclude prior from BF since it's already the odds
        double bf = 1.0;
        foreach (var e in _entries)
        {
            if (e.Kind != EvidenceKind.MatchType)
                bf *= e.BayesFactor;
        }

        var posteriorOdds = prior * bf;
        if (double.IsInfinity(posteriorOdds))
            return 1.0;
        return posteriorOdds / (1.0 + posteriorOdds);
    }

    /// <summary>Format this ledger as a JSONL line for structured logging.</summary>
    public string ToJsonl()
    {
        var sb = new StringBuilder();
        sb.Append("{\"schema\":\"palette-scoring-v1\",\"entries\":[");
        bool first = true;
        foreach (var e in _entries)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append($"{{\"kind\":\"{e.Kind:G}\",\"bf\":{e.BayesFactor:F4},\"desc\":\"{e.Description}\"}}");
        }
        sb.Append($"],\"combined_bf\":{CombinedBayesFactor():F6},\"posterior_prob\":{PosteriorProbability():F6}}}");
        return sb.ToString();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Evidence Ledger:");
        foreach (var e in _entries)
            sb.AppendLine($"  {e}");
        sb.AppendLine($"  Combined BF: {CombinedBayesFactor():F3}");
        sb.AppendLine($"  Posterior P: {PosteriorProbability():F3}");
        return sb.ToString();
    }
}

// ---------------------------------------------------------------------------
// Match Result
// ---------------------------------------------------------------------------

/// <summary>Result of scoring a query against a title.</summary>
public sealed class MatchResult
{
    /// <summary>Computed relevance score (posterior probability).</summary>
    public double Score { get; set; }

    /// <summary>Type of match detected.</summary>
    public MatchType MatchType { get; set; }

    /// <summary>Positions of matched characters in the title.</summary>
    public List<int> MatchPositions { get; set; } = new();

    /// <summary>Evidence ledger explaining the score.</summary>
    public EvidenceLedger Evidence { get; set; } = new();

    /// <summary>Create a no-match result.</summary>
    public static MatchResult NoMatch()
    {
        var evidence = new EvidenceLedger();
        evidence.Add(
            EvidenceKind.MatchType,
            0.0,
            new EvidenceDescription.Static("no matching characters found"));
        return new MatchResult
        {
            Score = 0.0,
            MatchType = MatchType.NoMatch,
            MatchPositions = new List<int>(),
            Evidence = evidence,
        };
    }
}

// ---------------------------------------------------------------------------
// Scorer
// ---------------------------------------------------------------------------

/// <summary>
/// Bayesian fuzzy matcher for command palette.
///
/// Computes relevance scores using a probabilistic model with
/// evidence tracking for explainable ranking.
/// </summary>
public sealed class BayesianScorer
{
    /// <summary>Whether to track detailed evidence (slower but explainable).</summary>
    public bool TrackEvidence { get; init; }

    /// <summary>Create a new scorer with evidence tracking enabled.</summary>
    public BayesianScorer()
    {
        TrackEvidence = true;
    }

    private BayesianScorer(bool trackEvidence)
    {
        TrackEvidence = trackEvidence;
    }

    /// <summary>Create a scorer without evidence tracking (faster).</summary>
    public static BayesianScorer Fast() => new BayesianScorer(trackEvidence: false);

    /// <summary>Create a scorer with default settings (no evidence tracking).</summary>
    public static BayesianScorer Default() => new BayesianScorer(trackEvidence: false);

    /// <summary>
    /// Score a query against a title.
    ///
    /// Returns a MatchResult with score, match type, positions, and evidence.
    /// </summary>
    public MatchResult Score(string query, string title)
    {
        // Quick rejection: query longer than title
        if (query.Length > title.Length)
            return MatchResult.NoMatch();

        // Empty query matches everything (show all)
        if (query.Length == 0)
            return ScoreEmptyQuery(title);

        // Normalize for case-insensitive matching
        var queryLower = query.ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();

        // Determine match type
        var (matchType, positions) = DetectMatchType(queryLower, titleLower, title);

        if (matchType == MatchType.NoMatch)
            return MatchResult.NoMatch();

        // Build evidence ledger and compute score
        return ComputeScore(matchType, positions, queryLower, title);
    }

    /// <summary>
    /// Score a query using a pre-lowercased query string.
    ///
    /// This avoids repeated query normalization when scoring against many titles.
    /// </summary>
    public MatchResult ScoreWithQueryLower(string query, string queryLower, string title)
    {
        var titleLower = title.ToLowerInvariant();
        return ScoreWithLoweredTitle(query, queryLower, title, titleLower);
    }

    /// <summary>
    /// Score a query with both query and title already lowercased.
    ///
    /// This avoids per-title lowercasing in hot loops.
    /// </summary>
    public MatchResult ScoreWithLoweredTitle(string query, string queryLower, string title, string titleLower)
    {
        return ScoreWithLoweredTitleAndWords(query, queryLower, title, titleLower, null);
    }

    /// <summary>Score a query with pre-lowercased title and optional word-start cache.</summary>
    public MatchResult ScoreWithLoweredTitleAndWords(
        string query,
        string queryLower,
        string title,
        string titleLower,
        IReadOnlyList<int>? wordStarts)
    {
        // Quick rejection: query longer than title
        if (query.Length > title.Length)
            return MatchResult.NoMatch();

        // Empty query matches everything (show all)
        if (query.Length == 0)
            return ScoreEmptyQuery(title);

        // Determine match type
        var (matchType, positions) = DetectMatchTypeWithWords(queryLower, titleLower, title, wordStarts);

        if (matchType == MatchType.NoMatch)
            return MatchResult.NoMatch();

        // Build evidence ledger and compute score
        return ComputeScore(matchType, positions, queryLower, title);
    }

    /// <summary>Score a query against a title with tags.</summary>
    public MatchResult ScoreWithTags(string query, string title, string[] tags)
    {
        var result = Score(query, title);

        // Check if query matches any tag
        var queryLower = query.ToLowerInvariant();
        bool tagMatch = false;
        foreach (var tag in tags)
        {
            if (ContainsIgnoreCase(tag, queryLower))
            {
                tagMatch = true;
                break;
            }
        }

        if (tagMatch && result.MatchType != MatchType.NoMatch)
        {
            // Strong positive evidence
            if (TrackEvidence)
            {
                result.Evidence.Add(
                    EvidenceKind.TagMatch,
                    3.0, // 3:1 in favor
                    new EvidenceDescription.Static("query matches tag"));
                result.Score = result.Evidence.PosteriorProbability();
            }
            else if (result.Score >= 0.0 && result.Score < 1.0)
            {
                var odds = result.Score / (1.0 - result.Score);
                var boosted = odds * 3.0;
                result.Score = boosted / (1.0 + boosted);
            }
        }

        return result;
    }

    /// <summary>Score when query is empty (returns all items with neutral score).</summary>
    private MatchResult ScoreEmptyQuery(string title)
    {
        // Shorter titles are more specific, slight preference
        var lengthFactor = 1.0 + (1.0 / (title.Length + 1.0)) * 0.1;
        if (TrackEvidence)
        {
            var evidence = new EvidenceLedger();
            evidence.Add(
                EvidenceKind.MatchType,
                1.0, // Neutral prior
                new EvidenceDescription.Static("empty query matches all"));
            evidence.Add(
                EvidenceKind.TitleLength,
                lengthFactor,
                new EvidenceDescription.TitleLengthChars(title.Length));
            var score = evidence.PosteriorProbability();
            return new MatchResult
            {
                Score = score,
                MatchType = MatchType.Fuzzy, // Treat as weak match
                MatchPositions = new List<int>(),
                Evidence = evidence,
            };
        }
        else
        {
            var odds = lengthFactor;
            var score = odds / (1.0 + odds);
            return new MatchResult
            {
                Score = score,
                MatchType = MatchType.Fuzzy,
                MatchPositions = new List<int>(),
                Evidence = new EvidenceLedger(),
            };
        }
    }

    /// <summary>Detect the type of match and positions of matched characters.</summary>
    private (MatchType, List<int>) DetectMatchType(string queryLower, string titleLower, string title)
    {
        return DetectMatchTypeWithWords(queryLower, titleLower, title, null);
    }

    /// <summary>Detect match type with optional precomputed word-start positions.</summary>
    private (MatchType, List<int>) DetectMatchTypeWithWords(
        string queryLower,
        string titleLower,
        string title,
        IReadOnlyList<int>? wordStarts)
    {
        if (IsAscii(queryLower) && IsAscii(titleLower))
        {
            return DetectMatchTypeAscii(queryLower, titleLower, wordStarts);
        }

        // Check exact match
        if (queryLower == titleLower)
        {
            var positions = Enumerable.Range(0, CountChars(title)).ToList();
            return (MatchType.Exact, positions);
        }

        // Check prefix match
        if (titleLower.StartsWith(queryLower, StringComparison.Ordinal))
        {
            var positions = Enumerable.Range(0, CountChars(queryLower)).ToList();
            return (MatchType.Prefix, positions);
        }

        // Check word-start match (e.g., "gd" matches "Go Dashboard")
        var wordStartPositions = WordStartMatch(queryLower, titleLower);
        if (wordStartPositions != null)
            return (MatchType.WordStart, wordStartPositions);

        // Check contiguous substring
        int byteStart = titleLower.IndexOf(queryLower, StringComparison.Ordinal);
        if (byteStart >= 0)
        {
            int charStart = CountChars(titleLower, 0, byteStart);
            int charLen = CountChars(queryLower);
            var positions = Enumerable.Range(charStart, charLen).ToList();
            return (MatchType.Substring, positions);
        }

        // Check fuzzy match
        var fuzzyPositions = FuzzyMatch(queryLower, titleLower);
        if (fuzzyPositions != null)
            return (MatchType.Fuzzy, fuzzyPositions);

        return (MatchType.NoMatch, new List<int>());
    }

    /// <summary>ASCII fast-path match detection.</summary>
    private (MatchType, List<int>) DetectMatchTypeAscii(
        string queryLower,
        string titleLower,
        IReadOnlyList<int>? wordStarts)
    {
        var queryBytes = Encoding.ASCII.GetBytes(queryLower);
        var titleBytes = Encoding.ASCII.GetBytes(titleLower);

        if (queryBytes.SequenceEqual(titleBytes))
        {
            var positions = Enumerable.Range(0, titleBytes.Length).ToList();
            return (MatchType.Exact, positions);
        }

        if (StartsWith(titleBytes, queryBytes))
        {
            var positions = Enumerable.Range(0, queryBytes.Length).ToList();
            return (MatchType.Prefix, positions);
        }

        var wordStartPositions = WordStartMatchAscii(queryBytes, titleBytes, wordStarts);
        if (wordStartPositions != null)
            return (MatchType.WordStart, wordStartPositions);

        int substringStart = titleLower.IndexOf(queryLower, StringComparison.Ordinal);
        if (substringStart >= 0)
        {
            var positions = Enumerable.Range(substringStart, queryBytes.Length).ToList();
            return (MatchType.Substring, positions);
        }

        var fuzzyPositions = FuzzyMatchAscii(queryBytes, titleBytes);
        if (fuzzyPositions != null)
            return (MatchType.Fuzzy, fuzzyPositions);

        return (MatchType.NoMatch, new List<int>());
    }

    /// <summary>Check if query matches word starts (e.g., "gd" → "Go Dashboard").</summary>
    private List<int>? WordStartMatch(string query, string title)
    {
        var positions = new List<int>();
        var queryChars = query.GetEnumerator();
        bool hasNextQuery = queryChars.MoveNext();
        if (!hasNextQuery)
            return positions; // empty query always matches

        var titleBytes = Encoding.UTF8.GetBytes(title);
        int charIndex = 0;
        int byteIndex = 0;

        foreach (var c in title)
        {
            // Is this a word start?
            byte prev = byteIndex > 0 ? titleBytes[byteIndex - 1] : (byte)' ';
            bool isWordStart = byteIndex == 0 || prev == ' ' || prev == '-' || prev == '_';

            if (isWordStart && hasNextQuery && c == queryChars.Current)
            {
                positions.Add(charIndex);
                hasNextQuery = queryChars.MoveNext();
            }

            charIndex++;
            // Advance byteIndex by the UTF-8 byte length of c
            byteIndex += Encoding.UTF8.GetByteCount(new[] { c });
        }

        if (!hasNextQuery)
            return positions;
        return null;
    }

    /// <summary>ASCII word-start match with optional precomputed word-start positions.</summary>
    internal List<int>? WordStartMatchAscii(byte[] query, byte[] title, IReadOnlyList<int>? wordStarts)
    {
        var positions = new List<int>();
        int queryIdx = 0;
        if (query.Length == 0)
            return positions;

        if (wordStarts != null)
        {
            foreach (int pos in wordStarts)
            {
                if (pos >= title.Length)
                    continue;
                if (title[pos] == query[queryIdx])
                {
                    positions.Add(pos);
                    queryIdx++;
                    if (queryIdx == query.Length)
                        return positions;
                }
            }
        }
        else
        {
            for (int i = 0; i < title.Length; i++)
            {
                bool isWordStart = i == 0 || title[i - 1] == ' ' || title[i - 1] == '-' || title[i - 1] == '_';
                if (isWordStart && title[i] == query[queryIdx])
                {
                    positions.Add(i);
                    queryIdx++;
                    if (queryIdx == query.Length)
                        return positions;
                }
            }
        }

        return null;
    }

    /// <summary>Check fuzzy match (characters in order).</summary>
    private List<int>? FuzzyMatch(string query, string title)
    {
        var positions = new List<int>();
        var queryEnum = query.GetEnumerator();
        bool hasNext = queryEnum.MoveNext();
        if (!hasNext)
            return positions;

        int charIndex = 0;
        foreach (var c in title)
        {
            if (hasNext && c == queryEnum.Current)
            {
                positions.Add(charIndex);
                hasNext = queryEnum.MoveNext();
            }
            charIndex++;
        }

        if (!hasNext)
            return positions;
        return null;
    }

    /// <summary>ASCII fuzzy match (characters in order).</summary>
    internal List<int>? FuzzyMatchAscii(byte[] query, byte[] title)
    {
        var positions = new List<int>();
        int queryIdx = 0;
        if (query.Length == 0)
            return positions;

        for (int i = 0; i < title.Length; i++)
        {
            if (title[i] == query[queryIdx])
            {
                positions.Add(i);
                queryIdx++;
                if (queryIdx == query.Length)
                    return positions;
            }
        }

        return null;
    }

    /// <summary>Compute score from match type and positions.</summary>
    private MatchResult ComputeScore(MatchType matchType, List<int> positions, string query, string title)
    {
        if (!TrackEvidence)
        {
            double combinedBf = matchType.PriorOdds();

            if (positions.Count > 0)
            {
                int firstPos = positions[0];
                var positionFactor = 1.0 + (1.0 / (firstPos + 1.0)) * 0.5;
                combinedBf *= positionFactor;
            }

            int wordBoundaryCount = CountWordBoundaries(positions, title);
            if (wordBoundaryCount > 0)
            {
                var boundaryFactor = 1.0 + wordBoundaryCount * 0.3;
                combinedBf *= boundaryFactor;
            }

            if (matchType == MatchType.Fuzzy && positions.Count > 1)
            {
                var totalGap = TotalGap(positions);
                var gapFactor = 1.0 / (1.0 + totalGap * 0.1);
                combinedBf *= gapFactor;
            }

            int titleLen = Math.Max(title.Length, 1);
            var lengthFactor = 1.0 + (query.Length / (double)titleLen) * 0.2;
            combinedBf *= lengthFactor;

            var score = combinedBf / (1.0 + combinedBf);
            return new MatchResult
            {
                Score = score,
                MatchType = matchType,
                MatchPositions = positions,
                Evidence = new EvidenceLedger(),
            };
        }

        var evidence = new EvidenceLedger();

        // Prior odds from match type
        var priorOdds = matchType.PriorOdds();
        evidence.Add(
            EvidenceKind.MatchType,
            priorOdds,
            new EvidenceDescription.Static(matchType.Description()));

        // Position bonus: matches at start are better
        if (positions.Count > 0)
        {
            int firstPos = positions[0];
            var positionFactor = 1.0 + (1.0 / (firstPos + 1.0)) * 0.5;
            evidence.Add(
                EvidenceKind.Position,
                positionFactor,
                new EvidenceDescription.FirstMatchPos(firstPos));
        }

        // Word boundary bonus
        int wbCount = CountWordBoundaries(positions, title);
        if (wbCount > 0)
        {
            var boundaryFactor = 1.0 + wbCount * 0.3;
            evidence.Add(
                EvidenceKind.WordBoundary,
                boundaryFactor,
                new EvidenceDescription.WordBoundaryCount(wbCount));
        }

        // Gap penalty for fuzzy matches
        if (matchType == MatchType.Fuzzy && positions.Count > 1)
        {
            var totalGap = TotalGap(positions);
            var gapFactor = 1.0 / (1.0 + totalGap * 0.1);
            evidence.Add(
                EvidenceKind.GapPenalty,
                gapFactor,
                new EvidenceDescription.GapTotal(totalGap));
        }

        // Title length: prefer shorter (more specific) titles
        int tLen = Math.Max(title.Length, 1);
        var lengthFact = 1.0 + (query.Length / (double)tLen) * 0.2;
        evidence.Add(
            EvidenceKind.TitleLength,
            lengthFact,
            new EvidenceDescription.CoveragePercent((query.Length / (double)tLen) * 100.0));

        var finalScore = evidence.PosteriorProbability();

        return new MatchResult
        {
            Score = finalScore,
            MatchType = matchType,
            MatchPositions = positions,
            Evidence = evidence,
        };
    }

    /// <summary>Count how many matched positions are at word boundaries.</summary>
    internal int CountWordBoundaries(IReadOnlyList<int> positions, string title)
    {
        var titleBytes = Encoding.UTF8.GetBytes(title);
        int count = 0;
        foreach (int pos in positions)
        {
            bool isBoundary = pos == 0
                || (pos > 0 && pos < titleBytes.Length
                    && (titleBytes[pos - 1] == ' ' || titleBytes[pos - 1] == '-' || titleBytes[pos - 1] == '_'));
            if (isBoundary)
                count++;
        }
        return count;
    }

    /// <summary>Calculate total gap between matched positions.</summary>
    internal int TotalGap(IReadOnlyList<int> positions)
    {
        if (positions.Count < 2)
            return 0;
        int total = 0;
        for (int i = 1; i < positions.Count; i++)
        {
            int gap = positions[i] - positions[i - 1];
            if (gap > 1) total += gap - 1;
        }
        return total;
    }

    // ---- Helpers ----

    private static bool IsAscii(string s)
    {
        foreach (char c in s)
            if (c > 127) return false;
        return true;
    }

    private static bool StartsWith(byte[] source, byte[] prefix)
    {
        if (prefix.Length > source.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
            if (source[i] != prefix[i]) return false;
        return true;
    }

    /// <summary>
    /// Allocation-free case-insensitive containment check.
    /// Mirrors upstream <c>contains_ignore_case</c> in <c>lib.rs</c>.
    /// </summary>
    internal static bool ContainsIgnoreCase(string haystack, string needleLower)
    {
        if (needleLower.Length == 0)
            return true;
        // Fast path for ASCII
        if (IsAscii(haystack) && IsAscii(needleLower))
        {
            if (needleLower.Length > haystack.Length) return false;
            for (int i = 0; i <= haystack.Length - needleLower.Length; i++)
            {
                bool matchFound = true;
                for (int j = 0; j < needleLower.Length; j++)
                {
                    if (char.ToLowerInvariant(haystack[i + j]) != needleLower[j])
                    {
                        matchFound = false;
                        break;
                    }
                }
                if (matchFound) return true;
            }
            return false;
        }
        // Fallback for Unicode (allocates, but correct)
        return haystack.ToLowerInvariant().Contains(needleLower, StringComparison.Ordinal);
    }

    private static int CountChars(string s) => s.Length; // C# strings are UTF-16; for pure ASCII/BMP this is char count

    private static int CountChars(string s, int byteStart, int byteEnd)
    {
        // For ASCII-only paths we use byte positions equal to char positions.
        // For Unicode (UTF-8 byte offsets), we must count actual chars up to the byte offset.
        // In our Unicode path the string is already a .NET string, and IndexOf returns char offsets.
        return byteEnd - byteStart;
    }
}

// ---------------------------------------------------------------------------
// Conformal Rank Confidence
// ---------------------------------------------------------------------------

/// <summary>
/// Confidence level for a ranking position.
///
/// Derived from distribution-free conformal prediction: we compute
/// nonconformity scores (score gaps) and calibrate them against the
/// empirical distribution of all gaps in the result set.
///
/// # Invariants
///
/// 1. <c>confidence</c> is in <c>[0.0, 1.0]</c>.
/// 2. A gap of zero always yields <see cref="RankStability.Unstable"/> stability.
/// 3. Deterministic: same scores → same confidence.
/// </summary>
public sealed class RankConfidence
{
    /// <summary>
    /// Probability that this item truly belongs at this rank position.
    /// Computed as the fraction of score gaps that are smaller than
    /// the gap between this item and the next.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>Absolute score gap to the next-ranked item (0.0 if last or tied).</summary>
    public double GapToNext { get; init; }

    /// <summary>Stability classification derived from gap analysis.</summary>
    public RankStability Stability { get; init; }

    /// <inheritdoc/>
    public override string ToString()
        => $"confidence={Confidence:F3} gap={GapToNext:F4} ({Stability.Label()})";
}

/// <summary>Stability classification for a rank position.</summary>
public enum RankStability
{
    /// <summary>Score gap is large relative to the distribution — rank is reliable.</summary>
    Stable,
    /// <summary>Score gap is moderate — rank is plausible but could swap with neighbors.</summary>
    Marginal,
    /// <summary>Score gap is negligible — rank is essentially a tie.</summary>
    Unstable,
}

/// <summary>Extension methods for <see cref="RankStability"/>.</summary>
public static class RankStabilityExtensions
{
    /// <summary>Human-readable label.</summary>
    public static string Label(this RankStability stability) => stability switch
    {
        RankStability.Stable   => "stable",
        RankStability.Marginal => "marginal",
        RankStability.Unstable => "unstable",
        _ => "unstable",
    };
}

/// <summary>Result of ranking a set of match results with conformal confidence.</summary>
public sealed class RankedResults
{
    /// <summary>Items sorted by descending score, each with rank confidence.</summary>
    public List<RankedItem> Items { get; init; } = new();

    /// <summary>Summary statistics about the ranking.</summary>
    public RankingSummary Summary { get; init; } = null!;
}

/// <summary>A single item in the ranked results.</summary>
public sealed class RankedItem
{
    /// <summary>Index into the original (pre-sort) input slice.</summary>
    public int OriginalIndex { get; init; }

    /// <summary>The match result.</summary>
    public MatchResult Result { get; init; } = null!;

    /// <summary>Conformal confidence for this rank position.</summary>
    public RankConfidence RankConfidence { get; init; } = null!;
}

/// <summary>Summary statistics for a ranked result set.</summary>
public sealed class RankingSummary
{
    /// <summary>Number of items in the ranking.</summary>
    public int Count { get; init; }

    /// <summary>Number of items with stable rank positions.</summary>
    public int StableCount { get; set; }

    /// <summary>Number of tie groups (sets of items with indistinguishable scores).</summary>
    public int TieGroupCount { get; init; }

    /// <summary>Median score gap between adjacent ranked items.</summary>
    public double MedianGap { get; init; }

    /// <inheritdoc/>
    public override string ToString()
        => $"{Count} items, {StableCount} stable, {TieGroupCount} tie groups, median gap {MedianGap:F4}";
}

/// <summary>
/// Conformal ranker that assigns distribution-free confidence to rank positions.
///
/// # Method
///
/// Given sorted scores <c>s_1 ≥ s_2 ≥ ... ≥ s_n</c>, we define the nonconformity
/// score for position <c>i</c> as the gap <c>g_i = s_i - s_{i+1}</c>.
///
/// The conformal p-value for position <c>i</c> is:
/// <code>
/// p_i = |{j : g_j ≤ g_i}| / (n - 1)
/// </code>
///
/// This gives the fraction of gaps that are at most as large as this item's
/// gap, which we interpret as confidence that the item is correctly ranked
/// above its successor.
///
/// # Tie Detection
///
/// Two scores are considered tied when their gap is below <c>tie_epsilon</c>.
/// The default epsilon is <c>1e-9</c>, suitable for f64 posterior probabilities.
///
/// # Failure Modes
///
/// - **All scores identical**: Every position is <see cref="RankStability.Unstable"/> with confidence 0.
/// - **Single item**: Confidence is 1.0 (trivially correct ranking).
/// - **Empty input**: Returns empty results with zeroed summary.
/// </summary>
public sealed class ConformalRanker
{
    /// <summary>Threshold below which two scores are considered tied.</summary>
    public double TieEpsilon { get; init; } = 1e-9;

    /// <summary>Confidence threshold for <see cref="RankStability.Stable"/> classification.</summary>
    public double StableThreshold { get; init; } = 0.7;

    /// <summary>Confidence threshold for <see cref="RankStability.Marginal"/> (below this is <see cref="RankStability.Unstable"/>).</summary>
    public double MarginalThreshold { get; init; } = 0.3;

    /// <summary>Create a ranker with default thresholds.</summary>
    public ConformalRanker() { }

    /// <summary>
    /// Rank a set of match results and assign conformal confidence.
    ///
    /// Results are sorted by descending score. Ties are broken by
    /// <see cref="MatchType"/> (higher variant first), then by shorter title length.
    /// </summary>
    public RankedResults Rank(List<MatchResult> results)
    {
        int count = results.Count;

        if (count == 0)
        {
            return new RankedResults
            {
                Items = new List<RankedItem>(),
                Summary = new RankingSummary
                {
                    Count = 0,
                    StableCount = 0,
                    TieGroupCount = 0,
                    MedianGap = 0.0,
                },
            };
        }

        // Tag each result with its original index, then sort descending by score.
        // Tie-break: higher MatchType variant first.
        var indexed = results.Select((r, i) => (index: i, result: r)).ToList();
        indexed.Sort((a, b) =>
        {
            int cmp = b.result.Score.CompareTo(a.result.Score);
            if (cmp != 0) return cmp;
            return ((int)b.result.MatchType).CompareTo((int)a.result.MatchType);
        });

        // Compute gaps between adjacent scores.
        var gaps = new List<double>(count - 1 > 0 ? count - 1 : 0);
        if (count > 1)
        {
            for (int i = 0; i < count - 1; i++)
                gaps.Add(Math.Max(0.0, indexed[i].result.Score - indexed[i + 1].result.Score));
        }

        // Sort gaps for computing conformal p-values (fraction of gaps ≤ g_i).
        var sortedGaps = gaps.ToList();
        sortedGaps.Sort((a, b) => a.CompareTo(b));

        // Compute rank confidence for each position.
        var items = new List<RankedItem>(count);
        int stableCount = 0;
        int tieGroupCount = 0;
        bool inTieGroup = false;

        for (int rank = 0; rank < indexed.Count; rank++)
        {
            var (origIdx, result) = indexed[rank];
            double gapToNext = rank < gaps.Count ? gaps[rank] : 0.0;

            // Conformal p-value: fraction of gaps that are ≤ this gap.
            double confidence;
            if (sortedGaps.Count == 0)
            {
                // Single item: trivially ranked correctly.
                confidence = 1.0;
            }
            else
            {
                int leqCount = PartitionPoint(sortedGaps, gapToNext + TieEpsilon * 0.5);
                confidence = leqCount / (double)sortedGaps.Count;
            }

            bool isTie = gapToNext < TieEpsilon;
            RankStability stability;
            if (isTie)
            {
                if (!inTieGroup)
                {
                    tieGroupCount++;
                    inTieGroup = true;
                }
                stability = RankStability.Unstable;
            }
            else
            {
                inTieGroup = false;
                if (confidence >= StableThreshold)
                {
                    stableCount++;
                    stability = RankStability.Stable;
                }
                else if (confidence >= MarginalThreshold)
                {
                    stability = RankStability.Marginal;
                }
                else
                {
                    stability = RankStability.Unstable;
                }
            }

            items.Add(new RankedItem
            {
                OriginalIndex = origIdx,
                Result = result,
                RankConfidence = new RankConfidence
                {
                    Confidence = confidence,
                    GapToNext = gapToNext,
                    Stability = stability,
                },
            });
        }

        double medianGap;
        if (sortedGaps.Count == 0)
        {
            medianGap = 0.0;
        }
        else
        {
            int mid = sortedGaps.Count / 2;
            if (sortedGaps.Count % 2 == 0)
                medianGap = (sortedGaps[mid - 1] + sortedGaps[mid]) / 2.0;
            else
                medianGap = sortedGaps[mid];
        }

        return new RankedResults
        {
            Items = items,
            Summary = new RankingSummary
            {
                Count = count,
                StableCount = stableCount,
                TieGroupCount = tieGroupCount,
                MedianGap = medianGap,
            },
        };
    }

    /// <summary>
    /// Convenience: rank the top-k items only.
    ///
    /// All items are still scored and sorted, but only the top <paramref name="k"/> are
    /// returned (with correct confidence relative to the full set).
    /// </summary>
    public RankedResults RankTopK(List<MatchResult> results, int k)
    {
        var ranked = Rank(results);
        if (ranked.Items.Count > k)
            ranked.Items.RemoveRange(k, ranked.Items.Count - k);
        // Stable count may decrease after truncation.
        ranked.Summary.StableCount = ranked.Items.Count(item => item.RankConfidence.Stability == RankStability.Stable);
        return ranked;
    }

    /// <summary>
    /// Returns the index of the first element in the sorted list that is strictly greater than
    /// <paramref name="value"/>. Equivalent to Rust's <c>partition_point(|&amp;g| g &lt;= value)</c>.
    /// </summary>
    private static int PartitionPoint(List<double> sortedList, double value)
    {
        int lo = 0, hi = sortedList.Count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (sortedList[mid] <= value)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }
}

// ---------------------------------------------------------------------------
// Incremental Scorer (bd-39y4.13)
// ---------------------------------------------------------------------------

/// <summary>Cached entry from a previous scoring pass.</summary>
internal sealed class CachedEntry
{
    /// <summary>Index into the corpus.</summary>
    public int CorpusIndex { get; init; }
}

/// <summary>Diagnostics for incremental scoring performance.</summary>
public sealed class IncrementalStats
{
    /// <summary>Number of full rescans performed.</summary>
    public ulong FullScans { get; set; }

    /// <summary>Number of incremental (pruned) scans performed.</summary>
    public ulong IncrementalScans { get; set; }

    /// <summary>Total items evaluated across all scans.</summary>
    public ulong TotalEvaluated { get; set; }

    /// <summary>Total items pruned (skipped) across incremental scans.</summary>
    public ulong TotalPruned { get; set; }

    /// <summary>Fraction of evaluations saved by incremental scoring.</summary>
    public double PruneRatio()
    {
        ulong total = TotalEvaluated + TotalPruned;
        if (total == 0) return 0.0;
        return TotalPruned / (double)total;
    }
}

/// <summary>
/// Incremental scorer that caches results across keystrokes.
///
/// When the user types one more character, the new query is a prefix-extension
/// of the old query. Items that didn't match the shorter query can't match the
/// longer one (monotonicity of substring/prefix/fuzzy matching), so we skip them.
///
/// # Invariants
///
/// 1. **Correctness**: Incremental results are identical to a full rescan.
///    Adding characters can only reduce or maintain the match set, never expand it.
/// 2. **Determinism**: Same (query, corpus) → identical results.
/// 3. **Cache coherence**: Cache is invalidated when corpus changes or query
///    does not extend the previous query.
///
/// # Performance Model
///
/// Let <c>N</c> = corpus size, <c>M</c> = number of matches for the previous query.
/// - Full scan: O(N × L) where L is average title length.
/// - Incremental (query extends previous): O(M × L) where M ≤ N.
/// - Typical command palettes have M ≪ N after 2-3 characters.
///
/// # Failure Modes
///
/// - **Corpus mutation**: If the corpus changes between calls, results may be
///   stale. Call <see cref="Invalidate"/> or let the generation counter detect it.
/// - **Non-extending query** (e.g., backspace): Falls back to full scan.
///   This is correct but loses the incremental speedup.
/// </summary>
public sealed class IncrementalScorer
{
    private BayesianScorer _scorer;
    private string _prevQuery;
    internal List<CachedEntry> _cache;  // internal for test access
    private ulong _corpusGeneration;
    private int _corpusLen;
    private readonly IncrementalStats _stats;

    /// <summary>Create a new incremental scorer (evidence tracking disabled for speed).</summary>
    public IncrementalScorer()
    {
        _scorer = BayesianScorer.Fast();
        _prevQuery = string.Empty;
        _cache = new List<CachedEntry>();
        _corpusGeneration = 0;
        _corpusLen = 0;
        _stats = new IncrementalStats();
    }

    /// <summary>Create with explicit scorer configuration.</summary>
    public IncrementalScorer(BayesianScorer scorer)
    {
        _scorer = scorer;
        _prevQuery = string.Empty;
        _cache = new List<CachedEntry>();
        _corpusGeneration = 0;
        _corpusLen = 0;
        _stats = new IncrementalStats();
    }

    /// <summary>Invalidate the cache (e.g., when corpus changes).</summary>
    public void Invalidate()
    {
        _prevQuery = string.Empty;
        _cache.Clear();
        _corpusGeneration = _corpusGeneration == ulong.MaxValue ? 0 : _corpusGeneration + 1;
    }

    /// <summary>Get diagnostic statistics.</summary>
    public IncrementalStats Stats() => _stats;

    /// <summary>
    /// Score a query against a corpus, using cached results when possible.
    ///
    /// Returns indices into the corpus and their match results, sorted by
    /// descending score. Only items with score &gt; 0 are returned.
    /// </summary>
    /// <param name="query">The current search query.</param>
    /// <param name="corpus">Slice of title strings to search.</param>
    /// <param name="generation">
    /// Optional generation counter; if it differs from the cached value, the cache is invalidated.
    /// </param>
    public List<(int, MatchResult)> ScoreCorpus(string query, string[] corpus, ulong? generation)
    {
        // Detect corpus changes.
        ulong generationVal = generation ?? _corpusGeneration;
        if (generationVal != _corpusGeneration || corpus.Length != _corpusLen)
        {
            Invalidate();
            _corpusGeneration = generationVal;
            _corpusLen = corpus.Length;
        }

        // Determine if we can use incremental scoring.
        bool canPrune = _prevQuery.Length > 0
            && query.StartsWith(_prevQuery, StringComparison.Ordinal)
            && _cache.Count > 0;

        var queryLower = query.ToLowerInvariant();
        List<(int, MatchResult)> results;
        if (canPrune)
            results = ScoreIncremental(query, queryLower, corpus);
        else
            results = ScoreFull(query, queryLower, corpus);

        // Update cache state.
        _prevQuery = query;
        _cache = results.Select(r => new CachedEntry { CorpusIndex = r.Item1 }).ToList();

        return results;
    }

    /// <summary>
    /// Score a query against a corpus with pre-lowercased titles.
    ///
    /// <paramref name="corpusLower"/> must align 1:1 with <paramref name="corpus"/>.
    /// </summary>
    public List<(int, MatchResult)> ScoreCorpusWithLowered(
        string query,
        string[] corpus,
        string[] corpusLower,
        ulong? generation)
    {
        System.Diagnostics.Debug.Assert(corpus.Length == corpusLower.Length,
            "corpusLower must match corpus length");

        // Detect corpus changes.
        ulong generationVal = generation ?? _corpusGeneration;
        if (generationVal != _corpusGeneration || corpus.Length != _corpusLen)
        {
            Invalidate();
            _corpusGeneration = generationVal;
            _corpusLen = corpus.Length;
        }

        bool canPrune = _prevQuery.Length > 0
            && query.StartsWith(_prevQuery, StringComparison.Ordinal)
            && _cache.Count > 0;

        var queryLower = query.ToLowerInvariant();
        List<(int, MatchResult)> results;
        if (canPrune)
            results = ScoreIncrementalLowered(query, queryLower, corpus, corpusLower);
        else
            results = ScoreFullLowered(query, queryLower, corpus, corpusLower);

        // Update cache state.
        _prevQuery = query;
        _cache = results.Select(r => new CachedEntry { CorpusIndex = r.Item1 }).ToList();

        return results;
    }

    /// <summary>
    /// Score a query against a corpus with pre-lowercased titles and word-start cache.
    ///
    /// <paramref name="corpusLower"/> and <paramref name="wordStarts"/> must align 1:1 with <paramref name="corpus"/>.
    /// </summary>
    public List<(int, MatchResult)> ScoreCorpusWithLoweredAndWords(
        string query,
        List<string> corpus,
        List<string> corpusLower,
        List<List<int>> wordStarts,
        ulong? generation)
    {
        System.Diagnostics.Debug.Assert(corpus.Count == corpusLower.Count,
            "corpusLower must match corpus length");
        System.Diagnostics.Debug.Assert(corpus.Count == wordStarts.Count,
            "wordStarts must match corpus length");

        // Detect corpus changes.
        ulong generationVal = generation ?? _corpusGeneration;
        if (generationVal != _corpusGeneration || corpus.Count != _corpusLen)
        {
            Invalidate();
            _corpusGeneration = generationVal;
            _corpusLen = corpus.Count;
        }

        bool canPrune = _prevQuery.Length > 0
            && query.StartsWith(_prevQuery, StringComparison.Ordinal)
            && _cache.Count > 0;

        var queryLower = query.ToLowerInvariant();
        List<(int, MatchResult)> results;
        if (canPrune)
            results = ScoreIncrementalLoweredWithWords(query, queryLower, corpus, corpusLower, wordStarts);
        else
            results = ScoreFullLoweredWithWords(query, queryLower, corpus, corpusLower, wordStarts);

        // Update cache state.
        _prevQuery = query;
        _cache = results.Select(r => new CachedEntry { CorpusIndex = r.Item1 }).ToList();

        return results;
    }

    /// <summary>
    /// Score an explicit subset of corpus indices with precomputed title caches.
    ///
    /// This is used by indexed callers that have already narrowed the candidate
    /// set. It deliberately does not update the incremental query cache: a prefix-only
    /// candidate set is not equivalent to the full fuzzy match set and must not constrain
    /// a later all-match query.
    /// </summary>
    public List<(int, MatchResult)> ScoreCandidateIndicesWithLoweredAndWords(
        string query,
        List<string> corpus,
        List<string> corpusLower,
        List<List<int>> wordStarts,
        List<int> candidateIndices,
        ulong? generation)
    {
        System.Diagnostics.Debug.Assert(corpus.Count == corpusLower.Count,
            "corpusLower must match corpus length");
        System.Diagnostics.Debug.Assert(corpus.Count == wordStarts.Count,
            "wordStarts must match corpus length");

        ulong generationVal = generation ?? _corpusGeneration;
        if (generationVal != _corpusGeneration || corpus.Count != _corpusLen)
        {
            Invalidate();
            _corpusGeneration = generationVal;
            _corpusLen = corpus.Count;
        }

        int candidateCount = candidateIndices.Count(idx => idx < corpus.Count);
        _stats.IncrementalScans++;
        _stats.TotalEvaluated += (ulong)candidateCount;
        ulong pruned = (ulong)corpus.Count - (ulong)Math.Min(candidateCount, corpus.Count);
        _stats.TotalPruned += pruned;

        var queryLower = query.ToLowerInvariant();
        var results = new List<(int, MatchResult)>(candidateCount);
        foreach (int idx in candidateIndices)
        {
            if (idx >= corpus.Count) continue;

            var title = corpus[idx];
            var titleLower = corpusLower[idx];
            var starts = wordStarts[idx];

            var result = _scorer.ScoreWithLoweredTitleAndWords(
                query, queryLower, title, titleLower, starts);
            if (result.Score > 0.0)
                results.Add((idx, result));
        }

        results.Sort(CompareRankedMatchResults);

        return results;
    }

    /// <summary>Full scan: score every item in the corpus.</summary>
    internal List<(int, MatchResult)> ScoreFull(string query, string queryLower, string[] corpus)
    {
        _stats.FullScans++;
        _stats.TotalEvaluated += (ulong)corpus.Length;

        var results = new List<(int, MatchResult)>(corpus.Length);
        for (int i = 0; i < corpus.Length; i++)
        {
            var result = _scorer.ScoreWithQueryLower(query, queryLower, corpus[i]);
            if (result.Score > 0.0)
                results.Add((i, result));
        }

        results.Sort(CompareRankedMatchResults);
        return results;
    }

    /// <summary>Full scan with pre-lowercased titles.</summary>
    private List<(int, MatchResult)> ScoreFullLowered(
        string query, string queryLower, string[] corpus, string[] corpusLower)
    {
        _stats.FullScans++;
        _stats.TotalEvaluated += (ulong)corpus.Length;

        var results = new List<(int, MatchResult)>(corpus.Length);
        for (int i = 0; i < corpus.Length; i++)
        {
            var result = _scorer.ScoreWithLoweredTitle(query, queryLower, corpus[i], corpusLower[i]);
            if (result.Score > 0.0)
                results.Add((i, result));
        }

        results.Sort(CompareRankedMatchResults);
        return results;
    }

    /// <summary>Full scan with pre-lowercased titles and word-start cache.</summary>
    internal List<(int, MatchResult)> ScoreFullLoweredWithWords(
        string query, string queryLower,
        List<string> corpus, List<string> corpusLower,
        List<List<int>> wordStarts)
    {
        _stats.FullScans++;
        _stats.TotalEvaluated += (ulong)corpus.Count;

        var results = new List<(int, MatchResult)>(corpus.Count);
        for (int i = 0; i < corpus.Count; i++)
        {
            var result = _scorer.ScoreWithLoweredTitleAndWords(
                query, queryLower, corpus[i], corpusLower[i], wordStarts[i]);
            if (result.Score > 0.0)
                results.Add((i, result));
        }

        results.Sort(CompareRankedMatchResults);
        return results;
    }

    /// <summary>Incremental scan: only re-score items that previously matched.</summary>
    internal List<(int, MatchResult)> ScoreIncremental(string query, string queryLower, string[] corpus)
    {
        _stats.IncrementalScans++;

        int prevMatchCount = _cache.Count;
        ulong pruned = (ulong)Math.Max(0, corpus.Length - prevMatchCount);
        _stats.TotalPruned += pruned;
        _stats.TotalEvaluated += (ulong)prevMatchCount;

        var results = new List<(int, MatchResult)>(Math.Min(_cache.Count, corpus.Length));
        foreach (var entry in _cache)
        {
            if (entry.CorpusIndex < corpus.Length)
            {
                var result = _scorer.ScoreWithQueryLower(query, queryLower, corpus[entry.CorpusIndex]);
                if (result.Score > 0.0)
                    results.Add((entry.CorpusIndex, result));
            }
        }

        results.Sort(CompareRankedMatchResults);
        return results;
    }

    /// <summary>Incremental scan with pre-lowercased titles.</summary>
    internal List<(int, MatchResult)> ScoreIncrementalLowered(
        string query, string queryLower, string[] corpus, string[] corpusLower)
    {
        _stats.IncrementalScans++;

        int prevMatchCount = _cache.Count;
        ulong pruned = (ulong)Math.Max(0, corpus.Length - prevMatchCount);
        _stats.TotalPruned += pruned;
        _stats.TotalEvaluated += (ulong)prevMatchCount;

        var results = new List<(int, MatchResult)>(Math.Min(_cache.Count, corpus.Length));
        foreach (var entry in _cache)
        {
            if (entry.CorpusIndex < corpus.Length)
            {
                var title = corpus[entry.CorpusIndex];
                var titleLower = corpusLower[entry.CorpusIndex];
                var result = _scorer.ScoreWithLoweredTitle(query, queryLower, title, titleLower);
                if (result.Score > 0.0)
                    results.Add((entry.CorpusIndex, result));
            }
        }

        results.Sort(CompareRankedMatchResults);
        return results;
    }

    /// <summary>Incremental scan with pre-lowercased titles and word-start cache.</summary>
    internal List<(int, MatchResult)> ScoreIncrementalLoweredWithWords(
        string query, string queryLower,
        List<string> corpus, List<string> corpusLower,
        List<List<int>> wordStarts)
    {
        _stats.IncrementalScans++;

        int prevMatchCount = _cache.Count;
        ulong pruned = (ulong)Math.Max(0, corpus.Count - prevMatchCount);
        _stats.TotalPruned += pruned;
        _stats.TotalEvaluated += (ulong)prevMatchCount;

        var results = new List<(int, MatchResult)>(Math.Min(_cache.Count, corpus.Count));
        foreach (var entry in _cache)
        {
            if (entry.CorpusIndex < corpus.Count)
            {
                var title = corpus[entry.CorpusIndex];
                var titleLower = corpusLower[entry.CorpusIndex];
                var starts = wordStarts[entry.CorpusIndex];
                var result = _scorer.ScoreWithLoweredTitleAndWords(
                    query, queryLower, title, titleLower, starts);
                if (result.Score > 0.0)
                    results.Add((entry.CorpusIndex, result));
            }
        }

        results.Sort((left, right) =>
        {
            int cmp = right.Item2.Score.CompareTo(left.Item2.Score);
            if (cmp != 0) return cmp;
            return ((int)right.Item2.MatchType).CompareTo((int)left.Item2.MatchType);
        });

        return results;
    }

    private static int CompareRankedMatchResults((int, MatchResult) left, (int, MatchResult) right)
    {
        int cmp = right.Item2.Score.CompareTo(left.Item2.Score);
        if (cmp != 0) return cmp;
        return ((int)right.Item2.MatchType).CompareTo((int)left.Item2.MatchType);
    }
}
