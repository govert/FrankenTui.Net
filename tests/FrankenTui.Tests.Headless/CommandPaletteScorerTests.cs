// Upstream source: crates/ftui-widgets/src/command_palette/scorer.rs (tests module)
// Full 1-1 port of all upstream scorer tests.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrankenTui.Widgets.CommandPaletteScoring;
using Xunit;
using MatchType = FrankenTui.Widgets.CommandPaletteScoring.MatchType;

namespace FrankenTui.Tests.Headless;

public class CommandPaletteScorerTests
{
    // -----------------------------------------------------------------------
    // Match Type Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ExactMatchHighestScore()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("Settings", "Settings");
        Assert.Equal(MatchType.Exact, result.MatchType);
        Assert.True(result.Score > 0.95, $"Exact match should score > 0.95, got {result.Score}");
    }

    [Fact]
    public void PrefixMatchHighScore()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("set", "Settings");
        Assert.Equal(MatchType.Prefix, result.MatchType);
        Assert.True(result.Score > 0.85, $"Prefix match should score > 0.85, got {result.Score}");
    }

    [Fact]
    public void WordStartMatchScore()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("gd", "Go Dashboard");
        Assert.Equal(MatchType.WordStart, result.MatchType);
        Assert.True(result.Score > 0.75, $"Word-start should score > 0.75, got {result.Score}");
    }

    [Fact]
    public void SubstringMatchModerateScore()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("set", "Asset Manager");
        Assert.Equal(MatchType.Substring, result.MatchType);
        Assert.True(result.Score > 0.5, $"Substring should score > 0.5, got {result.Score}");
    }

    [Fact]
    public void FuzzyMatchLowScore()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("stg", "Settings");
        Assert.Equal(MatchType.Fuzzy, result.MatchType);
        Assert.True(result.Score > 0.2, $"Fuzzy should score > 0.2, got {result.Score}");
        Assert.True(result.Score < 0.7, $"Fuzzy should score < 0.7, got {result.Score}");
    }

    [Fact]
    public void NoMatchReturnsZero()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("xyz", "Settings");
        Assert.Equal(MatchType.NoMatch, result.MatchType);
        Assert.Equal(0.0, result.Score);
    }

    // -----------------------------------------------------------------------
    // Internal ASCII Matcher Edge Cases
    // -----------------------------------------------------------------------

    [Fact]
    public void WordStartMatchAsciiEmptyQueryMatches()
    {
        var scorer = new BayesianScorer();
        var positions = scorer.WordStartMatchAscii(
            Array.Empty<byte>(),
            Encoding.ASCII.GetBytes("go dashboard"),
            null);
        Assert.NotNull(positions);
        Assert.Empty(positions!);
    }

    [Fact]
    public void WordStartMatchAsciiSkipsOutOfBoundsPrecomputedPositions()
    {
        var scorer = new BayesianScorer();
        var starts = new List<int> { 999, 0, 3 };
        var positions = scorer.WordStartMatchAscii(
            Encoding.ASCII.GetBytes("gd"),
            Encoding.ASCII.GetBytes("go dashboard"),
            starts);
        Assert.NotNull(positions);
        Assert.Equal(new List<int> { 0, 3 }, positions);
    }

    [Fact]
    public void FuzzyMatchAsciiEmptyQueryMatches()
    {
        var scorer = new BayesianScorer();
        var positions = scorer.FuzzyMatchAscii(
            Array.Empty<byte>(),
            Encoding.ASCII.GetBytes("settings"));
        Assert.NotNull(positions);
        Assert.Empty(positions!);
    }

    // -----------------------------------------------------------------------
    // Score Invariants
    // -----------------------------------------------------------------------

    [Fact]
    public void ScoreBounded()
    {
        var scorer = new BayesianScorer();
        var testCases = new[]
        {
            ("a", "abcdefghijklmnop"),
            ("full", "full"),
            ("", "anything"),
            ("xyz", "abc"),
            ("stg", "Settings"),
        };

        foreach (var (query, title) in testCases)
        {
            var result = scorer.Score(query, title);
            Assert.True(
                result.Score >= 0.0 && result.Score <= 1.0,
                $"Score for ({query}, {title}) = {result.Score} not in [0, 1]");
        }
    }

    [Fact]
    public void ScoreDeterministic()
    {
        var scorer = new BayesianScorer();
        var result1 = scorer.Score("nav", "Navigation");
        var result2 = scorer.Score("nav", "Navigation");
        Assert.True(
            Math.Abs(result1.Score - result2.Score) < double.Epsilon,
            "Same input should produce identical scores");
    }

    [Fact]
    public void TiebreakShorterFirst()
    {
        var scorer = new BayesianScorer();
        var shortResult = scorer.Score("set", "Set");
        var longResult = scorer.Score("set", "Settings");
        Assert.True(
            shortResult.Score >= longResult.Score,
            $"Shorter title should score >= longer: {shortResult.Score} vs {longResult.Score}");
    }

    // -----------------------------------------------------------------------
    // Case Insensitivity
    // -----------------------------------------------------------------------

    [Fact]
    public void CaseInsensitive()
    {
        var scorer = new BayesianScorer();
        var lower = scorer.Score("set", "Settings");
        var upper = scorer.Score("SET", "Settings");
        Assert.True(
            Math.Abs(lower.Score - upper.Score) < double.Epsilon,
            "Case should not affect score");
    }

    // -----------------------------------------------------------------------
    // Match Positions
    // -----------------------------------------------------------------------

    [Fact]
    public void MatchPositionsCorrect()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("gd", "Go Dashboard");
        Assert.Equal(new List<int> { 0, 3 }, result.MatchPositions);
    }

    [Fact]
    public void FuzzyMatchPositions()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("stg", "Settings");
        // s(0), t(3), g(6)
        Assert.Equal(3, result.MatchPositions.Count);
        Assert.Equal(0, result.MatchPositions[0]); // 's'
    }

    // -----------------------------------------------------------------------
    // Empty Query
    // -----------------------------------------------------------------------

    [Fact]
    public void EmptyQueryReturnsAll()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("", "Any Command");
        Assert.True(result.Score > 0.0, "Empty query should have positive score");
        Assert.True(result.Score < 1.0, "Empty query should not be max score");
    }

    [Fact]
    public void EmptyTitleIsSafe()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("x", "");
        Assert.Equal(MatchType.NoMatch, result.MatchType);
        Assert.True(double.IsFinite(result.Score), "Score should remain finite");
    }

    [Fact]
    public void EmptyQueryEmptyTitleIsFinite()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("", "");
        Assert.True(double.IsFinite(result.Score), "Score should remain finite");
        Assert.Equal(MatchType.Fuzzy, result.MatchType);
    }

    // -----------------------------------------------------------------------
    // Query Longer Than Title
    // -----------------------------------------------------------------------

    [Fact]
    public void QueryLongerThanTitle()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("verylongquery", "short");
        Assert.Equal(MatchType.NoMatch, result.MatchType);
        Assert.Equal(0.0, result.Score);
    }

    [Fact]
    public void LongQueryExactMatchIsHandled()
    {
        var scorer = new BayesianScorer();
        var query = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = scorer.Score(query, query);
        Assert.Equal(MatchType.Exact, result.MatchType);
        Assert.True(double.IsFinite(result.Score));
        Assert.True(result.Score > 0.9, $"Exact long query should score high, got {result.Score}");
    }

    [Fact]
    public void GapPenaltyPrefersTightFuzzyMatches()
    {
        var scorer = new BayesianScorer();
        var tight = scorer.Score("ace", "abcde");
        var gappy = scorer.Score("ace", "a...c...e");
        Assert.Equal(MatchType.Fuzzy, tight.MatchType);
        Assert.Equal(MatchType.Fuzzy, gappy.MatchType);
        Assert.True(
            tight.Score > gappy.Score,
            $"Tight fuzzy match should score higher than gappy: {tight.Score} vs {gappy.Score}");
    }

    // -----------------------------------------------------------------------
    // Tag Matching
    // -----------------------------------------------------------------------

    [Fact]
    public void TagMatchBoostsScore()
    {
        var scorer = new BayesianScorer();
        // Use a query that matches the title (fuzzy)
        var withoutTag = scorer.Score("set", "Settings");
        var withTag = scorer.ScoreWithTags("set", "Settings", new[] { "config", "setup" });
        // Tag "setup" contains "set", so it should boost the score
        Assert.True(
            withTag.Score > withoutTag.Score,
            $"Tag match should boost score: {withTag.Score} > {withoutTag.Score}");
    }

    // -----------------------------------------------------------------------
    // Evidence Ledger
    // -----------------------------------------------------------------------

    [Fact]
    public void EvidenceLedgerTracksFactors()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("set", "Settings");

        Assert.NotEmpty(result.Evidence.Entries());

        // Should have match type entry
        Assert.Contains(result.Evidence.Entries(), e => e.Kind == EvidenceKind.MatchType);
    }

    [Fact]
    public void EvidenceLedgerDisplay()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("gd", "Go Dashboard");
        var display = result.Evidence.ToString();
        Assert.Contains("Evidence Ledger", display);
        Assert.Contains("Posterior P:", display);
    }

    // -----------------------------------------------------------------------
    // Property Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void PropertyOrderingTotal()
    {
        var scorer = new BayesianScorer();
        var titles = new[] { "Settings", "Set Theme", "Reset View", "Asset" };

        var scores = titles.Select(t => (score: scorer.Score("set", t).Score, title: t)).ToList();

        // Sort should be stable and total
        scores.Sort((a, b) => b.score.CompareTo(a.score));

        // Verify no NaN or infinite scores
        foreach (var (score, _) in scores)
            Assert.True(double.IsFinite(score));
    }

    [Fact]
    public void PropertyPrefixMonotonic()
    {
        var scorer = new BayesianScorer();
        // Longer exact prefix match should score higher
        var oneChar = scorer.Score("s", "Settings");
        var threeChar = scorer.Score("set", "Settings");
        // Both are prefix matches, longer should be better
        Assert.True(
            threeChar.Score >= oneChar.Score,
            "Longer prefix should score >= shorter");
    }

    // -----------------------------------------------------------------------
    // Match Type Prior Odds
    // -----------------------------------------------------------------------

    [Fact]
    public void MatchTypePriorOrdering()
    {
        Assert.True(MatchType.Exact.PriorOdds() > MatchType.Prefix.PriorOdds());
        Assert.True(MatchType.Prefix.PriorOdds() > MatchType.WordStart.PriorOdds());
        Assert.True(MatchType.WordStart.PriorOdds() > MatchType.Substring.PriorOdds());
        Assert.True(MatchType.Substring.PriorOdds() > MatchType.Fuzzy.PriorOdds());
        Assert.True(MatchType.Fuzzy.PriorOdds() > MatchType.NoMatch.PriorOdds());
    }

    // -----------------------------------------------------------------------
    // Conformal Ranker Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ConformalTieBreaksByMatchType()
    {
        var exact = MatchResult.NoMatch(); exact.Score = 0.5; exact.MatchType = MatchType.Exact;
        var prefix = MatchResult.NoMatch(); prefix.Score = 0.5; prefix.MatchType = MatchType.Prefix;
        var wordStart = MatchResult.NoMatch(); wordStart.Score = 0.5; wordStart.MatchType = MatchType.WordStart;
        var substring = MatchResult.NoMatch(); substring.Score = 0.5; substring.MatchType = MatchType.Substring;

        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(new List<MatchResult> { substring, wordStart, prefix, exact });
        var order = ranked.Items.Select(item => item.Result.MatchType).ToList();

        Assert.Equal(
            new List<MatchType> { MatchType.Exact, MatchType.Prefix, MatchType.WordStart, MatchType.Substring },
            order);
    }

    [Fact]
    public void ConformalEmptyInput()
    {
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(new List<MatchResult>());
        Assert.Empty(ranked.Items);
        Assert.Equal(0, ranked.Summary.Count);
        Assert.Equal(0, ranked.Summary.StableCount);
        Assert.Equal(0, ranked.Summary.TieGroupCount);
        Assert.Equal(0.0, ranked.Summary.MedianGap);
    }

    [Fact]
    public void ConformalSingleItem()
    {
        var scorer = new BayesianScorer();
        var results = new List<MatchResult> { scorer.Score("set", "Settings") };
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        Assert.Single(ranked.Items);
        Assert.Equal(1.0, ranked.Items[0].RankConfidence.Confidence);
        Assert.Equal(1, ranked.Summary.Count);
    }

    [Fact]
    public void ConformalSortedDescending()
    {
        var scorer = new BayesianScorer();
        var results = new List<MatchResult>
        {
            scorer.Score("set", "Settings"),
            scorer.Score("set", "Asset Manager"),
            scorer.Score("set", "Reset Configuration Panel"),
        };
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        // Verify descending score order.
        for (int i = 0; i < ranked.Items.Count - 1; i++)
        {
            var a = ranked.Items[i].Result.Score;
            var b = ranked.Items[i + 1].Result.Score;
            Assert.True(a >= b, $"Items should be sorted descending: {a} >= {b}");
        }
    }

    [Fact]
    public void ConformalConfidenceBounded()
    {
        var scorer = new BayesianScorer();
        var titles = new[] { "Settings", "Set Theme", "Asset Manager", "Reset View", "Offset Tool", "Test Suite" };
        var results = titles.Select(t => scorer.Score("set", t)).ToList();
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        foreach (var item in ranked.Items)
        {
            Assert.True(
                item.RankConfidence.Confidence >= 0.0 && item.RankConfidence.Confidence <= 1.0,
                $"Confidence must be in [0, 1], got {item.RankConfidence.Confidence}");
            Assert.True(item.RankConfidence.GapToNext >= 0.0, "Gap must be non-negative");
        }
    }

    [Fact]
    public void ConformalDeterministic()
    {
        var scorer = new BayesianScorer();
        var titles = new[] { "Settings", "Set Theme", "Asset", "Reset" };

        var results1 = titles.Select(t => scorer.Score("set", t)).ToList();
        var results2 = titles.Select(t => scorer.Score("set", t)).ToList();

        var ranker = new ConformalRanker();
        var ranked1 = ranker.Rank(results1);
        var ranked2 = ranker.Rank(results2);

        for (int i = 0; i < ranked1.Items.Count; i++)
        {
            var a = ranked1.Items[i];
            var b = ranked2.Items[i];
            Assert.True(
                Math.Abs(a.RankConfidence.Confidence - b.RankConfidence.Confidence) < double.Epsilon,
                "Confidence should be deterministic");
            Assert.Equal(a.OriginalIndex, b.OriginalIndex);
        }
    }

    [Fact]
    public void ConformalTiesDetected()
    {
        var r1 = MatchResult.NoMatch(); r1.Score = 0.8; r1.MatchType = MatchType.Prefix;
        var r2 = MatchResult.NoMatch(); r2.Score = 0.8; r2.MatchType = MatchType.Prefix;
        var r3 = MatchResult.NoMatch(); r3.Score = 0.5; r3.MatchType = MatchType.Substring;

        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(new List<MatchResult> { r1, r2, r3 });

        // The first two have identical scores — their gap is 0 → unstable.
        Assert.Equal(RankStability.Unstable, ranked.Items[0].RankConfidence.Stability);
        Assert.True(ranked.Summary.TieGroupCount >= 1, "Should detect at least one tie group");
    }

    [Fact]
    public void ConformalAllIdenticalScores()
    {
        var results = new List<MatchResult>();
        for (int i = 0; i < 5; i++)
        {
            var r = MatchResult.NoMatch(); r.Score = 0.5; r.MatchType = MatchType.Fuzzy;
            results.Add(r);
        }

        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        // All gaps are zero → all unstable.
        foreach (var item in ranked.Items)
            Assert.Equal(RankStability.Unstable, item.RankConfidence.Stability);
    }

    [Fact]
    public void ConformalWellSeparatedScoresAreStable()
    {
        var results = new List<MatchResult>();
        foreach (var s in new[] { 0.9, 0.6, 0.3, 0.1 })
        {
            var r = MatchResult.NoMatch(); r.Score = s; r.MatchType = MatchType.Prefix;
            results.Add(r);
        }

        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        // With well-separated scores, most should be stable.
        Assert.True(
            ranked.Summary.StableCount >= 2,
            $"Well-separated scores should yield stable positions, got {ranked.Summary.StableCount}");
    }

    [Fact]
    public void ConformalTopKTruncates()
    {
        var scorer = new BayesianScorer();
        var titles = new[] { "Settings", "Set Theme", "Asset Manager", "Reset View", "Offset Tool" };
        var results = titles.Select(t => scorer.Score("set", t)).ToList();

        var ranker = new ConformalRanker();
        var ranked = ranker.RankTopK(results, 2);

        Assert.Equal(2, ranked.Items.Count);
        // Top-k items should still have confidence from the full ranking.
        Assert.True(ranked.Items[0].RankConfidence.Confidence > 0.0);
    }

    [Fact]
    public void ConformalOriginalIndicesPreserved()
    {
        var scorer = new BayesianScorer();
        var titles = new[] { "Zebra Tool", "Settings", "Apple" };
        var results = titles.Select(t => scorer.Score("set", t)).ToList();

        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        // "Settings" (index 1) should be ranked first (prefix match).
        Assert.Equal(1, ranked.Items[0].OriginalIndex);
    }

    [Fact]
    public void ConformalSummaryDisplay()
    {
        var scorer = new BayesianScorer();
        var results = new List<MatchResult>
        {
            scorer.Score("set", "Settings"),
            scorer.Score("set", "Set Theme"),
        };
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        var display = ranked.Summary.ToString();
        Assert.Contains("2 items", display);
    }

    [Fact]
    public void ConformalRankConfidenceDisplay()
    {
        var rc = new RankConfidence { Confidence = 0.85, GapToNext = 0.1234, Stability = RankStability.Stable };
        var display = rc.ToString();
        Assert.Contains("0.850", display);
        Assert.Contains("stable", display);
    }

    [Fact]
    public void ConformalStabilityLabels()
    {
        Assert.Equal("stable", RankStability.Stable.Label());
        Assert.Equal("marginal", RankStability.Marginal.Label());
        Assert.Equal("unstable", RankStability.Unstable.Label());
    }

    // -----------------------------------------------------------------------
    // Property: gap_to_next of last item is always 0
    // -----------------------------------------------------------------------

    [Fact]
    public void ConformalLastItemGapZero()
    {
        var scorer = new BayesianScorer();
        var results = new List<MatchResult>
        {
            scorer.Score("set", "Settings"),
            scorer.Score("set", "Asset"),
            scorer.Score("set", "Reset"),
        };
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        var last = ranked.Items.Last();
        Assert.Equal(0.0, last.RankConfidence.GapToNext);
    }

    // -----------------------------------------------------------------------
    // Property: median_gap is non-negative
    // -----------------------------------------------------------------------

    [Fact]
    public void ConformalMedianGapNonNegative()
    {
        var scorer = new BayesianScorer();
        var titles = new[]
        {
            "Settings", "Set Theme", "Asset Manager", "Reset View",
            "Offset Tool", "Test Suite", "System Settings", "Reset Defaults",
        };
        var results = titles.Select(t => scorer.Score("set", t)).ToList();
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);

        Assert.True(ranked.Summary.MedianGap >= 0.0, "Median gap must be non-negative");
    }

    // -----------------------------------------------------------------------
    // IncrementalScorer Tests (bd-39y4.13)
    // -----------------------------------------------------------------------

    private static string[] TestCorpus() => new[]
    {
        "Open File",
        "Save File",
        "Close Tab",
        "Git: Commit",
        "Git: Push",
        "Git: Pull",
        "Go to Line",
        "Find in Files",
        "Toggle Terminal",
        "Format Document",
    };

    [Fact]
    public void IncrementalFullScanOnFirstCall()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();
        var results = scorer.ScoreCorpus("git", corpus, null);

        Assert.NotEmpty(results);
        Assert.Equal(1UL, scorer.Stats().FullScans);
        Assert.Equal(0UL, scorer.Stats().IncrementalScans);
    }

    [Fact]
    public void IncrementalPrunesOnQueryExtension()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        // First query: "g" matches many items
        var r1 = scorer.ScoreCorpus("g", corpus, null);
        Assert.Equal(1UL, scorer.Stats().FullScans);

        // Extended query: "gi" — should use incremental path
        var r2 = scorer.ScoreCorpus("gi", corpus, null);
        Assert.Equal(1UL, scorer.Stats().IncrementalScans);
        Assert.True(r2.Count <= r1.Count, "Extended query should match <= items");

        // Further extension: "git" — still incremental
        var r3 = scorer.ScoreCorpus("git", corpus, null);
        Assert.Equal(2UL, scorer.Stats().IncrementalScans);
        Assert.True(r3.Count <= r2.Count);
    }

    [Fact]
    public void IncrementalCorrectnessMatchesFullScan()
    {
        var corpus = TestCorpus();

        // Incremental path
        var inc = new IncrementalScorer();
        inc.ScoreCorpus("g", corpus, null);
        var incResults = inc.ScoreCorpus("git", corpus, null);

        // Full scan path (fresh scorer, no cache)
        var full = new IncrementalScorer();
        var fullResults = full.ScoreCorpus("git", corpus, null);

        // Results should be identical.
        Assert.Equal(fullResults.Count, incResults.Count);

        for (int i = 0; i < incResults.Count; i++)
        {
            Assert.Equal(fullResults[i].Item1, incResults[i].Item1);
            Assert.True(
                Math.Abs(incResults[i].Item2.Score - fullResults[i].Item2.Score) < double.Epsilon,
                $"Same scores: {incResults[i].Item2.Score} vs {fullResults[i].Item2.Score}");
        }
    }

    [Fact]
    public void IncrementalFallsBackOnNonExtension()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        scorer.ScoreCorpus("git", corpus, null);
        Assert.Equal(1UL, scorer.Stats().FullScans);

        // "fi" doesn't extend "git" — must full scan
        scorer.ScoreCorpus("fi", corpus, null);
        Assert.Equal(2UL, scorer.Stats().FullScans);
    }

    [Fact]
    public void IncrementalInvalidateForcesFullScan()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        scorer.ScoreCorpus("g", corpus, null);
        scorer.Invalidate();

        // Even though "gi" extends "g", cache was cleared
        scorer.ScoreCorpus("gi", corpus, null);
        Assert.Equal(2UL, scorer.Stats().FullScans);
        Assert.Equal(0UL, scorer.Stats().IncrementalScans);
    }

    [Fact]
    public void IncrementalGenerationChangeInvalidates()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        scorer.ScoreCorpus("g", corpus, 1);

        // Generation changed — cache invalid
        scorer.ScoreCorpus("gi", corpus, 2);
        Assert.Equal(2UL, scorer.Stats().FullScans);
    }

    [Fact]
    public void IncrementalCorpusSizeChangeInvalidates()
    {
        var scorer = new IncrementalScorer();
        var corpus1 = TestCorpus();
        var corpus2 = corpus1.Take(5).ToArray();

        scorer.ScoreCorpus("g", corpus1, null);
        scorer.ScoreCorpus("gi", corpus2, null);
        // Corpus size changed → full scan
        Assert.Equal(2UL, scorer.Stats().FullScans);
    }

    [Fact]
    public void IncrementalSkipsOutOfBoundsCacheEntries()
    {
        var scorer = new IncrementalScorer();

        // Simulate a cache produced for a larger corpus. The scorer should safely
        // skip entries that are out of bounds for the current corpus.
        scorer._cache = new List<CachedEntry>
        {
            new CachedEntry { CorpusIndex = 0 },
            new CachedEntry { CorpusIndex = 999 },
        };

        var corpus = new[] { "a" };
        var results = scorer.ScoreIncremental("a", "a", corpus);
        Assert.Single(results);
        Assert.Equal(0, results[0].Item1);

        var corpusLower = new[] { "a" };
        var results2 = scorer.ScoreIncrementalLowered("a", "a", corpus, corpusLower);
        Assert.Single(results2);
        Assert.Equal(0, results2[0].Item1);

        var corpusS = new List<string> { "a" };
        var corpusLowerS = new List<string> { "a" };
        var wordStarts = new List<List<int>> { new List<int> { 0 } };
        var results3 = scorer.ScoreIncrementalLoweredWithWords("a", "a", corpusS, corpusLowerS, wordStarts);
        Assert.Single(results3);
        Assert.Equal(0, results3[0].Item1);
    }

    [Fact]
    public void IncrementalEmptyQuery()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        var results = scorer.ScoreCorpus("", corpus, null);
        // Empty query matches everything (with weak scores)
        Assert.Equal(corpus.Length, results.Count);
    }

    [Fact]
    public void IncrementalNoMatchQuery()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        var results = scorer.ScoreCorpus("zzz", corpus, null);
        Assert.Empty(results);
    }

    [Fact]
    public void IncrementalStatsPruneRatio()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        scorer.ScoreCorpus("g", corpus, null);
        scorer.ScoreCorpus("gi", corpus, null);
        scorer.ScoreCorpus("git", corpus, null);

        var stats = scorer.Stats();
        Assert.True(
            stats.PruneRatio() > 0.0,
            "Prune ratio should be > 0 after incremental scans");
        Assert.True(stats.TotalPruned > 0, "Should have pruned some items");
    }

    [Fact]
    public void IncrementalResultsSortedDescending()
    {
        var scorer = new IncrementalScorer();
        var corpus = TestCorpus();

        var results = scorer.ScoreCorpus("o", corpus, null);
        for (int i = 0; i < results.Count - 1; i++)
        {
            var a = results[i].Item2.Score;
            var b = results[i + 1].Item2.Score;
            Assert.True(a >= b, $"Results should be sorted descending: {a} >= {b}");
        }
    }

    [Fact]
    public void IncrementalLoweredMatchesFull()
    {
        var corpus = new List<string> { "Open File", "Save File", "Close", "Launch 🚀" };
        var corpusRefs = corpus.ToArray();
        var lower = corpus.Select(s => s.ToLowerInvariant()).ToList();
        var wordStarts = lower.Select(titleLower =>
        {
            var bytes = Encoding.UTF8.GetBytes(titleLower);
            var result = new List<int>();
            int byteIdx = 0;
            foreach (char ch in titleLower)
            {
                bool isWordStart = byteIdx == 0
                    || (byteIdx > 0 && bytes[byteIdx - 1] == ' ')
                    || (byteIdx > 0 && bytes[byteIdx - 1] == '-')
                    || (byteIdx > 0 && bytes[byteIdx - 1] == '_');
                if (isWordStart) result.Add(byteIdx);
                byteIdx += Encoding.UTF8.GetByteCount(new[] { ch });
            }
            return result;
        }).ToList();

        var full = new IncrementalScorer();
        var lowered = new IncrementalScorer();

        var a = full.ScoreCorpus("fi", corpusRefs, null);
        var b = lowered.ScoreCorpusWithLoweredAndWords("fi", corpus, lower, wordStarts, null);

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Item1, b[i].Item1);
            Assert.Equal(a[i].Item2.MatchType, b[i].Item2.MatchType);
            Assert.Equal(a[i].Item2.MatchPositions, b[i].Item2.MatchPositions);
            Assert.True(
                Math.Abs(a[i].Item2.Score - b[i].Item2.Score) < 1e-12,
                $"score mismatch: {a[i].Item2.Score} vs {b[i].Item2.Score}");
        }
    }

    [Fact]
    public void IncrementalDeterministic()
    {
        var corpus = TestCorpus();

        var s1 = new IncrementalScorer();
        var r1 = s1.ScoreCorpus("git", corpus, null);

        var s2 = new IncrementalScorer();
        var r2 = s2.ScoreCorpus("git", corpus, null);

        Assert.Equal(r1.Count, r2.Count);
        for (int i = 0; i < r1.Count; i++)
        {
            Assert.Equal(r1[i].Item1, r2[i].Item1);
            Assert.True(Math.Abs(r1[i].Item2.Score - r2[i].Item2.Score) < double.Epsilon);
        }
    }

    // -----------------------------------------------------------------------
    // MatchType::description coverage (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void MatchTypeDescriptions()
    {
        Assert.Equal("exact match", MatchType.Exact.Description());
        Assert.Equal("prefix match", MatchType.Prefix.Description());
        Assert.Equal("word-start match", MatchType.WordStart.Description());
        Assert.Equal("contiguous substring", MatchType.Substring.Description());
        Assert.Equal("fuzzy match", MatchType.Fuzzy.Description());
        Assert.Equal("no match", MatchType.NoMatch.Description());
    }

    [Fact]
    public void MatchTypeNoMatchPriorIsZero()
    {
        Assert.Equal(0.0, MatchType.NoMatch.PriorOdds());
    }

    // -----------------------------------------------------------------------
    // EvidenceDescription Display variants (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvidenceDescriptionStaticDisplay()
    {
        var d = new EvidenceDescription.Static("test message");
        Assert.Equal("test message", d.ToString());
    }

    [Fact]
    public void EvidenceDescriptionTitleLengthDisplay()
    {
        var d = new EvidenceDescription.TitleLengthChars(42);
        Assert.Equal("title length 42 chars", d.ToString());
    }

    [Fact]
    public void EvidenceDescriptionFirstMatchPosDisplay()
    {
        var d = new EvidenceDescription.FirstMatchPos(5);
        Assert.Equal("first match at position 5", d.ToString());
    }

    [Fact]
    public void EvidenceDescriptionWordBoundaryCountDisplay()
    {
        var d = new EvidenceDescription.WordBoundaryCount(3);
        Assert.Equal("3 word boundary matches", d.ToString());
    }

    [Fact]
    public void EvidenceDescriptionGapTotalDisplay()
    {
        var d = new EvidenceDescription.GapTotal(7);
        Assert.Equal("total gap of 7 characters", d.ToString());
    }

    [Fact]
    public void EvidenceDescriptionCoveragePercentDisplay()
    {
        var d = new EvidenceDescription.CoveragePercent(83.5);
        var s = d.ToString();
        Assert.Contains("84%", s);
    }

    // -----------------------------------------------------------------------
    // EvidenceEntry Display (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvidenceEntryDisplaySupports()
    {
        var entry = new EvidenceEntry
        {
            Kind = EvidenceKind.Position,
            BayesFactor = 1.5,
            Description = new EvidenceDescription.FirstMatchPos(0),
        };
        var s = entry.ToString();
        Assert.Contains("supports", s);
        Assert.Contains("Position", s);
    }

    [Fact]
    public void EvidenceEntryDisplayOpposes()
    {
        var entry = new EvidenceEntry
        {
            Kind = EvidenceKind.GapPenalty,
            BayesFactor = 0.5,
            Description = new EvidenceDescription.GapTotal(10),
        };
        var s = entry.ToString();
        Assert.Contains("opposes", s);
    }

    [Fact]
    public void EvidenceEntryDisplayNeutral()
    {
        var entry = new EvidenceEntry
        {
            Kind = EvidenceKind.TitleLength,
            BayesFactor = 1.0,
            Description = new EvidenceDescription.Static("neutral factor"),
        };
        var s = entry.ToString();
        Assert.Contains("neutral", s);
    }

    // -----------------------------------------------------------------------
    // EvidenceLedger direct method tests (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void LedgerCombinedBayesFactor()
    {
        var ledger = new EvidenceLedger();
        ledger.Add(EvidenceKind.Position, 2.0, new EvidenceDescription.Static("a"));
        ledger.Add(EvidenceKind.WordBoundary, 3.0, new EvidenceDescription.Static("b"));
        Assert.True(Math.Abs(ledger.CombinedBayesFactor() - 6.0) < double.Epsilon);
    }

    [Fact]
    public void LedgerCombinedBayesFactorEmpty()
    {
        var ledger = new EvidenceLedger();
        Assert.True(Math.Abs(ledger.CombinedBayesFactor() - 1.0) < double.Epsilon);
    }

    [Fact]
    public void LedgerPriorOddsPresent()
    {
        var ledger = new EvidenceLedger();
        ledger.Add(EvidenceKind.MatchType, 9.0, new EvidenceDescription.Static("prefix"));
        Assert.Equal(9.0, ledger.PriorOdds());
    }

    [Fact]
    public void LedgerPriorOddsAbsent()
    {
        var ledger = new EvidenceLedger();
        Assert.Null(ledger.PriorOdds());
    }

    [Fact]
    public void LedgerPosteriorProbabilityNoPrior()
    {
        var ledger = new EvidenceLedger();
        ledger.Add(EvidenceKind.Position, 2.0, new EvidenceDescription.Static("pos"));
        // No MatchType entry → prior defaults to 1.0
        // posterior_odds = 1.0 * 2.0 = 2.0 → prob = 2/3
        var prob = ledger.PosteriorProbability();
        Assert.True(Math.Abs(prob - 2.0 / 3.0) < 1e-9);
    }

    [Fact]
    public void LedgerPosteriorProbabilityInfiniteOdds()
    {
        var ledger = new EvidenceLedger();
        ledger.Add(EvidenceKind.MatchType, double.PositiveInfinity, new EvidenceDescription.Static("inf"));
        Assert.Equal(1.0, ledger.PosteriorProbability());
    }

    // -----------------------------------------------------------------------
    // BayesianScorer::fast() path (no evidence tracking) (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void FastScorerNoEvidence()
    {
        var scorer = BayesianScorer.Fast();
        var result = scorer.Score("set", "Settings");
        Assert.True(result.Score > 0.0);
        Assert.Empty(result.Evidence.Entries());
    }

    [Fact]
    public void FastScorerScoreMatchesProbabilityRange()
    {
        var scorer = BayesianScorer.Fast();
        var result = scorer.Score("set", "Settings");
        Assert.True(result.Score >= 0.0 && result.Score <= 1.0);
    }

    [Fact]
    public void FastScorerEmptyQuery()
    {
        var scorer = BayesianScorer.Fast();
        var result = scorer.Score("", "Test");
        Assert.True(result.Score > 0.0);
        Assert.Empty(result.Evidence.Entries());
    }

    [Fact]
    public void FastScorerFuzzyWithGapPenalty()
    {
        var scorer = BayesianScorer.Fast();
        var result = scorer.Score("ace", "a...b...c...d...e");
        Assert.Equal(MatchType.Fuzzy, result.MatchType);
        Assert.True(result.Score > 0.0);
    }

    [Fact]
    public void FastScorerTagBoost()
    {
        var scorer = BayesianScorer.Fast();
        var without = scorer.Score("set", "Settings");
        var with_ = scorer.ScoreWithTags("set", "Settings", new[] { "setup" });
        Assert.True(with_.Score > without.Score);
    }

    [Fact]
    public void FastScorerTagNoBoostAtScoreOne()
    {
        var scorer = BayesianScorer.Fast();
        // score_with_tags fast path only boosts when score is in (0, 1)
        var result = scorer.ScoreWithTags("Settings", "Settings", new[] { "settings" });
        Assert.True(result.Score <= 1.0);
    }

    // -----------------------------------------------------------------------
    // Unicode (non-ASCII) matching paths (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void UnicodeExactMatch()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("日本語", "日本語");
        Assert.Equal(MatchType.Exact, result.MatchType);
        Assert.True(result.Score > 0.9);
    }

    [Fact]
    public void UnicodePrefixMatch()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("日本", "日本語テスト");
        Assert.Equal(MatchType.Prefix, result.MatchType);
    }

    [Fact]
    public void UnicodeSubstringMatch()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("本語", "日本語テスト");
        Assert.Equal(MatchType.Substring, result.MatchType);
    }

    [Fact]
    public void UnicodeFuzzyMatch()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("日テ", "日本語テスト");
        Assert.Equal(MatchType.Fuzzy, result.MatchType);
    }

    [Fact]
    public void UnicodeNoMatch()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("ωψξ", "αβγ");
        Assert.Equal(MatchType.NoMatch, result.MatchType);
    }

    [Fact]
    public void UnicodeWordStartMatch()
    {
        var scorer = new BayesianScorer();
        // Word boundaries at space/dash/underscore
        var result = scorer.Score("gd", "go dashboard");
        Assert.Equal(MatchType.WordStart, result.MatchType);
    }

    // -----------------------------------------------------------------------
    // score_with_query_lower / score_with_lowered_title (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void ScoreWithQueryLowerMatchesScore()
    {
        var scorer = new BayesianScorer();
        var direct = scorer.Score("Set", "Settings");
        var pre = scorer.ScoreWithQueryLower("Set", "set", "Settings");
        Assert.True(Math.Abs(direct.Score - pre.Score) < double.Epsilon);
        Assert.Equal(direct.MatchType, pre.MatchType);
    }

    [Fact]
    public void ScoreWithLoweredTitleMatchesScore()
    {
        var scorer = new BayesianScorer();
        var direct = scorer.Score("set", "Settings");
        var pre = scorer.ScoreWithLoweredTitle("set", "set", "Settings", "settings");
        Assert.True(Math.Abs(direct.Score - pre.Score) < double.Epsilon);
    }

    [Fact]
    public void ScoreWithLoweredTitleAndWordsMatches()
    {
        var scorer = new BayesianScorer();
        var direct = scorer.Score("gd", "Go Dashboard");
        var pre = scorer.ScoreWithLoweredTitleAndWords(
            "gd", "gd", "Go Dashboard", "go dashboard", new[] { 0, 3 });
        Assert.Equal(direct.MatchType, pre.MatchType);
        Assert.True(Math.Abs(direct.Score - pre.Score) < double.Epsilon);
    }

    // -----------------------------------------------------------------------
    // Tag matching edge cases (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void TagMatchNoTitleMatchReturnsNoMatch()
    {
        var scorer = new BayesianScorer();
        var result = scorer.ScoreWithTags("xyz", "Settings", new[] { "xyz" });
        // Tag matches "xyz" but title doesn't match → no boost applied
        Assert.Equal(MatchType.NoMatch, result.MatchType);
        Assert.Equal(0.0, result.Score);
    }

    [Fact]
    public void TagMatchNoMatchingTag()
    {
        var scorer = new BayesianScorer();
        var without = scorer.Score("set", "Settings");
        var with_ = scorer.ScoreWithTags("set", "Settings", new[] { "foo", "bar" });
        // No tag matches → score unchanged
        Assert.True(Math.Abs(without.Score - with_.Score) < double.Epsilon);
    }

    [Fact]
    public void TagMatchCaseInsensitive()
    {
        var scorer = new BayesianScorer();
        var result = scorer.ScoreWithTags("set", "Settings", new[] { "SETUP" });
        var without = scorer.Score("set", "Settings");
        Assert.True(result.Score > without.Score);
    }

    // -----------------------------------------------------------------------
    // MatchResult::no_match() (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void NoMatchResultHasEvidence()
    {
        var r = MatchResult.NoMatch();
        Assert.Equal(0.0, r.Score);
        Assert.Equal(MatchType.NoMatch, r.MatchType);
        Assert.Empty(r.MatchPositions);
        Assert.Equal(1, r.Evidence.Entries().Count);
        Assert.Equal(EvidenceKind.MatchType, r.Evidence.Entries()[0].Kind);
        Assert.Equal(0.0, r.Evidence.Entries()[0].BayesFactor);
    }

    // -----------------------------------------------------------------------
    // count_word_boundaries / total_gap edge cases (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void CountWordBoundariesAtStart()
    {
        var scorer = new BayesianScorer();
        // Position 0 is always a word boundary
        var count = scorer.CountWordBoundaries(new[] { 0 }, "hello");
        Assert.Equal(1, count);
    }

    [Fact]
    public void CountWordBoundariesAfterSeparators()
    {
        var scorer = new BayesianScorer();
        // "a-b_c d" → positions 0, 2, 4, 6 are word starts
        var count = scorer.CountWordBoundaries(new[] { 0, 2, 4, 6 }, "a-b_c d");
        Assert.Equal(4, count);
    }

    [Fact]
    public void CountWordBoundariesMidWord()
    {
        var scorer = new BayesianScorer();
        // Position 2 in "hello" is mid-word
        var count = scorer.CountWordBoundaries(new[] { 2 }, "hello");
        Assert.Equal(0, count);
    }

    [Fact]
    public void TotalGapEmpty()
    {
        var scorer = new BayesianScorer();
        Assert.Equal(0, scorer.TotalGap(new List<int>()));
    }

    [Fact]
    public void TotalGapSingle()
    {
        var scorer = new BayesianScorer();
        Assert.Equal(0, scorer.TotalGap(new List<int> { 5 }));
    }

    [Fact]
    public void TotalGapContiguous()
    {
        var scorer = new BayesianScorer();
        // [0,1,2] → gaps are 0,0 → total 0
        Assert.Equal(0, scorer.TotalGap(new List<int> { 0, 1, 2 }));
    }

    [Fact]
    public void TotalGapWithGaps()
    {
        var scorer = new BayesianScorer();
        // [0, 3, 7] → gaps: (3-0-1)=2, (7-3-1)=3 → total 5
        Assert.Equal(5, scorer.TotalGap(new List<int> { 0, 3, 7 }));
    }

    // -----------------------------------------------------------------------
    // ConformalRanker edge cases (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void ConformalCustomThresholds()
    {
        var ranker = new ConformalRanker
        {
            TieEpsilon = 0.1,
            StableThreshold = 0.9,
            MarginalThreshold = 0.5,
        };
        // With tie_epsilon=0.1, scores within 0.1 are ties
        var r1 = MatchResult.NoMatch(); r1.Score = 0.5; r1.MatchType = MatchType.Prefix;
        var r2 = MatchResult.NoMatch(); r2.Score = 0.45; r2.MatchType = MatchType.Prefix;

        var ranked = ranker.Rank(new List<MatchResult> { r1, r2 });
        Assert.Equal(
            RankStability.Unstable,
            ranked.Items[0].RankConfidence.Stability);
    }

    [Fact]
    public void ConformalRankTopKLargerThanCount()
    {
        var scorer = new BayesianScorer();
        var results = new List<MatchResult> { scorer.Score("set", "Settings") };
        var ranker = new ConformalRanker();
        var ranked = ranker.RankTopK(results, 100);
        Assert.Single(ranked.Items);
    }

    [Fact]
    public void ConformalRankTopKZero()
    {
        var scorer = new BayesianScorer();
        var results = new List<MatchResult>
        {
            scorer.Score("set", "Settings"),
            scorer.Score("set", "Asset"),
        };
        var ranker = new ConformalRanker();
        var ranked = ranker.RankTopK(results, 0);
        Assert.Empty(ranked.Items);
        Assert.Equal(0, ranked.Summary.StableCount);
    }

    [Fact]
    public void ConformalRankConfidenceDisplayMarginal()
    {
        var rc = new RankConfidence { Confidence = 0.5, GapToNext = 0.05, Stability = RankStability.Marginal };
        var s = rc.ToString();
        Assert.Contains("marginal", s);
    }

    [Fact]
    public void ConformalRankConfidenceDisplayUnstable()
    {
        var rc = new RankConfidence { Confidence = 0.1, GapToNext = 0.001, Stability = RankStability.Unstable };
        var s = rc.ToString();
        Assert.Contains("unstable", s);
    }

    [Fact]
    public void ConformalMedianGapEvenCount()
    {
        // 5 items → 4 gaps → even
        var results = new List<MatchResult>();
        foreach (var s in new[] { 0.9, 0.7, 0.5, 0.3, 0.1 })
        {
            var r = MatchResult.NoMatch(); r.Score = s; r.MatchType = MatchType.Prefix;
            results.Add(r);
        }
        var ranker = new ConformalRanker();
        var ranked = ranker.Rank(results);
        // 4 gaps, all 0.2 → median = 0.2
        Assert.True(
            Math.Abs(ranked.Summary.MedianGap - 0.2) < 0.01,
            $"median_gap={ranked.Summary.MedianGap}, expected ~0.2");
    }

    // -----------------------------------------------------------------------
    // IncrementalScorer additional coverage (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void IncrementalWithScorerUsesProvidedScorer()
    {
        var scorer = new BayesianScorer(); // evidence tracking ON
        var inc = new IncrementalScorer(scorer);
        var corpus = TestCorpus();
        var results = inc.ScoreCorpus("git", corpus, null);
        Assert.NotEmpty(results);
        // Evidence tracking is on, so evidence should be populated
        Assert.NotEmpty(results[0].Item2.Evidence.Entries());
    }

    [Fact]
    public void IncrementalDefaultTrait()
    {
        var inc = new IncrementalScorer();
        Assert.Equal(0UL, inc.Stats().FullScans);
        Assert.Equal(0UL, inc.Stats().IncrementalScans);
    }

    [Fact]
    public void IncrementalStatsPruneRatioZeroWhenEmpty()
    {
        var stats = new IncrementalStats();
        Assert.Equal(0.0, stats.PruneRatio());
    }

    [Fact]
    public void IncrementalScoreCorpusWithLowered()
    {
        var corpus = new[] { "Open File", "Save File", "Close Tab" };
        var lower = new[] { "open file", "save file", "close tab" };
        var inc = new IncrementalScorer();
        var results = inc.ScoreCorpusWithLowered("fi", corpus, lower, null);
        Assert.NotEmpty(results);
        // All results should reference valid corpus indices
        foreach (var (idx, _) in results)
            Assert.True(idx < corpus.Length);
    }

    [Fact]
    public void IncrementalScoreCorpusWithLoweredIncrementalPath()
    {
        var corpus = new[] { "Open File", "Save File", "Close Tab" };
        var lower = new[] { "open file", "save file", "close tab" };
        var inc = new IncrementalScorer();
        inc.ScoreCorpusWithLowered("f", corpus, lower, null);
        var results = inc.ScoreCorpusWithLowered("fi", corpus, lower, null);
        Assert.Equal(1UL, inc.Stats().IncrementalScans);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void IncrementalScoreCorpusWithLoweredAndWords()
    {
        var corpus = new List<string> { "Open File", "Save File", "Close Tab" };
        var lower = new List<string> { "open file", "save file", "close tab" };
        var wordStarts = new List<List<int>> { new List<int> { 0, 5 }, new List<int> { 0, 5 }, new List<int> { 0, 6 } };

        var inc = new IncrementalScorer();
        var results = inc.ScoreCorpusWithLoweredAndWords("fi", corpus, lower, wordStarts, null);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void IncrementalScoreCorpusWithLoweredAndWordsIncrementalPath()
    {
        var corpus = new List<string> { "Open File", "Save File", "Close Tab" };
        var lower = new List<string> { "open file", "save file", "close tab" };
        var wordStarts = new List<List<int>> { new List<int> { 0, 5 }, new List<int> { 0, 5 }, new List<int> { 0, 6 } };

        var inc = new IncrementalScorer();
        inc.ScoreCorpusWithLoweredAndWords("f", corpus, lower, wordStarts, null);
        var results = inc.ScoreCorpusWithLoweredAndWords("fi", corpus, lower, wordStarts, null);
        Assert.Equal(1UL, inc.Stats().IncrementalScans);
        Assert.NotEmpty(results);
    }

    // -----------------------------------------------------------------------
    // BayesianScorer::Default (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void BayesianScorerDefaultHasEvidenceOff()
    {
        var scorer = BayesianScorer.Default();
        Assert.False(scorer.TrackEvidence);
    }

    // -----------------------------------------------------------------------
    // Word boundary separator: hyphen and underscore (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void WordStartMatchWithHyphens()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("fc", "file-commander");
        Assert.Equal(MatchType.WordStart, result.MatchType);
        Assert.Equal(new List<int> { 0, 5 }, result.MatchPositions);
    }

    [Fact]
    public void WordStartMatchWithUnderscores()
    {
        var scorer = new BayesianScorer();
        var result = scorer.Score("fc", "file_commander");
        Assert.Equal(MatchType.WordStart, result.MatchType);
        Assert.Equal(new List<int> { 0, 5 }, result.MatchPositions);
    }

    // -----------------------------------------------------------------------
    // MatchType Ord derivation (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void MatchTypeOrd()
    {
        var types = new List<MatchType>
        {
            MatchType.Exact,
            MatchType.NoMatch,
            MatchType.Fuzzy,
            MatchType.Prefix,
            MatchType.Substring,
            MatchType.WordStart,
        };
        types.Sort();
        Assert.Equal(
            new List<MatchType>
            {
                MatchType.NoMatch,
                MatchType.Fuzzy,
                MatchType.Substring,
                MatchType.WordStart,
                MatchType.Prefix,
                MatchType.Exact,
            },
            types);
    }

    // -----------------------------------------------------------------------
    // EvidenceKind equality (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void EvidenceKindEquality()
    {
        Assert.Equal(EvidenceKind.MatchType, EvidenceKind.MatchType);
        Assert.NotEqual(EvidenceKind.Position, EvidenceKind.GapPenalty);
    }

    // -----------------------------------------------------------------------
    // Scoring position bonus (bd-z1c65)
    // -----------------------------------------------------------------------

    [Fact]
    public void EarlierSubstringScoresHigher()
    {
        var scorer = new BayesianScorer();
        var early = scorer.Score("set", "set in stone");
        var late = scorer.Score("set", "the asset");
        // "set" at position 0 vs "set" at position 5
        Assert.True(
            early.Score > late.Score,
            $"Earlier match should score higher: {early.Score} vs {late.Score}");
    }
}

// ===========================================================================
// Performance regression tests (bd-39y4.5)
// Upstream source: crates/ftui-widgets/src/command_palette/scorer.rs (perf_tests module)
// Full 1-1 port of all upstream perf_tests.
// ===========================================================================

public class CommandPaletteScorerPerfTests
{
    // DIVERGENCE: Rust uses `std::time::Instant` and `std::hint::black_box`.
    // In .NET we use `System.Diagnostics.Stopwatch` for timing, and a volatile
    // field assignment to prevent dead-code elimination (equivalent to black_box).

    private struct PerfStats
    {
        public long P50Us;
        public long P95Us;
        public long P99Us;
        public double MeanUs;
        public double VarianceUs;
        public int Samples;
    }

    /// <summary>Budget thresholds for single-query scoring.</summary>
    private const long SingleScoreBudgetUs = 10;       // 10µs per score call
    private const long Corpus100BudgetUs = 500;        // 500µs for 100-item full scan
    private const long Corpus1000BudgetUs = 5_000;     // 5ms for 1000-item full scan
    private const long Corpus5000BudgetUs = 25_000;    // 25ms for 5000-item full scan
    private const long Incremental7Key100BudgetUs = 5_000;    // 5ms for 7 keystrokes on 100 items
    private const long Incremental7Key1000BudgetUs = 15_000;  // 15ms for 7 keystrokes on 1000 items

    private const long CoverageBudgetMultiplier = 5;

    private static bool IsCoverageRun()
    {
        return Environment.GetEnvironmentVariable("LLVM_PROFILE_FILE") != null
            || Environment.GetEnvironmentVariable("CARGO_LLVM_COV") != null;
    }

    private static long CoverageBudgetUsWithMode(long baseBudget, bool coverageRun)
    {
        // Coverage instrumentation adds substantial overhead; relax budgets to keep
        // runs stable while still logging the timings.
        return coverageRun ? baseBudget * CoverageBudgetMultiplier : baseBudget;
    }

    private static long CoverageBudgetUs(long baseBudget) =>
        CoverageBudgetUsWithMode(baseBudget, IsCoverageRun());

    [Fact]
    public void CoverageBudgetUsWithModeRespectsFlagFalse()
    {
        Assert.Equal(123L, CoverageBudgetUsWithMode(123, false));
    }

    [Fact]
    public void CoverageBudgetUsWithModeRespectsFlagTrue()
    {
        Assert.Equal(123L * CoverageBudgetMultiplier, CoverageBudgetUsWithMode(123, true));
    }

    /// <summary>Generate a command corpus of the specified size with realistic variety.</summary>
    private static List<string> MakeCorpus(int size)
    {
        var baseCommands = new[]
        {
            "Open File", "Save File", "Close Tab", "Split Editor Right",
            "Split Editor Down", "Toggle Terminal", "Go to Line", "Find in Files",
            "Replace in Files", "Git: Commit", "Git: Push", "Git: Pull",
            "Debug: Start", "Debug: Stop", "Debug: Step Over", "Format Document",
            "Rename Symbol", "Go to Definition", "Find All References", "Toggle Sidebar",
        };
        var result = new List<string>(size);
        for (int i = 0; i < size; i++)
        {
            var cmd = baseCommands[i % baseCommands.Length];
            result.Add(i < baseCommands.Length ? cmd : $"{cmd} ({i})");
        }
        return result;
    }

    private static PerfStats MeasureStatsUs(int iterations, Action f)
    {
        var times = new List<long>(iterations);
        // Warmup
        for (int i = 0; i < 3; i++) f();
        for (int i = 0; i < iterations; i++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            f();
            sw.Stop();
            times.Add(sw.ElapsedTicks * 1_000_000L / System.Diagnostics.Stopwatch.Frequency);
        }
        times.Sort();
        int len = times.Count;
        long p50 = times[len / 2];
        long p95 = times[(int)Math.Min(len * 0.95, len - 1)];
        long p99 = times[(int)Math.Min(len * 0.99, len - 1)];
        double mean = times.Average();
        double variance = times.Select(v => { double d = v - mean; return d * d; }).Average();
        return new PerfStats { P50Us = p50, P95Us = p95, P99Us = p99, MeanUs = mean, VarianceUs = variance, Samples = len };
    }

    // DIVERGENCE: Rust uses std::hint::black_box. We use a volatile sink to prevent DCE.
    private static volatile object? _sink;
    private static void BlackBox<T>(T value) { _sink = value; }

    [Fact]
    public void PerfSingleScoreUnderBudget()
    {
        // DIVERGENCE: Budget multiplied by 10 for .NET CI; .NET JIT warm-up and
        // Stopwatch granularity make sub-microsecond measurements unreliable.
        var scorer = BayesianScorer.Fast();
        var stats = MeasureStatsUs(200, () =>
        {
            BlackBox(scorer.Score("git co", "Git: Commit"));
        });
        // Log timings (equivalent to eprintln! in Rust)
        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_score_single\",\"samples\":{stats.Samples},\"p50_us\":{stats.P50Us},\"p95_us\":{stats.P95Us},\"p99_us\":{stats.P99Us},\"mean_us\":{stats.MeanUs:F2},\"variance_us\":{stats.VarianceUs:F2}}}");
        long budget = CoverageBudgetUs(SingleScoreBudgetUs) * 10; // DIVERGENCE: 10x for .NET JIT/timer overhead
        Assert.True(
            stats.P50Us <= budget,
            $"Single score p50 = {stats.P50Us}µs exceeds budget {budget}µs");
    }

    [Fact]
    public void PerfCorpus100UnderBudget()
    {
        var scorer = BayesianScorer.Fast();
        var corpus = MakeCorpus(100);
        var stats = MeasureStatsUs(50, () =>
        {
            var results = corpus
                .Select(t => scorer.Score("git co", t))
                .Where(r => r.Score > 0.0)
                .ToList();
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            BlackBox(results);
        });
        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_corpus_100\",\"samples\":{stats.Samples},\"p50_us\":{stats.P50Us},\"p95_us\":{stats.P95Us},\"p99_us\":{stats.P99Us},\"mean_us\":{stats.MeanUs:F2},\"variance_us\":{stats.VarianceUs:F2}}}");
        long budget = CoverageBudgetUs(Corpus100BudgetUs) * 10; // DIVERGENCE: 10x for .NET overhead
        Assert.True(
            stats.P95Us <= budget,
            $"100-item corpus p95 = {stats.P95Us}µs exceeds budget {budget}µs");
    }

    [Fact]
    public void PerfCorpus1000UnderBudget()
    {
        var scorer = BayesianScorer.Fast();
        var corpus = MakeCorpus(1_000);
        var stats = MeasureStatsUs(20, () =>
        {
            var results = corpus
                .Select(t => scorer.Score("git co", t))
                .Where(r => r.Score > 0.0)
                .ToList();
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            BlackBox(results);
        });
        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_corpus_1000\",\"samples\":{stats.Samples},\"p50_us\":{stats.P50Us},\"p95_us\":{stats.P95Us},\"p99_us\":{stats.P99Us},\"mean_us\":{stats.MeanUs:F2},\"variance_us\":{stats.VarianceUs:F2}}}");
        long budget = CoverageBudgetUs(Corpus1000BudgetUs) * 10; // DIVERGENCE: 10x for .NET overhead
        Assert.True(
            stats.P95Us <= budget,
            $"1000-item corpus p95 = {stats.P95Us}µs exceeds budget {budget}µs");
    }

    [Fact]
    public void PerfCorpus5000UnderBudget()
    {
        var scorer = BayesianScorer.Fast();
        var corpus = MakeCorpus(5_000);
        var stats = MeasureStatsUs(10, () =>
        {
            var results = corpus
                .Select(t => scorer.Score("git co", t))
                .Where(r => r.Score > 0.0)
                .ToList();
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            BlackBox(results);
        });
        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_corpus_5000\",\"samples\":{stats.Samples},\"p50_us\":{stats.P50Us},\"p95_us\":{stats.P95Us},\"p99_us\":{stats.P99Us},\"mean_us\":{stats.MeanUs:F2},\"variance_us\":{stats.VarianceUs:F2}}}");
        long budget = CoverageBudgetUs(Corpus5000BudgetUs) * 10; // DIVERGENCE: 10x for .NET overhead
        Assert.True(
            stats.P95Us <= budget,
            $"5000-item corpus p95 = {stats.P95Us}µs exceeds budget {budget}µs");
    }

    [Fact]
    public void PerfIncremental7Key100UnderBudget()
    {
        var corpus = MakeCorpus(100);
        var corpusArray = corpus.ToArray();
        var queries = new[] { "g", "gi", "git", "git ", "git c", "git co", "git com" };

        var stats = MeasureStatsUs(30, () =>
        {
            var inc = new IncrementalScorer();
            foreach (var query in queries)
            {
                BlackBox(inc.ScoreCorpus(query, corpusArray, null));
            }
        });
        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_incremental_7key_100\",\"samples\":{stats.Samples},\"p50_us\":{stats.P50Us},\"p95_us\":{stats.P95Us},\"p99_us\":{stats.P99Us},\"mean_us\":{stats.MeanUs:F2},\"variance_us\":{stats.VarianceUs:F2}}}");
        long budget = CoverageBudgetUs(Incremental7Key100BudgetUs) * 10; // DIVERGENCE: 10x for .NET overhead
        Assert.True(
            stats.P95Us <= budget,
            $"Incremental 7-key 100-item p95 = {stats.P95Us}µs exceeds budget {budget}µs");
    }

    [Fact]
    public void PerfIncremental7Key1000UnderBudget()
    {
        var corpus = MakeCorpus(1_000);
        var corpusArray = corpus.ToArray();
        var queries = new[] { "g", "gi", "git", "git ", "git c", "git co", "git com" };

        var stats = MeasureStatsUs(10, () =>
        {
            var inc = new IncrementalScorer();
            foreach (var query in queries)
            {
                BlackBox(inc.ScoreCorpus(query, corpusArray, null));
            }
        });
        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_incremental_7key_1000\",\"samples\":{stats.Samples},\"p50_us\":{stats.P50Us},\"p95_us\":{stats.P95Us},\"p99_us\":{stats.P99Us},\"mean_us\":{stats.MeanUs:F2},\"variance_us\":{stats.VarianceUs:F2}}}");
        long budget = CoverageBudgetUs(Incremental7Key1000BudgetUs) * 10; // DIVERGENCE: 10x for .NET overhead
        Assert.True(
            stats.P95Us <= budget,
            $"Incremental 7-key 1000-item p95 = {stats.P95Us}µs exceeds budget {budget}µs");
    }

    private static long ScalingRatioUs(long p50SmallUs, long p50LargeUs)
    {
        // DIVERGENCE: Rust returns f64; we return long (integer ratio ×10 for precision).
        // The Rust version returns f64; we keep the comparison logic intact.
        return p50SmallUs > 0 ? p50LargeUs / p50SmallUs : 0;
    }

    private static double ScalingRatioUsDouble(long p50SmallUs, long p50LargeUs)
    {
        return p50SmallUs > 0 ? p50LargeUs / (double)p50SmallUs : 0.0;
    }

    [Fact]
    public void ScalingRatioHandlesZeroDivisor()
    {
        Assert.Equal(0L, ScalingRatioUs(0, 123));
        // 10 / 25 = 0 in integer; Rust compares as float 2.5
        // DIVERGENCE: use the double version for the fractional comparison.
        Assert.Equal(2.5, ScalingRatioUsDouble(10, 25));
    }

    [Fact]
    public void PerfScalingSublinear()
    {
        var scorer = BayesianScorer.Fast();
        var corpus100 = MakeCorpus(100);
        var corpus1000 = MakeCorpus(1_000);

        var stats100 = MeasureStatsUs(30, () =>
        {
            var results = corpus100
                .Select(t => scorer.Score("git", t))
                .Where(r => r.Score > 0.0)
                .ToList();
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            BlackBox(results);
        });

        var stats1000 = MeasureStatsUs(20, () =>
        {
            var results = corpus1000
                .Select(t => scorer.Score("git", t))
                .Where(r => r.Score > 0.0)
                .ToList();
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            BlackBox(results);
        });

        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_scaling\",\"samples_100\":{stats100.Samples},\"p50_100_us\":{stats100.P50Us},\"samples_1000\":{stats1000.Samples},\"p50_1000_us\":{stats1000.P50Us}}}");

        // 10x corpus ⇒ O(n log n) theoretical ratio ≈ 15x. Use 25x to absorb
        // system noise when tests run in parallel (still catches O(n²) = 100x).
        double ratio = ScalingRatioUsDouble(stats100.P50Us, stats1000.P50Us);
        Assert.True(
            ratio < 25.0,
            $"1000/100 scaling ratio = {ratio:F1}x exceeds 25x threshold (100: {stats100.P50Us}µs, 1000: {stats1000.P50Us}µs)");
    }

    [Fact]
    public void PerfIncrementalFasterThanNaive()
    {
        var corpus = MakeCorpus(100);
        var corpusArray = corpus.ToArray();
        var scorer = BayesianScorer.Fast();
        var queries = new[] { "g", "gi", "git", "git ", "git c", "git co", "git com" };

        var naiveStats = MeasureStatsUs(30, () =>
        {
            foreach (var query in queries)
            {
                var results = corpus
                    .Select(t => scorer.Score(query, t))
                    .Where(r => r.Score > 0.0)
                    .ToList();
                results.Sort((a, b) => b.Score.CompareTo(a.Score));
                BlackBox(results);
            }
        });

        var incStats = MeasureStatsUs(30, () =>
        {
            var inc = new IncrementalScorer();
            foreach (var query in queries)
            {
                BlackBox(inc.ScoreCorpus(query, corpusArray, null));
            }
        });

        Console.Error.WriteLine(
            $"{{\"ts\":\"2026-02-04T00:00:00Z\",\"event\":\"palette_incremental_vs_naive\",\"samples\":{naiveStats.Samples},\"naive_p50_us\":{naiveStats.P50Us},\"naive_p95_us\":{naiveStats.P95Us},\"inc_p50_us\":{incStats.P50Us},\"inc_p95_us\":{incStats.P95Us}}}");

        // Incremental should not be more than 2x slower than naive
        // (in practice it's faster, but we set a relaxed threshold)
        Assert.True(
            incStats.P50Us <= naiveStats.P50Us * 2 + 50,
            $"Incremental p50 = {incStats.P50Us}µs is >2x slower than naive p50 = {naiveStats.P50Us}µs");
    }
}
