using System.Runtime.InteropServices;
using System.Text;

namespace FrankenTui.Render;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Cell : IEquatable<Cell>
{
    public static readonly Cell Empty = new(CellContent.Empty, PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);
    public static readonly Cell Continuation = new(CellContent.Continuation, PackedRgba.Transparent, PackedRgba.Transparent, CellAttributes.None);

    public Cell(CellContent content, PackedRgba foreground, PackedRgba background, CellAttributes attributes)
    {
        Content = content;
        Foreground = foreground;
        Background = background;
        Attributes = attributes;
    }

    public CellContent Content { get; }
    public PackedRgba Foreground { get; }
    public PackedRgba Background { get; }
    public CellAttributes Attributes { get; }

    public bool IsContinuation => Content.IsContinuation;

    public bool IsEmpty => Content.IsEmpty;

    public int WidthHint => Content.WidthHint;

    public static Cell FromChar(char value) => new(CellContent.FromChar(value), PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);

    public static Cell FromRune(Rune value) => new(CellContent.FromRune(value), PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);

    public bool BitsEqual(Cell other) =>
        Content.Raw == other.Content.Raw &&
        Foreground == other.Foreground &&
        Background == other.Background &&
        Attributes == other.Attributes;

    public bool SignificantEqual(Cell other) =>
        Content.Raw == other.Content.Raw &&
        Attributes.LinkId == other.Attributes.LinkId;

    public Cell WithChar(char value) => WithRune(new Rune(value));

    public Cell WithRune(Rune value) => new(CellContent.FromRune(value), Foreground, Background, Attributes);

    public Cell WithForeground(PackedRgba foreground) => new(Content, foreground, Background, Attributes);

    public Cell WithBackground(PackedRgba background) => new(Content, Foreground, background, Attributes);

    public Cell WithAttributes(CellAttributes attributes) => new(Content, Foreground, Background, attributes);

    public bool Equals(Cell other) => BitsEqual(other);

    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Content.Raw, Foreground, Background, Attributes);

    public static bool operator ==(Cell left, Cell right) => left.Equals(right);

    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
}
