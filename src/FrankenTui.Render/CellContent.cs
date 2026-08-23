using System.Runtime.InteropServices;
using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Render;

// Ported from crates/ftui-render/src/cell.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// This is one managed split of the single upstream cell module.
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly record struct CellContent(uint Raw)
{
    public const uint GraphemeFlag = 0x8000_0000;
    public static readonly CellContent Empty = new(0);
    public static readonly CellContent Continuation = new(0x7FFF_FFFF);

    public static CellContent FromChar(char value) => FromRune(new Rune(value));

    public static CellContent FromRune(Rune value) =>
        new((uint)(value.Value == '\t' ? ' ' : value.Value));

    public static CellContent FromGrapheme(GraphemeId id) => new(GraphemeFlag | id.Raw);

    public bool IsGrapheme => (Raw & GraphemeFlag) != 0;

    public bool IsContinuation => Raw == Continuation.Raw;

    public bool IsEmpty => Raw == Empty.Raw;

    public bool IsDefault => IsEmpty;

    public Rune? AsRune()
    {
        if (IsGrapheme || IsEmpty || IsContinuation)
        {
            return null;
        }

        return Rune.IsValid((int)Raw) ? new Rune((int)Raw) : null;
    }

    /// <summary>
    /// Extract the character if this is a direct char (not a grapheme).
    /// Returns null if this is empty, continuation, a grapheme reference, or a
    /// codepoint outside the BMP (the Rust <c>char</c> is 32-bit; C# <c>char</c>
    /// is 16-bit, so non-BMP codepoints have no single-char representation).
    /// Mirrors upstream <c>CellContent::as_char</c>.
    /// </summary>
    public char? AsChar()
    {
        if (IsGrapheme || IsEmpty || IsContinuation)
        {
            return null;
        }

        int value = (int)Raw;
        return value >= 0 && value <= 0xFFFF && !char.IsSurrogate((char)value)
            ? (char?)value
            : null;
    }

    public GraphemeId? GraphemeId => IsGrapheme ? new GraphemeId(Raw & ~GraphemeFlag) : null;

    public int WidthHint =>
        IsEmpty || IsContinuation
            ? 0
            : IsGrapheme
                ? (int)((Raw >> 27) & 0x0F)
                : 1;

    public int Width()
    {
        if (IsEmpty || IsContinuation)
        {
            return 0;
        }

        if (IsGrapheme)
        {
            return (int)((Raw >> 27) & 0x0F);
        }

        var rune = AsRune();
        return rune is null ? 1 : TerminalTextWidth.RuneWidth(rune.Value);
    }

    public override string ToString()
    {
        if (IsEmpty)
        {
            return "CellContent::EMPTY";
        }

        if (IsContinuation)
        {
            return "CellContent::CONTINUATION";
        }

        var rune = AsRune();
        if (rune is not null)
        {
            return $"CellContent::Char('{rune.Value}')";
        }

        var id = GraphemeId;
        return id is not null
            ? $"CellContent::Grapheme({id.Value})"
            : $"CellContent(0x{Raw:x8})";
    }
}
