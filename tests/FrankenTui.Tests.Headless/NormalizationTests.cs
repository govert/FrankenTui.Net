// SPDX-License-Identifier: Apache-2.0
// Source-equivalent tests for .external/frankentui/crates/ftui-text/src/normalization.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Text;
using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class NormalizationTests
{
    [Fact]
    public void NfcComposesCombiningCharacters()
    {
        Assert.Equal("é", TextNormalizer.Normalize("e\u0301", NormalizationForm.FormC));
    }

    [Fact]
    public void NfcPreservesAlreadyComposed()
    {
        Assert.Equal("é", TextNormalizer.Normalize("é", NormalizationForm.FormC));
    }

    [Fact]
    public void NfcHandlesMultipleCombiningMarks()
    {
        var result = TextNormalizer.Normalize("a\u0303\u0301", NormalizationForm.FormC);
        Assert.NotEmpty(result);
        Assert.True(TextNormalizer.IsNormalized(result, NormalizationForm.FormC));
    }

    [Fact]
    public void NfdDecomposesPrecomposedCharacter()
    {
        Assert.Equal("e\u0301", TextNormalizer.Normalize("é", NormalizationForm.FormD));
    }

    [Fact]
    public void NfdPreservesAscii()
    {
        Assert.Equal("hello world", TextNormalizer.Normalize("hello world", NormalizationForm.FormD));
    }

    [Fact]
    public void NfkcNormalizesCompatibilityLigature()
    {
        Assert.Equal("fi", TextNormalizer.Normalize("ﬁ", NormalizationForm.FormKC));
    }

    [Fact]
    public void NfkcNormalizesFullwidthCharacter()
    {
        Assert.Equal("A", TextNormalizer.Normalize("Ａ", NormalizationForm.FormKC));
    }

    [Fact]
    public void NfkcNormalizesSuperscript()
    {
        Assert.Equal("2", TextNormalizer.Normalize("²", NormalizationForm.FormKC));
    }

    [Fact]
    public void NfkdDecomposesCompatibilityLigature()
    {
        Assert.Equal("fi", TextNormalizer.Normalize("ﬁ", NormalizationForm.FormKD));
    }

    [Fact]
    public void NfkdDecomposesWithoutRecomposition()
    {
        Assert.Equal("e\u0301", TextNormalizer.Normalize("é", NormalizationForm.FormKD));
    }

    [Fact]
    public void IsNfcOnComposedCharacter()
    {
        Assert.True(TextNormalizer.IsNormalized("é", NormalizationForm.FormC));
    }

    [Fact]
    public void IsNfcOnDecomposedCharacter()
    {
        Assert.False(TextNormalizer.IsNormalized("e\u0301", NormalizationForm.FormC));
    }

    [Fact]
    public void IsNfdOnDecomposedCharacter()
    {
        Assert.True(TextNormalizer.IsNormalized("e\u0301", NormalizationForm.FormD));
    }

    [Fact]
    public void IsNfdOnComposedCharacter()
    {
        Assert.False(TextNormalizer.IsNormalized("é", NormalizationForm.FormD));
    }

    [Fact]
    public void AsciiIsNormalizedInAllForms()
    {
        const string text = "hello world 123";
        Assert.True(TextNormalizer.IsNormalized(text, NormalizationForm.FormC));
        Assert.True(TextNormalizer.IsNormalized(text, NormalizationForm.FormD));
        Assert.True(TextNormalizer.IsNormalized(text, NormalizationForm.FormKC));
        Assert.True(TextNormalizer.IsNormalized(text, NormalizationForm.FormKD));
    }

    [Fact]
    public void EqualsNormalizedComposedVersusDecomposed()
    {
        Assert.True(TextNormalizer.EqualsNormalized("é", "e\u0301", NormalizationForm.FormC));
        Assert.True(TextNormalizer.EqualsNormalized("é", "e\u0301", NormalizationForm.FormD));
    }

    [Fact]
    public void EqualsNormalizedDifferentStrings()
    {
        Assert.False(TextNormalizer.EqualsNormalized("a", "b", NormalizationForm.FormC));
    }

    [Fact]
    public void SearchNormalizationCaseFolds()
    {
        Assert.Equal(
            TextNormalizer.NormalizeForSearch("Hello"),
            TextNormalizer.NormalizeForSearch("hello"));
    }

    [Fact]
    public void SearchNormalizationHandlesCanonicalAccents()
    {
        Assert.Equal(
            TextNormalizer.NormalizeForSearch("é"),
            TextNormalizer.NormalizeForSearch("e\u0301"));
    }

    [Fact]
    public void SearchNormalizationHandlesCompatibilityCharacters()
    {
        Assert.Equal(
            TextNormalizer.NormalizeForSearch("Ａ"),
            TextNormalizer.NormalizeForSearch("a"));
    }

    [Fact]
    public void NfcIteratorMatchesNormalize()
    {
        const string input = "e\u0301 cafe\u0301";
        Assert.Equal(
            TextNormalizer.Normalize(input, NormalizationForm.FormC),
            Collect(TextNormalizer.NfcIter(input)));
    }

    [Fact]
    public void NfdIteratorMatchesNormalize()
    {
        const string input = "é café";
        Assert.Equal(
            TextNormalizer.Normalize(input, NormalizationForm.FormD),
            Collect(TextNormalizer.NfdIter(input)));
    }

    [Fact]
    public void EmptyStringAllForms()
    {
        Assert.Equal("", TextNormalizer.Normalize("", NormalizationForm.FormC));
        Assert.Equal("", TextNormalizer.Normalize("", NormalizationForm.FormD));
        Assert.Equal("", TextNormalizer.Normalize("", NormalizationForm.FormKC));
        Assert.Equal("", TextNormalizer.Normalize("", NormalizationForm.FormKD));
        Assert.True(TextNormalizer.IsNormalized("", NormalizationForm.FormC));
        Assert.True(TextNormalizer.IsNormalized("", NormalizationForm.FormD));
    }

    [Fact]
    public void HangulComposition()
    {
        Assert.Equal("한", TextNormalizer.Normalize("한", NormalizationForm.FormC));
    }

    [Fact]
    public void HangulDecomposition()
    {
        Assert.Equal("한", TextNormalizer.Normalize("한", NormalizationForm.FormD));
    }

    [Fact]
    public void MixedScriptNormalization()
    {
        const string input = "Hello 世界 🌍";
        Assert.Equal(input, TextNormalizer.Normalize(input, NormalizationForm.FormC));
    }

    [Fact]
    public void LongCombiningSequence()
    {
        var input = "a" + string.Concat(Enumerable.Repeat("\u0300", 20));
        var result = TextNormalizer.Normalize(input, NormalizationForm.FormC);
        Assert.NotEmpty(result);
        Assert.True(TextNormalizer.IsNormalized(result, NormalizationForm.FormC));
    }

    [Fact]
    public void CanonicalOrdering()
    {
        Assert.Equal(
            TextNormalizer.Normalize("A\u0327\u0301", NormalizationForm.FormC),
            TextNormalizer.Normalize("A\u0301\u0327", NormalizationForm.FormC));
    }

    [Fact]
    public void IsNfkcOnAscii()
    {
        Assert.True(TextNormalizer.IsNormalized("hello", NormalizationForm.FormKC));
    }

    [Fact]
    public void IsNfkcFalseForCompatibilityCharacter()
    {
        Assert.False(TextNormalizer.IsNormalized("Ａ", NormalizationForm.FormKC));
    }

    [Fact]
    public void IsNfkdFalseForComposedCharacter()
    {
        Assert.False(TextNormalizer.IsNormalized("é", NormalizationForm.FormKD));
    }

    [Fact]
    public void IsNfkdTrueForDecomposedAscii()
    {
        Assert.True(TextNormalizer.IsNormalized("abc", NormalizationForm.FormKD));
    }

    [Fact]
    public void EqualsNormalizedCompatibilityLigature()
    {
        Assert.True(TextNormalizer.EqualsNormalized("ﬁ", "fi", NormalizationForm.FormKC));
        Assert.True(TextNormalizer.EqualsNormalized("ﬁ", "fi", NormalizationForm.FormKD));
    }

    [Fact]
    public void EqualsNormalizedFullwidthVersusAscii()
    {
        Assert.True(TextNormalizer.EqualsNormalized("Ａ", "A", NormalizationForm.FormKC));
    }

    [Fact]
    public void EqualsNormalizedFalseForDifferentBaseCharacter()
    {
        Assert.False(TextNormalizer.EqualsNormalized("a\u0301", "o\u0301", NormalizationForm.FormC));
    }

    [Fact]
    public void NfkcIteratorMatchesNormalize()
    {
        const string input = "ﬁＡ²";
        Assert.Equal(
            TextNormalizer.Normalize(input, NormalizationForm.FormKC),
            Collect(TextNormalizer.NfkcIter(input)));
    }

    [Fact]
    public void NfkdIteratorMatchesNormalize()
    {
        const string input = "éﬁ";
        Assert.Equal(
            TextNormalizer.Normalize(input, NormalizationForm.FormKD),
            Collect(TextNormalizer.NfkdIter(input)));
    }

    [Fact]
    public void NormalizeIsIdempotentNfc()
    {
        const string input = "e\u0301 café";
        var once = TextNormalizer.Normalize(input, NormalizationForm.FormC);
        Assert.Equal(once, TextNormalizer.Normalize(once, NormalizationForm.FormC));
    }

    [Fact]
    public void NormalizeIsIdempotentNfkd()
    {
        const string input = "ﬁé";
        var once = TextNormalizer.Normalize(input, NormalizationForm.FormKD);
        Assert.Equal(once, TextNormalizer.Normalize(once, NormalizationForm.FormKD));
    }

    [Fact]
    public void SupplementaryPlaneEmojiRoundTrip()
    {
        const string input = "🦀🎉🌍";
        Assert.Equal(input, TextNormalizer.Normalize(input, NormalizationForm.FormC));
        Assert.Equal(input, TextNormalizer.Normalize(input, NormalizationForm.FormD));
        Assert.Equal(input, TextNormalizer.Normalize(input, NormalizationForm.FormKC));
        Assert.Equal(input, TextNormalizer.Normalize(input, NormalizationForm.FormKD));
    }

    [Fact]
    public void MathematicalBoldCapitalANfkc()
    {
        Assert.Equal("A", TextNormalizer.Normalize("𝐀", NormalizationForm.FormKC));
    }

    [Fact]
    public void ZeroWidthJoinerIsPreserved()
    {
        Assert.Contains('\u200D', TextNormalizer.Normalize("a\u200Db", NormalizationForm.FormC));
    }

    [Fact]
    public void NormalizeForSearchLigatureAndCase()
    {
        Assert.Equal("file", TextNormalizer.NormalizeForSearch("ﬁLE"));
    }

    [Fact]
    public void NormalizeForSearchEmpty()
    {
        Assert.Equal("", TextNormalizer.NormalizeForSearch(""));
    }

    [Fact]
    public void NormalizationFormMappingSupportsValueSemantics()
    {
        var form = NormalizationForm.FormC;
        var copy = form;
        Assert.Equal(form, copy);
        Assert.Equal("FormC", form.ToString());
    }

    [Fact]
    public void NormalizationFormMappingsAreDistinct()
    {
        NormalizationForm[] forms =
            [NormalizationForm.FormC, NormalizationForm.FormD, NormalizationForm.FormKC, NormalizationForm.FormKD];
        Assert.Equal(4, forms.Distinct().Count());
    }

    [Fact]
    public void ExistingManagedNormalizeDefaultRemainsNfc()
    {
        Assert.Equal("é", TextNormalizer.Normalize("e\u0301"));
    }

    [Fact]
    public void MalformedUtf16IsRejectedByManagedNormalization()
    {
        const string unpairedHighSurrogate = "\uD800";
        Assert.Throws<ArgumentException>(() => TextNormalizer.Normalize(unpairedHighSurrogate));
        Assert.Throws<ArgumentException>(() => TextNormalizer.IsNormalized(unpairedHighSurrogate));
        Assert.Throws<ArgumentException>(() => TextNormalizer.NfcIter(unpairedHighSurrogate));
    }

    [Fact]
    public void IteratorsYieldUnicodeScalarsRatherThanUtf16CodeUnits()
    {
        var rune = Assert.Single(TextNormalizer.NfcIter("🦀"));
        Assert.Equal(0x1F980, rune.Value);
    }

    private static string Collect(IEnumerable<Rune> runes) =>
        string.Concat(runes.Select(rune => rune.ToString()));
}
