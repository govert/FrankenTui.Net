// Upstream source: crates/ftui-render/src/sanitize.rs
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Direct 1-1 port of the terminal-output sanitizer. Strips ANSI/CSI escape
// sequences (and forbidden C0/C1 controls, DEL) from untrusted text so that
// payloads printed through Live/Console cannot inject terminal control codes.
//
// The previous OutputSanitizer.Sanitize only dropped individual forbidden
// bytes, leaving the CSI parameter/final bytes (e.g. "[2J") behind as printable
// ASCII. The upstream sanitize strips the *entire* escape sequence. This port
// reproduces that behavior faithfully.

using System.Text;

namespace FrankenTui.Render;

/// <summary>
/// Terminal-output sanitizer. Port of <c>pub fn sanitize(input: &amp;str) -&gt; Cow&lt;'_, str&gt;</c>
/// in sanitize.rs. Strips escape sequences and forbidden control characters.
/// </summary>
public static class Sanitize
{
    /// <summary>
    /// Sanitize untrusted text by stripping escape sequences and forbidden
    /// controls. Returns the sanitized string. Port of <c>sanitize</c>.
    /// </summary>
    public static string SanitizeString(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Fast path: if there is no ESC byte, no DEL, no forbidden C0, and no
        // C1 controls, the input is already clean. This mirrors the upstream
        // memchr-based fast path that returns Cow::Borrowed(input).
        if (!NeedsSanitizing(input))
        {
            return input;
        }

        return SanitizeSlow(input);
    }

    /// <summary>
    /// Fast-path predicate mirroring the upstream fast-path checks: presence of
    /// ESC (0x1B), DEL (0x7F), forbidden C0 controls, or C1 controls
    /// (UTF-8 \xC2\x80..\xC2\x9F).
    /// </summary>
    private static bool NeedsSanitizing(string input)
    {
        for (var index = 0; index < input.Length; index++)
        {
            var c = input[index];
            if (c == '\x1B' || c == '\x7F' || IsForbiddenC0(c))
            {
                return true;
            }
            // C1 controls U+0080..U+009F.
            if (c >= '\u0080' && c <= '\u009F')
            {
                return true;
            }

            // Rust `str` cannot contain malformed Unicode. Managed strings can,
            // so isolated UTF-16 surrogates are a target-only invalid-input class
            // and deliberately take the slow path where they are stripped.
            if (char.IsHighSurrogate(c))
            {
                if (index + 1 >= input.Length || !char.IsLowSurrogate(input[index + 1]))
                {
                    return true;
                }

                index++;
            }
            else if (char.IsLowSurrogate(c))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Forbidden C0 controls: 0x00-0x08, 0x0B-0x0C, 0x0E-0x1A, 0x1C-0x1F.
    /// Allowed: TAB (0x09), LF (0x0A), CR (0x0D). Port of <c>is_forbidden_c0</c>.</summary>
    private static bool IsForbiddenC0(char c)
    {
        var b = (int)c;
        return (b >= 0x00 && b <= 0x08) || (b >= 0x0B && b <= 0x0C) ||
               (b >= 0x0E && b <= 0x1A) || (b >= 0x1C && b <= 0x1F);
    }

    /// <summary>Slow path: strip escape sequences and forbidden controls. Port of <c>sanitize_slow</c>.</summary>
    private static string SanitizeSlow(string input)
    {
        var output = new StringBuilder(input.Length);
        var i = 0;
        while (i < input.Length)
        {
            var c = input[i];
            if (c == '\x1B')
            {
                i = SkipEscapeSequence(input, i);
                continue;
            }
            if (c == '\t' || c == '\n' || c == '\r')
            {
                output.Append(c);
                i++;
                continue;
            }
            if (IsForbiddenC0(c))
            {
                i++;
                continue;
            }
            if (c == '\x7F')
            {
                i++;
                continue;
            }
            if (c >= 0x20 && c <= 0x7E)
            {
                output.Append(c);
                i++;
                continue;
            }
            // High-bit set: UTF-8 decoded char. Skip C1 controls (U+0080..U+009F),
            // otherwise preserve. .NET strings are UTF-16, so valid surrogate
            // pairs are copied together and isolated surrogates are stripped.
            if (c >= '\u0080' && c <= '\u009F')
            {
                i++;
                continue;
            }
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                {
                    output.Append(c).Append(input[i + 1]);
                    i += 2;
                }
                else
                {
                    i++;
                }

                continue;
            }
            if (char.IsLowSurrogate(c))
            {
                i++;
                continue;
            }
            output.Append(c);
            i++;
        }
        return output.ToString();
    }

    /// <summary>Skip over an escape sequence, returning the index after it. Port of <c>skip_escape_sequence</c>.</summary>
    private static int SkipEscapeSequence(string s, int start)
    {
        var i = start + 1; // Skip ESC
        if (i >= s.Length)
        {
            return i;
        }

        var c = s[i];
        switch (c)
        {
            // CSI sequence: ESC [ params... final_byte (0x40-0x7E)
            case '[':
            {
                i++;
                while (i < s.Length)
                {
                    var b = s[i];
                    if (b >= 0x40 && b <= 0x7E)
                    {
                        return i + 1;
                    }
                    // Valid parameter/intermediate bytes are 0x20-0x3F.
                    if (!(b >= 0x20 && b <= 0x3F))
                    {
                        // Invalid char in CSI: abort to avoid eating valid text.
                        return i;
                    }
                    i++;
                }
                break;
            }
            // OSC sequence: ESC ] ... (BEL or ST)
            case ']':
            {
                i++;
                while (i < s.Length)
                {
                    var b = s[i];
                    if (b == 0x07)
                    {
                        return i + 1;
                    }
                    if (b == 0x1B && i + 1 < s.Length && s[i + 1] == '\\')
                    {
                        return i + 2;
                    }
                    // The UTF-16 representation of the 8-bit C1 String
                    // Terminator accepted by real terminals. Without this,
                    // valid text following an OSC terminated by U+009C is
                    // swallowed as sequence content through end of input.
                    if (b == '\u009C')
                    {
                        return i + 1;
                    }
                    if (b == 0x1B)
                    {
                        return i;
                    }
                    if (b < 0x20)
                    {
                        return i;
                    }
                    i++;
                }
                break;
            }
            // DCS/PM/APC: ESC P/^/_ ... ST
            case 'P' or '^' or '_':
            {
                i++;
                while (i < s.Length)
                {
                    var b = s[i];
                    if (b == 0x1B && i + 1 < s.Length && s[i + 1] == '\\')
                    {
                        return i + 2;
                    }
                    if (b == '\u009C')
                    {
                        return i + 1;
                    }
                    if (b == 0x1B)
                    {
                        return i;
                    }
                    if (b < 0x20)
                    {
                        return i;
                    }
                    i++;
                }
                break;
            }
            // Single-char escape sequences (ESC followed by 0x20-0x7E)
            case >= (char)0x20 and <= (char)0x7E:
                return i + 1;
            // Unknown or invalid - just skip the ESC
            default:
                break;
        }

        return i;
    }
}

/// <summary>
/// Controlled trust state corresponding to the two variants of upstream
/// <c>sanitize::Text</c>.
/// </summary>
public enum TextTrust
{
    Sanitized,
    Trusted,
}

/// <summary>
/// Immutable text carrying an explicit sanitized/trusted stance.
/// </summary>
/// <remarks>
/// Rust's borrowed-versus-owned <c>Cow&lt;str&gt;</c> distinction has no direct
/// lifetime analogue for immutable managed strings. The four source factories
/// remain explicit, while <see cref="IntoOwned"/> returns an equivalent wrapper.
/// </remarks>
public sealed class Text : IEquatable<Text>
{
    private Text(string value, TextTrust trust)
    {
        Value = value;
        Trust = trust;
    }

    public string Value { get; }

    public TextTrust Trust { get; }

    public static Text Sanitized(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Text(Sanitize.SanitizeString(value), TextTrust.Sanitized);
    }

    public static Text Trusted(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Text(value, TextTrust.Trusted);
    }

    public static Text SanitizedOwned(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Text(Sanitize.SanitizeString(value), TextTrust.Sanitized);
    }

    public static Text TrustedOwned(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Text(value, TextTrust.Trusted);
    }

    public string AsString() => Value;

    public string AsRef() => Value;

    public bool IsSanitized() => Trust == TextTrust.Sanitized;

    public bool IsTrusted() => Trust == TextTrust.Trusted;

    public Text IntoOwned() => new(Value, Trust);

    public Text Clone() => new(Value, Trust);

    public bool Equals(Text? other) =>
        other is not null && Trust == other.Trust && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is Text other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Trust);

    public override string ToString() => Value;

    public static bool operator ==(Text? left, Text? right) => Equals(left, right);

    public static bool operator !=(Text? left, Text? right) => !Equals(left, right);
}
