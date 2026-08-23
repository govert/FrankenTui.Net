// Behavioral port of the None and basic Knuth-Plass tests in
// .external/frankentui/crates/ftui-text/src/wrap.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;
using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class WrapTests
{
    [Fact]
    public void WrapNoneModeDoesNotWidthWrap()
    {
        var lines = TextWrapper.WrapLine("hello world", 5, TextWrapMode.None);

        Assert.Equal(["hello world"], lines);
    }

    [Fact]
    public void WrapNoneModeSplitsEmbeddedNewlines()
    {
        Assert.Equal(["a", "b"], TextWrapper.WrapLine("a\nb", 5, TextWrapMode.None));
        Assert.Equal(["a", "b"], TextWrapper.WrapLine("a\r\nb", 5, TextWrapMode.None));
        Assert.Equal(["a", "b"], TextWrapper.WrapLine("a\nb", 0, TextWrapMode.Word));
        Assert.Equal(["a", ""], TextWrapper.WrapLine("a\n", 5, TextWrapMode.None));
    }

    [Fact]
    public void WrapNoneModeAppliesDefaultTrailingTrim()
    {
        Assert.Equal(["hello", "world"], TextWrapper.WrapLine("hello   \nworld\t", 20, TextWrapMode.None));
    }

    [Fact]
    public void WrapOptimalDropsLeadingWhitespace()
    {
        var result = TextWrapper.WrapOptimal("   foo", 4);

        Assert.Equal(["foo"], result.Lines);
        Assert.Equal(0UL, result.TotalCost);
        Assert.Equal(
            TextWrapper.WrapLine("   foo bar baz", 10, TextWrapMode.Word),
            TextWrapper.WrapTextOptimal("   foo bar baz", 10));
    }

    [Fact]
    public void WrapOptimalWhitespaceOnlyIsSingleEmptyLine()
    {
        var result = TextWrapper.WrapOptimal("   ", 4);

        Assert.Equal([""], result.Lines);
        Assert.Equal(0UL, result.TotalCost);
    }

    [Fact]
    public void UnitBadnessIsMonotone()
    {
        const ushort width = 80;
        var previous = TextWrapper.KnuthPlassBadness(0, width, false);

        for (var slack = 1; slack <= width; slack++)
        {
            var current = TextWrapper.KnuthPlassBadness(slack, width, false);
            Assert.True(current >= previous, $"badness({slack})={current} < previous {previous}");
            previous = current;
        }
    }

    [Fact]
    public void UnitBadnessZeroSlackIsZero()
    {
        Assert.Equal(0UL, TextWrapper.KnuthPlassBadness(0, 80, false));
        Assert.Equal(0UL, TextWrapper.KnuthPlassBadness(0, 80, true));
    }

    [Fact]
    public void UnitBadnessOverflowIsInfinite()
    {
        Assert.Equal(TextWrapper.BadnessInfinity, TextWrapper.KnuthPlassBadness(-1, 80, false));
        Assert.Equal(TextWrapper.BadnessInfinity, TextWrapper.KnuthPlassBadness(-10, 80, false));
    }

    [Fact]
    public void UnitBadnessLastLineIsAlwaysZero()
    {
        Assert.Equal(0UL, TextWrapper.KnuthPlassBadness(0, 80, true));
        Assert.Equal(0UL, TextWrapper.KnuthPlassBadness(40, 80, true));
        Assert.Equal(0UL, TextWrapper.KnuthPlassBadness(79, 80, true));
    }

    [Fact]
    public void UnitBadnessHasCubicGrowth()
    {
        var b10 = TextWrapper.KnuthPlassBadness(10, 100, false);
        var b20 = TextWrapper.KnuthPlassBadness(20, 100, false);
        var b40 = TextWrapper.KnuthPlassBadness(40, 100, false);

        Assert.True(b20 >= b10 * 6);
        Assert.True(b40 >= b20 * 6);
    }

    [Fact]
    public void UnitForceBreakPenaltyIsApplied()
    {
        var result = TextWrapper.WrapOptimal("superlongwordthatcannotfit", 10);

        Assert.True(result.TotalCost >= TextWrapper.ForceBreakPenalty);
    }

    [Fact]
    public void OptimalSimpleWrapFitsRequestedWidth()
    {
        var result = TextWrapper.WrapOptimal("Hello world foo bar", 10);

        Assert.All(result.Lines, line => Assert.True(TerminalTextWidth.DisplayWidth(line) <= 10));
        Assert.True(result.Lines.Count >= 2);
    }

    [Fact]
    public void OptimalPerfectFitHasZeroCost()
    {
        var result = TextWrapper.WrapOptimal("aaaa bbbb", 9);

        Assert.Equal(["aaaa bbbb"], result.Lines);
        Assert.Equal(0UL, result.TotalCost);
    }

    [Fact]
    public void OptimalFindsBalancedBreaksInsteadOfGreedyBreaks()
    {
        var result = TextWrapper.WrapOptimal("aaa bb cc ddddd", 6);

        Assert.Equal(["aaa", "bb cc", "ddddd"], result.Lines);
        Assert.All(result.Lines, line => Assert.True(TerminalTextWidth.DisplayWidth(line) <= 6));
    }

    [Fact]
    public void OptimalEmptyTextIsOneZeroCostLine()
    {
        var result = TextWrapper.WrapOptimal("", 80);

        Assert.Equal([""], result.Lines);
        Assert.Equal(0UL, result.TotalCost);
        Assert.Equal([0UL], result.LineBadness);
    }

    [Fact]
    public void OptimalSingleWordIsOneZeroCostLine()
    {
        var result = TextWrapper.WrapOptimal("hello", 80);

        Assert.Equal(["hello"], result.Lines);
        Assert.Equal(0UL, result.TotalCost);
    }

    [Fact]
    public void OptimalMultiParagraphInputPreservesExplicitNewlines()
    {
        var lines = TextWrapper.WrapLine("hello world\nfoo bar baz", 10, TextWrapMode.Optimal);

        Assert.True(lines.Count >= 2);
        Assert.All(lines, line => Assert.True(TerminalTextWidth.DisplayWidth(line) <= 10));
        Assert.Equal(["hello", "world", "foo bar", "baz"], lines);
    }

    [Fact]
    public void OptimalTokenizerCapturesContentAndTrailingGlueWidths()
    {
        var words = TextWrapper.TokenizeOptimal("hello world foo");

        Assert.Equal(3, words.Count);
        Assert.Equal(("hello", 5, 1), (words[0].Content, words[0].ContentWidth, words[0].SpaceWidth));
        Assert.Equal(("world", 5, 1), (words[1].Content, words[1].ContentWidth, words[1].SpaceWidth));
        Assert.Equal(("foo", 3, 0), (words[2].Content, words[2].ContentWidth, words[2].SpaceWidth));
    }

    [Fact]
    public void OptimalDiagnosticsMatchLineCountAndLastLineBadness()
    {
        var result = TextWrapper.WrapOptimal("short text here for testing the dp", 15);

        Assert.Equal(result.Lines.Count, result.LineBadness.Count);
        Assert.Equal(0UL, result.LineBadness[^1]);
    }

    [Fact]
    public void OptimalIsDeterministic()
    {
        const string text = "The quick brown fox jumps over the lazy dog near a riverbank";

        var first = TextWrapper.WrapOptimal(text, 20);
        var second = TextWrapper.WrapOptimal(text, 20);

        Assert.Equal(first.Lines, second.Lines);
        Assert.Equal(first.TotalCost, second.TotalCost);
        Assert.Equal(first.LineBadness, second.LineBadness);
    }

    [Fact]
    public void OptimalKnownTwoLineCaseMatchesCostRange()
    {
        var fitting = TextWrapper.WrapOptimal("hello world", 11);
        Assert.Equal(["hello world"], fitting.Lines);
        Assert.Equal(0UL, fitting.TotalCost);

        var split = TextWrapper.WrapOptimal("hello world", 7);
        Assert.Equal(["hello", "world"], split.Lines);
        Assert.InRange(split.TotalCost, 1UL, 299UL);
    }

    [Fact]
    public void OptimalCostDoesNotExceedGreedyCost()
    {
        const ushort width = 10;
        var greedy = TextWrapper.WrapLine("the quick brown fox", width, TextWrapMode.Word);
        var optimal = TextWrapper.WrapOptimal("the quick brown fox", width);
        var greedyCost = 0UL;

        for (var index = 0; index < greedy.Count; index++)
        {
            var slack = width - TerminalTextWidth.DisplayWidth(greedy[index]);
            greedyCost += TextWrapper.KnuthPlassBadness(slack, width, index == greedy.Count - 1);
        }

        Assert.True(optimal.TotalCost <= greedyCost, $"optimal={optimal.TotalCost}, greedy={greedyCost}");
    }

    [Fact]
    public void OptimalLookaheadPreservesNormalText()
    {
        const string text = "a b c d e f g h i j k l m n o p q r s t u v w x y z";
        var result = TextWrapper.WrapOptimal(text, 10);

        Assert.All(result.Lines, line => Assert.True(TerminalTextWidth.DisplayWidth(line) <= 10));
        var joined = string.Join(' ', result.Lines);
        foreach (var letter in Enumerable.Range('a', 26).Select(value => (char)value))
        {
            Assert.Contains(letter, joined);
        }
    }

    [Fact]
    public void OptimalVeryNarrowWidthUsesOneWordPerLine()
    {
        Assert.Equal(["ab", "cd", "ef"], TextWrapper.WrapOptimal("ab cd ef", 2).Lines);
    }

    [Fact]
    public void OptimalWideWidthUsesSingleZeroCostLine()
    {
        var result = TextWrapper.WrapOptimal("hello world", 1000);

        Assert.Equal(["hello world"], result.Lines);
        Assert.Equal(0UL, result.TotalCost);
    }

    [Fact]
    public void OptimalLongWordRemainsIntactOnOverfullLine()
    {
        var result = TextWrapper.WrapLine("supercalifragilistic", 10, TextWrapMode.Optimal);

        Assert.Equal(["supercalifragilistic"], result);
    }

    [Fact]
    public void NonBreakingSpacesRemainInsideOptimalContentTokens()
    {
        var result = TextWrapper.WrapOptimal("alpha\u00A0beta gamma", 7);

        Assert.Equal("alpha\u00A0beta", result.Lines[0]);
    }

    [Fact]
    public void WordModeWrapsAtWordBoundaries()
    {
        Assert.Equal(["hello"], TextWrapper.WrapText("hello", 10, TextWrapMode.Word));
        Assert.Equal(["hello", "world"], TextWrapper.WrapText("hello world", 5, TextWrapMode.Word));
        Assert.Equal(
            ["hello world", "foo bar"],
            TextWrapper.WrapText("hello world foo bar", 11, TextWrapMode.Word));
    }

    [Fact]
    public void EveryModePreservesExplicitLineStructure()
    {
        Assert.Equal(["line1", "line2"], TextWrapper.WrapText("line1\nline2", 20, TextWrapMode.Word));
        Assert.Equal(["line1", "line2", ""], TextWrapper.WrapText("line1\r\nline2\r\n", 20, TextWrapMode.Word));
        Assert.Equal(["line1", ""], TextWrapper.WrapText("line1\n", 20, TextWrapMode.Word));
        Assert.Equal(["", ""], TextWrapper.WrapText("\n", 20, TextWrapMode.Word));
        Assert.Equal(["line1", ""], TextWrapper.WrapText("line1\n", 20, TextWrapMode.Char));
        Assert.Equal([""], TextWrapper.WrapText("", 10, TextWrapMode.Word));
    }

    [Fact]
    public void WordAndWordCharDifferOnlyForLongWordFallback()
    {
        const string word = "supercalifragilistic";
        Assert.Equal([word], TextWrapper.WrapText(word, 10, TextWrapMode.Word));
        var fallback = TextWrapper.WrapText(word, 10, TextWrapMode.WordChar);
        Assert.True(fallback.Count > 1);
        Assert.All(fallback, line => Assert.True(TextWrapper.DisplayWidth(line) <= 10));
        Assert.Equal(word, string.Concat(fallback));
    }

    [Fact]
    public void CharModePreservesRawSpaces()
    {
        Assert.Equal(["hello", " worl", "d"], TextWrapper.WrapText("hello world", 5, TextWrapMode.Char));
        Assert.Equal(
            TextWrapper.WrapText("hello world", 5, TextWrapMode.Char),
            TextWrapper.WrapText("hello world", 5, TextWrapMode.Character));
    }

    [Fact]
    public void PreserveIndentDoesNotCreatePhantomWhitespaceLine()
    {
        var trimmed = new WrapOptions(4)
            .WithMode(TextWrapMode.Word)
            .WithPreserveIndent(true);
        Assert.Equal(["aaaa", "bb"], TextWrapper.WrapWithOptions("aaaa   bb", trimmed));
        Assert.Equal(
            ["aaaa", "bb"],
            TextWrapper.WrapWithOptions("aaaa   bb", trimmed.WithTrimTrailing(false)));
    }

    [Theory]
    [InlineData("你好世界", 4, "你好|世界")]
    [InlineData("你好世", 5, "你好|世")]
    [InlineData("hi你好", 4, "hi你|好")]
    public void CharModeUsesCellWidthForCjk(string text, int width, string expected)
    {
        Assert.Equal(expected.Split('|'), TextWrapper.WrapText(text, width, TextWrapMode.Char));
    }

    [Fact]
    public void EmojiAndZwJSequencesRemainAtomic()
    {
        Assert.Equal(["😀😀", "😀"], TextWrapper.WrapText("😀😀😀", 4, TextWrapMode.Char));
        Assert.Equal(["👨‍👩‍👧"], TextWrapper.WrapText("👨‍👩‍👧", 2, TextWrapMode.Char));
        Assert.Equal(["a😀", "b"], TextWrapper.WrapText("a😀b", 3, TextWrapMode.Char));
    }

    [Theory]
    [InlineData("hello", 10, "...", "hello")]
    [InlineData("hello world", 8, "...", "hello...")]
    [InlineData("你好世界", 6, "...", "你...")]
    [InlineData("hello", 5, "...", "hello")]
    [InlineData("hello world", 5, "", "hello")]
    public void TruncateWithEllipsisMatchesUpstream(
        string text,
        int width,
        string ellipsis,
        string expected) =>
        Assert.Equal(expected, TextWrapper.TruncateWithEllipsis(text, width, ellipsis));

    [Theory]
    [InlineData("hello world", 5, "hello")]
    [InlineData("你好世界", 4, "你好")]
    [InlineData("你好", 3, "你")]
    [InlineData("", 10, "")]
    [InlineData("hello", 0, "")]
    public void TruncateToWidthMatchesUpstream(string text, int width, string expected) =>
        Assert.Equal(expected, TextWrapper.TruncateToWidth(text, width));

    [Fact]
    public void TruncationNeverSplitsCombiningSequence()
    {
        var result = TextWrapper.TruncateToWidth("e\u0301test", 2);
        Assert.Equal("e\u0301t", result);
    }

    [Theory]
    [InlineData("hello", 5)]
    [InlineData("你好", 4)]
    [InlineData("👩‍🔬", 2)]
    [InlineData("👨‍👩‍👧‍👦", 2)]
    [InlineData("👩‍🚀x", 3)]
    [InlineData("⏳", 2)]
    [InlineData("⌛", 2)]
    [InlineData("❤️", 1)]
    [InlineData("⌨️", 1)]
    [InlineData("⚠️", 1)]
    [InlineData("⌚", 2)]
    [InlineData("⭐", 2)]
    [InlineData("🇺🇸", 2)]
    [InlineData("🇺🇸🇯🇵", 4)]
    [InlineData("👍🏻", 2)]
    [InlineData("👩‍💻", 2)]
    [InlineData("A😀B", 4)]
    [InlineData("ok ✅", 5)]
    [InlineData("📁", 2)]
    [InlineData("🔗", 2)]
    [InlineData("⚡️", 2)]
    [InlineData("⚙️", 1)]
    [InlineData("🖼️", 1)]
    [InlineData("e\u0301\u0308", 1)]
    [InlineData("", 0)]
    public void DisplayWidthMatchesUpstreamCases(string text, int expected) =>
        Assert.Equal(expected, TextWrapper.DisplayWidth(text));

    [Fact]
    public void NeutralSymbolWidthsRemainConsistent()
    {
        var airplane = TextWrapper.DisplayWidth("✈");
        Assert.InRange(airplane, 1, 2);
        Assert.Equal(airplane, TextWrapper.DisplayWidth("⬆"));
    }

    [Fact]
    public void GraphemeWidthMatchesEmojiCases()
    {
        Assert.Equal(2, TextWrapper.GraphemeWidth("👩‍🔬"));
        Assert.Equal(2, TextWrapper.GraphemeWidth("🇺🇸"));
        Assert.Equal(2, TextWrapper.GraphemeWidth("👍🏽"));
    }

    [Fact]
    public void AsciiWidthAcceptsOnlyPrintableAscii()
    {
        Assert.Equal(5, TextWrapper.AsciiWidth("hello"));
        Assert.Equal(15, TextWrapper.AsciiWidth("hello world 123"));
        Assert.Equal(0, TextWrapper.AsciiWidth(""));
        Assert.Null(TextWrapper.AsciiWidth("你好"));
        Assert.Null(TextWrapper.AsciiWidth("héllo"));
        Assert.Null(TextWrapper.AsciiWidth("hello😀"));
        Assert.Null(TextWrapper.AsciiWidth("\t"));
        Assert.Null(TextWrapper.AsciiWidth("\n"));
        Assert.Null(TextWrapper.AsciiWidth("\r"));
        Assert.Null(TextWrapper.AsciiWidth("\0"));
        Assert.Null(TextWrapper.AsciiWidth("\x7f"));
    }

    [Fact]
    public void AsciiWidthAcceptsEntirePrintableRange()
    {
        var printable = new string(Enumerable.Range(0x20, 0x7f - 0x20).Select(value => (char)value).ToArray());
        Assert.Equal(printable.Length, TextWrapper.AsciiWidth(printable));
    }

    [Fact]
    public void WideAndAsciiPredicatesMatchContent()
    {
        Assert.True(TextWrapper.HasWideChars("hi你好"));
        Assert.True(TextWrapper.HasWideChars("hello😀"));
        Assert.False(TextWrapper.HasWideChars("hello"));
        Assert.True(TextWrapper.IsAsciiOnly("hello world 123"));
        Assert.False(TextWrapper.IsAsciiOnly("héllo"));
    }

    [Theory]
    [InlineData("hello", 5)]
    [InlineData("", 0)]
    [InlineData("e\u0301", 1)]
    [InlineData("e\u0301\u0308", 1)]
    [InlineData("你好", 2)]
    [InlineData("😀", 1)]
    [InlineData("👍🏻", 1)]
    [InlineData("👨‍👩‍👧", 1)]
    [InlineData("🇺🇸", 1)]
    public void GraphemeCountMatchesExtendedClusters(string text, int expected) =>
        Assert.Equal(expected, TextWrapper.GraphemeCount(text));

    [Fact]
    public void GraphemeEnumerationPreservesClusters()
    {
        Assert.Equal(["e\u0301", "b", "c"], TextWrapper.Graphemes("e\u0301bc"));
        Assert.Empty(TextWrapper.Graphemes(""));
        Assert.Equal(["你", "好"], TextWrapper.Graphemes("你好"));
    }

    [Theory]
    [InlineData("hello world", 5, "hello", 5)]
    [InlineData("你好世界", 3, "你", 2)]
    [InlineData("e\u0301bc", 2, "e\u0301b", 2)]
    [InlineData("hi", 10, "hi", 2)]
    [InlineData("", 10, "", 0)]
    [InlineData("hello", 0, "", 0)]
    [InlineData("a你好", 2, "a", 1)]
    public void TruncateWithInfoReturnsPrefixAndMeasuredWidth(
        string text,
        int width,
        string expectedText,
        int expectedWidth)
    {
        var result = TextWrapper.TruncateToWidthWithInfo(text, width);
        Assert.Equal(expectedText, result.Text);
        Assert.Equal(expectedWidth, result.Width);
    }

    [Fact]
    public void WordBoundaryAndSegmentHelpersPreserveWhitespaceRuns()
    {
        Assert.Contains(6, TextWrapper.WordBoundaries("hello world"));
        Assert.Contains(3, TextWrapper.WordBoundaries("a  b"));
        Assert.Empty(TextWrapper.WordBoundaries("helloworld"));
        Assert.NotEmpty(TextWrapper.WordBoundaries("   "));
        Assert.Contains("hello", TextWrapper.WordSegments("hello  world"));
        Assert.Contains("world", TextWrapper.WordSegments("hello  world"));
        Assert.Empty(TextWrapper.WordSegments(""));
        Assert.Equal(["hello"], TextWrapper.WordSegments("hello"));
    }

    [Fact]
    public void WrapOptionsBuilderAndDefaultMatchUpstream()
    {
        var options = new WrapOptions(40)
            .WithMode(TextWrapMode.Char)
            .WithPreserveIndent(true)
            .WithTrimTrailing(false);
        Assert.Equal(40, options.Width);
        Assert.Equal(TextWrapMode.Char, options.Mode);
        Assert.True(options.PreserveIndent);
        Assert.False(options.TrimTrailing);

        Assert.Equal(80, WrapOptions.Default.Width);
        Assert.Equal(TextWrapMode.Word, WrapOptions.Default.Mode);
        Assert.False(WrapOptions.Default.PreserveIndent);
        Assert.True(WrapOptions.Default.TrimTrailing);
    }

    [Fact]
    public void WrapOptionsControlTrailingWhitespaceAndIndentation()
    {
        var trimming = new WrapOptions(10).WithTrimTrailing(true);
        Assert.All(TextWrapper.WrapWithOptions("hello   world", trimming), line => Assert.False(line.EndsWith(' ')));

        var preserve = new WrapOptions(7).WithMode(TextWrapMode.Word).WithPreserveIndent(true);
        Assert.Equal(["word12", "  abcde"], TextWrapper.WrapWithOptions("word12  abcde", preserve));
        Assert.Equal(
            ["word12", "abcde"],
            TextWrapper.WrapWithOptions("word12  abcde", preserve.WithPreserveIndent(false)));
    }

    [Fact]
    public void ZeroWidthSuppressesOnlyWidthBasedWrapping()
    {
        Assert.Equal(["hello"], TextWrapper.WrapText("hello", 0, TextWrapMode.Word));
        Assert.Equal(["a", "b"], TextWrapper.WrapText("a\nb", 0, TextWrapMode.Word));
    }

    [Fact]
    public void NoneAndOptimalIgnoreInapplicableWidthOptions()
    {
        var none = new WrapOptions(2).WithMode(TextWrapMode.None).WithTrimTrailing(false);
        Assert.Equal(["abc   "], TextWrapper.WrapWithOptions("abc   ", none));

        var optimal = new WrapOptions(4)
            .WithMode(TextWrapMode.Optimal)
            .WithPreserveIndent(true)
            .WithTrimTrailing(false);
        Assert.Equal(["foo"], TextWrapper.WrapWithOptions("   foo   ", optimal));
    }

    [Theory]
    [InlineData(-0.8, FitnessClass.Tight)]
    [InlineData(-0.5, FitnessClass.Normal)]
    [InlineData(0.0, FitnessClass.Normal)]
    [InlineData(0.49, FitnessClass.Normal)]
    [InlineData(0.5, FitnessClass.Loose)]
    [InlineData(0.99, FitnessClass.Loose)]
    [InlineData(1.0, FitnessClass.VeryLoose)]
    [InlineData(2.0, FitnessClass.VeryLoose)]
    public void FitnessClassFromRatio(double ratio, FitnessClass expected) =>
        Assert.Equal(expected, FitnessClassExtensions.FromRatio(ratio));

    [Fact]
    public void FitnessCompatibilityUsesAdjacentClasses()
    {
        Assert.False(FitnessClass.Tight.IsIncompatibleWith(FitnessClass.Tight));
        Assert.False(FitnessClass.Tight.IsIncompatibleWith(FitnessClass.Normal));
        Assert.True(FitnessClass.Tight.IsIncompatibleWith(FitnessClass.Loose));
        Assert.True(FitnessClass.Tight.IsIncompatibleWith(FitnessClass.VeryLoose));
        Assert.False(FitnessClass.Normal.IsIncompatibleWith(FitnessClass.Loose));
        Assert.True(FitnessClass.Normal.IsIncompatibleWith(FitnessClass.VeryLoose));
    }

    [Fact]
    public void ParagraphObjectiveDefaultsAndTerminalPresetMatchUpstream()
    {
        var objective = ParagraphObjective.Default;
        Assert.Equal(10UL, objective.LinePenalty);
        Assert.Equal(100UL, objective.FitnessDemerit);
        Assert.Equal(100UL, objective.DoubleHyphenDemerit);
        Assert.Equal(10_000UL, objective.BadnessScale);

        var terminal = ParagraphObjective.Terminal;
        Assert.Equal(20UL, terminal.LinePenalty);
        Assert.Equal(0.0, terminal.MinAdjustmentRatio);
        Assert.True(terminal.MaxAdjustmentRatio > 2.0);
        Assert.Equal(ParagraphObjective.Default, ParagraphObjective.Typographic);
    }

    [Fact]
    public void ParagraphBadnessHonorsAdjustmentBounds()
    {
        var objective = ParagraphObjective.Default;
        Assert.Equal(0UL, objective.Badness(0, 80));
        Assert.InRange(objective.Badness(10, 80)!.Value, 1UL, 99UL);
        Assert.Null(objective.Badness(240, 80));
        Assert.NotNull(objective.Badness(-40, 80));
        Assert.Null(objective.Badness(-100, 80));
        Assert.Null(ParagraphObjective.Terminal.Badness(-1, 80));
        Assert.Equal(0UL, objective.Badness(0, 0));
        Assert.Null(objective.Badness(5, 0));
    }

    [Fact]
    public void ParagraphDemeritsApplyBreakPenaltyKinds()
    {
        var objective = ParagraphObjective.Default;
        var badness = objective.Badness(10, 80)!.Value;
        var expectedSpace = (objective.LinePenalty + badness) * (objective.LinePenalty + badness);
        var space = objective.Demerits(10, 80, BreakPenalty.Space);
        Assert.Equal(expectedSpace, space);
        Assert.True(objective.Demerits(10, 80, BreakPenalty.Hyphen) > space);
        Assert.Equal(objective.LinePenalty * objective.LinePenalty, objective.Demerits(0, 80, BreakPenalty.Forced));
        Assert.Null(objective.Demerits(300, 80, BreakPenalty.Space));
        Assert.True(BreakPenalty.Hyphen.Flagged);
        Assert.Equal(5_000, BreakPenalty.Emergency.Value);
    }

    [Fact]
    public void ParagraphAdjacencyDemeritsComposeIndependently()
    {
        var objective = ParagraphObjective.Default;
        Assert.Equal(
            objective.FitnessDemerit,
            objective.AdjacencyDemerits(FitnessClass.Tight, FitnessClass.Loose, false, false));
        Assert.Equal(
            0UL,
            objective.AdjacencyDemerits(FitnessClass.Normal, FitnessClass.Loose, false, false));
        Assert.Equal(
            objective.DoubleHyphenDemerit,
            objective.AdjacencyDemerits(FitnessClass.Normal, FitnessClass.Normal, true, true));
        Assert.Equal(
            objective.FitnessDemerit + objective.DoubleHyphenDemerit,
            objective.AdjacencyDemerits(FitnessClass.Tight, FitnessClass.VeryLoose, true, true));
    }

    [Fact]
    public void ParagraphWidowOrphanAndRatioRulesMatchBoundaries()
    {
        var objective = ParagraphObjective.Default;
        Assert.Equal(objective.WidowDemerit, objective.WidowDemerits(5));
        Assert.Equal(objective.WidowDemerit, objective.WidowDemerits(14));
        Assert.Equal(0UL, objective.WidowDemerits(15));
        Assert.Equal(objective.OrphanDemerit, objective.OrphanDemerits(19));
        Assert.Equal(0UL, objective.OrphanDemerits(20));
        Assert.Equal(0.125, objective.AdjustmentRatio(10, 80), 10);
        Assert.Equal(0.0, objective.AdjustmentRatio(5, 0));
    }

    [Fact]
    public void ManagedNegativeWidthsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WrapOptions(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WrapOptions(1) { Width = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => TextWrapper.WrapText("text", -1, TextWrapMode.Word));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextWrapper.WrapOptimal("text", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextWrapper.TruncateToWidth("text", -1));
    }

    [Fact]
    public void OptimalSnapshotMatrixIsDeterministicAndContentPreserving()
    {
        string[] paragraphs =
        [
            "The quick brown fox jumps over the lazy dog near a riverbank while the sun sets behind the mountains in the distance",
            "To be or not to be that is the question whether tis nobler in the mind to suffer the slings and arrows of outrageous fortune",
            "aaa bb cc ddddd ee fff gg hhhh ii jjj kk llll mm nnn oo pppp qq rrr ss tttt",
        ];
        int[] widths = [20, 40, 60, 80];

        foreach (var paragraph in paragraphs)
        {
            foreach (var width in widths)
            {
                var first = TextWrapper.WrapOptimal(paragraph, width);
                var second = TextWrapper.WrapOptimal(paragraph, width);
                Assert.Equal(Fnv1aLines(first.Lines), Fnv1aLines(second.Lines));
                Assert.All(first.Lines, line =>
                {
                    Assert.NotEmpty(line);
                    Assert.True(TextWrapper.DisplayWidth(line) <= width);
                });
                Assert.Equal(
                    paragraph.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries),
                    first.Lines.SelectMany(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
                Assert.Equal(0UL, first.LineBadness[^1]);
            }
        }
    }

    [Fact]
    public void OptimalLargeParagraphMeetsUpstreamFunctionalBudget()
    {
        string[] words =
        ["the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "and", "then", "runs", "back", "to", "its", "den", "in"];
        var paragraph = string.Join(' ', Enumerable.Range(0, 1_000).Select(index => words[index % words.Length]));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (var iteration = 0; iteration < 20; iteration++)
        {
            Assert.NotEmpty(TextWrapper.WrapOptimal(paragraph, 80).Lines);
        }

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Elapsed: {stopwatch.Elapsed}");
    }

    [Fact]
    public void GeneratedCharWordCharAndTruncationPropertiesHold()
    {
        var random = new Random(0x15CC6543);
        for (var sample = 0; sample < 250; sample++)
        {
            var width = random.Next(5, 30);
            var length = random.Next(1, 101);
            var source = new string(Enumerable.Range(0, length)
                .Select(_ => random.Next(0, 7) == 0 ? ' ' : (char)('a' + random.Next(26)))
                .ToArray());

            var characterLines = TextWrapper.WrapText(source, width, TextWrapMode.Char);
            Assert.All(characterLines, line => Assert.True(TextWrapper.DisplayWidth(line) <= width));
            Assert.Equal(
                source.Replace(" ", string.Empty, StringComparison.Ordinal),
                string.Concat(characterLines).Replace(" ", string.Empty, StringComparison.Ordinal));

            var wordCharLines = TextWrapper.WrapText(source, width, TextWrapMode.WordChar);
            Assert.All(wordCharLines, line => Assert.True(TextWrapper.DisplayWidth(line) <= width));

            var alphanumeric = source.Replace(" ", "0", StringComparison.Ordinal);
            var ellipsized = TextWrapper.TruncateWithEllipsis(alphanumeric, width, "...");
            Assert.True(TextWrapper.DisplayWidth(ellipsized) <= width);
            var truncated = TextWrapper.TruncateToWidth(alphanumeric, width);
            Assert.True(TextWrapper.DisplayWidth(truncated) <= width);
        }
    }

    [Fact]
    public void GeneratedOptimalPropertiesHoldAgainstGreedy()
    {
        var random = new Random(0x4B500501);
        for (var sample = 0; sample < 200; sample++)
        {
            var width = random.Next(8, 40);
            var wordCount = random.Next(3, 21);
            var words = Enumerable.Range(0, wordCount)
                .Select(_ => new string(Enumerable.Range(0, random.Next(1, 7))
                    .Select(__ => (char)('a' + random.Next(26)))
                    .ToArray()))
                .ToArray();
            var text = string.Join(' ', words);
            var greedy = TextWrapper.WrapText(text, width, TextWrapMode.Word);
            var optimal = TextWrapper.WrapOptimal(text, width);

            var greedyCost = 0UL;
            for (var index = 0; index < greedy.Count; index++)
            {
                var lineWidth = TextWrapper.DisplayWidth(greedy[index]);
                var badness = lineWidth > width
                    ? TextWrapper.ForceBreakPenalty
                    : TextWrapper.KnuthPlassBadness(width - lineWidth, width, index == greedy.Count - 1);
                greedyCost = ulong.MaxValue - greedyCost < badness
                    ? ulong.MaxValue
                    : greedyCost + badness;
            }

            Assert.True(optimal.TotalCost <= greedyCost);
            Assert.All(optimal.Lines, line => Assert.True(TextWrapper.DisplayWidth(line) <= width));
            Assert.Equal(words, optimal.Lines.SelectMany(line =>
                line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
        }
    }

    [Fact]
    public void OptimalBenchmarkScenariosRemainDeterministic()
    {
        string[] words =
        ["the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "and", "then", "runs", "back", "to", "its", "den", "in", "forest", "while", "birds", "sing", "above", "trees", "near"];
        (int Count, int Width)[] scenarios = [(50, 40), (50, 80), (200, 40), (200, 80), (500, 40), (500, 80)];
        foreach (var scenario in scenarios)
        {
            var paragraph = string.Join(' ', Enumerable.Range(0, scenario.Count).Select(index => words[index % words.Length]));
            var expected = Fnv1aLines(TextWrapper.WrapOptimal(paragraph, scenario.Width).Lines);
            for (var iteration = 0; iteration < 5; iteration++)
            {
                Assert.Equal(expected, Fnv1aLines(TextWrapper.WrapOptimal(paragraph, scenario.Width).Lines));
            }
        }
    }

    private static ulong Fnv1aLines(IReadOnlyList<string> lines)
    {
        const ulong offset = 0xcbf29ce484222325;
        const ulong prime = 0x100000001b3;
        var hash = offset;
        for (var index = 0; index < lines.Count; index++)
        {
            // The Rust oracle hashes the line index as an explicit little-endian u32.
            for (var byteIndex = 0; byteIndex < sizeof(uint); byteIndex++)
            {
                var value = (byte)((uint)index >> (byteIndex * 8));
                hash ^= value;
                hash = unchecked(hash * prime);
            }

            foreach (var value in System.Text.Encoding.UTF8.GetBytes(lines[index]))
            {
                hash ^= value;
                hash = unchecked(hash * prime);
            }
        }

        return hash;
    }
}
