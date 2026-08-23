using System.Runtime.InteropServices;
using System.Text;

namespace FrankenTui.Render;

// Ported from crates/ftui-render/src/cell.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// This is one managed split of the single upstream cell module.
// DIVERGENCE: the CLR can guarantee the 16-byte size and field order but has no
// stable user-defined equivalent of Rust's repr(align(16)); consumers must not
// assume a 16-byte address alignment and should use unaligned SIMD loads.
// DIVERGENCE: CLR zero-initialization bypasses struct constructors, so
// default(Cell) cannot carry Rust's opaque-white foreground. Use new Cell() or
// Cell.Empty for the source Default value; grid stores must initialize with it.
[StructLayout(LayoutKind.Sequential, Size = 16)]
public readonly struct Cell : IEquatable<Cell>
{
    public static readonly Cell Empty = new(CellContent.Empty, PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);
    public static readonly Cell Continuation = new(CellContent.Continuation, PackedRgba.Transparent, PackedRgba.Transparent, CellAttributes.None);

    /// <summary>Construct the upstream default cell.</summary>
    public Cell()
        : this(CellContent.Empty, PackedRgba.White, PackedRgba.Transparent, CellAttributes.None)
    {
    }

    /// <summary>Construct a cell from content using upstream default colors and attributes.</summary>
    public Cell(CellContent content)
        : this(content, PackedRgba.White, PackedRgba.Transparent, CellAttributes.None)
    {
    }

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
        (Content.Raw == other.Content.Raw) &
        (Foreground == other.Foreground) &
        (Background == other.Background) &
        Attributes == other.Attributes;

    public bool SignificantEqual(Cell other) =>
        Content.Raw == other.Content.Raw &&
        Attributes.LinkId == other.Attributes.LinkId;

    public Cell WithChar(char value) => WithRune(new Rune(value));

    public Cell WithRune(Rune value) => new(CellContent.FromRune(value), Foreground, Background, Attributes);

    /// <summary>Create a cell with a different foreground color, preserving all other fields.</summary>
    public Cell WithForeground(PackedRgba foreground) => new(Content, foreground, Background, Attributes);

    /// <summary>Create a cell with a different background color, preserving all other fields.</summary>
    public Cell WithBackground(PackedRgba background) => new(Content, Foreground, background, Attributes);

    public Cell WithContent(CellContent content) => new(content, Foreground, Background, Attributes);

    public Cell WithAttributes(CellAttributes attributes) => new(Content, Foreground, Background, attributes);

    public bool Equals(Cell other) => BitsEqual(other);

    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Content.Raw, Foreground, Background, Attributes);

    public override string ToString() =>
        $"Cell {{ content: {Content}, fg: {Foreground}, bg: {Background}, attrs: {Attributes} }}";

    public static bool operator ==(Cell left, Cell right) => left.Equals(right);

    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
}
