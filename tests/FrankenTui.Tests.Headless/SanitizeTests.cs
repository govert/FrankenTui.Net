// SPDX-License-Identifier: Apache-2.0
// Behavioral projection of crates/ftui-render/src/sanitize.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Reflection;
using System.Text;
using FrankenTui.Render;
using RenderText = FrankenTui.Render.Text;

namespace FrankenTui.Tests.Headless;

public sealed class SanitizeTests
{
    public static TheoryData<string, string> CsiCases => new()
    {
        { "Hello \u001b[31mred\u001b[0m text", "Hello red text" },
        { "Before\u001b[2JAfter", "BeforeAfter" },
        { "Before\u001b[10;20HAfter", "BeforeAfter" },
        { "Before\u001b[?1049hAfter\u001b[?1049lDone", "BeforeAfterDone" },
        { "Before\u001b[?1000hAfter", "BeforeAfter" },
        { "Before\u001b[?2004hAfter", "BeforeAfter" },
        { "Before\u001b[?1004hAfter", "BeforeAfter" },
        { "Before\u001b[?25lHidden\u001b[?25hAfter", "BeforeHiddenAfter" },
        { "Before\u001b[cAfter\u001b[>cDone\u001b[6nEnd", "BeforeAfterDoneEnd" },
        { "Before\u001b[1;2;3;4;5mAfter", "BeforeAfter" },
        { "Before\u001b7Middle\u001b8After", "BeforeMiddleAfter" },
        { "A\u001b[31\U0001f600B", "A\U0001f600B" },
        { "A\u001b[31\nB", "A\nB" },
    };

    public static TheoryData<string, string> ControlStringCases => new()
    {
        { "Before\u001b]0;Evil title\aAfter", "BeforeAfter" },
        { "Before\u001b]2;Evil title\u001b\\After", "BeforeAfter" },
        { "Before\u001b]8;;https://evil.test\aLink\u001b]8;;\aAfter", "BeforeLinkAfter" },
        { "Before\u001b]52;c;SGVsbG8=\aAfter", "BeforeAfter" },
        { "Before\u001b]52;c;SGVsbG8=\u001b\\After", "BeforeAfter" },
        { "Before\u001bP1;2;3+qpayload\u001b\\After", "BeforeAfter" },
        { "Before\u001b_application command\u001b\\After", "BeforeAfter" },
        { "Before\u001b^private message\u001b\\After", "BeforeAfter" },
        { "Before\u001b]0;title\u009chello world", "Beforehello world" },
        { "Before\u001bPpayload\u009cAfter", "BeforeAfter" },
        { "Before\u001b^payload\u009cAfter", "BeforeAfter" },
        { "Before\u001b_payload\u009cAfter", "BeforeAfter" },
        { "A\u001b]title\nB", "A\nB" },
        { "A\u001b]old\u001b]new\aB", "AB" },
        { "A\u001bPdata\u001b\\B", "AB" },
    };

    public static TheoryData<string, string> TruncatedCases => new()
    {
        { "Hello\u001b[", "Hello" },
        { "Hello\u001bP1;2;3", "Hello" },
        { "Hello\u001b_test", "Hello" },
        { "Hello\u001b^secret", "Hello" },
        { "Hello\u001b]0;Title", "Hello" },
        { "Hello\u001b", "Hello" },
        { "\u001b", string.Empty },
        { "Before\u001b!After", "BeforeAfter" },
    };

    [Fact]
    public void FastPathReturnsOriginalReferenceForCleanInput()
    {
        var cases = new[]
        {
            string.Empty,
            "Normal log message",
            "Tab\tline\nreturn\r",
            "Hello, 世界!",
            "Emoji: \U0001f600",
            "Combining: e\u0301",
            new string('A', 4096),
        };

        foreach (var input in cases)
        {
            Assert.Same(input, Sanitize.SanitizeString(input));
        }
    }

    [Theory]
    [MemberData(nameof(CsiCases))]
    public void CsiAndSingleCharacterEscapesAreFullyStripped(string input, string expected) =>
        Assert.Equal(expected, Sanitize.SanitizeString(input));

    [Theory]
    [MemberData(nameof(ControlStringCases))]
    public void OscDcsPmAndApcAreStrippedWithoutSwallowingFollowingText(string input, string expected) =>
        Assert.Equal(expected, Sanitize.SanitizeString(input));

    [Theory]
    [MemberData(nameof(TruncatedCases))]
    public void TruncatedAndUnknownEscapesAreHandledSafely(string input, string expected) =>
        Assert.Equal(expected, Sanitize.SanitizeString(input));

    [Fact]
    public void ForbiddenC0DelAndEveryC1ControlAreStripped()
    {
        var forbiddenC0 = Enumerable.Range(0, 0x20)
            .Where(value => value is not (0x09 or 0x0a or 0x0d))
            .Select(value => (char)value)
            .ToArray();
        Assert.Equal("AB", Sanitize.SanitizeString($"A{new string(forbiddenC0)}\u007fB"));

        for (var value = 0x80; value <= 0x9f; value++)
        {
            Assert.Equal("AB", Sanitize.SanitizeString($"A{(char)value}B"));
        }
    }

    [Fact]
    public void TabLfAndCrAreTheOnlyPreservedControlCharacters()
    {
        Assert.Equal("A\tB\nC\rD", Sanitize.SanitizeString("A\tB\nC\rD"));
    }

    [Fact]
    public void UnicodeEmojiAndCombiningSequencesSurviveMixedEscapes()
    {
        const string input = "日本語 \U0001f600 e\u0301 \u001b[31mкрасный\u001b[0m fin";
        Assert.Equal("日本語 \U0001f600 e\u0301 красный fin", Sanitize.SanitizeString(input));
    }

    [Fact]
    public void C1IntroducersAreRemovedAsControlsWithoutParsingFollowingText()
    {
        Assert.Equal("text31mmalicious", Sanitize.SanitizeString("text\u009b31mmalicious"));
        Assert.Equal("text0;titlemalicious", Sanitize.SanitizeString("text\u009d0;title\amalicious"));
        Assert.Equal("Adevice controlB", Sanitize.SanitizeString("A\u0090device control\u001b\\B"));
        Assert.Equal("Aapp commandB", Sanitize.SanitizeString("A\u009fapp command\u001b\\B"));
        Assert.Equal("Aprivate msgB", Sanitize.SanitizeString("A\u009eprivate msg\u001b\\B"));
    }

    [Fact]
    public void MalformedUtf16IsTargetOnlyInvalidInputAndIsStripped()
    {
        const string malformed = "A\ud800B\udc00C\ud800\U0001f600\udc00D";
        var result = Sanitize.SanitizeString(malformed);

        Assert.Equal("ABC\U0001f600D", result);
        Assert.False(HasUnpairedSurrogate(result));
        Assert.True(result.Length <= malformed.Length, "managed code-unit count must not grow");
    }

    [Fact]
    public void ValidInputRespectsBothManagedCodeUnitAndRustUtf8ByteLengthBounds()
    {
        const string input = "α\U0001f600\u001b[31mβ\u001b[0m終";
        var output = Sanitize.SanitizeString(input);

        Assert.Equal("α\U0001f600β終", output);
        Assert.True(output.Length <= input.Length, "UTF-16 code-unit length grew");
        Assert.True(
            Encoding.UTF8.GetByteCount(output) <= Encoding.UTF8.GetByteCount(input),
            "UTF-8 byte length grew relative to Rust str testimony");
    }

    [Fact]
    public void VeryLongAndRepeatedSequencesRemainLinearAndFullyStripped()
    {
        var csi = $"A\u001b[{new string('1', 100_000)}mB";
        var osc = $"A\u001b]52;c;{new string('x', 100_000)}\u001b\\B";
        var dcs = $"A\u001bP{new string('x', 100_000)}\u001b\\B";
        var repeated = string.Concat(Enumerable.Repeat("A\u001b[31mB\u001b[0m", 1_000));

        Assert.Equal("AB", Sanitize.SanitizeString(csi));
        Assert.Equal("AB", Sanitize.SanitizeString(osc));
        Assert.Equal("AB", Sanitize.SanitizeString(dcs));
        Assert.Equal(string.Concat(Enumerable.Repeat("AB", 1_000)), Sanitize.SanitizeString(repeated));
    }

    [Fact]
    public void AdversarialProtocolCorpusNeverLeaksForbiddenControls()
    {
        string[] attacks =
        [
            "\u001b[2Jfake shell$ ",
            "\u001b[Hpassword: ",
            "\u001b[3Aoverwrite",
            "\u001b[2Khidden",
            "\u001b[L\u001b[Mlines",
            "\u001b]0;evil\a",
            "\u001b]52;c;SGVsbG8=\a",
            "\u001b[?1049h\u001b[?1000h\u001b[?2004h",
            "\u001b[?25lhidden\u001b[?25h",
            "\u001b_Gf=100,t=d;AAAA\u001b\\kitty",
            "\u001bPq~sixel\u001b\\",
            "\u001b[31;42;1;4mcolor soup\u001b[0m",
            "\u001b]8;;https://evil.test\aClick\u001b]8;;\a",
            "prefix\0\a\b\v\f\x7fsuffix",
        ];

        foreach (var input in attacks)
        {
            AssertSafe(Sanitize.SanitizeString(input));
        }
    }

    [Fact]
    public void SeededPropertyCorpusIsSafeIdempotentAndNonExpanding()
    {
        string[] fragments =
        [
            "plain", " ", "\t", "\n", "\r", "世界", "\U0001f600", "e\u0301",
            "\u001b[31m", "\u001b[0m", "\u001b[2J", "\u001b]0;title\a",
            "\u001bPpayload\u001b\\", "\u001b_payload\u001b\\",
            "\0", "\a", "\x7f", "\u0085", "\u009b", "\u009d",
        ];
        var random = new Random(0x15cc6543);

        for (var sample = 0; sample < 2_048; sample++)
        {
            var builder = new StringBuilder();
            var fragmentCount = random.Next(0, 32);
            for (var index = 0; index < fragmentCount; index++)
            {
                builder.Append(fragments[random.Next(fragments.Length)]);
            }

            var input = builder.ToString();
            var output = Sanitize.SanitizeString(input);
            AssertSafe(output);
            Assert.Equal(output, Sanitize.SanitizeString(output));
            Assert.True(output.Length <= input.Length);
            Assert.True(Encoding.UTF8.GetByteCount(output) <= Encoding.UTF8.GetByteCount(input));
        }
    }

    [Fact]
    public void TextFactoriesPreserveTrustAndApplySanitizationExactlyOnce()
    {
        var sanitized = RenderText.Sanitized("safe\u001b[31m red\u001b[0m");
        var sanitizedOwned = RenderText.SanitizedOwned("safe\u001b[31m red\u001b[0m");
        var trusted = RenderText.Trusted("safe\u001b[31m red\u001b[0m");
        var trustedOwned = RenderText.TrustedOwned("trusted");

        Assert.Equal("safe red", sanitized.AsString());
        Assert.Equal("safe red", sanitized.AsRef());
        Assert.Equal(sanitized, sanitizedOwned);
        Assert.True(sanitized.IsSanitized());
        Assert.False(sanitized.IsTrusted());
        Assert.Equal(TextTrust.Sanitized, sanitized.Trust);

        Assert.Equal("safe\u001b[31m red\u001b[0m", trusted.Value);
        Assert.True(trusted.IsTrusted());
        Assert.False(trusted.IsSanitized());
        Assert.Equal(TextTrust.Trusted, trusted.Trust);
        Assert.Equal("trusted", trustedOwned.ToString());
    }

    [Fact]
    public void TextCloneAndIntoOwnedPreserveValueEqualityButNotWrapperIdentity()
    {
        var text = RenderText.Sanitized("value");
        var clone = text.Clone();
        var owned = text.IntoOwned();

        Assert.Equal(text, clone);
        Assert.Equal(text, owned);
        Assert.NotSame(text, clone);
        Assert.NotSame(text, owned);
        Assert.True(text == clone);
        Assert.False(text != owned);
        Assert.NotEqual(text, RenderText.Trusted("value"));
        Assert.Equal(text.GetHashCode(), clone.GetHashCode());
    }

    [Fact]
    public void ExistingOutputSanitizerRouteRemainsCompatible()
    {
        const string input = "safe\u001b]52;c;SGVsbG8=\u001b\\tail\u009d\n";
        Assert.Equal(Sanitize.SanitizeString(input), OutputSanitizer.Sanitize(input));
    }

    [Fact]
    public void PublicDenominatorContainsFunctionTextStatesAndOperations()
    {
        Assert.NotNull(typeof(Sanitize).GetMethod(nameof(Sanitize.SanitizeString), BindingFlags.Public | BindingFlags.Static));
        Assert.Equal([TextTrust.Sanitized, TextTrust.Trusted], Enum.GetValues<TextTrust>());

        var methods = typeof(RenderText).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        string[] required =
        [
            nameof(RenderText.Sanitized), nameof(RenderText.Trusted), nameof(RenderText.SanitizedOwned),
            nameof(RenderText.TrustedOwned), nameof(RenderText.AsString), nameof(RenderText.AsRef),
            nameof(RenderText.IsSanitized), nameof(RenderText.IsTrusted), nameof(RenderText.IntoOwned),
            nameof(RenderText.Clone), nameof(RenderText.ToString),
        ];
        Assert.All(required, method => Assert.Contains(method, methods));
    }

    private static void AssertSafe(string output)
    {
        Assert.DoesNotContain('\u001b', output);
        Assert.DoesNotContain('\x7f', output);
        Assert.DoesNotContain(output, character =>
            character is >= '\u0080' and <= '\u009f' ||
            character is < ' ' and not ('\t' or '\n' or '\r'));
        Assert.False(HasUnpairedSurrogate(output));
    }

    private static bool HasUnpairedSurrogate(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return true;
                }

                index++;
            }
            else if (char.IsLowSurrogate(value[index]))
            {
                return true;
            }
        }

        return false;
    }
}
