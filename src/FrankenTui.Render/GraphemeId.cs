namespace FrankenTui.Render;

public readonly record struct GraphemeId(uint Raw)
{
    public const uint MaxSlot = 0xFFFF;
    public const byte MaxWidth = 15;
    public const ushort MaxGeneration = 2047;

    public GraphemeId(uint slot, ushort generation, byte width)
        : this(
            (slot & MaxSlot) |
            (((uint)generation & 0x7FF) << 16) |
            ((uint)width << 27))
    {
    }

    public int Slot => (int)(Raw & MaxSlot);

    public ushort Generation => (ushort)((Raw >> 16) & 0x7FF);

    public int Width => (int)((Raw >> 27) & 0x0F);
}
