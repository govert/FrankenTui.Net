using System.Runtime.InteropServices;

namespace FrankenTui.Render;

// Ported from crates/ftui-render/src/cell.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// This is one managed split of the single upstream cell module.
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly record struct GraphemeId(uint Raw)
{
    public const uint MaxSlot = 0xFFFF;
    public const byte MaxWidth = 15;
    public const ushort MaxGeneration = 2047;

    public GraphemeId(uint slot, ushort generation, byte width)
        : this(
            (slot & MaxSlot) |
            (((uint)generation & 0x7FF) << 16) |
            (((uint)width & 0x0F) << 27))
    {
    }

    public static GraphemeId FromRaw(uint raw) => new(raw);

    public int Slot => (int)(Raw & MaxSlot);

    public ushort Generation => (ushort)((Raw >> 16) & 0x7FF);

    public int Width => (int)((Raw >> 27) & 0x0F);

    public override string ToString() =>
        $"GraphemeId {{ slot: {Slot}, gen: {Generation}, width: {Width} }}";
}
