// SPDX-License-Identifier: Apache-2.0
// Contract tests ported from .external/frankentui/crates/ftui-text/src/search.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Text;
using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class SearchTests
{
    [Fact]
    public void ExactBasic()
    {
        var results = TextSearch.SearchExact("hello world hello", "hello");
        Assert.Equal(2, results.Count);
        AssertRange(results[0], 0, 5);
        AssertRange(results[1], 12, 17);
    }

    [Fact]
    public void ExactNoMatch() =>
        Assert.Empty(TextSearch.SearchExact("hello world", "xyz"));

    [Fact]
    public void ExactEmptyNeedle() =>
        Assert.Empty(TextSearch.SearchExact("hello", string.Empty));

    [Fact]
    public void ExactEmptyHaystack() =>
        Assert.Empty(TextSearch.SearchExact(string.Empty, "hello"));

    [Fact]
    public void ExactNeedleEqualsHaystack()
    {
        var result = Assert.Single(TextSearch.SearchExact("hello", "hello"));
        AssertRange(result, 0, 5);
    }

    [Fact]
    public void ExactNeedleLonger() =>
        Assert.Empty(TextSearch.SearchExact("hi", "hello"));

    [Fact]
    public void ExactAdjacentMatches() =>
        Assert.Equal(3, TextSearch.SearchExact("aaa", "a").Count);

    [Fact]
    public void ExactTextExtraction()
    {
        const string haystack = "foo bar baz";
        var result = Assert.Single(TextSearch.SearchExact(haystack, "bar"));
        Assert.Equal("bar", result.Text(haystack));
    }

    [Fact]
    public void ExactUnicode() =>
        Assert.Equal(2, TextSearch.SearchExact("café résumé café", "café").Count);

    [Fact]
    public void ExactCjk() =>
        Assert.Equal(2, TextSearch.SearchExact("你好世界你好", "你好").Count);

    [Fact]
    public void OverlappingBasic()
    {
        var results = TextSearch.SearchExactOverlapping("aaa", "aa");
        Assert.Equal(2, results.Count);
        AssertRange(results[0], 0, 2);
        AssertRange(results[1], 1, 3);
    }

    [Fact]
    public void OverlappingNoOverlap() =>
        Assert.Equal(2, TextSearch.SearchExactOverlapping("abcabc", "abc").Count);

    [Fact]
    public void OverlappingEmptyNeedle() =>
        Assert.Empty(TextSearch.SearchExactOverlapping("abc", string.Empty));

    [Fact]
    public void AsciiCaseInsensitiveBasic() =>
        Assert.Equal(2, TextSearch.SearchAsciiCaseInsensitive("Hello World HELLO", "hello").Count);

    [Fact]
    public void AsciiCaseInsensitiveMixedCase() =>
        Assert.Equal(2, TextSearch.SearchAsciiCaseInsensitive("FoO BaR fOo", "foo").Count);

    [Fact]
    public void AsciiCaseInsensitiveNoMatch() =>
        Assert.Empty(TextSearch.SearchAsciiCaseInsensitive("hello", "xyz"));

    [Fact]
    public void ResultsHaveValidRanges()
    {
        var cases = new (string Haystack, string Needle)[]
        {
            ("hello world", "o"),
            ("aaaa", "aa"),
            (string.Empty, "x"),
            ("x", string.Empty),
            ("café", "é"),
            ("🌍 world 🌍", "🌍"),
        };

        foreach (var (haystack, needle) in cases)
        {
            foreach (var result in TextSearch.SearchExact(haystack, needle))
            {
                AssertValidRange(haystack, result);
            }
        }
    }

    [Fact]
    public void EmojiSearch()
    {
        const string haystack = "hello 🌍 world 🌍 end";
        var results = TextSearch.SearchExact(haystack, "🌍");
        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.Equal("🌍", result.Text(haystack)));
    }

    [Fact]
    public void CaseInsensitiveUnicode()
    {
        var results = TextSearch.SearchCaseInsensitive("Straße Strasse", "strasse");
        Assert.NotEmpty(results);
    }

    [Fact]
    public void CaseInsensitiveExpansionRangeMapsToGrapheme()
    {
        const string haystack = "STRAßE";
        var result = Assert.Single(TextSearch.SearchCaseInsensitive(haystack, "straße"));
        Assert.Equal(haystack, result.Text(haystack));
        AssertValidRange(haystack, result);
    }

    [Fact]
    public void CaseInsensitiveAccented() =>
        Assert.Equal(3, TextSearch.SearchCaseInsensitive("CAFÉ café Café", "café").Count);

    [Fact]
    public void CaseInsensitiveEmpty() =>
        Assert.Empty(TextSearch.SearchCaseInsensitive("hello", string.Empty));

    [Fact]
    public void CaseInsensitiveFullwidth()
    {
        var results = TextSearch.SearchCaseInsensitive("ＨＥＬＬＯ", "hello");
        Assert.NotEmpty(results);
    }

    [Fact]
    public void NormalizedComposedVsDecomposed()
    {
        var results = TextSearch.SearchNormalized("cafe\u0301", "café", NormalizationForm.FormC);
        Assert.Single(results);
    }

    [Fact]
    public void NormalizedNoFalsePositive() =>
        Assert.Empty(TextSearch.SearchNormalized("hello", "world", NormalizationForm.FormC));

    [Fact]
    public void NormalizedResultRangesValid()
    {
        const string haystack = "café résumé café";
        foreach (var result in TextSearch.SearchNormalized(haystack, "café", NormalizationForm.FormC))
        {
            AssertValidRange(haystack, result);
        }
    }

    [Fact]
    public void CaseInsensitiveResultRangesValid()
    {
        const string haystack = "Hello WORLD hello";
        foreach (var result in TextSearch.SearchCaseInsensitive(haystack, "hello"))
        {
            AssertValidRange(haystack, result);
        }
    }

    [Fact]
    public void WidthModeAsciiIsOne()
    {
        foreach (var value in new[] { 'a', 'Z', '0', ' ', '~' })
        {
            Assert.Equal(1, WidthMode.Standard.CharWidth(value));
            Assert.Equal(1, WidthMode.CjkAmbiguousWide.CharWidth(value));
        }
    }

    [Fact]
    public void WidthModeCjkIdeographIsTwo()
    {
        foreach (var value in new[] { '中', '国', '字' })
        {
            Assert.Equal(2, WidthMode.Standard.CharWidth(value));
            Assert.Equal(2, WidthMode.CjkAmbiguousWide.CharWidth(value));
        }

        Assert.Equal(2, WidthMode.Standard.CharWidth(new Rune(0x17A4)));
        Assert.Equal(2, WidthMode.Standard.CharWidth(new Rune(0x17D8)));
        Assert.Equal(1, WidthMode.Standard.CharWidth(new Rune(0x2D7F)));
        Assert.Equal(1, WidthMode.Standard.CharWidth(new Rune(0x1F1E6)));
    }

    [Fact]
    public void WidthModeEastAsianAmbiguousDiffers()
    {
        foreach (var value in new[] { '─', '│', '┌', '→', '←', '↑', '↓', '°', '×', '®' })
        {
            Assert.Equal(1, WidthMode.Standard.CharWidth(value));
            Assert.Equal(2, WidthMode.CjkAmbiguousWide.CharWidth(value));
        }
    }

    [Fact]
    public void WidthModeCombiningMarksZero()
    {
        foreach (var value in new[] { '\u0300', '\u0301', '\u0302' })
        {
            Assert.Equal(0, WidthMode.Standard.CharWidth(value));
            Assert.Equal(0, WidthMode.CjkAmbiguousWide.CharWidth(value));
        }
    }

    [Fact]
    public void WidthModeStringWidth()
    {
        Assert.Equal(5, WidthMode.Standard.StrWidth("hello"));
        Assert.Equal(4, WidthMode.Standard.StrWidth("中国"));
        Assert.Equal(5, WidthMode.CjkAmbiguousWide.StrWidth("hello"));
        Assert.Equal(5, WidthMode.CjkAmbiguousWide.StrWidth("→ 中"));
        Assert.Equal(4, WidthMode.Standard.StrWidth("→ 中"));
    }

    [Fact]
    public void WidthModeDefaultIsStandard() =>
        Assert.Equal(WidthMode.Standard, default);

    [Fact]
    public void DisplayColumnAtAscii()
    {
        const string text = "hello world";
        Assert.Equal(0, TextSearch.DisplayColumnAt(text, 0, WidthMode.Standard));
        Assert.Equal(5, TextSearch.DisplayColumnAt(text, 5, WidthMode.Standard));
        Assert.Equal(11, TextSearch.DisplayColumnAt(text, 11, WidthMode.Standard));
    }

    [Fact]
    public void DisplayColumnAtCjk()
    {
        const string text = "你好world";
        Assert.Equal(0, TextSearch.DisplayColumnAt(text, 0, WidthMode.Standard));
        Assert.Equal(2, TextSearch.DisplayColumnAt(text, 1, WidthMode.Standard));
        Assert.Equal(4, TextSearch.DisplayColumnAt(text, 2, WidthMode.Standard));
        Assert.Equal(9, TextSearch.DisplayColumnAt(text, 7, WidthMode.Standard));
    }

    [Fact]
    public void DisplayColumnAtEastAsianAmbiguousDiffers()
    {
        const string text = "─→text";
        Assert.Equal(1, TextSearch.DisplayColumnAt(text, 1, WidthMode.Standard));
        Assert.Equal(2, TextSearch.DisplayColumnAt(text, 1, WidthMode.CjkAmbiguousWide));
        Assert.Equal(2, TextSearch.DisplayColumnAt(text, 2, WidthMode.Standard));
        Assert.Equal(4, TextSearch.DisplayColumnAt(text, 2, WidthMode.CjkAmbiguousWide));
    }

    [Fact]
    public void DisplayColumnAtCombiningMarks()
    {
        const string text = "e\u0301x";
        Assert.Equal(0, TextSearch.DisplayColumnAt(text, 0, WidthMode.Standard));
        Assert.Equal(1, TextSearch.DisplayColumnAt(text, 1, WidthMode.Standard));
        Assert.Equal(1, TextSearch.DisplayColumnAt(text, 2, WidthMode.Standard));
        Assert.Equal(2, TextSearch.DisplayColumnAt(text, 3, WidthMode.Standard));
    }

    [Fact]
    public void PolicyStandardPreset()
    {
        var policy = SearchPolicy.Standard;
        Assert.Equal(NormalizationForm.FormKC, policy.NormForm);
        Assert.True(policy.CaseInsensitive);
        Assert.Equal(WidthMode.Standard, policy.WidthMode);
    }

    [Fact]
    public void PolicyCjkPreset()
    {
        var policy = SearchPolicy.Cjk;
        Assert.Equal(NormalizationForm.FormKC, policy.NormForm);
        Assert.True(policy.CaseInsensitive);
        Assert.Equal(WidthMode.CjkAmbiguousWide, policy.WidthMode);
    }

    [Fact]
    public void PolicyExactNfcPreset()
    {
        var policy = SearchPolicy.ExactNfc;
        Assert.Equal(NormalizationForm.FormC, policy.NormForm);
        Assert.False(policy.CaseInsensitive);
        Assert.Equal(WidthMode.Standard, policy.WidthMode);
    }

    [Fact]
    public void PolicySearchBasicAscii()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("hello world", "hello", SearchPolicy.Standard));
        AssertRange(result, 0, 5);
        Assert.Equal(0, result.ColumnStart);
        Assert.Equal(5, result.ColumnEnd);
    }

    [Fact]
    public void PolicySearchCaseInsensitive()
    {
        const string haystack = "Hello WORLD hello";
        var results = TextSearch.SearchWithPolicy(haystack, "hello", SearchPolicy.Standard);
        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Text(haystack));
        Assert.Equal("hello", results[1].Text(haystack));
    }

    [Fact]
    public void PolicySearchCaseSensitive()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("Hello hello", "hello", SearchPolicy.ExactNfc));
        Assert.Equal(6, result.Start);
    }

    [Fact]
    public void PolicySearchEmptyNeedle() =>
        Assert.Empty(TextSearch.SearchWithPolicy("hello", string.Empty, SearchPolicy.Standard));

    [Fact]
    public void PolicySearchEmptyHaystack() =>
        Assert.Empty(TextSearch.SearchWithPolicy(string.Empty, "hello", SearchPolicy.Standard));

    [Fact]
    public void PolicySearchNoMatch() =>
        Assert.Empty(TextSearch.SearchWithPolicy("hello", "world", SearchPolicy.Standard));

    [Fact]
    public void PolicySearchComposedVsDecomposed()
    {
        var results = TextSearch.SearchWithPolicy("café", "cafe\u0301", SearchPolicy.ExactNfc);
        Assert.Single(results);
    }

    [Fact]
    public void PolicySearchFullwidthNfkc() =>
        Assert.NotEmpty(TextSearch.SearchWithPolicy("ＨＥＬＬＯ", "hello", SearchPolicy.Standard));

    [Fact]
    public void PolicySearchNfcDoesNotMatchCompatibility() =>
        Assert.Empty(TextSearch.SearchWithPolicy("ﬁle", "file", SearchPolicy.ExactNfc));

    [Fact]
    public void PolicySearchNfkcMatchesCompatibility() =>
        Assert.NotEmpty(TextSearch.SearchWithPolicy("ﬁle", "file", SearchPolicy.Standard));

    [Fact]
    public void PolicySearchCjkColumnOffsets()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("你好world你好", "world", SearchPolicy.Standard));
        Assert.Equal(4, result.ColumnStart);
        Assert.Equal(9, result.ColumnEnd);
    }

    [Fact]
    public void PolicySearchCjkInCjk()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("你好世界你好", "世界", SearchPolicy.Standard));
        Assert.Equal(4, result.ColumnStart);
        Assert.Equal(8, result.ColumnEnd);
    }

    [Fact]
    public void PolicySearchEastAsianAmbiguousColumnDivergence()
    {
        const string haystack = "→hello";
        var standard = Assert.Single(TextSearch.SearchWithPolicy(haystack, "hello", SearchPolicy.Standard));
        var cjk = Assert.Single(TextSearch.SearchWithPolicy(haystack, "hello", SearchPolicy.Cjk));
        Assert.Equal((standard.Start, standard.End), (cjk.Start, cjk.End));
        Assert.Equal(1, standard.ColumnStart);
        Assert.Equal(2, cjk.ColumnStart);
        Assert.Equal(6, standard.ColumnEnd);
        Assert.Equal(7, cjk.ColumnEnd);
    }

    [Fact]
    public void PolicySearchBoxDrawingColumnDivergence()
    {
        const string haystack = "──text";
        var standard = Assert.Single(TextSearch.SearchWithPolicy(haystack, "text", SearchPolicy.Standard));
        var cjk = Assert.Single(TextSearch.SearchWithPolicy(haystack, "text", SearchPolicy.Cjk));
        Assert.Equal(2, standard.ColumnStart);
        Assert.Equal(4, cjk.ColumnStart);
    }

    [Fact]
    public void PolicySearchCombiningMarkOffsets()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("café", "fé", SearchPolicy.Standard));
        Assert.Equal(2, result.ColumnStart);
        Assert.Equal(4, result.ColumnEnd);
    }

    [Fact]
    public void PolicySearchDecomposedCombiningOffsets()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("cafe\u0301", "fé", SearchPolicy.ExactNfc));
        Assert.Equal(2, result.ColumnStart);
        Assert.Equal(4, result.ColumnEnd);
    }

    [Fact]
    public void PolicyResultDisplayWidth()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("你好hello", "hello", SearchPolicy.Standard));
        Assert.Equal(5, result.DisplayWidth());
    }

    [Fact]
    public void PolicyResultDisplayWidthCjkMatch()
    {
        var result = Assert.Single(TextSearch.SearchWithPolicy("abc你好def", "你好", SearchPolicy.Standard));
        Assert.Equal(4, result.DisplayWidth());
    }

    [Fact]
    public void PolicyResultTextExtraction()
    {
        const string haystack = "Hello World";
        var result = Assert.Single(TextSearch.SearchWithPolicy(haystack, "world", SearchPolicy.Standard));
        Assert.Equal("World", result.Text(haystack));
    }

    [Fact]
    public void PolicySearchMultipleMatches()
    {
        var results = TextSearch.SearchWithPolicy("foo bar foo baz foo", "foo", SearchPolicy.Standard);
        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].ColumnStart);
        Assert.Equal(8, results[1].ColumnStart);
        Assert.Equal(16, results[2].ColumnStart);
    }

    [Fact]
    public void PolicySearchRangesAlwaysValid()
    {
        var cases = new (string Haystack, string Needle, SearchPolicy Policy)[]
        {
            ("hello world", "o", SearchPolicy.Standard),
            ("CAFÉ café", "café", SearchPolicy.Standard),
            ("你好世界", "世", SearchPolicy.Cjk),
            ("─→text", "text", SearchPolicy.Cjk),
            ("ﬁle", "file", SearchPolicy.Standard),
            ("e\u0301", "é", SearchPolicy.ExactNfc),
        };

        foreach (var (haystack, needle, policy) in cases)
        {
            foreach (var result in TextSearch.SearchWithPolicy(haystack, needle, policy))
            {
                AssertValidRange(haystack, result);
                Assert.True(result.ColumnStart <= result.ColumnEnd);
            }
        }
    }

    [Fact]
    public void PolicySearchColumnsMonotonicallyIncreasing()
    {
        var results = TextSearch.SearchWithPolicy("aa bb aa cc aa", "aa", SearchPolicy.Standard);
        Assert.Equal(3, results.Count);
        for (var index = 1; index < results.Count; index++)
        {
            Assert.True(results[index - 1].ColumnEnd <= results[index].ColumnStart);
        }
    }

    [Fact]
    public void PolicyCustomNfdCaseSensitive()
    {
        var policy = new SearchPolicy(NormalizationForm.FormD, false, WidthMode.Standard);
        Assert.Single(TextSearch.SearchWithPolicy("é", "e\u0301", policy));
    }

    [Fact]
    public void PolicyCustomNfkdCaseInsensitive()
    {
        var policy = new SearchPolicy(NormalizationForm.FormKD, true, WidthMode.CjkAmbiguousWide);
        Assert.NotEmpty(TextSearch.SearchWithPolicy("ﬁ", "FI", policy));
    }

    [Fact]
    public void PolicySearchAgreesWithSearchCaseInsensitive()
    {
        var cases = new (string Haystack, string Needle)[]
        {
            ("Hello World HELLO", "hello"),
            ("CAFÉ café Café", "café"),
            ("ＨＥＬＬＯ", "hello"),
        };

        foreach (var (haystack, needle) in cases)
        {
            var oldResults = TextSearch.SearchCaseInsensitive(haystack, needle);
            var policyResults = TextSearch.SearchWithPolicy(haystack, needle, SearchPolicy.Standard);
            Assert.Equal(oldResults.Count, policyResults.Count);
            for (var index = 0; index < oldResults.Count; index++)
            {
                Assert.Equal(
                    (oldResults[index].Start, oldResults[index].End),
                    (policyResults[index].Start, policyResults[index].End));
            }
        }
    }

    [Fact]
    public void PolicySearchAgreesWithSearchNormalized()
    {
        const string haystack = "cafe\u0301 résumé";
        var oldResults = TextSearch.SearchNormalized(haystack, "café", NormalizationForm.FormC);
        var policyResults = TextSearch.SearchWithPolicy(haystack, "café", SearchPolicy.ExactNfc);
        Assert.Equal(oldResults.Count, policyResults.Count);
        for (var index = 0; index < oldResults.Count; index++)
        {
            Assert.Equal(
                (oldResults[index].Start, oldResults[index].End),
                (policyResults[index].Start, policyResults[index].End));
        }
    }

    [Fact]
    public void ManagedUtf16CoordinatesRepresentSameEmojiScalars()
    {
        const string haystack = "a🌍b";
        var result = Assert.Single(TextSearch.SearchExact(haystack, "🌍"));
        AssertRange(result, 1, 3);
        Assert.Equal("🌍", result.Text(haystack));
    }

    [Fact]
    public void MalformedUtf16AndSplitScalarRangesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => TextSearch.SearchExact("\uD800", "x"));
        Assert.Throws<ArgumentException>(() => WidthMode.Standard.StrWidth("\uDC00"));
        Assert.Throws<ArgumentException>(() => SearchResult.New(1, 2).Text("🌍"));
    }

    [Fact]
    public void NormalizedCompatibilityProjectionMapsOriginalDocumentColumn()
    {
        var document = TextDocument.FromString("Cafe\u0301 x");
        var result = Assert.Single(TextSearch.FindAllNormalized(
            document,
            "x",
            NormalizationForm.FormC,
            StringComparison.Ordinal));
        Assert.Equal(new TextCursor(0, 6), result);
    }

    [Fact]
    public void NormalizationExpansionMapsWholeOriginalGrapheme()
    {
        const string haystack = "ﬁle";
        var result = Assert.Single(TextSearch.SearchNormalized(haystack, "file", NormalizationForm.FormKC));
        AssertRange(result, 0, 3);
        Assert.Equal(haystack, result.Text(haystack));
    }

    private static void AssertRange(SearchResult result, int start, int end) =>
        Assert.Equal((start, end), (result.Start, result.End));

    private static void AssertRange(PolicySearchResult result, int start, int end) =>
        Assert.Equal((start, end), (result.Start, result.End));

    private static void AssertValidRange(string source, SearchResult result)
    {
        Assert.True(result.Start <= result.End);
        Assert.InRange(result.End, 0, source.Length);
        Assert.True(IsScalarBoundary(source, result.Start));
        Assert.True(IsScalarBoundary(source, result.End));
    }

    private static void AssertValidRange(string source, PolicySearchResult result)
    {
        Assert.True(result.Start <= result.End);
        Assert.InRange(result.End, 0, source.Length);
        Assert.True(IsScalarBoundary(source, result.Start));
        Assert.True(IsScalarBoundary(source, result.End));
    }

    private static bool IsScalarBoundary(string source, int offset) =>
        offset == 0
        || offset == source.Length
        || !(char.IsHighSurrogate(source[offset - 1]) && char.IsLowSurrogate(source[offset]));
}
