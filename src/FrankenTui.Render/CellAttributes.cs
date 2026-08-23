using System.Runtime.InteropServices;

namespace FrankenTui.Render;

// Ported from crates/ftui-render/src/cell.rs (`CellAttrs`).
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// This is one managed split of the single upstream cell module. The established
// managed name CellAttributes is retained as a compatibility adaptation.
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly record struct CellAttributes(uint Raw)
{
    public const uint LinkIdNone = 0;
    public const uint LinkIdMax = 0x00FF_FFFF;
    public static readonly CellAttributes None = new(0);

    public CellAttributes(CellStyleFlags flags, uint linkId)
        : this((((uint)(byte)flags) << 24) | (linkId & 0x00FF_FFFF))
    {
    }

    public CellStyleFlags Flags => (CellStyleFlags)(byte)(Raw >> 24);

    public uint LinkId => Raw & 0x00FF_FFFF;

    public CellAttributes WithFlags(CellStyleFlags flags) =>
        new((Raw & 0x00FF_FFFF) | ((uint)(byte)flags << 24));

    public CellAttributes WithLink(uint linkId) =>
        new((Raw & 0xFF00_0000) | (linkId & 0x00FF_FFFF));

    public CellAttributes MergedFlags(CellStyleFlags extra) => WithFlags(Flags | extra);

    public bool HasFlag(CellStyleFlags flag) => (Flags & flag) == flag;

    public override string ToString() => $"CellAttrs({Raw})";
}
