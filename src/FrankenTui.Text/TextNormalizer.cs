// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-text/src/normalization.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: Rust NormForm maps directly to the pre-existing managed
// NormalizationForm API. Results follow the active .NET globalization
// implementation and Unicode database rather than unicode-normalization's.

using System.Text;

namespace FrankenTui.Text;

public static class TextNormalizer
{
    /// <summary>Normalize text to NFC by default, or to the selected Unicode form.</summary>
    public static string Normalize(string text, NormalizationForm normalization = NormalizationForm.FormC)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Normalize(normalization);
    }

    /// <summary>Whether text is already normalized to the selected form.</summary>
    public static bool IsNormalized(
        string text,
        NormalizationForm normalization = NormalizationForm.FormC)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.IsNormalized(normalization);
    }

    /// <summary>Apply NFKC followed by invariant lower-casing for search comparison.</summary>
    public static string NormalizeForSearch(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
    }

    /// <summary>Compare two strings after normalization using ordinal equality.</summary>
    public static bool EqualsNormalized(
        string first,
        string second,
        NormalizationForm normalization = NormalizationForm.FormC)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return string.Equals(
            first.Normalize(normalization),
            second.Normalize(normalization),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Enumerate NFC Unicode scalars. Unlike the Rust streaming normalizer, the
    /// BCL projection first materializes the normalized string.
    /// </summary>
    public static IEnumerable<Rune> NfcIter(string text) =>
        NormalizeRunes(text, NormalizationForm.FormC);

    /// <summary>Enumerate NFD Unicode scalars after materializing normalized text.</summary>
    public static IEnumerable<Rune> NfdIter(string text) =>
        NormalizeRunes(text, NormalizationForm.FormD);

    /// <summary>Enumerate NFKC Unicode scalars after materializing normalized text.</summary>
    public static IEnumerable<Rune> NfkcIter(string text) =>
        NormalizeRunes(text, NormalizationForm.FormKC);

    /// <summary>Enumerate NFKD Unicode scalars after materializing normalized text.</summary>
    public static IEnumerable<Rune> NfkdIter(string text) =>
        NormalizeRunes(text, NormalizationForm.FormKD);

    private static IEnumerable<Rune> NormalizeRunes(string text, NormalizationForm normalization)
    {
        ArgumentNullException.ThrowIfNull(text);
        return EnumerateRunes(text.Normalize(normalization));
    }

    private static IEnumerable<Rune> EnumerateRunes(string normalized)
    {
        for (var index = 0; index < normalized.Length;)
        {
            var first = normalized[index];
            if (char.IsHighSurrogate(first)
                && index + 1 < normalized.Length
                && char.IsLowSurrogate(normalized[index + 1]))
            {
                yield return new Rune(first, normalized[index + 1]);
                index += 2;
            }
            else
            {
                // Normalize rejects malformed UTF-16, so a non-pair here is a scalar BMP value.
                yield return new Rune(first);
                index++;
            }
        }
    }
}
