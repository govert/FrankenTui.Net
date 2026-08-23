// SPDX-License-Identifier: Apache-2.0
// Source-equivalent tests for .external/frankentui/crates/ftui-text/src/hyphenation.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Text;
using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class HyphenationTests
{
    [Fact]
    public void CompileSimplePattern()
    {
        var pattern = Assert.IsType<HyphenationPattern>(Hyphenation.CompilePattern("hy3p"));
        Assert.Equal([new Rune('h'), new Rune('y'), new Rune('p')], pattern.Chars);
        Assert.Equal<byte>([0, 0, 3, 0], pattern.Levels);
    }

    [Fact]
    public void CompileLeadingDigit()
    {
        var pattern = Assert.IsType<HyphenationPattern>(Hyphenation.CompilePattern("2ph"));
        Assert.Equal([new Rune('p'), new Rune('h')], pattern.Chars);
        Assert.Equal<byte>([2, 0, 0], pattern.Levels);
    }

    [Fact]
    public void CompileTrailingDigit()
    {
        var pattern = Assert.IsType<HyphenationPattern>(Hyphenation.CompilePattern("ab4"));
        Assert.Equal([new Rune('a'), new Rune('b')], pattern.Chars);
        Assert.Equal<byte>([0, 0, 4], pattern.Levels);
    }

    [Fact]
    public void CompileDotDelimiter()
    {
        var pattern = Assert.IsType<HyphenationPattern>(Hyphenation.CompilePattern(".ex5am"));
        Assert.Equal(
            [new Rune('.'), new Rune('e'), new Rune('x'), new Rune('a'), new Rune('m')],
            pattern.Chars);
        Assert.Equal<byte>([0, 0, 0, 5, 0, 0], pattern.Levels);
    }

    [Fact]
    public void CompileMultipleDigits()
    {
        var pattern = Assert.IsType<HyphenationPattern>(Hyphenation.CompilePattern("a1b2c3d"));
        Assert.Equal([new Rune('a'), new Rune('b'), new Rune('c'), new Rune('d')], pattern.Chars);
        Assert.Equal<byte>([0, 1, 2, 3, 0], pattern.Levels);
    }

    [Fact]
    public void CompileEmptyReturnsNull()
    {
        Assert.Null(Hyphenation.CompilePattern(""));
        Assert.Null(Hyphenation.CompilePattern("123"));
    }

    [Fact]
    public void CompileAllZeros()
    {
        var pattern = Assert.IsType<HyphenationPattern>(Hyphenation.CompilePattern("abc"));
        Assert.Equal<byte>([0, 0, 0, 0], pattern.Levels);
    }

    [Fact]
    public void TrieSinglePattern()
    {
        var trie = new PatternTrie([Hyphenation.CompilePattern("hy3p")!]);
        var levels = trie.ApplyAll(".hyp.");
        Assert.Equal(3, levels[3]);
    }

    [Fact]
    public void TrieMaximumLevelWins()
    {
        var trie = new PatternTrie(
            [Hyphenation.CompilePattern("ab2c")!, Hyphenation.CompilePattern("b5c")!]);
        Assert.Contains((byte)5, trie.ApplyAll(".abc."));
    }

    [Fact]
    public void ParseExceptionBasic()
    {
        var (word, breaks) = Hyphenation.ParseException("hy-phen-ation");
        Assert.Equal("hyphenation", word);
        Assert.Equal([2, 6], breaks);
    }

    [Fact]
    public void ParseExceptionWithoutHyphens()
    {
        var (word, breaks) = Hyphenation.ParseException("present");
        Assert.Equal("present", word);
        Assert.Empty(breaks);
    }

    [Fact]
    public void ParseExceptionSingleHyphen()
    {
        var (word, breaks) = Hyphenation.ParseException("ta-ble");
        Assert.Equal("table", word);
        Assert.Equal([2], breaks);
    }

    [Fact]
    public void DictionaryExceptionOverridesPatterns()
    {
        var dictionary = new HyphenationDict("en", ["a1b", "b1c"], ["ab-c"]);
        Assert.All(dictionary.Hyphenate("abc"), point => Assert.Equal(2, point.Offset));
    }

    [Fact]
    public void DictionaryShortWordHasNoBreaks()
    {
        Assert.Empty(Hyphenation.EnglishDictMini().Hyphenate("cat"));
    }

    [Fact]
    public void DictionaryRespectsLeftMinimum()
    {
        var dictionary = new HyphenationDict("en", ["1a1b1c1d1e"], []);
        Assert.All(dictionary.Hyphenate("abcde"), point => Assert.True(point.Offset >= 2));
    }

    [Fact]
    public void DictionaryRespectsRightMinimum()
    {
        var dictionary = new HyphenationDict("en", ["1a1b1c1d1e1f1g"], []);
        Assert.All(dictionary.Hyphenate("abcdefg"), point => Assert.True(point.Offset <= 4));
    }

    [Fact]
    public void DictionaryCustomMargins()
    {
        var original = new HyphenationDict("en", ["1a1b1c1d1e1f1g1h"], []);
        var dictionary = original.WithMargins(3, 4);
        Assert.Equal(2, original.LeftMin);
        Assert.Equal(3, original.RightMin);
        Assert.Equal(3, dictionary.LeftMin);
        Assert.Equal(4, dictionary.RightMin);
        Assert.All(
            dictionary.Hyphenate("abcdefgh"),
            point => Assert.InRange(point.Offset, 3, 4));
    }

    [Fact]
    public void DictionaryIsCaseInsensitive()
    {
        var dictionary = new HyphenationDict("en", ["hy3p"], []);
        var lower = dictionary.Hyphenate("hyper");
        Assert.Equal(lower, dictionary.Hyphenate("HYPER"));
        Assert.Equal(lower, dictionary.Hyphenate("Hyper"));
    }

    [Fact]
    public void DictionaryCanHyphenate()
    {
        Assert.True(Hyphenation.EnglishDictMini().CanHyphenate("table"));
    }

    [Fact]
    public void DictionaryEmptyWordHasNoBreaks()
    {
        Assert.Empty(Hyphenation.EnglishDictMini().Hyphenate(""));
    }

    [Fact]
    public void EnglishMiniLoads()
    {
        var dictionary = Hyphenation.EnglishDictMini();
        Assert.Equal("en", dictionary.Language);
        Assert.Equal(121, Hyphenation.EnglishPatternsMini.Count);
        Assert.Equal(11, Hyphenation.EnglishExceptionsMini.Count);
    }

    [Fact]
    public void EnglishMiniExceptionTable()
    {
        var point = Assert.Single(Hyphenation.EnglishDictMini().Hyphenate("table"));
        Assert.Equal(2, point.Offset);
    }

    [Fact]
    public void EnglishMiniExceptionAssociate()
    {
        var breaks = Hyphenation.EnglishDictMini().Hyphenate("associate");
        Assert.Equal([2, 4], breaks.Select(point => point.Offset));
    }

    [Fact]
    public void EnglishMiniNonExceptionWordDoesNotFail()
    {
        _ = Hyphenation.EnglishDictMini().Hyphenate("computer");
    }

    [Fact]
    public void PenaltyLevelOne()
    {
        var penalty = new HyphenBreakPoint(3, 1).ToPenalty();
        Assert.Equal(50L, penalty.Value);
        Assert.True(penalty.Flagged);
    }

    [Fact]
    public void PenaltyLevelThree()
    {
        var penalty = new HyphenBreakPoint(3, 3).ToPenalty();
        Assert.Equal(40L, penalty.Value);
        Assert.True(penalty.Flagged);
    }

    [Fact]
    public void PenaltyLevelFive()
    {
        var penalty = new HyphenBreakPoint(3, 5).ToPenalty();
        Assert.Equal(30L, penalty.Value);
        Assert.True(penalty.Flagged);
    }

    [Fact]
    public void BreakPenaltiesPreserveOffsets()
    {
        var penalties = Hyphenation.BreakPenalties(
            [new HyphenBreakPoint(2, 1), new HyphenBreakPoint(5, 3)]);
        Assert.Equal(2, penalties.Count);
        Assert.Equal(2, penalties[0].Offset);
        Assert.Equal(5, penalties[1].Offset);
        Assert.All(penalties, value => Assert.True(value.Penalty.Flagged));
    }

    [Fact]
    public void FlaggedPenaltiesFeedParagraphAdjacencyDemerits()
    {
        var penalties = Hyphenation.BreakPenalties(
            [new HyphenBreakPoint(2, 1), new HyphenBreakPoint(5, 3)]);
        var objective = ParagraphObjective.Default;
        var demerits = objective.AdjacencyDemerits(
            FitnessClass.Normal,
            FitnessClass.Normal,
            penalties[0].Penalty.Flagged,
            penalties[1].Penalty.Flagged);
        Assert.Equal(objective.DoubleHyphenDemerit, demerits);
    }

    [Fact]
    public void DeterministicForSameInput()
    {
        var dictionary = Hyphenation.EnglishDictMini();
        Assert.Equal(dictionary.Hyphenate("hyphenation"), dictionary.Hyphenate("hyphenation"));
    }

    [Fact]
    public void DeterministicAcrossDictionaryRebuilds()
    {
        var first = Hyphenation.EnglishDictMini();
        var second = Hyphenation.EnglishDictMini();
        Assert.Equal(first.Hyphenate("associate"), second.Hyphenate("associate"));
    }

    [Fact]
    public void SingleCharacterWordHasNoBreaks()
    {
        Assert.Empty(Hyphenation.EnglishDictMini().Hyphenate("a"));
    }

    [Fact]
    public void TwoCharacterWordHasNoBreaks()
    {
        Assert.Empty(Hyphenation.EnglishDictMini().Hyphenate("an"));
    }

    [Fact]
    public void RepeatedCharactersDoNotFail()
    {
        _ = Hyphenation.EnglishDictMini().Hyphenate("aaaaaaa");
    }

    [Fact]
    public void UnicodeWordDoesNotFail()
    {
        _ = Hyphenation.EnglishDictMini().Hyphenate("über");
    }

    [Fact]
    public void OnlyOddLevelsProduceBreaks()
    {
        var dictionary = new HyphenationDict("test", ["a2b2c2d2e2f"], []);
        Assert.Empty(dictionary.Hyphenate("abcdef"));
    }

    [Fact]
    public void MixedOddAndEvenLevels()
    {
        var dictionary = new HyphenationDict("test", ["a1b2c3d"], []).WithMargins(1, 1);
        var offsets = dictionary.Hyphenate("abcd").Select(point => point.Offset).ToArray();
        Assert.Contains(1, offsets);
        Assert.DoesNotContain(2, offsets);
        Assert.Contains(3, offsets);
    }

    [Fact]
    public void CompiledPatternsUseValueEquality()
    {
        Assert.Equal(Hyphenation.CompilePattern("hy3p"), Hyphenation.CompilePattern("hy3p"));
        Assert.NotEqual(Hyphenation.CompilePattern("hy3p"), Hyphenation.CompilePattern("hy5p"));
    }

    [Fact]
    public void ManagedNegativeCoordinatesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HyphenBreakPoint(-1, 1));
        var dictionary = Hyphenation.EnglishDictMini();
        Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.WithMargins(-1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.WithMargins(2, -1));
    }

    [Fact]
    public void ExistingManagedModeValuesRemainStable()
    {
        Assert.Equal(0, (int)TextHyphenationMode.Disabled);
        Assert.Equal(1, (int)TextHyphenationMode.EvaluateSoftHyphenOnly);
        Assert.Equal(TextHyphenationMode.Disabled, TextRenderOptions.Default.HyphenationMode);
    }
}
