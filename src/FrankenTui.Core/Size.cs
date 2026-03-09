namespace FrankenTui.Core;

public readonly record struct Size(ushort Width, ushort Height)
{
    public static readonly Size Zero = new(0, 0);
    public static readonly Size Max = new(ushort.MaxValue, ushort.MaxValue);

    public bool IsEmpty => Width == 0 || Height == 0;

    public uint Area => (uint)Width * Height;

    public Size ClampMax(Size max) =>
        new(
            Width > max.Width ? max.Width : Width,
            Height > max.Height ? max.Height : Height);

    public Size ClampMin(Size min) =>
        new(
            Width < min.Width ? min.Width : Width,
            Height < min.Height ? min.Height : Height);

    public static implicit operator Size((ushort Width, ushort Height) value) =>
        new(value.Width, value.Height);
}
