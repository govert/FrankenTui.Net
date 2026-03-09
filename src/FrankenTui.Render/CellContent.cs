using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Render;

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

    public Rune? AsRune()
    {
        if (IsGrapheme || IsEmpty || IsContinuation)
        {
            return null;
        }

        return new Rune((int)Raw);
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
}
