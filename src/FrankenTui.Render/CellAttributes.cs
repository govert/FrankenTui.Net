namespace FrankenTui.Render;

public readonly record struct CellAttributes(uint Raw)
{
    public const uint LinkIdNone = 0;
    public const uint LinkIdMax = 0x00FF_FFFE;
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
}
